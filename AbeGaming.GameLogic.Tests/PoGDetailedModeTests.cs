using AbeGaming.GameLogic.PoG;

namespace AbeGaming.GameLogic.Tests;

/// <summary>
/// Detailed (unit-level) PoG mode: the unit catalog, step-loss allocation (12.4) and the
/// unit-level aftermath, checked against the rulebook's examples and sample game.
/// </summary>
public class PoGDetailedModeTests
{
    private static PoGUnit Unit(string id, bool reduced = false, bool reserve = true) => new(id, reduced, reserve);

    private static PoGBattle Detailed(
        PoGUnit[] attackers,
        PoGUnit[] defenders,
        Terrain terrain = Terrain.Clear,
        FortressLevel fort = FortressLevel.None,
        int trench = 0,
        int attackerDrm = 0,
        int defenderDrm = 0,
        bool flank = false,
        int flankDrm = 0) =>
        new(new BattleSideInfo(FireTable.Corps, 0, attackerDrm),
            new BattleSideInfo(FireTable.Corps, 0, defenderDrm),
            terrain, fort, trench, flank, flankDrm,
            Detailed: new PoGDetailedForces(attackers, defenders));

    // ---- The unit catalog: every row as supplied by the project owner (2026-10-01) ----

    [Theory]
    [InlineData("FR_ARMY", 3, 3, 2, 3, "FR_CORPS")]
    [InlineData("FR_AOO", 3, 3, 2, 3, "FR_CORPS")]
    [InlineData("FR_CORPS", 1, 1, 1, 1, null)]
    [InlineData("GE_ARMY", 5, 3, 3, 3, "GE_CORPS")]
    [InlineData("GE_CORPS", 2, 1, 1, 1, null)]
    [InlineData("RU_ARMY", 3, 2, 2, 2, "RU_CORPS")]
    [InlineData("RU_CAU", 3, 2, 2, 2, "RU_CORPS")]
    [InlineData("RU_CORPS", 1, 1, 0, 1, null)]
    [InlineData("AH_ARMY", 3, 2, 1, 2, "AH_CORPS")]
    [InlineData("AH_CORPS", 1, 1, 0, 1, null)]
    [InlineData("IT_ARMY", 2, 2, 1, 2, "IT_CORPS")]
    [InlineData("IT_CORPS", 1, 1, 0, 1, null)]
    [InlineData("BE_ARMY", 2, 3, 1, 3, "BE_CORPS")]
    [InlineData("BE_CORPS", 1, 1, 0, 1, null)]
    [InlineData("SB_ARMY", 2, 2, 1, 2, "SB_CORPS")]
    [InlineData("SB_CORPS", 1, 1, 0, 1, null)]
    [InlineData("TU_ARMY", 1, 2, 1, 2, "TU_CORPS")]
    [InlineData("TU_CORPS", 1, 1, 0, 1, null)]
    [InlineData("BR_ARMY", 4, 3, 3, 3, "BR_CORPS")]
    [InlineData("BR_MEF", 1, 2, 1, 2, "BR_CORPS")]
    [InlineData("BR_NE", 4, 3, 3, 3, "BR_CORPS")]
    [InlineData("BR_BEF_ARMY", 5, 3, 4, 3, "BR_BEF_CORPS")]
    [InlineData("BR_CORPS", 2, 1, 1, 1, null)]
    [InlineData("BR_BEF_CORPS", 2, 2, 2, 1, null)]
    [InlineData("US_ARMY", 5, 3, 3, 3, "US_CORPS")]
    [InlineData("US_CORPS", 2, 1, 1, 1, null)]
    [InlineData("RO_CORPS", 1, 1, 0, 1, null)]
    [InlineData("BU_CORPS", 2, 1, 0, 1, null)]
    [InlineData("MN_CORPS", 1, 1, 0, 1, null)]
    [InlineData("SN_CORPS", 1, 1, 0, 1, null)]
    [InlineData("ANA_CORPS", 1, 1, 0, 1, null)]
    [InlineData("PT_CORPS", 1, 1, 0, 1, null)]
    public void Catalog_MatchesTheSuppliedUnitList(string id, int fullCf, int fullLf, int reducedCf, int reducedLf, string? replacement)
    {
        PoGUnitType type = PoGUnitCatalog.Get(id);

        Assert.Equal((fullCf, fullLf, reducedCf, reducedLf, replacement),
            (type.FullCf, type.FullLf, type.ReducedCf, type.ReducedLf, type.ReplacementCorps));
        Assert.Equal(replacement is null ? PoGUnitKind.Corps : PoGUnitKind.Army, type.Kind);
    }

