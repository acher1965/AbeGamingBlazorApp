using AbeGaming.GameLogic.PoG;

namespace AbeGaming.GameLogic.Tests;

/// <summary>
/// Correctness tests taken from the Paths of Glory Deluxe Living Rules (August 2022) and the
/// fire tables / Terrain Effects Chart in RulesAndTables/PoGCRTs.png: the rulebook's combat
/// examples, the combats in its sample game, the TEC rows, and the rules fixed in the
/// 2026-10-01 review (POG-RULES-REVIEW-2026-10-01.md). Expected values come from the rules.
/// </summary>
public class PoGRulebookCasesTests
{
    private static PoGBattle Battle(
        FireTable attackerTable, int attackerFactors,
        FireTable defenderTable, int defenderFactors,
        Terrain terrain = Terrain.Clear,
        FortressLevel fort = FortressLevel.None,
        int trench = 0,
        int attackerDrm = 0,
        int defenderDrm = 0,
        bool flank = false,
        int flankDrm = 0) =>
        new(new BattleSideInfo(attackerTable, attackerFactors, attackerDrm),
            new BattleSideInfo(defenderTable, defenderFactors, defenderDrm),
            terrain, fort, trench, flank, flankDrm);

    // ---- Rulebook combat examples (12.5) ----

    [Fact]
    public void Example1_Tannenberg_FlankAttack()
    {
        // GE 8th Army (5) + 1 Corps (2) flank-attack the RU 2nd Army (3) in Tannenberg (Forest).
        // Flank roll 3 +1 = 4 succeeds; GE fire 7 on the Army table rolls 3: Loss Number 4.
        // The Russians fire back after losses and get 1. Difference 3: two-space retreat;
        // the advance stops in the Forest space.
        PoGBattle battle = Battle(FireTable.Army, 7, FireTable.Army, 3, Terrain.Forest, flank: true, flankDrm: 1);

        PoGBattleResult result = battle.Outcome(attackerDieRoll: 3, defenderDieRoll: 4, flankAttackDieRoll: 3);

        Assert.True(result.FlankAttackSucceeded);
        Assert.Equal(4, result.HitsByAttacker);
        Assert.Equal(1, result.HitsByDefender);
        Assert.Equal(Winner.Attacker, result.Winner);
        Assert.Equal(2, result.DefenderRetreatLength);
        Assert.Equal(1, result.AdvanceMaxLength);
    }

    [Fact]
    public void Example2_Cambrai_Trench2_DefenderWins()
    {
        // Allies 13 factors vs German 9 factors in a Level 2 trench; Germans play +1 DRM.
        // German fire moves to the 12-14 column and rolls 5+1: 7. Allied fire moves to 6-8 and rolls 4: 4.
        PoGBattle battle = Battle(FireTable.Army, 13, FireTable.Army, 9, trench: 2, defenderDrm: 1);

        PoGBattleResult result = battle.Outcome(attackerDieRoll: 4, defenderDieRoll: 5);

        Assert.Equal(4, result.HitsByAttacker);
        Assert.Equal(7, result.HitsByDefender);
        Assert.Equal(Winner.Defender, result.Winner);
        Assert.Equal(0, result.DefenderRetreatLength);
    }

    // ---- Sample game (August 1914) ----

    [Fact]
    public void SampleGame_Sedan_GermansWin_TwoSpaceRetreat_AdvanceStopsInForest()
    {
        // GE 1st, 2nd, 3rd Armies (15) vs FR 5th Army (3) in Sedan (Forest):
        // GE roll 2 on the 15 column: 5; FR roll 3 on the 3 column: 2.
        PoGBattleResult result = Battle(FireTable.Army, 15, FireTable.Army, 3, Terrain.Forest)
            .Outcome(attackerDieRoll: 2, defenderDieRoll: 3);

        Assert.Equal(5, result.HitsByAttacker);
        Assert.Equal(2, result.HitsByDefender);
        Assert.Equal(2, result.DefenderRetreatLength);
        Assert.Equal(1, result.AdvanceMaxLength);
    }

    [Fact]
    public void SampleGame_Mulhouse_TrenchAndMountainShiftTwoLeft()
    {
        // (GE 7th Army) 3 CF attacks a FR Corps and Fort in Belfort (Mountain, trench 1):
        // two columns left, to the 1 column, rolling 5 for 2. The French fire 3 CF one column right.
        PoGBattle battle = Battle(FireTable.Army, 3, FireTable.Corps, 1, Terrain.Mountain, FortressLevel.LevelTwo, trench: 1);

        PoGBattleResult result = battle.Outcome(attackerDieRoll: 5, defenderDieRoll: 1);

        Assert.Equal(0, result.AttackerFireColumnIndex); // Army "1" column
        Assert.Equal(2, result.HitsByAttacker);
        Assert.Equal(4, result.DefenderFireColumnIndex); // Corps "4" column (3 shifted one right)
    }

