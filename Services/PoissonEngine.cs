namespace FairOddsConsole.Services;

public class PoissonEngine
{
    public double Poisson(int k, double lambda)
    {
        var factorial = Factorial(k);
        return Math.Pow(lambda, k) * Math.Exp(-lambda) / factorial;
    }

    public double Factorial(int k)
    {
        if (k == 0 || k == 1)
        {
            return 1.0;
        }

        double result = 1;
        for (int i = 2; i <= k; i++)
        {
            result *= i;
        }

        return result;
    }
}