    [Fact]
    public void Catalog_HasExactlyTheSuppliedUnitTypes()
    {
        Assert.Equal(32, PoGUnitCatalog.All.Count);
    }

    // ---- Step-loss allocation (12.4.3, 12.4.4), from the sample game ----

    [Fact]
    public void Cambrai_GermansTakeFour_ReduceTheArmyAndACorps()
    {
        // "The German player reduces the 2nd Army and a Corps" (CF 9 -> 6), not two Corps eliminated (CF 5).
        PoGSideForce germans = PoGSideForce.FromUnits([Unit("GE_ARMY"), Unit("GE_CORPS"), Unit("GE_CORPS")]);

        PoGSideForce after = germans.TakeLosses(4);

        Assert.Equal(6, after.CombatFactors);
        Assert.Equal(PoGUnitStage.Reduced, after.Units[0].Stage);
        Assert.Equal(1, after.Units.Count(u => u.Stage == PoGUnitStage.Reduced && u.Unit.TypeId == "GE_CORPS"));
    }

    [Fact]
    public void Sedan_FrenchArmyTakesFive_OnlyOneStep_NeverMoreThanTheLossNumber()
    {
        // "the FR 5th Army loses one step for three points ... They do not lose a second step".
        PoGSideForce french = PoGSideForce.FromUnits([Unit("FR_ARMY")]);

        PoGSideForce after = french.TakeLosses(5);

        Assert.Equal(PoGUnitStage.Reduced, after.Units[0].Stage);
    }

    [Fact]
    public void Nancy_GermansTakeFive_ReduceAnArmyAndEliminateTheReducedCorps()
    {
        PoGSideForce germans = PoGSideForce.FromUnits([Unit("GE_ARMY"), Unit("GE_ARMY"), Unit("GE_ARMY"), Unit("GE_CORPS", reduced: true)]);

        PoGSideForce after = germans.TakeLosses(5);

        Assert.Equal(1, after.Units.Count(u => u.Stage == PoGUnitStage.Reduced));
        Assert.Equal(PoGUnitStage.Eliminated, after.Units[3].Stage);
    }

    [Fact]
    public void Nancy_FrenchArmyTakesSeven_GoesToAReducedCorps()
    {
        // 3 (reduce) + 3 (eliminate, replaced by a Corps) + 1 (reduce the Corps) = 7; the fort survives.
        PoGSideForce french = PoGSideForce.FromUnits([Unit("FR_ARMY")], fortCf: 2);

        PoGSideForce after = french.TakeLosses(7);

        Assert.Equal(PoGUnitStage.ReplacementReduced, after.Units[0].Stage);
        Assert.True(after.FortIntact);
    }

    [Fact]
    public void NoReserveCorps_ReducedArmyIsSimplyEliminated()
    {
        PoGSideForce russians = PoGSideForce.FromUnits([Unit("RU_ARMY", reserve: false)]);

        PoGSideForce after = russians.TakeLosses(4);

        Assert.Equal(PoGUnitStage.Eliminated, after.Units[0].Stage);
        Assert.False(after.AnyUnitAlive);
    }

    [Fact]
    public void Fort_TakesLossesOnlyAfterAllUnitsAreGone()
    {
        // RU Corps (1+1) then the CF 2 fort: a Loss Number of 4 destroys both; 3 leaves the fort.
        PoGSideForce four = PoGSideForce.FromUnits([Unit("RU_CORPS")], fortCf: 2).TakeLosses(4);
        PoGSideForce three = PoGSideForce.FromUnits([Unit("RU_CORPS")], fortCf: 2).TakeLosses(3);

        Assert.False(four.FortIntact);
        Assert.True(three.FortIntact);
        Assert.False(three.AnyUnitAlive);
    }

    // ---- Full combats: these were approximations in factor mode ----

