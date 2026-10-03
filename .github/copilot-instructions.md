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
  - `TNW/` - The Napoleonic Wars: `TnwDicePool` (shared trinomial dice math), `TnwLandBattle*`
    (Battle calculator, rule 11), `TnwSiege*` (Siege calculator, rule 12), `TnwNavalBattle*` and
    `TnwFleet*` (Naval Battle calculator, rule 13). Each battle type has its own page
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

## FtP Rulebook Tests

- `FtpRulebookCasesTests.cs` holds correctness tests whose expected results come from the 2024
  rulebook, not from the code: the worked examples (Gettysburg, Thomas vs Longstreet, Sherman at
  Little Rock, Fort Pulaski), the cases in play note 18.0 on amphibious assaults, and the rules
  fixed in the 2026-10-01 review (7.33, 7.34, 7.71/7.72, 7.82, 5.72 both ways, 7.32 Division moves).
- The golden values below are regression tests only: they were captured from the code. If a rules
  fix changes them, regenerate them and explain the rule in a comment next to the changed values.
- Rules decisions: in an amphibious assault on a fort the fort is a surviving zero-SP defender, so
  7.34 (both sides eliminated) does not apply; land battles at a fort still use 7.34. The 10-1
  overrun eliminates the smaller force whichever side it is on, but is not applied to amphibious
  assaults.

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
- PoG has no Monte Carlo simulation and no golden-value tests yet; its tests live in `PoGBattleRulesTests.cs`
  and `PoGRulebookCasesTests.cs`. The latter holds correctness tests from the Deluxe rulebook: its combat
  examples (Tannenberg, Cambrai), the sample-game combats (Sedan, Mulhouse, Nancy and others), the Terrain
  Effects Chart rows, and rule 15.1.6 (an unoccupied fort gets no trench benefit).
- The fire tables and TEC are in `RulesAndTables/PoGCRTs.png`. All 114 fire-table cells were verified
  against the code on 2026-10-01 (POG-RULES-REVIEW-2026-10-01.md).
- Factor mode works with combat factors, not individual units, so flank-attack return fire, the
  "full-strength attacker remains" condition for retreats and the "one step left" condition for
  cancelling a retreat are approximations. Detailed mode (`PoGBattle.Detailed`) removes them: units
  come from `PoG/PoGUnitTypes.json` (embedded resource, loaded by `PoGUnitCatalog`; values supplied by
  the project owner - do not edit them without being asked), losses are allocated by
  `PoGSideForce.TakeLosses` (12.4.3-12.4.6, including the attacker's 12.4.5 loss priority and
  12.4.4.2), and resolution is in `PoGCRT.Detailed.cs`. Factor mode must stay unchanged.
- The JSON file order is the display order. Each unit has a `faction`, and the page offers each side
  only its own faction's units. `attackerLossPriority` holds the 12.4.5 ranks.
- `PoGDetailedModeTests.cs` pins every catalog row and reproduces the rulebook's Tannenberg and Tarnopol
  flank attacks, Combat Example 2, the 12.4.4.2 cases and the Sedan, Nancy and Cambrai loss
  allocations exactly. Keep it passing.
- `LayoutState` (scoped service) lets a page ask `MainLayout` for the narrow-screen nav on a phone held
  in landscape; the PoG page does so while "Detailed units" is ticked and resets it in `Dispose`.
- `PoGBattle` holds unit lists in `EquatableList<T>` so battles keep value equality; the page shows
  results only while `LastStatsBattle == CurrentBattle`.
- For a rule change, add an outcome test for a specific die roll that checks hits, retreats and column shifts
  (see `Outcome_CorpsTableBaseline_UsesExpectedHitsAndRetreat`), plus a test that invalid inputs throw
  (see `Outcome_TrenchBlocksFlank_Throws`).
- For `PoGExactStats.Calculate`, test invariants (probabilities sum to one, conditional means are consistent)
  rather than hardcoding values, unless you have derived the expected value independently from the rules.
- UI clamping and input behaviour for PoG components is covered by bUnit tests (`PoGSideInputTests.cs`)
  and Playwright tests (`PoGBattleE2ETests.cs`).

## Tests for TNW Siege

- TNW's Siege calculator (rule 12) has a small, strictly bounded state space (at most
  `TnwSiegeMethods.FortressStrength` Rounds - 2 normally, 4 for Gibraltar), so it ships with exact
  stats only, no Monte Carlo cross-check - see `TNW-FEASIBILITY-2026-09-27.md` if that report is
  still present.
- `TnwSiegeRulesTests.cs` reproduces the rulebook's own worked example (Castanos besieging Lisbon,
  12.3) verbatim as a golden test, plus the edges that are easy to miss on a first read of the
  rules: the Fortress does not fall if the besiegers are wiped out the same Round it would
  otherwise fall (12.3), and kills take priority over disrupts when capacity runs out (11.3).
