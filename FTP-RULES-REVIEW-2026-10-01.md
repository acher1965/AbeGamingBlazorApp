# FtP rules review (2026-10-01)

I checked the For The People calculator against both PDFs in `RulesAndTables/`:
`FTP_RULES_2024-FINAL-web.pdf` (the 2024 rulebook) and `FTP2_Charts.pdf` (the charts
sheet). No code was changed.

## Summary

**The core of the calculator is correct.** I verified the Combat Results Table cell by
cell against the rulebook's copy (7.10), which is real text rather than a scan. All 60
results, for small, medium and large battles, match the code, including where the
asterisks fall. The rulebook's three worked examples all reproduce exactly:
- Gettysburg (7.74): 8 gives 4\*, and 10 gives 6.
- Thomas vs Longstreet (7.10, Example 1): 8 gives 2\* and 7 gives 2, so the attacker
  wins on the asterisk.
- Sherman at Little Rock (7.10, Example 2): 7 gives 1\* and 3 gives 1, and the asterisk
  is cancelled because Little Rock is a Resource space.

Also correct:
- **Battle size:** 5 SPs or fewer is small, 6-19 medium, 20 or more large (7.31).
- **Force ratio:** +2 at 3-1, +3 at 4-1, +4 at 5-1 or more, and +4 against an
  ungarrisoned fort (7.53, 6.81).
- **The other modifiers:** +2 for intercepting (5.83), +2 for a garrisoned fort in a land
  battle (6.87), +1 per elite unit (7.51), +2 when the opponent is out of supply (8.3).
- **Amphibious modifier:** ironclad +2, fort +2, Hunley +1 and torpedoes +1 against the
  Union's assault level plus 2 for Foote or Porter. The lesser is subtracted from the
  greater and the net is capped at +3 (6.41). The land-battle fort +2 is correctly not
  counted twice.
- **Who wins:** decided on CRT results, not on losses actually taken. Ties go to the
  defender unless there's an asterisk (cancelled in Resource and capital spaces) or the
  defender is an ungarrisoned fort and the attacker survives (7.32, 6.81).
- **Loss caps:** at most twice your own strength, and at most 1 SP from an ungarrisoned
  fort (7.31, 7.81).
- **Overrun:** at 10-1 with no fort (5.72).
- **Continue moving:** for an attacker that wins at 2-1 or more (5.73, 7.32), and in the
  7.33 A/B/C cases for a defender that wins but is wiped out. The amphibious exception in
  6.42 is respected.
- **General casualties:** the thresholds (1-3 for the player who rolled a modified 10,
  1 for the other) and the force-ratio exclusions (7.71, 7.72).
- **Size limits:** 15 SPs for an Army; 3 SPs for a naval move, or 9 for Grant or
  Sherman's river move (5.13, 6.12, 6.15b).

**Seven bugs found, all in edge cases.** None affects the CRT or the modifiers. They're
in how the battle's aftermath is resolved, in general casualties, and in elite losses.
Each was confirmed by feeding fixed dice to `BattleResult`. All seven probe tests fail
on current code exactly where the rules predict; the cases are at the end.

Both the exact stats and Monte Carlo call `FtpBattleMethods.BattleResult`, so each fix
lands in one place.

## Findings, by impact

### 1. Amphibious assault on a fort: a 1 SP force that is wiped out still "captures" it (high)

Rules 7.33 and 7.34, as interpreted by the designer's play note in **18.0**.

The case: 1 Union SP assaults a fort (garrisoned or ungarrisoned), and both sides lose
1 SP with the asterisk on the Union side (1-1\*). The play note is explicit: **"If there
is 1 Union SP, the invasion fails, because 7.34 does not apply, but 7.33 does, since
there is a surviving defending zero SP."**

The code instead treats this as both sides eliminated (7.34). That rule lets the winner
keep 1 SP, so it gives the Union 1 surviving SP and reports that it stays in the fort.
`defenderWipedOut` is computed as `finalHitsToDefender >= DefenderSize`, which is always
true for a 0 SP ungarrisoned fort.

