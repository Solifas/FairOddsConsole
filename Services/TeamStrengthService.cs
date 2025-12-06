using FairOddsConsole.Domain.Models;

namespace FairOddsConsole.Services;

public class TeamStrengthService
{
    private readonly Dictionary<string, TeamStrength> _strengths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Arsenal"] = new TeamStrength { Name = "Arsenal", AttackStrength = 1.25, DefenceStrength = 0.85 },
        ["Manchester City"] = new TeamStrength { Name = "Manchester City", AttackStrength = 1.35, DefenceStrength = 0.80 },
        ["Liverpool"] = new TeamStrength { Name = "Liverpool", AttackStrength = 1.30, DefenceStrength = 0.90 },
        ["Bayern Munich"] = new TeamStrength { Name = "Bayern Munich", AttackStrength = 1.40, DefenceStrength = 0.85 },
        ["PSV Eindhoven"] = new TeamStrength { Name = "PSV Eindhoven", AttackStrength = 1.30, DefenceStrength = 0.95 },
        ["Ajax"] = new TeamStrength { Name = "Ajax", AttackStrength = 1.25, DefenceStrength = 1.00 },
        ["LA Galaxy"] = new TeamStrength { Name = "LA Galaxy", AttackStrength = 1.05, DefenceStrength = 1.05 },
        ["Orlando City"] = new TeamStrength { Name = "Orlando City", AttackStrength = 1.00, DefenceStrength = 1.00 },
        ["Kaizer Chiefs"] = new TeamStrength { Name = "Kaizer Chiefs", AttackStrength = 1.00, DefenceStrength = 1.05 },
        ["Mamelodi Sundowns"] = new TeamStrength { Name = "Mamelodi Sundowns", AttackStrength = 1.20, DefenceStrength = 0.90 }
    };

    public TeamStrength GetStrength(string team)
    {
        if (_strengths.TryGetValue(team, out var strength))
        {
            return strength;
        }

        return new TeamStrength
        {
            Name = team,
            AttackStrength = 1.0,
            DefenceStrength = 1.0
        };
    }
}
