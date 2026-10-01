# PoG rules review (2026-10-01)

I checked the Paths of Glory calculator against the files in `RulesAndTables/`: the
Deluxe Living Rules (`PoG-DeluxeRules2022Final.pdf` and its text version `.txt`) and
`PoGCRTs.png`, which holds the fire tables and the Terrain Effects Chart. There is only
one PoG PDF; the second file is the image. No code was changed.

## Summary

**The core of the calculator is correct.**

- **Fire tables: all 114 cells verified** (Corps/Fort table 9 x 6, Army table 10 x 6).
  Windows' built-in OCR only recognised the 2s and 3s in the image, so I cut each cell
  out and classified it by shape:
  - The dashes (0) and thin strokes (1) match the code cell for cell.
  - The wider digits fall into groups that each contain exactly one of the code's values.
  - The groups are tied to real digits by the cells the rulebook's own examples quote.
  - Every 2 and 3 the OCR did read matches as well.
- **Column labels:** Corps 0-8+; Army 1, 2, 3, 4, 5, 6-8, 9-11, 12-14, 15, 16+.
- **Terrain Effects Chart:** every row matches the code.

  | Terrain | Combat effect | Cancel retreat? | Advance must stop? | Flank allowed? |
  |---|---|---|---|---|
  | Clear | - | No | No | Yes |
  | Mountain, Swamp | Offensive fire 1L | Yes | Yes | No |
  | Forest | - | Yes | Yes | Yes |
  | Desert | No summer combat | Yes | Yes | Yes |
  | Trench 1 | Offensive 1L, defensive 1R | Yes | No | No |
  | Trench 2 | Offensive 2L, defensive 1R | Yes | No | No |

- **Combat procedure (12.2):**
  - A fort's CF is added to the defender's strength.
  - The Army table is used if any Army fires, otherwise the Corps/Fort table.
  - Column shifts stop at the table edges.
  - Modified die rolls are kept within 1-6.
  - All attackers in the Sinai get -3.
  - The higher Loss Number wins; equal Loss Numbers mean both lose.
  - The retreat is 1 space for a difference of 1, otherwise 2 (12.5.2).
- **Flank attacks (12.3):** the conditions (an Army attacking; not into Swamp, Mountain,
  trenches or an unoccupied fort) and success on a modified 4 or more.
- **Worked examples:** the rulebook's examples all reproduce:
  - Tannenberg: a flank attack; 7 factors with a roll of 3 gives 4, the reply gives 1,
    and the retreat is 2.
  - Cambrai: a Level 2 trench; the defender moves to the 12-14 column and a modified 6
    gives 7; the attacker's 13 factors move to 6-8 and a 4 gives 4.
  - The sample game: 15 column, roll 2 gives 5; 3 column, roll 3 gives 2; 6-8 column,
    roll 3 gives 4; 9-11 column, roll 1 gives 3; Corps 1, roll 4 gives 1; Mulhouse's
    two left shifts; Nancy's 16 factors shifted to the 15 column.

**Two bugs found, both about forts.** There are also some approximations that come from
the calculator working with combat factors rather than individual units (see Notes).

## Findings

### 1. An unoccupied fort still gets the trench column shifts (medium)

Rule 15.1.6: "Forts without friendly units in their space may never be the target of a
Flank Attack **or benefit from any trench in their space**."

The flank-attack half is implemented, but the code always applies the trench shifts
(attacker 1L or 2L, defender 1R) whatever the defender's own factors. When the defender
has 0 factors plus a fort, the attacker fires too low and the fort too high.

**Fix:** if the defender has no combat factors of its own (fort only), treat the trench
level as 0 for both column shifts.

### 2. "Besieged" in the Fortress list is worth 1 factor, with no rule behind it (low to medium)

The Fortress list offers None, LevelOne, LevelTwo, LevelThree, Besieged and Destroyed,
and the code counts Besieged as 1 factor. The rules never reduce a fort's CF for being
besieged. A besieged fort keeps its printed CF (Verdun 3, for example); being besieged
only affects who may attack it (15.1.3), supply (15.2.2) and the surrender roll (15.3).
So choosing "Besieged" for Verdun understates the defender by 2 factors. "Destroyed" is
the same as "None", which is harmless.

**Fix:** remove "Besieged" (and possibly "Destroyed") and let the user pick the fort's
printed CF. If the besieged status matters for the user's own bookkeeping, a tooltip can
say that besieging doesn't change the CF.

## Notes - approximations and things not modelled

These follow from the calculator working with combat factors rather than individual
units and steps. They aren't bugs, but they are worth knowing, and some may deserve a
tooltip.

- **Return fire after a flank attack** (12.3.3). The side firing second fires with its
  strength after losses. The code subtracts the Loss Number from the factors, but real
  CF losses depend on which units take which steps, and a reduced Army replaced by a
  Corps fires on the Corps table. In the Tannenberg example the code fires the Russians
  from 0 factors on the Army table while the rules fire 1 factor on the Corps table; both
  happen to give 1. This can't be exact without a list of units.
