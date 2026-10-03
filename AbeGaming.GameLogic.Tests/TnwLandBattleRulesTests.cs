using AbeGaming.GameLogic.TNW;

namespace AbeGaming.GameLogic.Tests;

public class TnwLandBattleRulesTests
{
    private static TnwBattleSide Side(int units, int battleRating = 0, TnwForceComposition composition = TnwForceComposition.Power, int eventDice = 0) =>
        new(units, battleRating, composition, eventDice);

    private static TnwLandBattle Battle(
        TnwBattleSide attacker,
        TnwBattleSide defender,
        TnwTerrain terrain = TnwTerrain.None,
        int failedEvasions = 0,
        bool defenderCannotRetreat = false,
        bool defenderWithoutArmyGroup = false,
        TnwAmphibiousLanding amphibiousLanding = TnwAmphibiousLanding.None) =>
        new(attacker, defender, terrain, failedEvasions, defenderCannotRetreat, defenderWithoutArmyGroup, amphibiousLanding);

    // ---- Dice (11.2, 11.21, 11.22, 10.2, 9.7) ----

    [Fact]
    public void DiceForRound_CountsUnitsCommanderNationalityAndFirstRoundEvents()
    {
        TnwLandBattle battle = Battle(Side(8, battleRating: 4, TnwForceComposition.MajorityFrench, eventDice: 1), Side(5));
        TnwBattleSideState attacker = TnwLandBattleMethods.InitialState(battle.Attacker);

        Assert.Equal(8 + 4 + 2 + 1, TnwLandBattleMethods.DiceForRound(battle, isAttacker: true, attacker, round: 1));
        // Battle events affect only one Round (11.1).
        Assert.Equal(8 + 4 + 2, TnwLandBattleMethods.DiceForRound(battle, isAttacker: true, attacker, round: 2));
    }

    [Theory]
    [InlineData(TnwTerrain.None, 0)]
    [InlineData(TnwTerrain.Rough, 1)]
    [InlineData(TnwTerrain.Pass, 2)]
    [InlineData(TnwTerrain.Marsh, 3)]
    public void DiceForRound_TerrainHelpsTheDefenderInRoundOneOnly(TnwTerrain terrain, int bonus)
    {
        TnwLandBattle battle = Battle(Side(4), Side(4, composition: TnwForceComposition.Minor), terrain);
        TnwBattleSideState defender = TnwLandBattleMethods.InitialState(battle.Defender);

        Assert.Equal(4 + bonus, TnwLandBattleMethods.DiceForRound(battle, isAttacker: false, defender, round: 1));
        Assert.Equal(4, TnwLandBattleMethods.DiceForRound(battle, isAttacker: false, defender, round: 2));
    }

    [Fact]
    public void DiceForRound_FailedEvasionAddsAttackerDiceAndForfeitsTerrain()
    {
        TnwLandBattle battle = Battle(Side(4, composition: TnwForceComposition.Minor), Side(4, composition: TnwForceComposition.Minor),
            TnwTerrain.Marsh, failedEvasions: 2);

        Assert.Equal(4 + 2, TnwLandBattleMethods.DiceForRound(battle, true, TnwLandBattleMethods.InitialState(battle.Attacker), 1));
        Assert.Equal(4, TnwLandBattleMethods.DiceForRound(battle, false, TnwLandBattleMethods.InitialState(battle.Defender), 1));
    }

    [Fact]
    public void DiceForRound_DefenderWithoutArmyGroup_CommanderAddsNoDice()
    {
        TnwLandBattle battle = Battle(Side(4), Side(4, battleRating: 3, TnwForceComposition.Minor), defenderWithoutArmyGroup: true);

        Assert.Equal(4, TnwLandBattleMethods.DiceForRound(battle, false, TnwLandBattleMethods.InitialState(battle.Defender), 1));
    }

    [Fact]
    public void DiceForRound_NegativeEventDiceNeverGoBelowZero()
    {
        TnwLandBattle battle = Battle(Side(1, composition: TnwForceComposition.Minor, eventDice: -5), Side(4));

        Assert.Equal(0, TnwLandBattleMethods.DiceForRound(battle, true, TnwLandBattleMethods.InitialState(battle.Attacker), 1));
    }

