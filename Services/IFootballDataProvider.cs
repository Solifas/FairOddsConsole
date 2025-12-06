using FairOddsConsole.Domain.Models;

namespace FairOddsConsole.Services;

public interface IFootballDataProvider
{
    Task<List<Fixture>> GetFixturesAsync(DateTime from, DateTime to);
    Task<Odds?> GetOddsAsync(Fixture fx);
}
