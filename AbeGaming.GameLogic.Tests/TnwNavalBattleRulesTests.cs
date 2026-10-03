using AbeGaming.GameLogic.TNW;

namespace AbeGaming.GameLogic.Tests;

public class TnwNavalBattleRulesTests
{
    private static TnwFleetComposition Fleet(params (TnwNavalNation Nation, int Squadrons, int Refit)[] groups)
    {
        TnwFleetComposition fleet = TnwFleetComposition.Empty;
        foreach ((TnwNavalNation nation, int squadrons, int refit) in groups)
            fleet = fleet.WithSquadrons(nation, squadrons, refit);
        return fleet;
    }

    private static TnwNavalBattle AtSea(
        TnwFleetComposition active,
        TnwFleetComposition inactive,
        bool activeEvasionDie = false,
        bool inactiveEvasionDie = false,
        bool activeFischer = false,
        bool inactiveFischer = false) =>
        new(active, inactive, TnwNavalBattleLocation.OpenSea, activeEvasionDie, inactiveEvasionDie, activeFischer, inactiveFischer);

    private static TnwNavalBattle InPort(TnwFleetComposition active, TnwFleetComposition inactive, bool fortress = false) =>
        new(active, inactive, fortress ? TnwNavalBattleLocation.EnemyFortressPort : TnwNavalBattleLocation.EnemyPort, false, false);

    // ---- The rulebook's worked example (13.4) ----

    /// <summary>
    /// An Active British Fleet of four Squadrons intercepts an Imperial Fleet of two French
    /// and two Spanish Squadrons, which fails to evade.
    /// </summary>
    private static TnwNavalBattle BritishVsImperials() => AtSea(
        Fleet((TnwNavalNation.Britain, 4, 0)),
        Fleet((TnwNavalNation.France, 2, 0), (TnwNavalNation.Spain, 2, 0)),
        activeEvasionDie: true);

    [Fact]
    public void RulebookExample_Round1Dice_13Versus6()
    {
        TnwNavalBattle battle = BritishVsImperials();

        Assert.Equal(13, TnwNavalBattleMethods.DiceForRound(battle, true, TnwNavalBattleMethods.InitialState(battle.Active), 1));
        Assert.Equal(6, TnwNavalBattleMethods.DiceForRound(battle, false, TnwNavalBattleMethods.InitialState(battle.Inactive), 1));
    }

    [Fact]
    public void RulebookExample_FullBattle_TieThenTie_ActiveBritishLose()
    {
        TnwNavalBattle battle = BritishVsImperials();
        TnwFleetState british = TnwNavalBattleMethods.InitialState(battle.Active);
        TnwFleetState imperials = TnwNavalBattleMethods.InitialState(battle.Inactive);

        // Round 1: the British roll two 5s and a 6; the Imperials roll three 5s.
        TnwFleetState imperials1 = TnwNavalBattleMethods.ApplyHits(battle.Inactive, imperials, sixes: 1, fives: 2);
        TnwFleetState british1 = TnwNavalBattleMethods.ApplyHits(battle.Active, british, sixes: 0, fives: 3);

        // "sinks one Spanish Squadron and reduces the French Fleet to three Battle Dice"
        Assert.Equal(1, imperials1.Remaining.Total(TnwNavalNation.Spain));
        Assert.Equal(2, imperials1.Remaining.Total(TnwNavalNation.France));
        Assert.Equal(3, TnwNavalBattleMethods.DiceForRound(battle, false, imperials1, 2));
        // "reduces the British Fleet to nine Battle Dice ... the added Battle Die ... no longer applies"
        Assert.Equal(9, TnwNavalBattleMethods.DiceForRound(battle, true, british1, 2));
        // "Both sides roll three hits - resulting in a second round"
        Assert.Equal(TnwNavalRoundVerdict.SecondRound, TnwNavalBattleMethods.Verdict(battle, british1, imperials1, 1));

        // Round 2: the Imperials roll a 5; the British roll a 6.
        TnwFleetState imperials2 = TnwNavalBattleMethods.ApplyHits(battle.Inactive, imperials1, sixes: 1, fives: 0);
        TnwFleetState british2 = TnwNavalBattleMethods.ApplyHits(battle.Active, british1, sixes: 0, fives: 1);

        // "The Imperials lose a French squadron this time ... must split losses evenly"
        Assert.Equal(1, imperials2.Remaining.Total(TnwNavalNation.France));
        Assert.Equal(1, imperials2.Remaining.Total(TnwNavalNation.Spain));
        // "the British lost the battle ... as the Active player they failed to score more hits"
        TnwNavalRoundVerdict verdict = TnwNavalBattleMethods.Verdict(battle, british2, imperials2, 2);
        Assert.Equal(TnwNavalRoundVerdict.InactiveWins, verdict);

        TnwNavalBattleOutcome outcome = TnwNavalBattleMethods.Conclude(battle, british2, imperials2, 2, activeWins: false);
        Assert.Equal(0, outcome.ActiveSquadronsLost);
        Assert.Equal(2, outcome.InactiveSquadronsLost);
        Assert.False(outcome.InactiveEliminated);
    }

