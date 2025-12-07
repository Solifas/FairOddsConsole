using FairOddsConsole.Domain.Models;
using FairOddsConsole.Infrastructure;
using FairOddsConsole.Services;

//var apiKey = "9d4f4fbbe8b174c928ba25a54e476137";
var apiKey = "995a0d2b8fd2234076e2586965349a64";
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("API_FOOTBALL_KEY environment variable not set. Please provide your API-Football key.");
    return;
}

var httpClient = new HttpClient();
var dataProvider = new ApiFootballDataProvider(httpClient, apiKey);

var teamStrengthService = new TeamStrengthService();
var leagueConfigService = new LeagueConfigService();
var expectedGoalsCalculator = new ExpectedGoalsCalculator();
var poissonEngine = new PoissonEngine();
var marketProbabilityService = new MarketProbabilityService(poissonEngine);
var fairOddsService = new FairOddsService();
var recommendationService = new BetRecommendationService(
    teamStrengthService,
    leagueConfigService,
    expectedGoalsCalculator,
    marketProbabilityService,
    fairOddsService);

var from = DateTime.UtcNow;
var to = from.AddHours(24);

Console.WriteLine($"Fetching fixtures between {from:u} and {to:u} ...\n");
List<Fixture> fixtures;
try
{
    fixtures = await dataProvider.GetFixturesAsync(from, to);
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to fetch fixtures: {ex.Message}");
    return;
}

if (fixtures.Count == 0)
{
    Console.WriteLine("No fixtures found in the next 24 hours.");
    return;
}

foreach (var fixture in fixtures)
{
    Odds? odds;
    try
    {
        odds = await dataProvider.GetOddsAsync(fixture);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to fetch odds for {fixture.HomeTeam} vs {fixture.AwayTeam}: {ex.Message}");
        continue;
    }

    if (odds is null)
    {
        Console.WriteLine($"No odds available for {fixture.HomeTeam} vs {fixture.AwayTeam}.");
        continue;
    }

    var (_, recommendations) = recommendationService.Evaluate(fixture, odds);

    Console.WriteLine($"{fixture.League} | {fixture.HomeTeam} vs {fixture.AwayTeam} | Kickoff: {fixture.Kickoff:u}");

    var recommendedList = recommendations.ToList();
    if (recommendedList.Count == 0)
    {
        Console.WriteLine("  No favorable markets (edge > 2%).\n");
        continue;
    }

    Console.WriteLine("  Favorable markets (edge > 2%):");
    foreach (var rec in recommendedList)
    {
        Console.WriteLine($"    {rec.Market,-12} Edge {rec.Edge:+0.00;-0.00} | API {rec.ApiOdds:F2} | Fair {rec.FairOdds:F2} | Model P {rec.ModelProbability:F2}");
    }

    Console.WriteLine();
}
