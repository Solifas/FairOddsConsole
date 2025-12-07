using System.Globalization;
using System.Text.Json;
using FairOddsConsole.Domain.Models;
using FairOddsConsole.Services;

namespace FairOddsConsole.Infrastructure;

public class ApiFootballDataProvider : IFootballDataProvider
{
    private const string BaseUrl = "https://api.the-odds-api.com/v4";
    private static readonly string[] SoccerSportKeys = new[]
    {
        "soccer_epl",
        "soccer_spain_la_liga",
        "soccer_germany_bundesliga",
        "soccer_italy_serie_a",
        "soccer_france_ligue_one",
        "soccer_netherlands_eredivisie",
        "soccer_portugal_primeira_liga",
        "soccer_belgium_first_div",
        "soccer_brazil_campeonato",
        "soccer_argentina_primera_division",
        "soccer_usa_mls"
    };
    // Odds API v4 supports markets like h2h, spreads, totals, outrights. BTTS is not available, so we
    // derive only the markets present and leave BTTS at 0 when absent.
    private const string MarketsRequested = "h2h,totals";
    private const string RegionsRequested = "eu";

    private readonly HttpClient _client;
    private readonly string _apiKey;
    private readonly Dictionary<string, Odds> _oddsCache = new();

    public ApiFootballDataProvider(HttpClient client, string apiKey)
    {
        _client = client;
        _apiKey = apiKey;
    }

    public async Task<List<Fixture>> GetFixturesAsync(DateTime from, DateTime to)
    {
        _oddsCache.Clear();

        // Lock to the next 24 hours from "now" per request.
        var windowStart = DateTime.UtcNow;
        var windowEnd = windowStart.AddHours(24);

        var fixtures = new Dictionary<string, Fixture>(StringComparer.OrdinalIgnoreCase);

        foreach (var sportKey in SoccerSportKeys)
        {
            var url = BuildOddsUrl(sportKey);
            using var response = await _client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Odds API request failed for {sportKey}: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var ev in doc.RootElement.EnumerateArray())
            {
                var fixture = MapFixture(ev);
                if (fixture is null)
                {
                    continue;
                }

                if (fixture.Kickoff < windowStart || fixture.Kickoff > windowEnd)
                {
                    continue;
                }

                fixtures.TryAdd(fixture.Id, fixture);

                var markets = ExtractMarkets(ev, fixture.HomeTeam, fixture.AwayTeam);
                if (markets.HasAny)
                {
                    _oddsCache[fixture.Id] = new Odds
                    {
                        HomeWin = markets.Home,
                        Draw = markets.Draw,
                        AwayWin = markets.Away,
                        Under2_5 = markets.Under2_5,
                        Over2_5 = markets.Over2_5,
                        BTTS_Yes = markets.BttsYes,
                        BTTS_No = markets.BttsNo
                    };
                }
            }
        }