    // ---- Dice (13.4) ----

    [Theory]
    [InlineData(TnwNavalNation.Britain, 3, 2)]
    [InlineData(TnwNavalNation.France, 2, 1)]
    [InlineData(TnwNavalNation.Denmark, 2, 1)]
    [InlineData(TnwNavalNation.Sweden, 2, 1)]
    [InlineData(TnwNavalNation.Russia, 1, 0)]
    [InlineData(TnwNavalNation.Spain, 1, 0)]
    [InlineData(TnwNavalNation.Ottoman, 1, 0)]
    public void SquadronDice_ByNation_RefitRollsOneLessToZero(TnwNavalNation nation, int ready, int refit)
    {
        Assert.Equal(ready, nation.SquadronDice(underRefit: false));
        Assert.Equal(refit, nation.SquadronDice(underRefit: true));
    }

    [Fact]
    public void DiceForRound_FivesNeverTakeTheFleetBelowZero()
    {
        TnwNavalBattle battle = AtSea(Fleet((TnwNavalNation.Russia, 2, 0)), Fleet((TnwNavalNation.Britain, 3, 0)));
        TnwFleetState russians = TnwNavalBattleMethods.ApplyHits(battle.Active, TnwNavalBattleMethods.InitialState(battle.Active), 0, 5);

        Assert.Equal(0, TnwNavalBattleMethods.DiceForRound(battle, true, russians, 2));
    }

    [Fact]
    public void FleetComposition_TotalSquadrons_MatchesSumOfCounts_UpToEveryCountAtMaximum()
    {
        TnwFleetComposition fleet = TnwFleetComposition.Empty;
        int expected = 0;
        foreach (TnwNavalNation nation in TnwNavalNationExtensions.All)
        {
            fleet = fleet.With(nation, false, TnwFleetComposition.MaxCount).With(nation, true, TnwFleetComposition.MaxCount);
            expected += 2 * TnwFleetComposition.MaxCount;
            Assert.Equal(expected, fleet.TotalSquadrons);
        }

        Assert.Equal(7 * 2 * 15, fleet.TotalSquadrons);
    }

    [Fact]
    public void FleetComposition_RefitIsClampedToTotal()
    {
        TnwFleetComposition fleet = TnwFleetComposition.Empty.WithSquadrons(TnwNavalNation.France, 2, 5);

        Assert.Equal(2, fleet.Total(TnwNavalNation.France));
        Assert.Equal(2, fleet.Count(TnwNavalNation.France, underRefit: true));
        Assert.Equal(2, fleet.Dice);
    }

    // ---- Gallant Danes event (Admiral Fischer) ----