    // ---- Casualties (11.3, 11.33) ----

    [Fact]
    public void ApplyHits_KillsTakePriority_CommanderOnlyWhenUnitsAreGone()
    {
        TnwBattleSideState state = TnwLandBattleMethods.InitialState(Side(1, battleRating: 2));

        TnwBattleSideState after = TnwLandBattleMethods.ApplyHits(state, kills: 2, disrupts: 1);

        Assert.Equal(0, after.UnitsAlive);
        Assert.False(after.CommanderAlive);
        Assert.True(after.Eliminated);
        Assert.Equal(3, after.HitsReceived);
    }

    [Fact]
    public void ApplyHits_CommanderSurvivesWhileUnitsCanAbsorbKills()
    {
        TnwBattleSideState state = TnwLandBattleMethods.InitialState(Side(3, battleRating: 2));

        TnwBattleSideState after = TnwLandBattleMethods.ApplyHits(state, kills: 2, disrupts: 0);

        Assert.Equal(1, after.UnitsAlive);
        Assert.True(after.CommanderAlive);
        Assert.True(after.CommanderUndisrupted);
    }

    [Fact]
    public void ApplyHits_ExcessDisruptsHitCommanderThenNationalityDice()
    {
        TnwLandBattle battle = Battle(Side(2, battleRating: 3, TnwForceComposition.MajorityFrench), Side(4));
        TnwBattleSideState state = TnwLandBattleMethods.InitialState(battle.Attacker);

        // Two disrupts take both Units, the third the Commander, the fourth one French bonus die.
        TnwBattleSideState after = TnwLandBattleMethods.ApplyHits(state, kills: 0, disrupts: 4);

        Assert.Equal(0, after.UnitsUndisrupted);
        Assert.False(after.CommanderUndisrupted);
        Assert.True(after.CommanderAlive);
        Assert.Equal(1, after.BonusDiceCancelled);
        Assert.Equal(1, TnwLandBattleMethods.DiceForRound(battle, true, after, round: 2));
    }

    [Fact]
    public void ApplyHits_KillsFallOnDisruptedUnitsFirst()
    {
        TnwBattleSideState state = new(UnitsAlive: 4, UnitsUndisrupted: 2, CommanderAlive: false, CommanderUndisrupted: false,
            BonusDiceCancelled: 0, KillsReceived: 0, HitsReceived: 2);

        TnwBattleSideState after = TnwLandBattleMethods.ApplyHits(state, kills: 2, disrupts: 0);

        Assert.Equal(2, after.UnitsAlive);
        Assert.Equal(2, after.UnitsUndisrupted);
    }

    // ---- Victor (11.32) ----

    [Fact]
    public void Verdict_MoreCasualtiesSufferedLoses()
    {
        TnwBattleSideState attacker = TnwLandBattleMethods.InitialState(Side(6)) with { HitsReceived = 2 };
        TnwBattleSideState defender = TnwLandBattleMethods.InitialState(Side(6)) with { HitsReceived = 3 };

        Assert.Equal(TnwRoundVerdict.AttackerWins, TnwLandBattleMethods.Verdict(attacker, defender, 1));
    }

    [Fact]
    public void Verdict_SideEliminatedByKillsLoses_EvenIfItInflictedMore()
    {
        TnwBattleSideState attacker = TnwLandBattleMethods.ApplyHits(TnwLandBattleMethods.InitialState(Side(2)), kills: 2, disrupts: 0);
        TnwBattleSideState defender = TnwLandBattleMethods.InitialState(Side(6)) with { HitsReceived = 5 };

        Assert.Equal(TnwRoundVerdict.DefenderWins, TnwLandBattleMethods.Verdict(attacker, defender, 1));
    }

