using AbeGaming.GameLogic.TNW;

namespace AbeGaming.GameLogic.Tests;

public class TnwSiegeRulesTests
{
    /// <summary>
    /// The rulebook's own worked example (12.3): a Spanish (Minor) Army of four Units
    /// under Castanos (Battle Rating 1, implied by the example's "five dice") besieges
    /// Lisbon (a normal Fortress, strength 2). Command Rating 4 lets all four Units roll.
    /// </summary>
    private static TnwSiegeBattle CastanosVsLisbon() => new(
        Units: 4,
        CommandRating: 4,
        CommanderBattleRating: 1,
        Composition: TnwForceComposition.Minor,
        ZoneModifierApplies: false,
        IsGibraltar: false);

    /// <summary>
    /// The playtester's example: Napoleon (Command Rating 8, Battle Rating 4) commands an
    /// Army of 8, with Soult's Army of 4 French Units alongside. Only Napoleon's Army
    /// besieges, so it rolls 8 + 2 (French) + 4 (Napoleon) = 14 dice, not 18. Gibraltar is
    /// used so a two-sixes Round does not end the Siege.
    /// </summary>
    private static TnwSiegeBattle NapoleonWithSoultInReserve() => new(
        Units: 12,
        CommandRating: 8,
        CommanderBattleRating: 4,
        Composition: TnwForceComposition.MajorityFrench,
        ZoneModifierApplies: false,
        IsGibraltar: true);

    [Fact]
    public void ResolveRound_CastanosExample_Round1_DoesNotFallAndContinues()
    {
        TnwSiegeBattle battle = CastanosVsLisbon();
        TnwSiegeState initial = TnwSiegeMethods.InitialState(battle);

        Assert.Equal(5, TnwSiegeMethods.BesiegerDiceThisRound(battle, initial.UnitsAvailable, initial.CommanderAvailable));

        // Castanos throws five dice and inflicts a single casualty ("6");
        // the Fortress throws two dice and inflicts no casualties.
        TnwSiegeRoundResult round1 = TnwSiegeMethods.ResolveRound(
            battle, initial, round: 1, besiegerSixes: 1, fortressSixes: 0, fortressFives: 0);

        Assert.False(round1.Ended);
        Assert.False(round1.FortressFalls);
        Assert.Equal(1, round1.State.CumulativeSixes);
        Assert.Equal(4, round1.State.UnitsAlive);
        Assert.Equal(4, round1.State.UnitsAvailable);
    }

    [Fact]
    public void ResolveRound_CastanosExample_Round2_FailsToRollSix_EndsWithoutFalling()
    {
        TnwSiegeBattle battle = CastanosVsLisbon();
        TnwSiegeState initial = TnwSiegeMethods.InitialState(battle);
        TnwSiegeRoundResult round1 = TnwSiegeMethods.ResolveRound(
            battle, initial, round: 1, besiegerSixes: 1, fortressSixes: 0, fortressFives: 0);

        // Round 2: the Fortress inflicts two casualties (one "5" and one "6"),
        // Castanos fails to roll a "6".
        TnwSiegeRoundResult round2 = TnwSiegeMethods.ResolveRound(
            battle, round1.State, round: 2, besiegerSixes: 0, fortressSixes: 1, fortressFives: 1);

        Assert.True(round2.Ended);
        Assert.False(round2.FortressFalls);
        Assert.False(round2.Overrun);
        Assert.False(round2.BesiegersEliminated);
        Assert.Equal(2, round2.Round);
        // "Castanos loses one of his Units killed (from the '6')"
        Assert.Equal(3, round2.State.UnitsAlive);
        Assert.True(round2.State.CommanderAlive);
    }

    [Fact]
    public void ResolveRound_CastanosExample_Round2_RollsSix_FortressFallsWithoutOverrun()
    {
        TnwSiegeBattle battle = CastanosVsLisbon();
        TnwSiegeState initial = TnwSiegeMethods.InitialState(battle);
        TnwSiegeRoundResult round1 = TnwSiegeMethods.ResolveRound(
            battle, initial, round: 1, besiegerSixes: 1, fortressSixes: 0, fortressFives: 0);

        // Rulebook's counterfactual: "had Castanos rolled a '6' in the second Round,
        // Lisbon would have fallen ... even though Lisbon rolled more casualties (two)
        // in the second Round than Castanos (one)."
        TnwSiegeRoundResult round2 = TnwSiegeMethods.ResolveRound(
            battle, round1.State, round: 2, besiegerSixes: 1, fortressSixes: 1, fortressFives: 1);

        Assert.True(round2.Ended);
        Assert.True(round2.FortressFalls);
        // Fell in exactly 2 rounds with exactly 2 cumulative sixes - matching the
        // Fortress's strength on both counts, so this is not an Overrun (12.31).
        Assert.False(round2.Overrun);
    }