    [Fact]
    public void Fischer_DanishSquadronsRollOneExtraDieEach_ReadyAndUnderRefit()
    {
        TnwNavalBattle battle = AtSea(
            Fleet((TnwNavalNation.Denmark, 3, 1)), Fleet((TnwNavalNation.Britain, 1, 0)), activeFischer: true);

        // Without Fischer: 2 ready (2 each) + 1 Refit (1) = 5. With Fischer: 2 ready (3 each) + 1 Refit (2) = 8.
        Assert.Equal(5, battle.Active.Dice);
        Assert.Equal(8, battle.Active.DiceWithFischer);
        Assert.Equal(8, TnwNavalBattleMethods.DiceForRound(battle, true, TnwNavalBattleMethods.InitialState(battle.Active), 1));
    }

    [Fact]
    public void Fischer_DoesNotAffectNonDanishSquadronsOrTheOtherFleet()
    {
        TnwNavalBattle battle = AtSea(
            Fleet((TnwNavalNation.Denmark, 1, 0), (TnwNavalNation.Britain, 1, 0)), Fleet((TnwNavalNation.Denmark, 1, 0)), activeFischer: true);

        // Active: 3 (Danish, 2+1 bonus) + 3 (British, unaffected) = 6. Inactive has no Fischer: 2 (Danish, unaffected).
        Assert.Equal(6, TnwNavalBattleMethods.DiceForRound(battle, true, TnwNavalBattleMethods.InitialState(battle.Active), 1));
        Assert.Equal(2, TnwNavalBattleMethods.DiceForRound(battle, false, TnwNavalBattleMethods.InitialState(battle.Inactive), 1));
    }

    [Fact]
    public void Fischer_VoidsOneSix_OncePerBattle()
    {
        TnwNavalBattle battle = AtSea(Fleet((TnwNavalNation.Denmark, 3, 0)), Fleet((TnwNavalNation.Britain, 1, 0)), activeFischer: true);
        TnwFleetState danes = TnwNavalBattleMethods.InitialState(battle.Active);

        // Round 1: two 6es rolled against the Danes - one is voided, one sinks a Squadron.
        danes = TnwNavalBattleMethods.ApplyHits(battle.Active, danes, sixes: 2, fives: 0, fischerActive: true);
        Assert.Equal(2, danes.Remaining.Total(TnwNavalNation.Denmark));
        Assert.Equal(1, danes.HitsReceived);
        Assert.True(danes.FischerSixVoided);

        // Round 2: the void has already been used, so both 6es now count.
        danes = TnwNavalBattleMethods.ApplyHits(battle.Active, danes, sixes: 2, fives: 0, fischerActive: true);
        Assert.Equal(0, danes.Remaining.Total(TnwNavalNation.Denmark));
        Assert.Equal(1 + 2, danes.HitsReceived);
    }

    [Fact]
    public void Fischer_DoesNotVoidAnythingWhenNoSixIsRolled()
    {
        TnwFleetComposition fleet = Fleet((TnwNavalNation.Denmark, 1, 0));
        TnwFleetState after = TnwNavalBattleMethods.ApplyHits(fleet, TnwNavalBattleMethods.InitialState(fleet), sixes: 0, fives: 2, fischerActive: true);

        Assert.False(after.FischerSixVoided);
        Assert.Equal(2, after.HitsReceived);
    }

    [Fact]
    public void Fischer_WithoutTheFlag_SixesAreNeverVoided()
    {
        TnwFleetComposition fleet = Fleet((TnwNavalNation.Denmark, 1, 0));
        TnwFleetState after = TnwNavalBattleMethods.ApplyHits(fleet, TnwNavalBattleMethods.InitialState(fleet), sixes: 1, fives: 0);

        Assert.Equal(0, after.Remaining.TotalSquadrons);
        Assert.Equal(1, after.HitsReceived);
    }

