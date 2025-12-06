# Fair Odds Console

Console application that fetches fixtures and odds from API-Football, models expected goals via a Poisson process, converts model probabilities into fair odds, and highlights positive-edge markets.

## Prerequisites
- .NET 8 SDK
- API-Football key available as environment variable `API_FOOTBALL_KEY`

## Running
```bash
# restore & run
export API_FOOTBALL_KEY="<your key>"
dotnet build
dotnet run
```

The app fetches fixtures in the next 24 hours, retrieves odds for each fixture, prints modeled probabilities and fair prices, and lists only markets with an edge greater than 0.02.
