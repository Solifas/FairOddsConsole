# Fair Odds Console

Console application that fetches fixtures and odds from API-Football, models expected goals via a Poisson process, converts model probabilities into fair odds, and highlights positive-edge markets.

## Prerequisites
- .NET 8 SDK
- API-Football key available as environment variable `API_FOOTBALL_KEY`

## Running
```bash
# restore & run (uses bundled .NET 8 SDK pinned to runtime pack 8.0.8)
export API_FOOTBALL_KEY="<your key>"
DOTNET_ROOT=$PWD/.dotnet8 PATH=$DOTNET_ROOT:$PATH DOTNET_CLI_HOME=$PWD/.dotnet dotnet build
DOTNET_ROOT=$PWD/.dotnet8 PATH=$DOTNET_ROOT:$PATH DOTNET_CLI_HOME=$PWD/.dotnet dotnet run
```

The app fetches fixtures in the next 24 hours, retrieves odds for each fixture, prints modeled probabilities and fair prices, and lists only markets with an edge greater than 0.02.

## Notes
- `NuGet.Config` clears external feeds to avoid network calls; no external packages are required.
- If you prefer a global SDK, install .NET 8 locally and remove/ignore `.dotnet8`.