    [Fact]
    public void IsBattleDefinitionConsistent_FischerRequiresADanishSquadron()
    {
        TnwFleetComposition noDanes = Fleet((TnwNavalNation.Britain, 1, 0));
        TnwFleetComposition withDanes = Fleet((TnwNavalNation.Denmark, 1, 0));

        Assert.False(TnwNavalBattleInputRules.IsBattleDefinitionConsistent(AtSea(noDanes, withDanes, activeFischer: true), out string? error));
        Assert.NotNull(error);
        Assert.False(TnwNavalBattleInputRules.IsBattleDefinitionConsistent(AtSea(withDanes, noDanes, inactiveFischer: true), out error));
        Assert.NotNull(error);
        Assert.True(TnwNavalBattleInputRules.IsBattleDefinitionConsistent(AtSea(withDanes, withDanes, activeFischer: true, inactiveFischer: true), out _));
    }

    // ---- Kill allocation ----

    [Fact]
    public void ApplyHits_SinksRefitSquadronFirstWithinANation()
    {
        TnwFleetComposition fleet = Fleet((TnwNavalNation.Britain, 3, 1));
        TnwFleetState after = TnwNavalBattleMethods.ApplyHits(fleet, TnwNavalBattleMethods.InitialState(fleet), 1, 0);

        Assert.Equal(0, after.Remaining.Count(TnwNavalNation.Britain, underRefit: true));
        // 3 + 3 + 2 (Refit) = 8 dice before; sinking the Refit Squadron leaves 3 + 3.
        Assert.Equal(6, after.Remaining.Dice);
    }

    [Fact]
    public void ApplyHits_SixesBeyondTheLastSquadronHaveNoEffect()
    {
        TnwFleetComposition fleet = Fleet((TnwNavalNation.Spain, 1, 0));
        TnwFleetState after = TnwNavalBattleMethods.ApplyHits(fleet, TnwNavalBattleMethods.InitialState(fleet), 3, 0);

        Assert.Equal(0, after.Remaining.TotalSquadrons);
        Assert.Equal(3, after.HitsReceived);
    }

    [Fact]
    public void Verdict_FleetSunkLosesEvenWithMoreHitsInflicted()
    {
        TnwNavalBattle battle = AtSea(Fleet((TnwNavalNation.Spain, 1, 0)), Fleet((TnwNavalNation.Britain, 2, 0)));
        TnwFleetState spanish = TnwNavalBattleMethods.ApplyHits(battle.Active, TnwNavalBattleMethods.InitialState(battle.Active), 1, 0);
        TnwFleetState british = TnwNavalBattleMethods.ApplyHits(battle.Inactive, TnwNavalBattleMethods.InitialState(battle.Inactive), 0, 3);

        Assert.Equal(TnwNavalRoundVerdict.InactiveWins, TnwNavalBattleMethods.Verdict(battle, spanish, british, 1));
    }

    // ---- Port battles (13.5) ----

    [Fact]
    public void Port_ShoreBatteriesJoinTheDefenderEveryRound_AndAreNotReducedByFives()
    {
        TnwNavalBattle battle = InPort(Fleet((TnwNavalNation.Britain, 2, 0)), Fleet((TnwNavalNation.France, 1, 0)), fortress: true);
        TnwFleetState french = TnwNavalBattleMethods.ApplyHits(battle.Inactive, TnwNavalBattleMethods.InitialState(battle.Inactive), 0, 5);

        Assert.Equal(2 + 4, TnwNavalBattleMethods.DiceForRound(battle, false, TnwNavalBattleMethods.InitialState(battle.Inactive), 1));
        Assert.Equal(0 + 4, TnwNavalBattleMethods.DiceForRound(battle, false, french, 2));
    }