        return fixtures.Values.ToList();
    }

    public Task<Odds?> GetOddsAsync(Fixture fx)
    {
        if (_oddsCache.TryGetValue(fx.Id, out var odds))
        {
            return Task.FromResult<Odds?>(odds);
        }

        return Task.FromResult<Odds?>(null);
    }

    private string BuildOddsUrl(string sportKey)
    {
        var apiKeyParam = Uri.EscapeDataString(_apiKey);

        // v4 odds endpoint does not accept commence time filters; request upcoming and filter client-side.
        return $"{BaseUrl}/sports/{Uri.EscapeDataString(sportKey)}/odds?apiKey={apiKeyParam}&regions={RegionsRequested}&markets={MarketsRequested}&oddsFormat=decimal&dateFormat=iso";
    }

    private static Fixture? MapFixture(JsonElement ev)
    {
        if (!ev.TryGetProperty("id", out var idEl))
        {
            return null;
        }

        var id = idEl.GetString();
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var home = ev.TryGetProperty("home_team", out var homeEl) ? homeEl.GetString() ?? string.Empty : string.Empty;
        var away = ev.TryGetProperty("away_team", out var awayEl) ? awayEl.GetString() ?? string.Empty : string.Empty;
        var league = ev.TryGetProperty("sport_title", out var leagueEl) ? leagueEl.GetString() ?? string.Empty : string.Empty;

        if (!ev.TryGetProperty("commence_time", out var kickoffEl) ||
            kickoffEl.ValueKind != JsonValueKind.String ||
            !DateTime.TryParse(kickoffEl.GetString(), null, DateTimeStyles.AdjustToUniversal, out var kickoff))
        {
            return null;
        }

        return new Fixture
        {
            Id = id,
            League = league,
            HomeTeam = home,
            AwayTeam = away,
            Kickoff = kickoff
        };
    }

    private static Markets ExtractMarkets(JsonElement ev, string homeTeam, string awayTeam)
    {
        var markets = new Markets();

        if (!ev.TryGetProperty("bookmakers", out var bookmakers) || bookmakers.ValueKind != JsonValueKind.Array)
        {
            return markets;
        }

        foreach (var bookmaker in bookmakers.EnumerateArray())
        {
            if (!bookmaker.TryGetProperty("markets", out var marketsEl) || marketsEl.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var market in marketsEl.EnumerateArray())
            {
                var key = market.TryGetProperty("key", out var keyEl) ? keyEl.GetString() ?? string.Empty : string.Empty;
                if (!market.TryGetProperty("outcomes", out var outcomesEl) || outcomesEl.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                if (key.Equals("h2h", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var outcome in outcomesEl.EnumerateArray())
                    {
                        var name = outcome.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
                        var price = ReadPrice(outcome);
                        if (price <= 0)
                        {
                            continue;
                        }

                        if (name.Equals(homeTeam, StringComparison.OrdinalIgnoreCase))
                            markets.Home = PreferFirst(markets.Home, price);
                        else if (name.Equals(awayTeam, StringComparison.OrdinalIgnoreCase))
                            markets.Away = PreferFirst(markets.Away, price);
                        else if (IsDrawName(name))
                            markets.Draw = PreferFirst(markets.Draw, price);
                    }
                }
                else if (key.Equals("totals", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var outcome in outcomesEl.EnumerateArray())
                    {
                        var price = ReadPrice(outcome);
                        var point = ReadPoint(outcome);
                        var name = outcome.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;

                        if (price <= 0 || !point.HasValue || Math.Abs(point.Value - 2.5) > 0.01)
                        {
                            continue;
                        }

                        if (name.Equals("Over", StringComparison.OrdinalIgnoreCase))
                            markets.Over2_5 = PreferFirst(markets.Over2_5, price);
                        else if (name.Equals("Under", StringComparison.OrdinalIgnoreCase))
                            markets.Under2_5 = PreferFirst(markets.Under2_5, price);
                    }
                }
            }
        }

        return markets;
    }

    private static double ReadPrice(JsonElement outcome)
    {
        if (!outcome.TryGetProperty("price", out var priceEl))
        {
            return 0;
        }

        return priceEl.ValueKind switch
        {
            JsonValueKind.Number => priceEl.TryGetDouble(out var dbl) ? dbl : 0,
            JsonValueKind.String => double.TryParse(priceEl.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var dbl) ? dbl : 0,
            _ => 0
        };
    }

    private static double? ReadPoint(JsonElement outcome)
    {
        if (!outcome.TryGetProperty("point", out var pointEl))
        {
            return null;
        }

        return pointEl.ValueKind switch
        {
            JsonValueKind.Number => pointEl.TryGetDouble(out var dbl) ? dbl : null,
            JsonValueKind.String => double.TryParse(pointEl.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var dbl) ? dbl : null,
            _ => null
        };
    }

    private static bool IsDrawName(string name) =>
        name.Equals("Draw", StringComparison.OrdinalIgnoreCase) || name.Equals("Tie", StringComparison.OrdinalIgnoreCase) || name.Equals("X", StringComparison.OrdinalIgnoreCase);

    private static double PreferFirst(double existing, double incoming) => existing > 0 ? existing : incoming;

    private struct Markets
    {
        public double Home;
        public double Draw;
        public double Away;
        public double Under2_5;
        public double Over2_5;
        public double BttsYes;
        public double BttsNo;

        public bool HasAny =>
            Home > 0 || Draw > 0 || Away > 0 || Under2_5 > 0 || Over2_5 > 0 || BttsYes > 0 || BttsNo > 0;
    }
}
