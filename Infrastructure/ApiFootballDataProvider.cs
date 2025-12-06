using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FairOddsConsole.Domain.Models;
using FairOddsConsole.Services;

namespace FairOddsConsole.Infrastructure;

public class ApiFootballDataProvider : IFootballDataProvider
{
    private const string FixturesEndpoint = "https://v3.football.api-sports.io/fixtures";
    private const string OddsEndpoint = "https://v3.football.api-sports.io/odds";

    private readonly HttpClient _client;
    private readonly string _apiKey;

    public ApiFootballDataProvider(HttpClient client, string apiKey)
    {
        _client = client;
        _apiKey = apiKey;
    }

    public async Task<List<Fixture>> GetFixturesAsync(DateTime from, DateTime to)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{FixturesEndpoint}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
        request.Headers.Add("x-apisports-key", _apiKey);

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<ApiResponse<List<FixtureResponse>>>(json, JsonOptions());

        var fixtures = new List<Fixture>();
        if (payload?.Response is null)
        {
            return fixtures;
        }

        foreach (var fx in payload.Response)
        {
            fixtures.Add(new Fixture
            {
                Id = fx.Fixture.Id,
                League = fx.League.Name ?? fx.League.Country ?? string.Empty,
                HomeTeam = fx.Teams.Home.Name ?? string.Empty,
                AwayTeam = fx.Teams.Away.Name ?? string.Empty,
                Kickoff = fx.Fixture.Date
            });
        }

        return fixtures;
    }

    public async Task<Odds?> GetOddsAsync(Fixture fx)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{OddsEndpoint}?fixture={fx.Id}");
        request.Headers.Add("x-apisports-key", _apiKey);

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<ApiResponse<List<OddsResponse>>>(json, JsonOptions());
        var oddsResponse = payload?.Response?.FirstOrDefault();

        if (oddsResponse is null)
        {
            return null;
        }

        var markets = ExtractMarkets(oddsResponse);
        return new Odds
        {
            HomeWin = markets.MatchWinner.Home,
            Draw = markets.MatchWinner.Draw,
            AwayWin = markets.MatchWinner.Away,
            Under2_5 = markets.UnderOver.Under2_5,
            Over2_5 = markets.UnderOver.Over2_5,
            BTTS_Yes = markets.Btts.Yes,
            BTTS_No = markets.Btts.No
        };
    }

    private static (MatchWinner MarketWinner, UnderOverMarket UnderOver, BttsMarket Btts) ExtractMarkets(OddsResponse oddsResponse)
    {
        var matchWinner = new MatchWinner();
        var underOver = new UnderOverMarket();
        var btts = new BttsMarket();

        foreach (var bookmaker in oddsResponse.Bookmakers)
        {
            foreach (var bet in bookmaker.Bets)
            {
                if (string.Equals(bet.Name, "Match Winner", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var value in bet.Values)
                    {
                        if (string.Equals(value.Value, "Home", StringComparison.OrdinalIgnoreCase))
                            matchWinner.Home = ParseOdd(value.Odd);
                        else if (string.Equals(value.Value, "Draw", StringComparison.OrdinalIgnoreCase))
                            matchWinner.Draw = ParseOdd(value.Odd);
                        else if (string.Equals(value.Value, "Away", StringComparison.OrdinalIgnoreCase))
                            matchWinner.Away = ParseOdd(value.Odd);
                    }
                }
                else if (bet.Name?.Contains("Over/Under", StringComparison.OrdinalIgnoreCase) == true)
                {
                    foreach (var value in bet.Values)
                    {
                        if (string.Equals(value.Value, "Over 2.5", StringComparison.OrdinalIgnoreCase))
                            underOver.Over2_5 = ParseOdd(value.Odd);
                        else if (string.Equals(value.Value, "Under 2.5", StringComparison.OrdinalIgnoreCase))
                            underOver.Under2_5 = ParseOdd(value.Odd);
                    }
                }
                else if (bet.Name?.Contains("Both Teams To Score", StringComparison.OrdinalIgnoreCase) == true)
                {
                    foreach (var value in bet.Values)
                    {
                        if (string.Equals(value.Value, "Yes", StringComparison.OrdinalIgnoreCase))
                            btts.Yes = ParseOdd(value.Odd);
                        else if (string.Equals(value.Value, "No", StringComparison.OrdinalIgnoreCase))
                            btts.No = ParseOdd(value.Odd);
                    }
                }
            }
        }

        return (matchWinner, underOver, btts);
    }

    private static double ParseOdd(string? odd)
    {
        return double.TryParse(odd, out var value) ? value : 0;
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private class ApiResponse<T>
    {
        [JsonPropertyName("response")]
        public T? Response { get; set; }
    }

    private class FixtureResponse
    {
        [JsonPropertyName("fixture")]
        public FixtureInfo Fixture { get; set; } = new();

        [JsonPropertyName("teams")]
        public Teams Teams { get; set; } = new();

        [JsonPropertyName("league")]
        public League League { get; set; } = new();
    }

    private class FixtureInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }
    }

    private class Teams
    {
        [JsonPropertyName("home")]
        public Team Home { get; set; } = new();

        [JsonPropertyName("away")]
        public Team Away { get; set; } = new();
    }

    private class Team
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private class League
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }
    }

    private class OddsResponse
    {
        [JsonPropertyName("bookmakers")]
        public List<Bookmaker> Bookmakers { get; set; } = new();
    }

    private class Bookmaker
    {
        [JsonPropertyName("bets")]
        public List<Bet> Bets { get; set; } = new();
    }

    private class Bet
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("values")]
        public List<BetValue> Values { get; set; } = new();
    }

    private class BetValue
    {
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("odd")]
        public string Odd { get; set; } = string.Empty;
    }

    private record MatchWinner
    {
        public double Home { get; set; }
        public double Draw { get; set; }
        public double Away { get; set; }
    }

    private record UnderOverMarket
    {
        public double Under2_5 { get; set; }
        public double Over2_5 { get; set; }
    }

    private record BttsMarket
    {
        public double Yes { get; set; }
        public double No { get; set; }
    }
}
