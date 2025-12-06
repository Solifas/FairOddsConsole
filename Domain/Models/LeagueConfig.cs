namespace FairOddsConsole.Domain.Models;

public class LeagueConfig
{
    public string LeagueCode { get; set; } = string.Empty;
    public double AvgLeagueGoals { get; set; }
    public double HomeAdvantageMultiplier { get; set; } = 1.10;
}