This is the commonest Union amphibious play (1-3 SPs against a coastal fort), so it
visibly overstates the 🏠 "attacker stays" probability.

**Fix:** in an amphibious assault on a fort space, don't apply the 7.34 reprieve. The
fort is a surviving zero-SP defender, so an eliminated attacker has failed (7.33).

**Question for the tester:** should the same apply to a *land* battle against a
garrisoned fort where both sides are eliminated? The play note only covers amphibious
assaults, so I would leave land battles under 7.34 unless told otherwise.

### 2. General casualties ignore out-of-supply (medium)

Rules 7.71 and 7.72: if the **attacker is out of supply**, there is no *defender*
General Casualty roll; if the **defender is out of supply**, there is no *attacker* roll.
The code applies only the force-ratio half of each rule (1-3 or worse, 3-1 or better).
Out of supply is a common situation, so the 🗡️💀 / 🛡️💀 probabilities are overstated
whenever either side is out of supply.

**Fix:** add the two out-of-supply conditions where the casualty thresholds are set in
`FtpCRT.Outcome`.

### 3. Both sides eliminated, tie without an asterisk: only the defender keeps 1 SP (low)

Rule 7.34: "In case of ties involving no asterisk, **both sides retain 1 SP**, but the
attacker is the loser." The code reprieves only the winner, so the attacker is still
wiped out. This only happens in small and medium battles, for example 1 SP against 1 SP.

**Fix:** in that tie case, leave 1 SP on both sides.

### 4. An attacker that wins but is eliminated is reported as staying, or moving on (low)

Rule 7.33: "If the attacker wins but is eliminated, the defender does not retreat, but
instead remains in the space." The code sets `attackerCanStay` whenever the attacker
wins, and never clears it when the attacker is wiped out. Example: 3 SPs attacking 17
wins 5\* to 4, but loses all 3 SPs.

The same gap exists in `attackerCanContinueMoving`. That flag can also be true for an
eliminated attacker, when the attacker is exactly twice the defender and the losses are
capped at that.

**Fix:** set both flags only if the attacker survives, after the 7.34 reprieve has been
resolved.

### 5. Elite unit loss uses the CRT result, not losses taken (low)

Rules 7.51 and 7.82: one elite is lost "if ... the force **takes** two or more SP losses";
the Gettysburg example also uses losses taken. The code tests the raw CRT result
(`hitsToAttacker > 1`, `hitsToDefender > 1`), so a capped loss still costs an elite.
Example: a 6 SP Army against an ungarrisoned fort gets a CRT result of 2, but the fort
can only inflict 1.

**Fix:** test the final losses for both the attacker and the defender.

(Findings 4 and 5 each cover two code sites, which is why there are seven probe tests
for five findings.)

## Notes - not bugs, worth a decision

- **Continue moving needs an Army or Corps move** (7.32): a Division can't continue. The
  calculator has no input for the move type, so it may overstate the 🚶 chance for small
  forces. One option is an "Army/Corps move" checkbox.
- **Overrun when the defender is 10 times the attacker** (5.72): "the smaller force is
  eliminated and the larger force may continue moving." The code only applies an overrun
  when the attacker is the larger force. When the defender is 10 times larger it fights
  a normal battle with +4 to the defender. The rule doesn't say which side, but "may
  continue moving" suggests the moving force, so the code's reading is defensible.
- **Army-size naval move** (6.15a): McClellan, Grant or Sherman moving an Army between
  Washington, Aquia Creek, Urbanna and Fort Monroe has no stated SP limit, so up to 15.
  The calculator caps amphibious Army moves at 9, the river-move limit from 6.15b.
- **General casualties** report the chance that *a* general dies. An Army's commanding
  general is never killed unless he is the only general there (7.73). The existing
  tooltip ("assumes a leader is present") covers this.

## Golden tests

`ExactStatsGoldenTestCases()` in `FtpBattleStatsTests.cs` holds values captured from
this code. They are **regression tests, not correctness tests**: where a fix changes
losses (findings 1 and 3), the affected small-battle cases need regenerating through
the documented `GetGoldenTemp` workflow. The probe cases below should become permanent
correctness tests (for example `FtpRulebookCasesTests.cs`), together with the three
rulebook examples and the full table of cases in play note 18.0.