    [Fact]
    public void BesiegerDice_NapoleonExample_OnlyTheBesiegingArmyRolls()
    {
        TnwSiegeBattle battle = NapoleonWithSoultInReserve();
        TnwSiegeState initial = TnwSiegeMethods.InitialState(battle);

        Assert.Equal(14, TnwSiegeMethods.BesiegerDiceThisRound(battle, initial.UnitsAvailable, initial.CommanderAvailable));
    }

    [Fact]
    public void ResolveRound_NapoleonExample_LossReplacedFromReserve_StillRolls14()
    {
        TnwSiegeBattle battle = NapoleonWithSoultInReserve();
        TnwSiegeState initial = TnwSiegeMethods.InitialState(battle);

        // "fa due danni ma prende un danno, scende a 7": two sixes, one Unit killed.
        TnwSiegeRoundResult round1 = TnwSiegeMethods.ResolveRound(
            battle, initial, round: 1, besiegerSixes: 2, fortressSixes: 1, fortressFives: 0);

        Assert.False(round1.Ended);
        Assert.Equal(11, round1.State.UnitsAlive);
        // "prende un punto dall'armata di Soult e al secondo round tira di nuovo 14 dadi, non 13"
        Assert.Equal(14, TnwSiegeMethods.BesiegerDiceThisRound(battle, round1.State.UnitsAvailable, round1.State.CommanderAvailable));
    }

    [Fact]
    public void ResolveRound_NoReserves_LossReducesDiceNextRound()
    {
        TnwSiegeBattle battle = NapoleonWithSoultInReserve() with { Units = 8 };
        TnwSiegeState initial = TnwSiegeMethods.InitialState(battle);

        TnwSiegeRoundResult round1 = TnwSiegeMethods.ResolveRound(
            battle, initial, round: 1, besiegerSixes: 2, fortressSixes: 1, fortressFives: 0);

        Assert.Equal(13, TnwSiegeMethods.BesiegerDiceThisRound(battle, round1.State.UnitsAvailable, round1.State.CommanderAvailable));
    }

    [Fact]
    public void ResolveRound_FallsInFewerRoundsThanStrength_IsOverrun()
    {
        TnwSiegeBattle battle = CastanosVsLisbon();
        TnwSiegeState initial = TnwSiegeMethods.InitialState(battle);

        // Two sixes in Round 1 alone: strength 2 reached in fewer than 2 rounds.
        TnwSiegeRoundResult round1 = TnwSiegeMethods.ResolveRound(
            battle, initial, round: 1, besiegerSixes: 2, fortressSixes: 0, fortressFives: 0);

        Assert.True(round1.Ended);
        Assert.True(round1.FortressFalls);
        Assert.True(round1.Overrun);
    }

    [Fact]
    public void ResolveRound_BesiegersFullyEliminated_FortressDoesNotFallEvenIfThresholdReached()
    {
        // Rule 12.3: "It falls only if it suffers [enough sixes] ... and if the
        // Besiegers are not eliminated." One Unit and the Commander against Gibraltar:
        // two kills eliminate both in the Round the besieger reaches the threshold.
        var battle = new TnwSiegeBattle(
            Units: 1,
            CommandRating: 4,
            CommanderBattleRating: 1,
            Composition: TnwForceComposition.Minor,
            ZoneModifierApplies: false,
            IsGibraltar: true);
        TnwSiegeState initial = TnwSiegeMethods.InitialState(battle);

        TnwSiegeRoundResult round1 = TnwSiegeMethods.ResolveRound(
            battle, initial, round: 1, besiegerSixes: 4, fortressSixes: 2, fortressFives: 0);

        Assert.True(round1.Ended);
        Assert.True(round1.BesiegersEliminated);
        Assert.False(round1.FortressFalls);
        Assert.Equal(0, round1.State.UnitsAlive);
        Assert.False(round1.State.CommanderAlive);
    }

    [Fact]
    public void ResolveRound_KillsTakePriorityOverDisruptsWhenCapacityRunsOut()
    {
        // One Unit left and the Commander already disrupted: capacity is 1. The Fortress
        // rolls a "6" and a "5" - only the kill is recorded (11.3: kills take priority).
        TnwSiegeBattle battle = CastanosVsLisbon() with { Units = 1 };
        var state = new TnwSiegeState(UnitsAlive: 1, UnitsAvailable: 1, CommanderAlive: true, CommanderAvailable: false, CumulativeSixes: 0);

        TnwSiegeRoundResult round = TnwSiegeMethods.ResolveRound(
            battle, state, round: 1, besiegerSixes: 0, fortressSixes: 1, fortressFives: 1);

        Assert.Equal(0, round.State.UnitsAlive);
        Assert.True(round.State.CommanderAlive);
        Assert.True(round.Ended);
    }

