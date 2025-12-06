using FairOddsConsole.Domain.Models;

namespace FairOddsConsole.Services;

public class BetRecommendationService
{
    private readonly TeamStrengthService _teamStrengthService;
    private readonly LeagueConfigService _leagueConfigService;
    private readonly ExpectedGoalsCalculator _expectedGoalsCalculator;
    private readonly MarketProbabilityService _marketProbabilityService;
    private readonly FairOddsService _fairOddsService;

    public BetRecommendationService(
        TeamStrengthService teamStrengthService,
        LeagueConfigService leagueConfigService,
        ExpectedGoalsCalculator expectedGoalsCalculator,
        MarketProbabilityService marketProbabilityService,
        FairOddsService fairOddsService)
    {
        _teamStrengthService = teamStrengthService;
        _leagueConfigService = leagueConfigService;
        _expectedGoalsCalculator = expectedGoalsCalculator;
        _marketProbabilityService = marketProbabilityService;
        _fairOddsService = fairOddsService;
    }

    public (MarketProbabilities Probabilities, IEnumerable<MarketEdge> Recommendations) Evaluate(Fixture fixture, Odds odds)
    {
        var homeStrength = _teamStrengthService.GetStrength(fixture.HomeTeam);
        var awayStrength = _teamStrengthService.GetStrength(fixture.AwayTeam);
        var leagueConfig = _leagueConfigService.GetConfig(fixture.League);

        var (lambdaHome, lambdaAway) = _expectedGoalsCalculator.Compute(homeStrength, awayStrength, leagueConfig);
        var probabilities = _marketProbabilityService.Calculate(lambdaHome, lambdaAway);

        var markets = new List<MarketEdge>();

        void TryAdd(string label, double apiOdds, double modelProb)
        {
            if (apiOdds <= 0 || modelProb <= 0)
            {
                return;
            }

            markets.Add(_fairOddsService.Calculate(label, apiOdds, modelProb));
        }

        TryAdd("Home Win", odds.HomeWin, probabilities.HomeWin);
        TryAdd("Draw", odds.Draw, probabilities.Draw);
        TryAdd("Away Win", odds.AwayWin, probabilities.AwayWin);
        TryAdd("Under 2.5", odds.Under2_5, probabilities.Under2_5);
        TryAdd("Over 2.5", odds.Over2_5, probabilities.Over2_5);
        TryAdd("BTTS Yes", odds.BTTS_Yes, probabilities.BTTS_Yes);
        TryAdd("BTTS No", odds.BTTS_No, probabilities.BTTS_No);

        var recommendations = markets.Where(m => m.Edge > 0.02).OrderByDescending(m => m.Edge);
        return (probabilities, recommendations);
    }
}
