# Feasibility Report: "The Napoleonic Wars" Battle Calculator (2026-09-27)

Speculative exploration requested: could this app do for **The Napoleonic Wars**
(TNW, GMT Games, Mark McLaughlin, Living Rules v1.3f, `RulesAndTables/TNW-Rules-2008.pdf`)
what it already does for FtP and PoG? This report is a planning document. It was
revised same-evening after the modification below; implementation of stage 1
(Siege) followed immediately — see the last section for what changed while you
were asleep.

## 0.1 Modification reviewed: Siege first, separate pages per battle type

You asked to reorder the plan so **Siege ships first** (you judged it simplest),
and to consider **separate pages** for Land Battle, Siege, and Naval Battle rather
than one page. You asked me to check this with the advisor for serious impediments
before acting, and to bring in two arguing subagents if the question was genuinely
contested.

**Advisor's conclusion: no serious impediment — both changes are improvements, not
just acceptable alternatives.** I did not spawn the two arguing subagents; the
advisor was clear enough that manufacturing a "con" side would have been arguing
for its own sake, not surfacing a real risk. Reasoning:

- **Siege first builds the shared machinery on the easiest case.** Siege is
  one-sided (Army vs. a fixed Fortress, not two composed forces), has no
  retreat/rout for the attacker (12.3: "A Besieging Army does not retreat or
  rout"), and — once checked carefully (§1.1-revised below) — has a **strictly
  bounded** number of rounds (at most the Fortress's strength: 2 normally, 4 for
  Gibraltar), unlike Land Battle's already-small 2-round cap. The dice-pool
  trinomial engine this needs (`TnwDicePool` in the implementation) is exactly
  what Land Battle will reuse later — building it against the simpler consumer
  first, then extending, beats building it against the harder consumer first.
- **Separate pages match existing precedent, not a new pattern.** FtP and PoG
  already live on separate pages despite being "the same kind of thing" (a
  single-battle calculator); Land/Siege/Naval are mechanically distinct enough
  (two-sided-with-rout vs. one-sided-vs-fixed-fortress vs.
  per-nationality-fleet-dice) that cramming them into one page with conditional UI
  would be *more* complex, not less. One caveat worth a look later, not now: three
  new TNW pages plus the existing 6 push the nav to 9-10 items — a TNW landing
  page or a grouped nav submenu is a reasonable follow-up, not a blocker today.

This *did* change the technical picture, though — my first pass at the Siege rules
undersold how clean the math is (§1.1-revised) and almost missed one real rule edge
(besiegers wiped out block the Fortress from falling, 12.3). Both are corrected
below and were caught before any code was written.

## 0. Read this first: TNW is a different kind of game

FtP and PoG are 2-player wargames where combat is resolved by rolling dice **against
a printed Combat Results Table (CRT)** — a lookup keyed by strength ratio and die
roll modifiers. The calculators exist because doing that lookup and its modifiers by
hand is fiddly, and because computing exact win/loss probabilities across every
possible roll (rather than rolling once) is not something you can do with a table
and a pair of dice.

TNW is a 2-5 player diplomacy-and-war game (Napoleon-era Europe: builds, cards,
diplomacy, movement, sieges, naval war). Its Battle system (rule 11) has **no CRT at
all** — it is a straight dice pool: roll one die per unit (plus modifiers), a "6"
kills, a "5" disrupts. There is nothing to transcribe from a chart, which removes a
category of work FtP/PoG needed (the "golden regression" workflow in
`copilot-instructions.md` exists specifically to get exact CRT numbers by hand).

That said, don't undersell it: a real TNW battle can throw well over a dozen dice
per side, so *counting 5s and 6s across two rounds, with disrupts changing the pool
between rounds, casualties split by nationality, and a rout check* is exactly the
kind of bookkeeping a calculator earns its keep on — arguably more tedious by hand
than FtP's single CRT lookup. The "Roll 1 Battle" button has genuine table utility
here. The open question isn't "is this useful" so much as "how much of the game do
we want to model."

## 1. The rules, at a glance

Extracted via `pdftotext -layout` (no OCR needed — text PDF, 24 rules pages plus a
"Veteran's Quick Intro to 2nd Edition"). Sections relevant to a battle calculator:

| Section | Covers | Relevant to a calculator? |
|---|---|---|
| 10. Interception & Evasion | Pre-battle 2d6 rolls that decide whether a battle even happens, and add +1 die per failed evasion | Optional upstream step — see 4.3 |
| **11. Battle** | The core land-battle dice mechanic | **Yes — this is the MVP** |
| 12. Fortresses (Siege) | A separate, simpler dice-vs-fixed-strength mechanic | Optional extension |
| 13. Naval Affairs | A parallel dice-pool system for fleets, plus shore batteries and port battles | Optional extension |
| 2-9, 14-17 | Setup, builds, diplomacy, movement, resources, interphase | Not applicable — these are full-game bookkeeping, not a single-battle calculation |

### 1.1 The Battle mechanic (rule 11), in calculator terms

Each side rolls a dice pool sized as:

```
dice = undisrupted units (incl. non-Commander leaders as 1 unit each)
     + Commander's Battle Rating (if undisrupted)
     + nationality bonus (+1 normally, +2 if >50% French, +0 if >=50% Minor)
     + terrain bonus for the defender, first round only (Rough +1 / Pass +2 / Marsh +3)
     + 1 per failed enemy evasion attempt, first round only
     + event card modifier (+/-, if any events are in play)
```

Each die: **6 = kill, 5 = disrupt, other = miss.** Kills and disrupts are simultaneous;
kills take priority over disrupts when there aren't enough units to record both.

- **Round 1** always happens. Compare total casualties (kills + disrupts) each side
  inflicted. Higher total wins; the loser retreats. **On an exact tie**, fight
  **Round 2** with whatever undisrupted units/dice remain (disrupted units are out;
  excess disrupts beyond a side's own unit count cancel bonus dice — Commander's
  rating first, then event/nationality bonuses "at the player's discretion", per
  11.33). If Round 2 is *also* tied, the attacker retreats. **Battles never exceed
  two rounds.**
- **Rout** (11.5): if the victor's casualty margin is >=3, all of the loser's
  *disrupted* units/leaders are eliminated outright, and routing an Army/Army Group
  gives the victor a further die-roll chance at a Resource.
- **Overrun** (11.6) / **Flag Overrun** (11.7): if a side is wiped out entirely, or
  the winner's kills exceed the loser's total units/leaders, extra effects trigger
  (continued movement, a free Flag placement).

~~This is genuinely simpler to compute exactly than FtP's CRT ... Exact enumeration is
very feasible.~~ **Corrected 2026-09-28, after building it.** Each Round alone is a small
trinomial per side, but the battle is two-sided: every tied Round-1 state pair needs a
joint enumeration of both sides' Round-2 rolls. The work grows roughly with the sixth
power of the dice pool. Measured natively, 12 v 12 took 180 ms and 24 v 24 took 7 s,
and the browser is about 10x slower. So exact stats only cover small and medium
battles, and larger ones fall back to Monte Carlo (see §9). The two engines agree to
within half a percentage point where both can run.

### 1.1-revised The Siege mechanic (rule 12), corrected and in calculator terms

My first pass under-specified this; here's the corrected version, checked against
the rulebook's own worked example (Castanos besieging Lisbon, 12.3) before writing
any code.

Each Siege Round, **both sides roll simultaneously**:

- The **Fortress** rolls a fixed dice pool = its strength (2 normally, **4 for
  Gibraltar**) at the besieger. Its 6's kill, its 5's disrupt, the same as any
  Battle (11.3) — kills take priority over disrupts when the besieger doesn't have
  enough undisrupted units/Commander left to record both.
- The **Besieging Army** rolls its own pool = undisrupted units + Commander's
  Battle Rating (if undisrupted) + nationality bonus - 1 if besieging a
  Fortress-Port whose owner controls an adjacent Zone (12.32). Only its **6's**
  matter, and only as a running total: they accumulate toward the Fortress's fall
  threshold (its own strength number again — the 5's/misses do nothing, since the
  Fortress isn't a formation that can be disrupted).

**Round 1 always happens.** A Round **continues** only if the besieger's 6's this
round strictly exceed the losses it suffered this round (12.3: "may attack again
... if it has caused more casualties than it has suffered"). Since that requires
at least one net six per continuing round, and the Fortress needs a cumulative
`strength` sixes to fall, **the number of rounds is bounded by the Fortress's own
strength — 2 rounds normally, 4 for Gibraltar** (proof: each continuing round
contributes >=1 toward the running total; you can't need more continuing rounds
than the total itself). This is *more* bounded than Land Battle's already-small
2-round cap, and rules out the "open-ended rounds" reading I first assumed.

**The Fortress falls** when cumulative sixes >= strength, **unless the Besieging
Army was wiped out in that same round** (12.3: "...if the Besiegers are not
eliminated") — an edge easy to miss on a first read, and one my initial pass
missed. **Overrun** (12.31) is then a deterministic check: the Fortress falls in
*fewer* rounds than its strength, or with *more* sixes than its strength needed.

This is a small, bounded, exact-enumerable state machine — genuinely the simplest
of the three battle types, confirming your read that it's the right one to start
with.

### 1.2 Where the real complexity is: the input surface

FtP and PoG each ask for ~4-5 numbers per side (SP/Factors, DRM, Elites/Table, OOS)
plus a handful of shared checkboxes. TNW's dice-pool formula above needs noticeably
more, because "how many dice do I roll" depends on composition, not just a total:

- Unit count *and* whether a Commander is present, *and* the Commander's Battle
  Rating (a leader adds one bonus die worth of dice, not just "+1").
- Nationality composition as a **share**, not a count (>50% French / >=50% Minor /
  else) — this needs at least a coarse breakdown, not a single strength number.
- Which terrain (if any) the attacker crossed to reach the battle (Rough/Pass/Marsh
  are mutually exclusive, each with a different bonus).
- Number of failed evasion attempts by the defender this impulse (0 in the common
  case, but a real input if modeling interception/evasion at all — see 4.3).
- Any battle-event die modifier in play (+/-, optional).

That's roughly 3-4x the input fields of a single PoG side. Expect the UI component
(`TnwSideInput.razor`) to be noticeably larger than `PoGSideInput.razor`, and the
page to need more careful layout (grouping "who's fighting" from "terrain/events",
which are properties of the battle, not either side).

## 2. Scope options (revised staging: Siege first)

Mirroring how FtP and PoG were each scoped to "the land battle only" (FtP has no
naval rules to speak of; PoG has no sieges), I'd stage TNW the same way — reordered
per your call in §0.1, each on its **own page**:

| Tier | What it adds | Depends on |
|---|---|---|
| **A — Siege (stage 1, in progress)** | Section 12: one-sided dice-pool vs. a fixed Fortress strength, bounded rounds, exact stats. Builds the shared `TnwDicePool` trinomial engine. | — |
| **B — Land Battle** | Section 11: two-sided dice pools, two rounds, rout/overrun/flag-overrun, exact stats + Monte Carlo cross-check. Reuses `TnwDicePool` from A. | A (for the shared dice-pool engine; not a hard blocker, just efficient sequencing) |
| **C — Naval Battle** | Section 13: a parallel dice-pool system (per-nationality dice, not per-unit; shore battery pre-fire; port battles) — comparable size to B, built independently | A |

B and C are genuinely separate features that happen to share the "roll a pool of
d6, count 5s/6s" primitive with A — some shared code (`TnwDicePool`) is realistic
and is exactly what A is building; the surrounding rules (two-sided rout/retreat
vs. nationality-based fleet dice) are distinct enough that bolting them on isn't a
small add.

## 3. Decisions needed before starting (yours to make, not mine)

1. **Which tier to build** — A only, A+B, A+C, or all three (table in §5 prices each).
2. **The 11.33 discretion rule.** When excess disrupts in Round 2 must cancel bonus
   dice, the rulebook leaves the order up to the player ("at the player's
   discretion"). A calculator computing probabilities needs one fixed rule to
   enumerate against. I'd default to canceling in a fixed order (Commander first,
   as the rule requires, then nationality bonus before event bonus, as the smaller
   value) and document that assumption in the code — the same approach already
   used for the FtP rule-7.33C ambiguity we discussed earlier.
3. **How far to carry consequences.** Do you want the calculator to also report
   Rout-driven Resource-gain probability and Flag-Overrun likelihood, or stop at
   "who wins, what does each side lose, does Round 2 happen"? The battle-only stats
   are self-contained; Resource/Flag effects reference broader game state
   (Resources, Flags) that only matters in a full game, not an isolated calculation
   — reporting their *probability* is fine, but modeling their *effect* is out of
   scope for a single-battle calculator either way.
4. **Whether to model evasion/interception (rule 10) upstream**, or take "number of
   failed evasion attempts" as a given input the way FtP takes "Interception" as a
   checkbox. I'd recommend the latter for the MVP — chaining a second independent
   2d6 mechanic in front of the battle roll is a reasonable v2, not a v1 requirement.
5. **Exact stats vs. Monte Carlo only.** Given §1.1's simpler combinatorics, I'd
   recommend building exact stats as the primary output (matching FtP/PoG's pattern)
   with Monte Carlo as the cross-check, not the other way around.

## 4. Effort estimate

**Method, stated plainly:** I have no access to this project's actual historical
token or dollar spend, and the repository's own history doesn't settle it either —
the commit that built PoG (`c3a035b`, "Implement PoG battle calculator beta with
rules, UI, and tests", 2026-03-09) carries no Claude Code attribution, so I can't
tell whether that work was done with an AI assistant at all, let alone read its
token cost from `git log`. **If you want a measured anchor instead of an estimate,**
check your own Claude Code usage/cost history (or the Anthropic Console) around
2026-03-09 — that would beat anything below.

Absent that, here's the estimate, built from two things I *can* verify in this repo:

- **Code-size anchor:** PoG's initial implementation (`c3a035b` plus the three
  same-day follow-up commits) changed **~1,220 net lines across 19 files** — new
  `GameLogic/PoG/*` engine and exact-stats math, two new UI components, a battle
  page, and test files — done in one day. FtP's equivalent surface (`FtP/*.cs` +
  its UI + its tests) totals 668 + 1,070 + 903 = **2,641 lines** today, reflecting
  more rules edge cases (amphibious assault, leader deaths, elites) accumulated
  since.
- **Task-shape anchor:** the input surface analysis in §1.2 (3-4x the fields of a
  PoG side) and the rules-complexity analysis in §1.1 (simpler core math, but two
  extra deterministic post-processing steps — rout, overrun/flag-overrun — that
  PoG/FtP don't have).

Net LOC estimate scales PoG's anchor up moderately for tier A (bigger input surface,
extra post-processing) and adds comparably-sized independent chunks for B and C.
Token and dollar figures then apply a **4:1 input:output ratio** (typical for
agentic coding sessions that re-send growing conversation history each turn) against
current list pricing — **Sonnet 5** blends to $3.60/MTok, **Opus 5.5** to
$7.20/MTok. Two things push the real number lower than shown: prompt caching
typically cuts realized input cost by ~90%, and if this work is done under a Claude
Code subscription rather than pay-as-you-go API access, the marginal dollar cost is
$0 regardless of tokens — the token column is what would actually track cost/effort
in that case.

**These are order-of-magnitude estimates, not quotes — treat them as +/-2x.**

| Scope | Net LOC (code+UI+tests) | Est. tokens | $ @ Sonnet 5 | $ @ Opus 5.5 |
|---|---|---|---|---|
| **A — Land Battle (MVP)** | ~1,600 - 2,200 | ~3M - 5M | ~$11 - $18 | ~$22 - $36 |
| **B — + Siege** | +~300 - 450 | +~0.6M - 1M | +~$2 - $4 | +~$4 - $7 |
| **C — + Naval Battle** | +~900 - 1,300 | +~2M - 3.5M | +~$7 - $13 | +~$14 - $25 |
| **A + B + C (full parity)** | ~2,800 - 3,950 | ~5.5M - 9.5M | ~$20 - $34 | ~$40 - $68 |

For calibration, the FtP tooltip feature we just shipped (`4617040`, five icon
tooltips + hover text + 8 new tests) is a small fraction of tier A — maybe 5-10% of
it by LOC — so tier A alone is genuinely a multi-session undertaking, not an
afternoon's polish, even though each individual piece (dice-pool math, a Blazor
input component, xUnit tests) is a pattern this codebase already has three worked
examples of (FtP land, FtP amphibious, PoG).

## 5. What tier A (Siege) concretely touches

Following the existing project shape, on its own page per §0.1:

- `AbeGaming.GameLogic/TNW/` — `TnwForceComposition.cs` (shared enum),
  `TnwDicePool.cs` (shared trinomial dice-pool math — reused by Land Battle later),
  `TnwSiegeBattle.cs` (input record), `TnwSiegeState.cs`, `TnwSiegeMethods.cs`
  (round resolution + single-roll simulation), `TnwSiegeExactStats.cs`,
  `TnwSiegeStats.cs` (output record), `TnwSiegeInputRules.cs` (clamps).
- `AbeGaming.GameLogic.Tests/TnwSiegeRulesTests.cs` — the Castanos worked example
  verbatim, probabilities-sum-to-1, the overrun/eliminated edges from §7.
- `AbeGamingBlazorApp/Components/TnwSiegeInput.razor`, `TnwSiegeStatsDisplay.razor`
  — new components.
- `AbeGamingBlazorApp/Pages/TnwSiegePage.razor` — new page, nav link, icon.
- `AbeGaming.BlazorApp.Component.Tests/TnwSiegeInputTests.cs` — bUnit tests.
- `AbeGaming.BlazorApp.E2E.Tests/TnwSiegeE2ETests.cs` — Playwright test.
- `README.md` / `.github/copilot-instructions.md` — document the new calculator
  and the assumptions in §7, same as FtP's golden-regression note and PoG's
  testing guidance.

## 6. Recommendation

Start with **Siege (Tier A)** — done, see below. It's self-contained, builds the
shared dice-pool engine Land Battle and Naval will both reuse, and gives you a
concrete result (and a real cost data point, unlike this report's estimates)
before deciding whether Land Battle or Naval are worth the same treatment.

## 7. Assumptions made while you slept (please review)

Siege (Tier A) was implemented tonight per your instruction. The core mechanic in
§1.1-revised is unambiguous and directly checked against the rulebook's own
Castanos example (a golden test in `TnwSiegeRulesTests.cs` reproduces it exactly).
A few things the rulebook leaves genuinely open needed a fixed choice to make a
calculator computable — flagged here, not buried in code comments only:

1. **Casualty allocation order (Units before Commander).** When the Fortress's
   kills/disrupts exceed what the besieger's remaining Units can absorb, the
   excess falls on the Commander. The rulebook doesn't state an order (11.3 only
   says the *owner* decides which nationality absorbs excess kills in a
   multinational force, and separately requires leader losses to have the owner's
   consent) — I assumed a rational owner always sacrifices a Unit over the
   Commander when there's a choice, so the Commander is only ever touched once
   Units are exhausted. This only changes the outcome when the Fortress's roll
   would otherwise have had "spare" capacity to spend on the Commander instead of
   a Unit — a narrow case, but worth you confirming it matches how you'd actually
   play it.
2. **11.33's discretionary bonus-cancellation (Land Battle only) does not apply to
   Sieges.** That rule sits inside 11.3 (Battle's casualty resolution); Section 12
   is explicit about the ways it diverges from Section 11 (no retreat, no rout,
   its own fall condition) and never cross-references 11.33. I read that as
   deliberate rather than an oversight, so a Siege's nationality bonus and
   Commander rating apply at full value every round they're eligible, with no
   disrupt-driven cancellation. Flag this one if you read 11.33 as broader than I
   did.
3. **"May attack again" (12.3) is modeled as "always does".** The rule makes
   continuing optional for the besieging player; a calculator computing
   probabilities needs one fixed policy to enumerate against, so I assumed the
   besieger always presses the attack whenever the rules permit it (this is also
   the natural reading if you're using the calculator to evaluate *whether it's
   worth sieging*, which wants the best-case continuation policy, not a random
   one).
4. ~~**12.33 (Army Group reserves replacing losses mid-siege) is out of scope.**~~
   **Superseded 2026-09-28 by playtest feedback — see §8.** Leaving it out was a
   mistake, not a harmless cut: the reserve rule changes the dice count in every
   Round after a loss.
5. **No Monte Carlo cross-check for Siege.** Exact stats are cheap enough here
   (small, strictly bounded state space) that a simulation adds little; this
   matches how PoG itself originally shipped exact-stats-only. Worth adding later
   if Land Battle's Monte Carlo makes the pattern feel expected.

Also implemented, not requiring review: input clamps (Units 0-30 since 2026-09-28,
Commander Battle Rating 1-4 per the rulebook's own leader-rating range), a "Roll 1 Siege"
single-random-resolution button mirroring FtP/PoG's UX, and the standard
build/test/docs pass (GameLogic tests including the Castanos golden test, bUnit,
one Playwright test, nav entry, a `TNW` section in `.github/copilot-instructions.md`
and a `Home.razor` bullet). **Nothing was committed** — same as every other session
this evening; that's still your call in the morning.

## 8. Playtest corrections (2026-09-28)

A human playtester checked the preview deployment and found the Siege inputs didn't
match how the rules actually work. All three points are right, and follow directly
from 12.2 and 12.33, which the first version had misread or left out:

1. **The Commander is always present.** Only an Army may Siege (12.2), and an Army
   always has a leader, so the "Commander present" checkbox was removed.
2. **Only one Army rolls, capped at its Command Rating.** The number of Units that
   roll each Round is at most the Commander's Command Rating (4, 6 or 8). Other
   Armies in the same Duchy don't add dice. There is now a Command Rating input.
3. **Spare Units replace losses (12.33).** The "Units" input is now *every* Unit
   available in the Duchy. Units beyond the Command Rating don't roll, but they step
   in to replace the rolling Army's disrupted or killed Units in later Rounds. Armies
   are single-nationality, so the nationality bonus applies to the whole force (the
   dropdown now reads French / Other Power / Minor).

The playtester's worked example is now a golden test
(`ResolveRound_NapoleonExample_LossReplacedFromReserve_StillRolls14`). Napoleon
(Command Rating 8, Battle Rating 4) has 8 Units, and Soult's Army of 4 is with him.
Round 1 rolls 8 + 2 (French) + 4 (Napoleon) = **14 dice, not 18**. After one Unit is
killed, a Unit from Soult's Army steps in, so Round 2 rolls **14 again, not 13**.

One modeling choice follows from this, adding to §7's list: **Units waiting beyond
the Command Rating can't be hit by the Fortress's fire.** Only the Units that rolled
that Round (plus the Commander) absorb its kills and disrupts, and the reserve then
refills the gaps. This fits "replace any disrupted/eliminated Units in the Besieging
Army" (12.33). **Confirmed correct by the user, 2026-09-28.**

The bound on the number of Rounds (§1.1-revised) is unchanged. Continuing still needs
at least one net six per Round.

## 9. Land Battle (stage 2) - implemented 2026-09-28

The Land Battle calculator is at `/tnwbattle`, on its own page as agreed. It uses the
same structure as Siege: one pure rules path (`TnwLandBattleMethods`) shared by exact
stats, Monte Carlo and "Roll 1 Battle".

**Exact or Monte Carlo.** Exact stats are computed when they fit a budget of 1M outcome
evaluations. Measured in the browser, that's about 1.3 s at worst, around 11 v 11 with
default settings. Bigger battles use a 65,536-trial Monte Carlo estimate, which takes
about 1.2 s and is within about 0.4 percentage points. The result header says which
one you got.

**Rules settled from the text (no assumption needed):**
- There is no Command Rating cap in battle, unlike Siege. Every piece in the Duchy
  fights (9.7), and leaders other than the Commander count as Units (11.2).
- The victor is decided on casualties *as rolled*, uncapped (11.32). This matches the
  rulebook's naval example (13.4), the only two-Round example in the book. A side wiped
  out by kills loses whatever the totals.
- A tie after Round 1 means Round 2. A tie after Round 2 means the attacker (the Active
  Formation) retreats.
- Terrain helps the defender in Round 1 only, and is lost entirely if any defender
  evasion failed (11.22).
- Defending Armies not formed into an Army Group fight with Commander rating 0, but the
  printed rating still counts for the rout Resource roll (9.7). This is a checkbox.
- Rout (margin of 3 or more) eliminates the loser's disrupted pieces, and gives a
  Resource chance of the routed Commander's Battle Rating out of 6 (11.5). A defender
  that can't retreat is eliminated (11.42). Flag Overrun happens when the kills rolled
  exceed the loser's pieces, unless both sides are eliminated (11.7).

**Assumptions - please review, as with Siege:**
1. **Kill allocation.** Kills fall on already-disrupted Units first, then undisrupted
   Units, and on the Commander only when no Unit is left. The owner chooses (11.3); this
   is the owner-optimal choice, the same philosophy as Siege. It only matters in Round 2,
   and only for how many Units a rout then eliminates.
2. **Excess disrupts (11.33).** The first disrupt beyond the Units disrupts the
   Commander, whose whole rating is then lost for Round 2. Each further excess disrupt
   cancels **one** nationality bonus die. The rule says they "detract from" the bonuses
   "at the player's discretion"; per-die is my reading.
3. **Event dice apply to Round 1 only.** Events affect one Round (11.1), and events
   played before Round 2 aren't modeled. The same input is not applied twice.
4. **One terrain type.** If Formations entered across different terrain (say Rough and
   Pass from two directions), the calculator takes the single terrain you pick. The rule
   lists each case separately without saying whether they stack.
5. **A losing attacker can always retreat** to a Duchy it came from, without Attrition.
   11.41 Attrition retreats and attacker no-retreat eliminations are out of scope.
6. **Nationality composition is fixed** for the battle, even if losses would change the
   French/Minor majority.
7. **Flag Overrun** reads "eliminations" as kills rolled against the loser, not counting
   rout eliminations. The Fortress exception is a tooltip, not an input.

Nav now has 9 items. The grouping caveat from §0.1 still applies.

## 10. Naval Battle (stage 3) - implemented 2026-09-28

The Naval Battle calculator is at `/tnwnaval`. It has the same structure as Land Battle:
one pure rules path, exact stats within a work budget, and Monte Carlo above it. The
page opens with the rulebook's own example (four British Squadrons against two French
and two Spanish that failed to evade), and shows each Fleet's Round-1 dice live. The
tester should see 13 and 6.

**Rules settled from the text (no assumption needed):**
- **Dice per Squadron:** British 3; French, Danish or Swedish 2; Russian, Turkish or
  Spanish 1. A Squadron under Refit rolls one less, never below 0. There are no events
  and no nationality bonus in naval combat (13.4).
- **Hits:** each "6" sinks a Squadron, and each "5" costs the Fleet one die in later
  Rounds, however often a Squadron is hit. There is no rout.
- **Sides are Active and Inactive, not attacker and defender.** A tie after two Rounds
  goes against the Active Fleet. The +1 die for a failed evasion goes to whichever Fleet
  the enemy failed to evade, in Round 1 only. Fleets in Port can't evade, so that option
  is disabled for Port battles.
- **Port battles (13.5):**
  - The shore batteries (2 dice, 4 for a Fortress) fire before combat. Their "5"s already
    cut the Active Fleet's Round 1 dice, and their hits count in Round 1's total.
  - They fire again with the defending Fleet in each Round.
  - A second Round needs both Fleets to still exist, so an empty Port that ties means the
    Active Fleet loses.
  - If the Port loses, its whole Fleet is eliminated.

**Assumptions - please review:**
1. **Which Squadron sinks.** It's the owner's choice, split as evenly as possible across
   nations. Among the nations that have lost the fewest so far, the owner sinks the
   Squadron rolling the fewest dice, Refit ones first. This reproduces the rulebook
   example: the first loss is Spanish rather than French, the second is French.
2. **Shore batteries are not a Fleet,** so "5"s never reduce their dice.
3. **An empty Port is not "eliminated".** It never had a Fleet, so the "surviving Fleet
   always wins" clause doesn't apply to it. The Port battle is still fought and decided
   on hits.
4. **Out of scope:**
   - Fortified straits (13.8), since the rule doesn't give the shore dice for a strait.
   - Squadrons under Build in a Port that loses (they aren't counted as losses).
   - The 2d6 interception and evasion rolls themselves: "failed to evade" is an input, as
     on the land page.
   - Where a defeated Fleet retreats (13.42).

**Performance, measured in the browser.** The first version took up to 13 s for exact
stats and 10 s for Monte Carlo. Two changes brought that to about 1.1 s and 1.4 s at
worst:
- Precomputing the order Squadrons sink in, once per Fleet.
- Counting Squadrons with bit arithmetic instead of a loop.

The exact/Monte Carlo boundary stays at 1M evaluations, as for Land Battle.

Nav now has 10 items. A single TNW landing page, or a grouped menu, is a reasonable next
tidy-up.

## 11. Amphibious Assault (rule 13.7) - implemented 2026-10-03

Added to the Land Battle calculator at `/tnwbattle` as an "Amphibious landing" selector
(Not a landing / Enemy Port / Enemy Fortress-Port), rather than a fourth page - it is the same
Land Battle (rule 11) with one extra pre-battle step, not a distinct mechanic like Siege or Naval.

**The rule, in calculator terms (13.7, via 13.5):** before Round 1, the Port's shore batteries
fire once at the landing attacker - 2 dice for a Port, 4 for a Fortress-Port. Casualties from that
fire count toward Round 1's casualty total and reduce the attacker's Round 1 dice, exactly like
the pre-battle shore fire already built for naval Port battles (`TnwNavalBattleMethods.
ShoreBatteryFire`) - the same `ApplyHits` is reused unchanged. The one place the naval pattern does
**not** transfer: 13.5's shore batteries fire again every Round alongside the defending Fleet, but
13.7 says the resulting land battle proceeds "without further shore battery fire" - so the
batteries fire exactly once, and `DiceForRound` is untouched (a regression test pins this: the
defender's dice are identical with or without a landing).

**Exact stats.** `TnwLandBattleExactStats.TryCalculate` now enumerates the shore fire's own
(sixes, fives) distribution as an extra weighted dimension ahead of Round 1 - each shore outcome
gives the attacker a different (always smaller or equal) starting dice count for Round 1, so the
work bound is summed across shore outcomes before the per-outcome loop runs, the same safety check
as the existing Round-1-to-Round-2 bound. An attacker wiped out by shore fire alone never reaches
Round 1 (`Rounds == 0`), mirroring the naval `Port_ActiveSunkByShoreFire_LosesWithoutARound` case.

**Assumptions - please review, as with the other two extensions:**
1. **Terrain and a landing are mutually exclusive.** 11.22's rough/pass/marsh bonus is about the
   line the attacker crossed on land; a sea landing crosses none of them. The page disables and
   clears the Terrain selector once a landing is chosen, and `TnwLandBattleInputRules` rejects the
   combination if set programmatically.
2. **Shore-fire kills count toward Flag Overrun (11.7).** `KillsReceived` accumulates shore-fire
   kills the same way it accumulates Round 1/2 kills, so a landing that is mostly wiped out by
   shore fire can still trigger Flag Overrun on the defender's side. Not stated either way by the
   rule; reads naturally as "kills rolled against the loser" without a reason to exclude this
   source.
3. **13.71 (no Amphibious Assault against a Port containing an enemy Fleet) and 13.6's Attrition
   on a forced retreat remain out of scope**, for the same reason Attrition is already out of scope
   for ordinary Land Battle retreats (§9, assumption 5): naval control and post-battle movement are
   full-game bookkeeping, not inputs to a single-battle calculator.
