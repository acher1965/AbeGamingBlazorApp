namespace AbeGaming.GameLogic.PoG
{
    /// <summary>
    /// Detailed (unit-level) combat resolution. Same procedure as the factor mode (12.2), but
    /// Combat Strength and Fire Table come from the units, losses are taken step by step
    /// (12.4), and the aftermath uses the unit-level conditions: a retreat needs a full strength
    /// attacking unit left (12.5.1), cancelling a retreat needs a defending step left after the
    /// extra loss (12.5.3), and only full strength units advance (12.7.1).
    /// </summary>
    public static partial class PoGCRT
    {
        private const int DieFaces = 6;

        private static PoGBattleResult DetailedOutcome(
            PoGBattle battle,
            PoGDetailedForces detailed,
            int attackerDieRoll,
            int defenderDieRoll,
            int? flankAttackDieRoll)
        {
            PoGSideForce attacker = PoGSideForce.FromUnits(detailed.Attackers);
            PoGSideForce defender = PoGSideForce.FromUnits(detailed.Defenders, battle.FortressLevel.CombatFactors());

            // 15.1.6: a fort with no friendly units in its space gets no benefit from a trench.
            int trench = battle.IsUnoccupiedFort() ? 0 : PoGBattleInputRules.ClampTrench(battle.Trench);
            int attackerShift = OffensiveColumnShift(battle.Terrain, trench);
            int defenderShift = DefensiveColumnShift(trench);
            int attackerDrm = battle.Attacker.DRM + (battle.IsAllAttackersInSinai ? -3 : 0);
            int defenderDrm = battle.Defender.DRM;

            int attackerModifiedDieRoll = PoGBattleInputRules.ClampModifiedDieRoll(PoGBattleInputRules.ClampDieRoll(attackerDieRoll) + attackerDrm);
            int defenderModifiedDieRoll = PoGBattleInputRules.ClampModifiedDieRoll(PoGBattleInputRules.ClampDieRoll(defenderDieRoll) + defenderDrm);

            int attackerColumn = ForceColumn(attacker, attackerShift);
            int defenderColumn = ForceColumn(defender, defenderShift);

            bool flankAttempted = battle.AttemptFlankAttack;
            int? flankRoll = null;
            int? flankModifiedRoll = null;
            bool flankSucceeded = false;
            if (flankAttempted)
            {
                if (flankAttackDieRoll is null)
                    throw new ArgumentException("Flank attack die roll is required when AttemptFlankAttack is true.");
                flankRoll = PoGBattleInputRules.ClampDieRoll(flankAttackDieRoll.Value);
                flankModifiedRoll = PoGBattleInputRules.ClampModifiedDieRoll(flankRoll.Value + PoGBattleInputRules.ClampFlankAttackDrm(battle.FlankAttackDrm));
                flankSucceeded = flankModifiedRoll.Value >= 4;
            }

            int hitsByAttacker;
            int hitsByDefender;
            PoGSideForce attackerAfter;
            PoGSideForce defenderAfter;
            FireTable defenderFireTable = defender.FireTable;

            if (!flankAttempted)
            {
                // Simultaneous fire; losses do not change the Loss Numbers already achieved (12.2.10).
                hitsByAttacker = ForceHits(attacker, attackerShift, attackerModifiedDieRoll);
                hitsByDefender = ForceHits(defender, defenderShift, defenderModifiedDieRoll);
                defenderAfter = defender.TakeLosses(hitsByAttacker);
                attackerAfter = attacker.TakeLosses(hitsByDefender);
            }
            else if (flankSucceeded)
            {
                // The attacker fires first; the defender fires back with what survives (12.3.3).
                hitsByAttacker = ForceHits(attacker, attackerShift, attackerModifiedDieRoll);
                defenderAfter = defender.TakeLosses(hitsByAttacker, f => ExpectedHits(f, defenderShift, defenderDrm));
                defenderColumn = ForceColumn(defenderAfter, defenderShift);
                defenderFireTable = defenderAfter.FireTable;
                hitsByDefender = ForceHits(defenderAfter, defenderShift, defenderModifiedDieRoll);
                attackerAfter = attacker.TakeLosses(hitsByDefender);
            }
            else
            {
                // A failed flank attack: the defender fires first.
                hitsByDefender = ForceHits(defender, defenderShift, defenderModifiedDieRoll);
                attackerAfter = attacker.TakeLosses(hitsByDefender, f => ExpectedHits(f, attackerShift, attackerDrm));
                attackerColumn = ForceColumn(attackerAfter, attackerShift);
                hitsByAttacker = ForceHits(attackerAfter, attackerShift, attackerModifiedDieRoll);
                defenderAfter = defender.TakeLosses(hitsByAttacker);
            }

            Winner winner = hitsByAttacker > hitsByDefender
                ? Winner.Attacker
                : hitsByAttacker < hitsByDefender ? Winner.Defender : Winner.Draw;

            bool attackerHasFullStrengthUnit = attackerAfter.HasFullStrengthUnit;
            bool defenderHadUnits = defender.AnyUnitAlive;
            bool defenderEliminated = defenderHadUnits && !defenderAfter.AnyUnitAlive;
            bool fortDestroyed = defender.FortIntact && !defenderAfter.FortIntact;

            // 12.5.1: a retreat needs the attacker to win with a full strength unit left, and defenders left to retreat.
            int retreat = winner == Winner.Attacker && attackerHasFullStrengthUnit && defenderAfter.AnyUnitAlive
                ? (hitsByAttacker - hitsByDefender >= 2 ? 2 : 1)
                : 0;

            // 12.2.13, 12.7: full strength units advance if the defenders retreated or were all
            // eliminated; into the defender's space only if eliminated (12.7.2). Against a fort with
            // no units the attacker cannot advance unless the fort is destroyed (12.7.1).
            int advance;
            if (!attackerHasFullStrengthUnit)
                advance = 0;
            else if (retreat > 0)
                advance = Math.Min(retreat, battle.Terrain == Terrain.Clear ? 2 : 1);
            else if (defenderEliminated || (!defenderHadUnits && fortDestroyed))
                advance = 1;
            else
                advance = 0;

            // 12.5.3: cancelling the retreat costs one more step and needs a defending step left afterwards.
            bool defenderCanIgnoreRetreat = retreat > 0
                && CanIgnoreRetreat(battle.Terrain, trench)
                && defenderAfter.StepsRemaining >= 2;

            return new PoGBattleResult(
                winner,
                hitsByAttacker,
                hitsByDefender,
                retreat,
                PoGBattleInputRules.ClampDieRoll(attackerDieRoll),
                PoGBattleInputRules.ClampDieRoll(defenderDieRoll),
                attackerModifiedDieRoll,
                defenderModifiedDieRoll,
                advance,
                defenderCanIgnoreRetreat,
                attackerColumn,
                defenderColumn,
                flankAttempted,
                flankSucceeded,
                flankRoll,
                flankModifiedRoll,
                DefenderFireTable: defenderFireTable,
                AttackerStepsLost: attacker.StepsRemaining - attackerAfter.StepsRemaining,
                DefenderStepsLost: defender.StepsRemaining - defenderAfter.StepsRemaining,
                DefenderEliminated: defenderEliminated,
                FortDestroyed: fortDestroyed,
                AttackerHasFullStrengthUnit: attackerHasFullStrengthUnit);
        }

        private static int ForceColumn(PoGSideForce force, int shift) =>
            FireColumnIndex(force.FireTable, PoGBattleInputRules.ClampFactors(force.CombatFactors), shift);

        /// <summary>The Loss Number a force inflicts; nothing if nothing is left to fire.</summary>
        private static int ForceHits(PoGSideForce force, int shift, int modifiedDieRoll) =>
            force.CanFire ? HitsFromColumn(force.FireTable, ForceColumn(force, shift), modifiedDieRoll) : 0;

        /// <summary>The average Loss Number a force would inflict over one die roll with its DRM.</summary>
        private static double ExpectedHits(PoGSideForce force, int shift, int drm)
        {
            double total = 0;
            for (int roll = 1; roll <= DieFaces; roll++)
                total += ForceHits(force, shift, PoGBattleInputRules.ClampModifiedDieRoll(roll + drm));
            return total / DieFaces;
        }
    }
}
