namespace FairOddsConsole.Domain.Models;

public class MarketEdge
{
    public string Market { get; set; } = string.Empty;
    public double ApiOdds { get; set; }
    public double ModelProbability { get; set; }
    public double FairOdds { get; set; }
    public double ImpliedProbability { get; set; }
    public double Edge { get; set; }
}