    [Fact]
    public void Tannenberg_Detailed_ReplacementCorpsFiresOnTheCorpsTable_AndCanCancelTheRetreat()
    {
        PoGBattle battle = Detailed([Unit("GE_ARMY"), Unit("GE_CORPS")], [Unit("RU_ARMY")], Terrain.Forest, flank: true, flankDrm: 1);

        PoGBattleResult result = battle.Outcome(attackerDieRoll: 3, defenderDieRoll: 4, flankAttackDieRoll: 3);

        Assert.True(result.FlankAttackSucceeded);
        Assert.Equal(4, result.HitsByAttacker);
        Assert.Equal(FireTable.Corps, result.DefenderFireTable);
        Assert.Equal(1, result.HitsByDefender);
        Assert.Equal(2, result.DefenderRetreatLength);
        Assert.Equal(1, result.AdvanceMaxLength);
        // The rulebook: the Russian could have used the Forest's no-retreat option for another step.
        Assert.True(result.DefenderCanIgnoreRetreat);
        Assert.Equal(1, result.AttackerStepsLost); // the GE Corps is reduced; the 8th Army stays full
    }

    [Fact]
    public void Tannenberg_FactorMode_StillApproximatesTheRetreatCancel()
    {
        // Documents the factor-mode approximation the detailed mode removes.
        PoGBattle battle = new(new BattleSideInfo(FireTable.Army, 7, 0), new BattleSideInfo(FireTable.Army, 3, 0),
            Terrain.Forest, FortressLevel.None, 0, AttemptFlankAttack: true, FlankAttackDrm: 1);

        Assert.False(battle.Outcome(3, 4, 3).DefenderCanIgnoreRetreat);
    }

    [Fact]
    public void Tarnopol_Detailed_AustrianArmyReplaced_RussiansTakeNoLoss()
    {
        // RU 3rd and 8th Armies (6) flank-attack the AH 3rd Army: roll 3 on the 6 column, 4.
        // The AH Corps fires 1 CF on the Corps table, roll 4: 1 - the smallest RU LF is 2, so no loss.
        PoGBattle battle = Detailed([Unit("RU_ARMY"), Unit("RU_ARMY")], [Unit("AH_ARMY")], flank: true, flankDrm: 1);

        PoGBattleResult result = battle.Outcome(attackerDieRoll: 3, defenderDieRoll: 4, flankAttackDieRoll: 4);

        Assert.Equal(4, result.HitsByAttacker);
        Assert.Equal(1, result.HitsByDefender);
        Assert.Equal(0, result.AttackerStepsLost);
        Assert.Equal(2, result.DefenderRetreatLength);
    }

    [Fact]
    public void Sedan_Detailed_GermanLfThreeAbsorbsNothing()
    {
        PoGBattle battle = Detailed([Unit("GE_ARMY"), Unit("GE_ARMY"), Unit("GE_ARMY")], [Unit("FR_ARMY")], Terrain.Forest);

        PoGBattleResult result = battle.Outcome(attackerDieRoll: 2, defenderDieRoll: 3);

        Assert.Equal(5, result.HitsByAttacker);
        Assert.Equal(2, result.HitsByDefender);
        Assert.Equal(0, result.AttackerStepsLost); // "since the lowest German LF is 3, there is no effect"
        Assert.Equal(1, result.DefenderStepsLost);
        Assert.Equal(2, result.DefenderRetreatLength);
        Assert.Equal(1, result.AdvanceMaxLength);
    }

    [Fact]
    public void RetreatNeedsAFullStrengthAttacker()
    {
        // A reduced GE Army beats a RU Army 4-0 but has no full strength unit left: no retreat, no advance (12.5.1).
        PoGBattle battle = Detailed([Unit("GE_ARMY", reduced: true)], [Unit("RU_ARMY")]);

        PoGBattleResult result = battle.Outcome(attackerDieRoll: 6, defenderDieRoll: 1);

        Assert.Equal(Winner.Attacker, result.Winner);
        Assert.False(result.AttackerHasFullStrengthUnit);
        Assert.Equal(0, result.DefenderRetreatLength);
        Assert.Equal(0, result.AdvanceMaxLength);
    }

