using FairOddsConsole.Domain.Models;

namespace FairOddsConsole.Services;

public class LeagueConfigService
{
    private readonly Dictionary<string, LeagueConfig> _configs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EPL"] = new LeagueConfig { LeagueCode = "EPL", AvgLeagueGoals = 2.7, HomeAdvantageMultiplier = 1.10 },
        ["PSL"] = new LeagueConfig { LeagueCode = "PSL", AvgLeagueGoals = 2.1, HomeAdvantageMultiplier = 1.10 },
        ["USL"] = new LeagueConfig { LeagueCode = "USL", AvgLeagueGoals = 2.5, HomeAdvantageMultiplier = 1.10 },
        ["Bundesliga"] = new LeagueConfig { LeagueCode = "Bundesliga", AvgLeagueGoals = 3.1, HomeAdvantageMultiplier = 1.10 },
        ["Eredivisie"] = new LeagueConfig { LeagueCode = "Eredivisie", AvgLeagueGoals = 3.2, HomeAdvantageMultiplier = 1.10 }
    };

    public LeagueConfig GetConfig(string leagueCode)
    {
        if (_configs.TryGetValue(leagueCode, out var config))
        {
            return config;
        }

        return new LeagueConfig
        {
            LeagueCode = leagueCode,
            AvgLeagueGoals = 2.5,
            HomeAdvantageMultiplier = 1.10
        };
    }
}