## Probe cases (fixed dice into `BattleResult`)

The dice order is attacker, defender, then the two general-casualty dice. Each comment
gives the rule's required result.

```csharp
private static FtpBattle Battle(int a, int d, bool fort = false, int aDrm = 0, int dDrm = 0,
    bool aOos = false, bool dOos = false, int aElites = 0, FtpAmphibious? amph = null) =>
    new(false, fort, false, true, a, d, aDrm, dDrm, aElites, 0, aOos, dOos, amph);

FtpAmphibious naval = new(false, false, false, false, false, 0);
FtpAmphibious armyNaval = new(true, false, false, false, false, 0);

// 1. 18.0: 1 SP vs ungarrisoned fort, 1-1* -> invasion fails (DamageToAttacker 1, cannot stay)
Battle(1, 0, fort: true, amph: naval).BattleResult([3, 1, 6, 6]);
// 1. 18.0: 1 SP vs garrisoned fort (1 SP), 1-1* -> invasion fails (DamageToAttacker 1, cannot stay)
Battle(1, 1, fort: true, aDrm: 3, amph: naval).BattleResult([4, 1, 6, 6]);
// 2. 7.71: attacker out of supply -> DefenderLeaderDeathDieRoll is null
Battle(5, 5, dDrm: 2, aOos: true).BattleResult([1, 6, 1, 1]);
// 2. 7.72: defender out of supply -> AttackerLeaderDeathDieRoll is null
Battle(5, 5, aDrm: 2, dOos: true).BattleResult([6, 1, 1, 1]);
// 3. 7.34: 1 vs 1, tie 1-1 without asterisk -> both lose 0 (each keeps its 1 SP), defender wins
Battle(1, 1).BattleResult([4, 2, 6, 6]);
// 4. 7.33: 3 vs 17, attacker wins 5*-4 but loses all 3 SPs -> AttackerCanStay false
Battle(3, 17, aDrm: 6).BattleResult([3, 3, 6, 6]);
// 5. 7.82: 6 SP Army vs ungarrisoned fort, CRT 2 but only 1 SP lost -> AttackerEliteLoss false
Battle(6, 0, fort: true, aElites: 1, amph: armyNaval).BattleResult([1, 5, 6, 6]);
```

## Proposed next step (your call)

Fix all five findings in one change: rule-based tests added, goldens regenerated where
the fixes change them, and the exact/Monte Carlo agreement test still passing. Then a
patch release, 1.3.1. I haven't bumped the version or committed anything.

## Outcome (2026-10-01, v1.3.1)

You approved the fixes and made these decisions:
- **Land battles at a fort** stay under the normal 7.34 rule. Only amphibious assaults on
  a fort follow the 18.0 play note.
- **Division move:** a new checkbox with a tooltip. A Division can't continue moving
  after winning a battle (5.73, 7.32), though an overrun still lets it move on.
- **Amphibious Army move:** up to 15 SPs (6.15a). The tooltip explains the 9 SP riverine
  limit (6.15b) and the 3 SP normal naval move (6.12).
- **Overrun (5.72)** now applies whichever side is 10 times larger: a 1 SP attacker
  moving into 10 SPs with no fort is eliminated without a battle.

All five findings are fixed. `FtpRulebookCasesTests.cs` (26 tests) adds the rulebook
examples, the play note 18.0 cases, and a test for each fix and decision. Three existing
expectations changed, each because of a rule:
- **1 v 1 small battle** (golden and Monte Carlo): the attacker's expected loss drops
  from 10/12 to 5/12. This is rule 7.34: a 1-1 tie with no asterisk, which has
  probability 5/12, leaves both sides 1 SP.
- **2 v 20 large battle** (golden): now an overrun of the attacker (5.72), so the
  defender takes no losses.
- **Amphibious Army move cap** (input rules): 9 becomes 15 (6.15a).

The new golden values were worked out by hand from the rules, and the exact enumeration
matches them.