    [Fact]
    public void DefendersEliminated_NoRetreat_AdvanceIntoTheSpace()
    {
        PoGBattle battle = Detailed([Unit("GE_ARMY"), Unit("GE_ARMY")], [Unit("RU_CORPS")]);

        PoGBattleResult result = battle.Outcome(attackerDieRoll: 6, defenderDieRoll: 1);

        Assert.True(result.DefenderEliminated);
        Assert.Equal(0, result.DefenderRetreatLength);
        Assert.Equal(1, result.AdvanceMaxLength);
    }

    [Theory]
    [InlineData("GE_ARMY", 6, true, 1)]   // 5 CF on the Army table, roll 6: 5 >= the fort's LF 3, destroyed
    [InlineData("GE_CORPS", 1, false, 0)] // 2 CF on the Corps table, roll 1: 0, the fort holds
    public void FortOnlyDefender_AdvanceOnlyIfTheFortIsDestroyed(string attackerId, int roll, bool destroyed, int advance)
    {
        // 12.7.1 exception: against a fort with no units the attacker advances only if the fort is destroyed.
        PoGBattle battle = Detailed([Unit(attackerId)], [], fort: FortressLevel.LevelThree);

        PoGBattleResult result = battle.Outcome(attackerDieRoll: roll, defenderDieRoll: 1);

        Assert.Equal(destroyed, result.FortDestroyed);
        Assert.Equal(advance, result.AdvanceMaxLength);
        Assert.Equal(0, result.DefenderRetreatLength);
    }

    [Fact]
    public void UnoccupiedFort_InDetailedMode_GetsNoTrenchBenefit()
    {
        PoGBattle withTrench = Detailed([Unit("GE_ARMY")], [], fort: FortressLevel.LevelThree, trench: 2);
        PoGBattle noTrench = withTrench with { Trench = 0 };

        Assert.Equal(noTrench.Outcome(3, 3).AttackerFireColumnIndex, withTrench.Outcome(3, 3).AttackerFireColumnIndex);
    }

    // ---- Engine and plumbing ----

    [Fact]
    public void ExactStats_Detailed_ProbabilitiesSumToOne()
    {
        PoGBattle battle = Detailed([Unit("GE_ARMY"), Unit("GE_CORPS")], [Unit("RU_ARMY"), Unit("RU_CORPS")], Terrain.Forest, flank: true, flankDrm: 1);

        PoGStats stats = PoGExactStats.Calculate(battle);

        Assert.True(stats.IsDetailed);
        Assert.InRange(stats.AttackerWinProbability + stats.DefenderWinProbability + stats.DrawProbability, 0.999999, 1.000001);
        Assert.InRange(stats.MeanDefenderStepsLost, 0, 6);
    }

    [Fact]
    public void BattlesWithEqualUnitLists_AreEqual()
    {
        PoGBattle a = Detailed([Unit("GE_ARMY")], [Unit("RU_ARMY")]);
        PoGBattle b = Detailed([Unit("GE_ARMY")], [Unit("RU_ARMY")]);
        PoGBattle c = Detailed([Unit("GE_ARMY", reduced: true)], [Unit("RU_ARMY")]);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void Validation_DetailedModeLimits()
    {
        Assert.False(PoGBattleInputRules.IsBattleDefinitionConsistent(Detailed([], [Unit("RU_ARMY")]), out _));
        Assert.False(PoGBattleInputRules.IsBattleDefinitionConsistent(Detailed([Unit("GE_ARMY")], []), out _));
        Assert.True(PoGBattleInputRules.IsBattleDefinitionConsistent(Detailed([Unit("GE_ARMY")], [], fort: FortressLevel.LevelOne), out _));
        Assert.False(PoGBattleInputRules.IsBattleDefinitionConsistent(
            Detailed([Unit("GE_ARMY")], [Unit("RU_CORPS"), Unit("RU_CORPS"), Unit("RU_CORPS"), Unit("RU_CORPS")]), out _));
        // Flank attacks need an attacking Army (12.3.1).
        Assert.False(PoGBattleInputRules.IsBattleDefinitionConsistent(Detailed([Unit("GE_CORPS")], [Unit("RU_CORPS")], flank: true), out _));
    }
}