    [Fact]
    public void Verdict_TieThenTieAgain_ActiveFormationRetreats()
    {
        // Mirrors the rulebook's naval example (13.4): three hits each forces a second
        // Round; one hit each in Round 2 leaves the Active side the loser.
        TnwBattleSideState attacker = TnwLandBattleMethods.InitialState(Side(6)) with { HitsReceived = 3 };
        TnwBattleSideState defender = TnwLandBattleMethods.InitialState(Side(6)) with { HitsReceived = 3 };
        Assert.Equal(TnwRoundVerdict.SecondRound, TnwLandBattleMethods.Verdict(attacker, defender, 1));

        attacker = attacker with { HitsReceived = 4 };
        defender = defender with { HitsReceived = 4 };
        Assert.Equal(TnwRoundVerdict.DefenderWins, TnwLandBattleMethods.Verdict(attacker, defender, 2));
    }

    // ---- Aftermath (11.5, 11.42, 11.6, 11.7) ----

    [Fact]
    public void Conclude_MarginOfThree_RoutsAndEliminatesDisruptedPieces()
    {
        TnwLandBattle battle = Battle(Side(6), Side(5, battleRating: 3));
        TnwBattleSideState attacker = TnwLandBattleMethods.ApplyHits(TnwLandBattleMethods.InitialState(battle.Attacker), 0, 1);
        TnwBattleSideState defender = TnwLandBattleMethods.ApplyHits(TnwLandBattleMethods.InitialState(battle.Defender), 1, 3);

        TnwLandBattleOutcome outcome = TnwLandBattleMethods.Conclude(battle, attacker, defender, 1, attackerWins: true);

        Assert.True(outcome.LoserRouted);
        // 1 killed + 3 disrupted eliminated by the rout.
        Assert.Equal(4, outcome.DefenderUnitsLost);
        Assert.Equal(0, outcome.AttackerUnitsLost);
        Assert.Equal(3.0 / 6.0, outcome.ResourceChance, 10);
    }

    [Fact]
    public void Conclude_MarginOfTwo_NoRout()
    {
        TnwLandBattle battle = Battle(Side(6), Side(5, battleRating: 3));
        TnwBattleSideState attacker = TnwLandBattleMethods.ApplyHits(TnwLandBattleMethods.InitialState(battle.Attacker), 0, 1);
        TnwBattleSideState defender = TnwLandBattleMethods.ApplyHits(TnwLandBattleMethods.InitialState(battle.Defender), 1, 2);

        TnwLandBattleOutcome outcome = TnwLandBattleMethods.Conclude(battle, attacker, defender, 1, attackerWins: true);

        Assert.False(outcome.LoserRouted);
        Assert.Equal(1, outcome.DefenderUnitsLost);
        Assert.Equal(0, outcome.ResourceChance);
    }

    [Fact]
    public void Conclude_DefenderWithoutArmyGroup_RoutUsesPrintedRatingForResource()
    {
        // 9.7: the Commander adds no dice, "although his printed Battle Rating still applies
        // as a liability should he be routed".
        TnwLandBattle battle = Battle(Side(6), Side(5, battleRating: 3), defenderWithoutArmyGroup: true);
        TnwBattleSideState attacker = TnwLandBattleMethods.InitialState(battle.Attacker);
        TnwBattleSideState defender = TnwLandBattleMethods.ApplyHits(TnwLandBattleMethods.InitialState(battle.Defender), 0, 3);

        TnwLandBattleOutcome outcome = TnwLandBattleMethods.Conclude(battle, attacker, defender, 1, attackerWins: true);

        Assert.True(outcome.LoserRouted);
        Assert.Equal(3.0 / 6.0, outcome.ResourceChance, 10);
    }

    [Fact]
    public void Conclude_RoutedFormationWithoutCommander_NoResourceChance()
    {
        TnwLandBattle battle = Battle(Side(6), Side(5));
        TnwBattleSideState attacker = TnwLandBattleMethods.InitialState(battle.Attacker);
        TnwBattleSideState defender = TnwLandBattleMethods.ApplyHits(TnwLandBattleMethods.InitialState(battle.Defender), 0, 4);

        TnwLandBattleOutcome outcome = TnwLandBattleMethods.Conclude(battle, attacker, defender, 1, attackerWins: true);

        Assert.True(outcome.LoserRouted);
        Assert.Equal(0, outcome.ResourceChance);
    }

