namespace FairOddsConsole.Domain.Models;

public class MarketProbabilities
{
    public double HomeWin { get; set; }
    public double Draw { get; set; }
    public double AwayWin { get; set; }
    public double Under2_5 { get; set; }
    public double Over2_5 { get; set; }
    public double BTTS_Yes { get; set; }
    public double BTTS_No { get; set; }
}