    [Fact]
    public void Port_PreBattleFire_ReducesRoundOneDiceAndCountsInTheRoundOneTotal()
    {
        TnwNavalBattle battle = InPort(Fleet((TnwNavalNation.Britain, 2, 0)), Fleet((TnwNavalNation.France, 1, 0)));
        TnwFleetState british = TnwNavalBattleMethods.ShoreBatteryFire(battle, TnwNavalBattleMethods.InitialState(battle.Active), 0, 2);

        Assert.Equal(6 - 2, TnwNavalBattleMethods.DiceForRound(battle, true, british, 1));
        Assert.Equal(2, british.HitsReceived);
    }

    [Fact]
    public void Port_EmptyPortTied_NoSecondRound_ActiveLoses()
    {
        TnwNavalBattle battle = InPort(Fleet((TnwNavalNation.Britain, 2, 0)), TnwFleetComposition.Empty);
        TnwFleetState british = TnwNavalBattleMethods.ApplyHits(battle.Active, TnwNavalBattleMethods.InitialState(battle.Active), 0, 2);
        TnwFleetState port = TnwNavalBattleMethods.ApplyHits(battle.Inactive, TnwNavalBattleMethods.InitialState(battle.Inactive), 0, 2);

        Assert.Equal(TnwNavalRoundVerdict.InactiveWins, TnwNavalBattleMethods.Verdict(battle, british, port, 1));
    }

    [Fact]
    public void Port_PortLoses_DefendingFleetIsEliminated()
    {
        TnwNavalBattle battle = InPort(Fleet((TnwNavalNation.Britain, 3, 0)), Fleet((TnwNavalNation.France, 3, 0)));
        TnwFleetState british = TnwNavalBattleMethods.InitialState(battle.Active);
        TnwFleetState french = TnwNavalBattleMethods.ApplyHits(battle.Inactive, TnwNavalBattleMethods.InitialState(battle.Inactive), 1, 1);

        TnwNavalBattleOutcome outcome = TnwNavalBattleMethods.Conclude(battle, british, french, 1, activeWins: true);

        Assert.True(outcome.InactiveEliminated);
        Assert.Equal(3, outcome.InactiveSquadronsLost);
    }

    [Fact]
    public void Port_ActiveSunkByShoreFire_LosesWithoutARound()
    {
        TnwNavalBattle battle = InPort(Fleet((TnwNavalNation.Spain, 1, 0)), TnwFleetComposition.Empty);

        Assert.True(TnwNavalBattleExactStats.TryCalculate(battle, TnwNavalBattleExactStats.DefaultWorkBudget, out TnwNavalBattleStats? stats));

        // One Spanish Squadron dies to a shore-battery 6 on either die: 1 - (5/6)^2.
        Assert.InRange(stats!.Active.EliminatedProbability, 11.0 / 36.0, 1.0);
    }

    // ---- Validation ----

    [Fact]
    public void IsBattleDefinitionConsistent_PortMayBeEmpty_SeaMayNot()
    {
        TnwFleetComposition british = Fleet((TnwNavalNation.Britain, 1, 0));

        Assert.True(TnwNavalBattleInputRules.IsBattleDefinitionConsistent(InPort(british, TnwFleetComposition.Empty), out _));
        Assert.False(TnwNavalBattleInputRules.IsBattleDefinitionConsistent(AtSea(british, TnwFleetComposition.Empty), out _));
        Assert.False(TnwNavalBattleInputRules.IsBattleDefinitionConsistent(AtSea(TnwFleetComposition.Empty, british), out _));
    }

    [Fact]
    public void IsBattleDefinitionConsistent_NoEvasionDieInPort()
    {
        TnwNavalBattle battle = InPort(Fleet((TnwNavalNation.Britain, 1, 0)), Fleet((TnwNavalNation.France, 1, 0))) with { ActiveGetsEvasionDie = true };

        Assert.False(TnwNavalBattleInputRules.IsBattleDefinitionConsistent(battle, out string? error));
        Assert.NotNull(error);
    }

