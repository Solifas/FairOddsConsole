using FairOddsConsole.Domain.Models;

namespace FairOddsConsole.Services;

public class FairOddsService
{
    public MarketEdge Calculate(string market, double apiOdds, double modelProbability)
    {
        if (apiOdds <= 0)
        {
            throw new ArgumentException("API odds must be positive", nameof(apiOdds));
        }

        var fairOdds = 1 / modelProbability;
        var impliedProbability = 1 / apiOdds;
        var edge = modelProbability - impliedProbability;

        return new MarketEdge
        {
            Market = market,
            ApiOdds = apiOdds,
            ModelProbability = modelProbability,
            FairOdds = fairOdds,
            ImpliedProbability = impliedProbability,
            Edge = edge
        };
    }
}