    [Fact]
    public void Conclude_DefenderCannotRetreat_IsEliminated()
    {
        TnwLandBattle battle = Battle(Side(6), Side(5, battleRating: 2), defenderCannotRetreat: true);
        TnwBattleSideState attacker = TnwLandBattleMethods.InitialState(battle.Attacker);
        TnwBattleSideState defender = TnwLandBattleMethods.ApplyHits(TnwLandBattleMethods.InitialState(battle.Defender), 1, 0);

        TnwLandBattleOutcome outcome = TnwLandBattleMethods.Conclude(battle, attacker, defender, 1, attackerWins: true);

        Assert.True(outcome.DefenderEliminated);
        Assert.Equal(5, outcome.DefenderUnitsLost);
        Assert.True(outcome.DefenderCommanderKilled);
    }

    [Fact]
    public void Conclude_KillsExceedingLoserPieces_FlagOverrun()
    {
        TnwLandBattle battle = Battle(Side(6), Side(2));
        TnwBattleSideState attacker = TnwLandBattleMethods.InitialState(battle.Attacker);
        TnwBattleSideState defender = TnwLandBattleMethods.ApplyHits(TnwLandBattleMethods.InitialState(battle.Defender), 3, 0);

        TnwLandBattleOutcome outcome = TnwLandBattleMethods.Conclude(battle, attacker, defender, 1, attackerWins: true);

        Assert.True(outcome.FlagOverrun);
        Assert.True(outcome.DefenderEliminated);
    }

    [Fact]
    public void Conclude_BothSidesEliminated_NoFlagOverrun()
    {
        TnwLandBattle battle = Battle(Side(1), Side(1));
        TnwBattleSideState attacker = TnwLandBattleMethods.ApplyHits(TnwLandBattleMethods.InitialState(battle.Attacker), 1, 0);
        TnwBattleSideState defender = TnwLandBattleMethods.ApplyHits(TnwLandBattleMethods.InitialState(battle.Defender), 3, 0);

        TnwLandBattleOutcome outcome = TnwLandBattleMethods.Conclude(battle, attacker, defender, 1, attackerWins: true);

        Assert.False(outcome.FlagOverrun);
    }

    // ---- Amphibious Assault (13.7) ----

    [Fact]
    public void ShoreBatteryFire_ReducesRoundOneDiceAndCountsInTheRoundOneTotal()
    {
        TnwBattleSide side = Side(4, composition: TnwForceComposition.Minor);
        TnwBattleSideState attacker = TnwLandBattleMethods.ShoreBatteryFire(
            TnwLandBattleMethods.InitialState(side), sixes: 0, fives: 2);

        TnwLandBattle battle = Battle(side, Side(4), amphibiousLanding: TnwAmphibiousLanding.EnemyPort);
        Assert.Equal(4 - 2, TnwLandBattleMethods.DiceForRound(battle, isAttacker: true, attacker, round: 1));
        Assert.Equal(2, attacker.HitsReceived);
    }

    [Fact]
    public void DiceForRound_AmphibiousLanding_DefenderDiceAreUnaffected()
    {
        // Unlike a naval Port battle (13.5), the shore batteries fire only once, before Round 1 -
        // they must never be added to the defender's own dice pool in any Round.
        TnwLandBattle battle = Battle(Side(4), Side(4), amphibiousLanding: TnwAmphibiousLanding.EnemyFortressPort);
        TnwLandBattle noLanding = Battle(Side(4), Side(4));

        TnwBattleSideState defender = TnwLandBattleMethods.InitialState(battle.Defender);
        Assert.Equal(
            TnwLandBattleMethods.DiceForRound(noLanding, isAttacker: false, defender, round: 1),
            TnwLandBattleMethods.DiceForRound(battle, isAttacker: false, defender, round: 1));
        Assert.Equal(
            TnwLandBattleMethods.DiceForRound(noLanding, isAttacker: false, defender, round: 2),
            TnwLandBattleMethods.DiceForRound(battle, isAttacker: false, defender, round: 2));
    }