    // ---- Engines ----

    public static TheoryData<TnwNavalBattle> SmallBattles() =>
    [
        BritishVsImperials(),
        AtSea(Fleet((TnwNavalNation.Russia, 2, 1)), Fleet((TnwNavalNation.Ottoman, 3, 0)), inactiveEvasionDie: true),
        InPort(Fleet((TnwNavalNation.Britain, 2, 0)), Fleet((TnwNavalNation.France, 1, 0), (TnwNavalNation.Spain, 1, 0))),
        InPort(Fleet((TnwNavalNation.Britain, 3, 0)), TnwFleetComposition.Empty, fortress: true),
        InPort(Fleet((TnwNavalNation.Britain, 3, 0)), Fleet((TnwNavalNation.France, 2, 0)), fortress: true),
        AtSea(Fleet((TnwNavalNation.Denmark, 2, 0)), Fleet((TnwNavalNation.Britain, 2, 0)), activeFischer: true),
    ];

    [Theory]
    [MemberData(nameof(SmallBattles))]
    public void ExactStats_ProbabilitiesSumToOne(TnwNavalBattle battle)
    {
        Assert.True(TnwNavalBattleExactStats.TryCalculate(battle, TnwNavalBattleExactStats.DefaultWorkBudget, out TnwNavalBattleStats? stats));

        Assert.True(stats!.IsExact);
        Assert.InRange(stats.Active.SquadronsLostDistribution.Values.Sum(), 0.999999, 1.000001);
        Assert.InRange(stats.Inactive.SquadronsLostDistribution.Values.Sum(), 0.999999, 1.000001);
        Assert.InRange(stats.ActiveWinProbability, 0, 1);
    }

    [Theory]
    [MemberData(nameof(SmallBattles))]
    public void ExactStats_And_MonteCarlo_ProduceSimilarResults(TnwNavalBattle battle)
    {
        const double Tolerance = 0.02;
        TnwNavalBattleExactStats.TryCalculate(battle, TnwNavalBattleExactStats.DefaultWorkBudget, out TnwNavalBattleStats? exact);
        TnwNavalBattleStats monteCarlo = TnwNavalBattleMonteCarlo.Run(battle, random: new Random(20260928));

        Assert.InRange(monteCarlo.ActiveWinProbability, exact!.ActiveWinProbability - Tolerance, exact.ActiveWinProbability + Tolerance);
        Assert.InRange(monteCarlo.SecondRoundProbability, exact.SecondRoundProbability - Tolerance, exact.SecondRoundProbability + Tolerance);
        Assert.InRange(monteCarlo.Inactive.MeanSquadronsLost, exact.Inactive.MeanSquadronsLost - 0.05, exact.Inactive.MeanSquadronsLost + 0.05);
        Assert.InRange(monteCarlo.Active.EliminatedProbability, exact.Active.EliminatedProbability - Tolerance, exact.Active.EliminatedProbability + Tolerance);
    }

    [Fact]
    public void Calculator_LargeBattle_FallsBackToMonteCarlo()
    {
        TnwNavalBattle battle = AtSea(Fleet((TnwNavalNation.Britain, 8, 0)), Fleet((TnwNavalNation.France, 10, 0)));

        TnwNavalBattleStats stats = TnwNavalBattleStatsCalculator.Calculate(battle);

        Assert.False(stats.IsExact);
    }

    [Fact]
    public void RollOnce_PortBattleLogsShoreFireAsRoundZero()
    {
        TnwNavalBattle battle = InPort(Fleet((TnwNavalNation.Britain, 4, 0)), Fleet((TnwNavalNation.France, 1, 0)));

        TnwNavalBattleRollResult result = TnwNavalBattleMethods.RollOnce(battle);

        Assert.Equal(0, result.RoundLog[0].Round);
        Assert.Equal(2, result.RoundLog[0].InactiveDice);
    }
}
