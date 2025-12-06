using FairOddsConsole.Domain.Models;
using FairOddsConsole.Infrastructure;
using FairOddsConsole.Services;

var apiKey = Environment.GetEnvironmentVariable("API_FOOTBALL_KEY");
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

    var (probabilities, recommendations) = recommendationService.Evaluate(fixture, odds);

    Console.WriteLine($"{fixture.League} | {fixture.HomeTeam} vs {fixture.AwayTeam} | Kickoff: {fixture.Kickoff:u}");
    Console.WriteLine($"API Odds: H {odds.HomeWin:F2}  D {odds.Draw:F2}  A {odds.AwayWin:F2} | U2.5 {odds.Under2_5:F2} O2.5 {odds.Over2_5:F2} | BTTS {odds.BTTS_Yes:F2} / {odds.BTTS_No:F2}");
    Console.WriteLine("Model:");

    void PrintMarket(string label, double apiOdds, double modelProb)
    {
        if (apiOdds <= 0 || modelProb <= 0)
        {
            Console.WriteLine($"  {label}: missing odds/model probability");
            return;
        }

        var edge = fairOddsService.Calculate(label, apiOdds, modelProb);
        Console.WriteLine($"  P({label}) = {modelProb:F2} → Fair {edge.FairOdds:F2} → Edge {edge.Edge:+0.00;-0.00}");
    }

    PrintMarket("Home", odds.HomeWin, probabilities.HomeWin);
    PrintMarket("Draw", odds.Draw, probabilities.Draw);
    PrintMarket("Away", odds.AwayWin, probabilities.AwayWin);
    PrintMarket("Under2.5", odds.Under2_5, probabilities.Under2_5);
    PrintMarket("Over2.5", odds.Over2_5, probabilities.Over2_5);
    PrintMarket("BTTS Yes", odds.BTTS_Yes, probabilities.BTTS_Yes);
    PrintMarket("BTTS No", odds.BTTS_No, probabilities.BTTS_No);

    var recommendedList = recommendations.ToList();
    if (recommendedList.Count == 0)
    {
        Console.WriteLine("No positive expected value markets found.\n");
        continue;
    }

    Console.WriteLine("Recommended Bets (Edge > 0.02):");
    foreach (var rec in recommendedList)
    {
        Console.WriteLine($"  {rec.Market}: API {rec.ApiOdds:F2} | Model P {rec.ModelProbability:F2} | Fair {rec.FairOdds:F2} | Edge {rec.Edge:+0.00;-0.00}");
    }

    Console.WriteLine();
}