- **A retreat needs a full-strength attacker left** (12.5.1), and only full-strength units
  advance (12.7.1). The calculator reports a retreat whenever the attacker wins.
- **Cancelling a retreat** (12.5.3) needs at least one defending *step* left after the
  extra loss. The code checks that at least 2 *factors* remain.
- **Fort-only defenders** (12.7.1 exception): an attacker that beats a fort with no units
  can advance only if the fort is destroyed. The "advance" figure doesn't know that.
- **Desert** (TEC, 15.2.5): no combat into or out of a Desert space in summer. The page
  has no season input, so it doesn't warn.
- **Die modifiers** are entered as 0-9 per side. If any combat card ever gives a
  *negative* modifier to the user's own side, it can't be entered (only the Sinai -3 is
  built in). I didn't find one in the rules text, but the cards are not in these files.

## Proposed next step (your call)

Fix findings 1 and 2. That means rule-based tests for 15.1.6 and for the fort CF, plus
permanent tests from this review: the Tannenberg and Cambrai examples, the sample-game
cells and the Terrain Effects Chart rows, in a `PoGRulebookCasesTests.cs` like the FtP
one. Optionally add tooltips for the approximations above. Then a 1.3.2 release. I
haven't changed any code or committed anything.

## Outcome (2026-10-01, v1.3.2)

You approved fixing both findings:
1. **15.1.6:** an unoccupied fort no longer gets trench column shifts, for either side.
2. **Fortress list:** "Besieged" is removed. The list now reads None, Fort CF 1, Fort CF 2,
   Fort CF 3 and Destroyed, and the tooltip says a besieged fort keeps its CF. The CF lookup,
   which had been duplicated in two files, is now one shared helper.

`PoGRulebookCasesTests.cs` (29 tests) adds the rulebook's combat examples, the sample-game
combats, every Terrain Effects Chart row, and tests for both fixes. No existing expectation
changed.

The approximations in the Notes section are still there. You asked whether they can be
improved; the answer, with options, comes with this release.

## Detailed units mode (2026-10-01, v1.4.0)

You asked whether the approximations could be improved, especially the first one. They
now can be: there is an optional **Detailed units** mode, using the unit values you
supplied.

**How it works:**
- **Units:** each side lists its units: unit type, full or reduced, and, for Armies,
  whether a replacement Corps is in the Reserve Box. The values come from
  `AbeGaming.GameLogic/PoG/PoGUnitTypes.json`, an embedded configuration file, and a test
  pins every row against your list.
- **Strength:** Combat Strength and Fire Table come from the units, plus the fort for the
  defender.
- **Losses** are taken step by step (12.4.3): exactly the Loss Number if possible,
  otherwise as much as possible, never more. A reduced Army that is eliminated is
  replaced by a Corps from the Reserve Box (12.4.4); the BEF Army is replaced only by the
  BEF Corps, and the MEF and NE Armies by a BR Corps. The fort takes losses only after
  every unit is gone (12.4.6).
- **The three approximations are now exact:**
  - after a flank attack, the side firing second fires with its real survivors,
    possibly on the Corps table;
  - a retreat needs a full-strength attacker left (12.5.1);
  - cancelling a retreat needs a defending step left after the extra loss (12.5.3).
  Only full-strength units advance, and against a fort with no units the attacker
  advances only if the fort is destroyed.
- **Rulebook check:** the rulebook's examples now reproduce exactly, including the parts
  factor mode got wrong. At Tannenberg the replacement Corps fires on the Corps table and
  the Russians can cancel the retreat. At Tarnopol the Russians take no loss because
  their smallest LF is 2. At Sedan, Nancy and Cambrai the losses are allocated exactly
  as the sample game describes.
- **Factor mode is unchanged.** All its tests pass untouched, and the flank tooltip now
  points out its approximation.

**Assumptions (please review):**
1. **Which steps to lose, when several choices fulfil the same Loss Number.** The owner is
   assumed to choose, in order:
   1. the best return fire, if the side still has to fire in this combat;
   2. keeping a full-strength unit;
   3. keeping the most steps;
   4. keeping the most CF.

   This reproduces every allocation in the sample game.
2. **A fort's LF equals its printed CF.** Fort LF values aren't in the rules files.
3. **The "Res." checkbox means a full-strength replacement Corps is available.** A
   reduced Corps in the Reserve Box isn't modelled. If two Armies of the same nation
   share one Corps in the Reserve Box, ticking it on both counts that Corps twice.

**Not modelled:**
- British, BEF/MEF and CAU loss priority (12.4.5).
- Forced permanent elimination when the full Loss Number can't be met (12.4.4.2).
- The MN Corps never advancing, and being eliminated if forced to retreat.
- AUS and CND Corps: they aren't in your list, and the rulebook's Cambrai example shows
  the Canadian Corps at 2-1-4 reduced, which isn't a BR Corps. Add their values if you
  want them.
