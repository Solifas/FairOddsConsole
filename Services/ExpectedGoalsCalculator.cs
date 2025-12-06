using FairOddsConsole.Domain.Models;

namespace FairOddsConsole.Services;

public class ExpectedGoalsCalculator
{
    public (double LambdaHome, double LambdaAway) Compute(TeamStrength home, TeamStrength away, LeagueConfig leagueConfig)
    {
        var lambdaHome = leagueConfig.AvgLeagueGoals * home.AttackStrength * away.DefenceStrength * leagueConfig.HomeAdvantageMultiplier;
        var lambdaAway = leagueConfig.AvgLeagueGoals * away.AttackStrength * home.DefenceStrength;
        return (lambdaHome, lambdaAway);
    }
}