    [Fact]
    public void Simulate_ShoreFireThenRoundOne_CasualtiesCarryIntoTheRoundOneVerdict()
    {
        // Shore fire inflicts 1 kill + 1 disrupt on a 4-Unit attacker (2 casualties already on
        // its Round 1 total); Round 1 itself then has the attacker score 2 hits and the defender
        // score 0 - an even 2-2 total, so it must tie into a second Round, not resolve as a
        // Round 1 attacker win.
        TnwBattleSideState attacker = TnwLandBattleMethods.ShoreBatteryFire(
            TnwLandBattleMethods.InitialState(Side(4)), sixes: 1, fives: 1);
        TnwBattleSideState defender0 = TnwLandBattleMethods.InitialState(Side(4));

        TnwBattleSideState defenderAfterRound1 = TnwLandBattleMethods.ApplyHits(defender0, kills: 2, disrupts: 0);
        TnwBattleSideState attackerAfterRound1 = TnwLandBattleMethods.ApplyHits(attacker, kills: 0, disrupts: 0);

        Assert.Equal(2, attackerAfterRound1.HitsReceived);
        Assert.Equal(2, defenderAfterRound1.HitsReceived);
        Assert.Equal(TnwRoundVerdict.SecondRound, TnwLandBattleMethods.Verdict(attackerAfterRound1, defenderAfterRound1, round: 1));
    }

    [Fact]
    public void ExactStats_AttackerWipedOutByShoreFire_LosesWithoutARound()
    {
        TnwLandBattle battle = Battle(Side(1), Side(1), amphibiousLanding: TnwAmphibiousLanding.EnemyFortressPort);

        Assert.True(TnwLandBattleExactStats.TryCalculate(battle, TnwLandBattleExactStats.DefaultWorkBudget, out TnwLandBattleStats? stats));

        // The lone attacking Unit dies to a shore-battery 6 on any of 4 dice: 1 - (5/6)^4.
        Assert.InRange(stats!.Attacker.EliminatedProbability, 1 - Math.Pow(5.0 / 6, 4) - 0.000001, 1.0);
    }

    [Fact]
    public void RollOnce_AmphibiousLanding_LogsShoreFireAsRoundZero()
    {
        TnwLandBattle battle = Battle(Side(6, 3), Side(5, 2), amphibiousLanding: TnwAmphibiousLanding.EnemyFortressPort);

        TnwLandBattleRollResult result = TnwLandBattleMethods.RollOnce(battle);

        Assert.Equal(0, result.RoundLog[0].Round);
        Assert.Equal(4, result.RoundLog[0].DefenderDice);
        Assert.Equal(0, result.RoundLog[0].AttackerDice);
    }

    [Fact]
    public void IsBattleDefinitionConsistent_NoTerrainBonusForAnAmphibiousLanding()
    {
        TnwLandBattle battle = Battle(Side(4), Side(4), TnwTerrain.Marsh, amphibiousLanding: TnwAmphibiousLanding.EnemyPort);

        Assert.False(TnwLandBattleInputRules.IsBattleDefinitionConsistent(battle, out string? error));
        Assert.NotNull(error);
        Assert.True(TnwLandBattleInputRules.IsBattleDefinitionConsistent(battle with { Terrain = TnwTerrain.None }, out _));
    }

    // ---- Engines ----

    public static TheoryData<TnwLandBattle> SmallBattles() =>
    [
        Battle(Side(1), Side(1)),
        Battle(Side(4, 2, TnwForceComposition.MajorityFrench), Side(3, 1, TnwForceComposition.Minor), TnwTerrain.Pass),
        Battle(Side(6, 3), Side(6, 2), failedEvasions: 1, defenderCannotRetreat: true),
        Battle(Side(8, 4, TnwForceComposition.MajorityFrench, 1), Side(7, 3), TnwTerrain.Rough, defenderWithoutArmyGroup: true),
        Battle(Side(5, 2), Side(4, 2), amphibiousLanding: TnwAmphibiousLanding.EnemyFortressPort),
    ];