- Two assumptions the rulebook leaves open, fixed for the calculator to compute against (see
  TNW-FEASIBILITY-2026-09-27.md §7 for the reasoning): excess casualties fall on Units before the
  Commander, and Land Battle's discretionary bonus-cancellation (11.33) does not apply to Sieges.
  Revisit both if a rule change or an official ruling settles them differently.
- Only one Army sieges, so a Commander is always present. At most `CommandRating` Units roll per
  Round; any further Units in the Duchy replace the Army's losses in later Rounds (12.33) and cannot
  be hit by the Fortress while waiting. The playtester's Napoleon example (14 dice in both Rounds)
  is a golden test.
- `TnwSiegeMethods.ResolveRound` is the single source of truth for one Siege Round; both the exact
  stats (`TnwSiegeExactStats`) and the single-roll simulation (`TnwSiegeMethods.RollOnce`) call it,
  so they cannot disagree with each other by construction.

## Tests for TNW Land Battle

- `TnwLandBattleMethods` holds one pure path (`DiceForRound`, `ApplyHits`, `Verdict`, `Conclude`)
  shared by the exact enumeration, the Monte Carlo simulation and "Roll 1 Battle", as for Siege.
- Exact stats get expensive with large dice pools: every tied Round-1 state pair needs a joint
  enumeration of both sides' Round-2 rolls. `TnwLandBattleExactStats.TryCalculate` estimates the
  work before Round 2 and declines above `DefaultWorkBudget` (1M evaluations, about 1.3 s in the
  browser, around 11 v 11); `TnwLandBattleStatsCalculator` then falls back to Monte Carlo and the
  result is labelled accordingly. Re-measure in the browser before raising the budget.
- `ExactStats_And_MonteCarlo_ProduceSimilarResults` cross-checks the two engines on small battles,
  using a seeded `Random` so it is deterministic - keep it passing after any rules change.
- Victory and rout are decided on casualties as rolled (uncapped), following the rulebook's naval
  example (13.4); a side wiped out by kills loses regardless of totals (11.32).
- Assumptions (see TNW-FEASIBILITY-2026-09-27.md §9): kills fall on already-disrupted Units first,
  then undisrupted Units, then the Commander; each disrupt beyond the Commander cancels one
  nationality bonus die; event dice apply to Round 1 only; a losing attacker can always retreat.
- Amphibious Assault (`TnwLandBattle.AmphibiousLanding`, rule 13.7): the landing attacker takes one
  round of shore battery fire (2 dice for a Port, 4 for a Fortress-Port, `TnwLandBattleMethods.
  ShoreBatteryFire`) before Round 1 via the same `ApplyHits`, so its casualties already count
  toward Round 1's total and reduce Round 1 dice. Unlike a naval Port battle (13.5), the batteries
  fire only once - do not add them to `DiceForRound`'s defender branch. If the attacker is wiped
  out by shore fire alone, the battle ends there (`Rounds == 0`, logged as Round 0). Terrain and an
  Amphibious Assault are mutually exclusive (`TnwLandBattleInputRules`) since a landing crosses no
  rough/pass/marsh line. `TnwLandBattleExactStats` enumerates every shore-fire outcome as an extra
  weighted dimension ahead of Round 1 - see its `AccumulateRoundOne` helper before changing it.

## Tests for TNW Naval Battle

- Same structure as Land Battle: one pure path in `TnwNavalBattleMethods`, exact stats with a work
  budget and a Monte Carlo fallback, and `ExactStats_And_MonteCarlo_ProduceSimilarResults`.
- The rulebook's naval example (13.4, British vs French and Spanish) is the golden test,
  `RulebookExample_FullBattle_TieThenTie_ActiveBritishLose`, step by step: 13 vs 6 dice, the Spanish
  Squadron sunk first, 9 vs 3 dice in Round 2, and the Active Fleet losing the second tie.
- `TnwFleetComposition` packs the per-nation counts into one `ulong` so battle definitions keep value
  equality - the page shows results only while `LastStatsBattle == CurrentBattle`. Don't replace it
  with an array or list.
- The optional "Gallant Danes" event (Admiral Fischer) is `TnwNavalBattle.ActiveHasFischer` /
  `InactiveHasFischer`: that side's Danish Squadrons roll one extra die each
  (`TnwFleetComposition.DiceWithFischer`), and `TnwFleetState.FischerSixVoided` tracks the
  once-per-battle voided "6" so `ApplyHits` only cancels one, across Rounds and shore battery fire.
  The page's checkbox (`TnwFleetInput`) is disabled, and cleared, without a Danish Squadron.
- Performance matters here: the first version took 13 s in the browser. Sinkings follow a fixed
  allocation rule, so the Fleet after k sinkings is precomputed once per starting Fleet
  (`LossSequence`), and `TotalSquadrons` sums the packed counts in constant time. Re-measure in the
  browser after touching `ApplyHits`, `Verdict` or `Conclude`.
- Assumptions (see TNW-FEASIBILITY-2026-09-27.md §10): among the nations with fewest losses, the
  owner sinks the Squadron rolling fewest dice, Refit first; shore battery dice are never reduced by
  "5"s; fortified straits (13.8) and Squadrons under Build are out of scope.
