namespace FairOddsConsole.Domain.Models;

public class Fixture
{
    public string Id { get; set; } = string.Empty;
    public string League { get; set; } = string.Empty;
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public DateTime Kickoff { get; set; }
}