    [Theory]
    [MemberData(nameof(SmallBattles))]
    public void ExactStats_ProbabilitiesSumToOne(TnwLandBattle battle)
    {
        Assert.True(TnwLandBattleExactStats.TryCalculate(battle, TnwLandBattleExactStats.DefaultWorkBudget, out TnwLandBattleStats? stats));

        Assert.True(stats!.IsExact);
        Assert.InRange(stats.AttackerWinProbability + stats.DefenderWinProbability, 0.999999, 1.000001);
        Assert.InRange(stats.Attacker.UnitsLostDistribution.Values.Sum(), 0.999999, 1.000001);
        Assert.InRange(stats.Defender.UnitsLostDistribution.Values.Sum(), 0.999999, 1.000001);
        Assert.InRange(stats.SecondRoundProbability, 0, 1);
    }

    [Theory]
    [MemberData(nameof(SmallBattles))]
    public void ExactStats_And_MonteCarlo_ProduceSimilarResults(TnwLandBattle battle)
    {
        const double Tolerance = 0.02;
        TnwLandBattleExactStats.TryCalculate(battle, TnwLandBattleExactStats.DefaultWorkBudget, out TnwLandBattleStats? exact);
        TnwLandBattleStats monteCarlo = TnwLandBattleMonteCarlo.Run(battle, random: new Random(20260928));

        Assert.False(monteCarlo.IsExact);
        Assert.InRange(monteCarlo.AttackerWinProbability, exact!.AttackerWinProbability - Tolerance, exact.AttackerWinProbability + Tolerance);
        Assert.InRange(monteCarlo.SecondRoundProbability, exact.SecondRoundProbability - Tolerance, exact.SecondRoundProbability + Tolerance);
        Assert.InRange(monteCarlo.Defender.RoutedProbability, exact.Defender.RoutedProbability - Tolerance, exact.Defender.RoutedProbability + Tolerance);
        Assert.InRange(monteCarlo.Defender.MeanUnitsLost, exact.Defender.MeanUnitsLost - 0.1, exact.Defender.MeanUnitsLost + 0.1);
    }

    [Fact]
    public void ExactStats_LargerForceWinsMoreOften()
    {
        TnwLandBattleExactStats.TryCalculate(Battle(Side(8, 2), Side(3, 2)), TnwLandBattleExactStats.DefaultWorkBudget, out TnwLandBattleStats? strong);
        TnwLandBattleExactStats.TryCalculate(Battle(Side(3, 2), Side(8, 2)), TnwLandBattleExactStats.DefaultWorkBudget, out TnwLandBattleStats? weak);

        Assert.True(strong!.AttackerWinProbability > 0.5);
        Assert.True(weak!.AttackerWinProbability < 0.5);
    }

    [Fact]
    public void Calculator_LargeBattle_FallsBackToMonteCarlo()
    {
        TnwLandBattle battle = Battle(Side(20, 4, TnwForceComposition.MajorityFrench), Side(20, 3));

        Assert.False(TnwLandBattleExactStats.TryCalculate(battle, TnwLandBattleExactStats.DefaultWorkBudget, out _));
        TnwLandBattleStats stats = TnwLandBattleStatsCalculator.Calculate(battle);

        Assert.False(stats.IsExact);
        Assert.Equal(1 << TnwLandBattleMonteCarlo.DefaultTrialsExponent, stats.MonteCarloTrials);
    }

    [Fact]
    public void Calculator_SmallBattle_IsExact()
    {
        Assert.True(TnwLandBattleStatsCalculator.Calculate(Battle(Side(4, 2), Side(4, 2))).IsExact);
    }

    [Fact]
    public void RollOnce_ProducesAConsistentLog()
    {
        TnwLandBattle battle = Battle(Side(6, 3), Side(5, 2));

        TnwLandBattleRollResult result = TnwLandBattleMethods.RollOnce(battle);

        Assert.Equal(result.Outcome.Rounds, result.RoundLog.Count);
        Assert.InRange(result.RoundLog.Count, 1, 2);
    }

    [Fact]
    public void IsBattleDefinitionConsistent_RequiresAPiecePerSide()
    {
        Assert.False(TnwLandBattleInputRules.IsBattleDefinitionConsistent(Battle(Side(0), Side(3)), out string? error));
        Assert.NotNull(error);
        Assert.True(TnwLandBattleInputRules.IsBattleDefinitionConsistent(Battle(Side(0, battleRating: 2), Side(3)), out _));
    }
}