    [Fact]
    public void SampleGame_Nancy_SixteenShiftedToFifteen_FiveShiftedToSixToEight()
    {
        // GE 16 CF vs FR 2nd Army + Fort (5 CF) behind a trench; both roll 6: GE 7, FR 5.
        PoGBattle battle = Battle(FireTable.Army, 16, FireTable.Army, 3, fort: FortressLevel.LevelTwo, trench: 1);

        PoGBattleResult result = battle.Outcome(attackerDieRoll: 6, defenderDieRoll: 6);

        Assert.Equal(8, result.AttackerFireColumnIndex); // "15" column
        Assert.Equal(5, result.DefenderFireColumnIndex); // "6-8" column
        Assert.Equal(7, result.HitsByAttacker);
        Assert.Equal(5, result.HitsByDefender);
        Assert.Equal(2, result.DefenderRetreatLength);
    }

    [Theory]
    [InlineData(FireTable.Army, 9, 1, 3)]   // CP 11 CF -> 9-11 column, roll 1: 3
    [InlineData(FireTable.Army, 6, 3, 4)]   // RU 3rd + 8th Armies, 6 column, roll 3: 4
    [InlineData(FireTable.Corps, 1, 4, 1)]  // AH replacement corps, 1 CF, roll 4: 1
    public void SampleGame_FireTableCells(FireTable table, int factors, int roll, int expectedLossNumber)
    {
        PoGBattleResult result = Battle(table, factors, FireTable.Army, 3).Outcome(attackerDieRoll: roll, defenderDieRoll: 1);

        Assert.Equal(expectedLossNumber, result.HitsByAttacker);
    }

    // ---- Terrain Effects Chart ----

    [Theory]
    [InlineData(Terrain.Clear, 0, false, 2)]
    [InlineData(Terrain.Mountain, 0, true, 1)]
    [InlineData(Terrain.Marsh, 0, true, 1)]
    [InlineData(Terrain.Forest, 0, true, 1)]
    [InlineData(Terrain.Desert, 0, true, 1)]
    [InlineData(Terrain.Clear, 1, true, 2)]
    [InlineData(Terrain.Clear, 2, true, 2)]
    public void Tec_CancelRetreatAndAdvanceStop(Terrain terrain, int trench, bool canCancelRetreat, int maxAdvance)
    {
        // A two-space retreat with plenty of defenders left.
        PoGBattleResult result = Battle(FireTable.Army, 16, FireTable.Army, 9, terrain, trench: trench)
            .Outcome(attackerDieRoll: 6, defenderDieRoll: 1);

        Assert.Equal(2, result.DefenderRetreatLength);
        Assert.Equal(canCancelRetreat, result.DefenderCanIgnoreRetreat);
        Assert.Equal(maxAdvance, result.AdvanceMaxLength);
    }

    [Theory]
    [InlineData(Terrain.Clear, 0, true)]
    [InlineData(Terrain.Forest, 0, true)]
    [InlineData(Terrain.Desert, 0, true)]
    [InlineData(Terrain.Mountain, 0, false)]
    [InlineData(Terrain.Marsh, 0, false)]
    [InlineData(Terrain.Clear, 1, false)]
    public void Tec_FlankAttackAllowed(Terrain terrain, int trench, bool allowed)
    {
        PoGBattle battle = Battle(FireTable.Army, 6, FireTable.Army, 3, terrain, trench: trench, flank: true);

        Assert.Equal(allowed, PoGBattleInputRules.IsBattleDefinitionConsistent(battle, out _));
    }

    // ---- Rules fixed in the 2026-10-01 review ----

    [Fact]
    public void Rule15_1_6_UnoccupiedFort_GetsNoTrenchBenefit()
    {
        // Fort CF 3 alone behind a Level 2 trench: no shift for either side.
        PoGBattle withTrench = Battle(FireTable.Army, 6, FireTable.Corps, 0, fort: FortressLevel.LevelThree, trench: 2);
        PoGBattle noTrench = withTrench with { Trench = 0 };

        PoGBattleResult a = withTrench.Outcome(attackerDieRoll: 3, defenderDieRoll: 3);
        PoGBattleResult b = noTrench.Outcome(attackerDieRoll: 3, defenderDieRoll: 3);

        Assert.Equal(b.AttackerFireColumnIndex, a.AttackerFireColumnIndex);
        Assert.Equal(b.DefenderFireColumnIndex, a.DefenderFireColumnIndex);
    }

    [Fact]
    public void Rule15_1_6_OccupiedFort_StillGetsTheTrench()
    {
        PoGBattle battle = Battle(FireTable.Army, 6, FireTable.Corps, 1, fort: FortressLevel.LevelThree, trench: 2);

        PoGBattleResult result = battle.Outcome(attackerDieRoll: 3, defenderDieRoll: 3);

        Assert.Equal(5 - 2, result.AttackerFireColumnIndex); // 6-8 column shifted two left, to 4
        Assert.Equal(4 + 1, result.DefenderFireColumnIndex); // 1 + 3 fort = 4, shifted one right
    }

    [Theory]
    [InlineData(FortressLevel.None, 0)]
    [InlineData(FortressLevel.LevelOne, 1)]
    [InlineData(FortressLevel.LevelTwo, 2)]
    [InlineData(FortressLevel.LevelThree, 3)]
    [InlineData(FortressLevel.Destroyed, 0)]
    public void FortCombatFactors_ArePrintedCf(FortressLevel fort, int cf)
    {
        Assert.Equal(cf, fort.CombatFactors());
    }

    [Fact]
    public void FortressLevel_HasNoBesiegedLevel()
    {
        Assert.DoesNotContain("Besieged", Enum.GetNames<FortressLevel>());
    }
}
