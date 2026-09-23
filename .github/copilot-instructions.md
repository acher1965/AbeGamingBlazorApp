# Copilot Instructions

## Project Guidelines
- Prefer explicit type names over 'var', unless the type name is too long.
- Keep game rules in `AbeGaming.GameLogic` (no UI dependencies); the Blazor app only collects input and displays results.
- `AbeGaming.GameLogic` builds with `TreatWarningsAsErrors`, so new warnings there fail the build.
- The SDK is pinned by `global.json` to any stable .NET 10 SDK; preview SDKs are excluded.

## Solution Layout
The solution file is `AbeGamingBlazorApp.slnx` (XML solution format).
- `AbeGaming.GameLogic/` - rules engine
  - `FtP/` - For The People: CRT, battle model and input rules, exact stats, Monte Carlo simulation
  - `PoG/` - Paths of Glory: CRTs, battle model and input rules, exact stats (`PoGExactStats.Calculate`)
- `AbeGamingBlazorApp/` - Blazor WebAssembly PWA (`Pages/`, `Components/`, `Layout/`, `wwwroot/`)
- `AbeGaming.GameLogic.Tests/` - xUnit tests for the rules engine
- `AbeGaming.BlazorApp.Component.Tests/` - bUnit component tests
- `AbeGaming.BlazorApp.E2E.Tests/` - Playwright browser tests (see its README; needs a running app)
- `RulesAndTables/` - reference rules and charts for both games; check rule changes against these

## Deployment and CI/CD
- This solution (AbeGamingBlazorApp) is hosted on GitHub at [AbeGamingBlazorApp](https://github.com/acher1965/AbeGamingBlazorApp).
- CI tests run via GitHub Actions (GameLogic and bUnit suites; the Playwright suite is not run in CI).
- Production is hosted on Cloudflare Pages, built by `build.sh`.
- Preview deployments are enabled for the develop branch, allowing mobile testing via Cloudflare's preview URLs before merging to master/production.
- Versioning and release tags are described in the "Versioning" section of `README.md`.

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
- Never commit `GetGoldenTemp`, and do not add it to the solution.

## Tests for PoG Battles
- PoG has no Monte Carlo simulation and no golden-value tests yet; its tests live in `PoGBattleRulesTests.cs`.
- For a rule change, add an outcome test for a specific die roll that checks hits, retreats and column shifts
  (see `Outcome_CorpsTableBaseline_UsesExpectedHitsAndRetreat`), plus a test that invalid inputs throw
  (see `Outcome_TrenchBlocksFlank_Throws`).
- For `PoGExactStats.Calculate`, test invariants (probabilities sum to one, conditional means are consistent)
  rather than hardcoding values, unless you have derived the expected value independently from the rules.
- UI clamping and input behaviour for PoG components is covered by bUnit tests (`PoGSideInputTests.cs`)
  and Playwright tests (`PoGBattleE2ETests.cs`).
