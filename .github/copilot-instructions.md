# Copilot Instructions

## Project Guidelines
- Prefer explicit type names over 'var', unless the type name is too long.

## Deployment and CI/CD
- This solution (AbeGamingBlazorApp) is hosted on GitHub at [AbeGamingBlazorApp](https://github.com/acher1965/AbeGamingBlazorApp).
- CI tests run via GitHub Actions.
- Production is hosted on Cloudflare Pages.
- Preview deployments are enabled for the develop branch, allowing mobile testing via Cloudflare's preview URLs before merging to master/production.

## Golden Regression Tests for FtP Battle Stats
- Create a temporary console project to get exact values: 
  - `dotnet new console -n GetGoldenTemp -o GetGoldenTemp` 
  - `dotnet add GetGoldenTemp/GetGoldenTemp.csproj reference AbeGaming.GameLogic/AbeGaming.GameLogic.csproj`
- Write code in `GetGoldenTemp/Program.cs` to call `battle.ExactStats()` and print all values: 
  - `BattleSize`, `AttackerWinProbability`, `DefenderWinProbability`, `MeanHtoA`, `MeanHtoD`, `StdDevHtoA`, `StdDevHtoD`, `StarResultProbability`
- Run the project with: `dotnet run --project GetGoldenTemp/GetGoldenTemp.csproj`
- Add the golden values to `ExactStatsGoldenTestCases()` in `FtpBattleStatsTests.cs`
- Add the same battle scenarios to `ComprehensiveBattleScenarios()` for ExactStats vs MonteCarlo comparison
- Clean up by removing the temporary project: `Remove-Item -Recurse -Force GetGoldenTemp`