    [Fact]
    public void ResolveRound_ReservesAreNotHitByFortressFire()
    {
        // 12 Units present, Command Rating 4: only the 4 rolling Units plus the Commander
        // (capacity 5) can absorb the Fortress's results.
        TnwSiegeBattle battle = CastanosVsLisbon() with { Units = 12, IsGibraltar = true };
        TnwSiegeState initial = TnwSiegeMethods.InitialState(battle);

        TnwSiegeRoundResult round1 = TnwSiegeMethods.ResolveRound(
            battle, initial, round: 1, besiegerSixes: 0, fortressSixes: 4, fortressFives: 0);

        Assert.Equal(8, round1.State.UnitsAlive);
        Assert.True(round1.State.CommanderAlive);
    }

    [Theory]
    [InlineData(1, 4, 1, TnwForceComposition.Minor, false, false)]
    [InlineData(4, 4, 1, TnwForceComposition.Minor, false, false)]
    [InlineData(6, 6, 3, TnwForceComposition.Power, true, false)]
    [InlineData(12, 8, 4, TnwForceComposition.MajorityFrench, false, true)]
    public void ExactStats_ProbabilitiesSumToOne(
        int units, int commandRating, int battleRating, TnwForceComposition composition, bool zoneModifier, bool gibraltar)
    {
        var battle = new TnwSiegeBattle(units, commandRating, battleRating, composition, zoneModifier, gibraltar);

        TnwSiegeStats stats = TnwSiegeExactStats.Calculate(battle);

        double roundsTotal = stats.RoundsDistribution.Values.Sum();
        double unitsLostTotal = stats.BesiegerUnitsLostDistribution.Values.Sum();
        Assert.InRange(roundsTotal, 0.999999, 1.000001);
        Assert.InRange(unitsLostTotal, 0.999999, 1.000001);
        Assert.InRange(stats.FortressFallsProbability, 0, 1);
        Assert.InRange(stats.OverrunProbabilityGivenFalls, 0, 1);
        Assert.InRange(stats.BesiegersEliminatedProbability, 0, 1);
    }

    [Fact]
    public void ExactStats_UnitsBeyondCommandRatingDoNotChangeRound1Odds_ButHelpAfterLosses()
    {
        // Reserves never add dice in Round 1, so a short Siege against a strength-2 Fortress
        // gains little; against Gibraltar (up to 4 Rounds) replacing losses must help.
        TnwSiegeBattle noReserves = NapoleonWithSoultInReserve() with { Units = 8 };
        TnwSiegeBattle withReserves = NapoleonWithSoultInReserve();

        TnwSiegeStats without = TnwSiegeExactStats.Calculate(noReserves);
        TnwSiegeStats with = TnwSiegeExactStats.Calculate(withReserves);

        Assert.True(with.FortressFallsProbability > without.FortressFallsProbability);
    }

    [Fact]
    public void ExactStats_CommandRatingCapsTheDice()
    {
        TnwSiegeBattle capped = new(Units: 8, CommandRating: 4, CommanderBattleRating: 1,
            TnwForceComposition.Minor, ZoneModifierApplies: false, IsGibraltar: false);
        TnwSiegeBattle uncapped = capped with { CommandRating = 8 };

        TnwSiegeStats cappedStats = TnwSiegeExactStats.Calculate(capped);
        TnwSiegeStats uncappedStats = TnwSiegeExactStats.Calculate(uncapped);

        Assert.True(uncappedStats.FortressFallsProbability > cappedStats.FortressFallsProbability);
    }

    [Fact]
    public void ExactStats_LargerBesiegingForce_FallsMoreOftenThanSmallerForce()
    {
        var weak = new TnwSiegeBattle(1, 4, 1, TnwForceComposition.Minor, false, false);
        var strong = new TnwSiegeBattle(8, 8, 4, TnwForceComposition.MajorityFrench, false, false);

        TnwSiegeStats weakStats = TnwSiegeExactStats.Calculate(weak);
        TnwSiegeStats strongStats = TnwSiegeExactStats.Calculate(strong);

        Assert.True(strongStats.FortressFallsProbability > weakStats.FortressFallsProbability);
    }

    [Fact]
    public void ExactStats_GibraltarNeedsMoreSixesAndTakesLongerToFall()
    {
        var normal = new TnwSiegeBattle(6, 6, 3, TnwForceComposition.Power, false, IsGibraltar: false);
        var gibraltar = new TnwSiegeBattle(6, 6, 3, TnwForceComposition.Power, false, IsGibraltar: true);

        TnwSiegeStats normalStats = TnwSiegeExactStats.Calculate(normal);
        TnwSiegeStats gibraltarStats = TnwSiegeExactStats.Calculate(gibraltar);

        Assert.True(gibraltarStats.FortressFallsProbability < normalStats.FortressFallsProbability);
        Assert.True(gibraltarStats.MeanRoundsTaken > normalStats.MeanRoundsTaken);
    }

    [Theory]
    [InlineData(3, 4)]
    [InlineData(5, 4)]
    [InlineData(7, 6)]
    [InlineData(9, 8)]
    [InlineData(0, 4)]
    public void ClampCommandRating_SnapsToPrintedValues(int input, int expected)
    {
        Assert.Equal(expected, TnwSiegeInputRules.ClampCommandRating(input));
    }
}
