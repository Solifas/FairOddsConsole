using FairOddsConsole.Domain.Models;

namespace FairOddsConsole.Services;

public class MarketProbabilityService
{
    private readonly PoissonEngine _poisson;
    // Limit goal simulation per specification; tail mass >6 is ignored.
    private const int MaxGoals = 6;

    public MarketProbabilityService(PoissonEngine poisson)
    {
        _poisson = poisson;
    }

    public MarketProbabilities Calculate(double lambdaHome, double lambdaAway)
    {
        double homeWin = 0, draw = 0, awayWin = 0;
        double under = 0, over = 0;
        double bttsYes = 0, bttsNo = 0;

        for (int h = 0; h <= MaxGoals; h++)
        {
            var pHome = _poisson.Poisson(h, lambdaHome);
            for (int a = 0; a <= MaxGoals; a++)
            {
                var pAway = _poisson.Poisson(a, lambdaAway);
                var joint = pHome * pAway;

                if (h > a) homeWin += joint;
                else if (h == a) draw += joint;
                else awayWin += joint;

                var totalGoals = h + a;
                if (totalGoals <= 2) under += joint;
                else over += joint;

                if (h >= 1 && a >= 1) bttsYes += joint;
                else bttsNo += joint;
            }
        }

        return new MarketProbabilities
        {
            HomeWin = homeWin,
            Draw = draw,
            AwayWin = awayWin,
            Under2_5 = under,
            Over2_5 = over,
            BTTS_Yes = bttsYes,
            BTTS_No = bttsNo
        };
    }
}
