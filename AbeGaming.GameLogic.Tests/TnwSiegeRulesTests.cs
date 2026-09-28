using AbeGaming.GameLogic.TNW;

namespace AbeGaming.GameLogic.Tests;

public class TnwSiegeRulesTests
{
    /// <summary>
    /// The rulebook's own worked example (12.3): a Spanish (Minor) Army of four Units
    /// under Castanos (Battle Rating 1, implied by the example's "five dice") besieges
    /// Lisbon (a normal Fortress, strength 2).
    /// </summary>
    private static TnwSiegeBattle CastanosVsLisbon() => new(
        Units: 4,
        CommanderPresent: true,
        CommanderBattleRating: 1,
        Composition: TnwForceComposition.Minor,
        ZoneModifierApplies: false,
        IsGibraltar: false);

    [Fact]
    public void ResolveRound_CastanosExample_Round1_DoesNotFallAndContinues()
    {
        TnwSiegeBattle battle = CastanosVsLisbon();
        TnwSiegeState initial = TnwSiegeMethods.InitialState(battle);

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
        // Besiegers are not eliminated." Two Units, no Commander, and the Fortress
        // kills both in the same round the besieger reaches the fall threshold.
        var battle = new TnwSiegeBattle(
            Units: 2,
            CommanderPresent: false,
            CommanderBattleRating: 0,
            Composition: TnwForceComposition.Minor,
            ZoneModifierApplies: false,
            IsGibraltar: false);
        TnwSiegeState initial = TnwSiegeMethods.InitialState(battle);

        TnwSiegeRoundResult round1 = TnwSiegeMethods.ResolveRound(
            battle, initial, round: 1, besiegerSixes: 2, fortressSixes: 2, fortressFives: 0);

        Assert.True(round1.Ended);
        Assert.True(round1.BesiegersEliminated);
        Assert.False(round1.FortressFalls);
        Assert.Equal(0, round1.State.UnitsAlive);
    }

    [Fact]
    public void ResolveRound_KillsTakePriorityOverDisruptsWhenCapacityRunsOut()
    {
        // One Unit left, no Commander: capacity is 1. The Fortress rolls a "6" and a
        // "5" - only the kill is recorded (11.3: kills take priority over disrupts).
        var battle = new TnwSiegeBattle(
            Units: 1,
            CommanderPresent: false,
            CommanderBattleRating: 0,
            Composition: TnwForceComposition.Minor,
            ZoneModifierApplies: false,
            IsGibraltar: false);
        var state = new TnwSiegeState(UnitsAlive: 1, UnitsAvailable: 1, CommanderAlive: false, CommanderAvailable: false, CumulativeSixes: 0);

        TnwSiegeRoundResult round = TnwSiegeMethods.ResolveRound(
            battle, state, round: 1, besiegerSixes: 0, fortressSixes: 1, fortressFives: 1);

        Assert.Equal(0, round.State.UnitsAlive);
        Assert.True(round.BesiegersEliminated);
    }

    [Theory]
    [InlineData(1, false, 0, TnwForceComposition.Minor, false, false)]
    [InlineData(4, true, 1, TnwForceComposition.Minor, false, false)]
    [InlineData(6, true, 3, TnwForceComposition.Power, true, false)]
    [InlineData(8, true, 4, TnwForceComposition.MajorityFrench, false, true)]
    public void ExactStats_ProbabilitiesSumToOne(
        int units, bool commanderPresent, int commanderRating, TnwForceComposition composition, bool zoneModifier, bool gibraltar)
    {
        var battle = new TnwSiegeBattle(units, commanderPresent, commanderRating, composition, zoneModifier, gibraltar);

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
    public void ExactStats_NoCommander_ReportsZeroCommanderLostProbability()
    {
        var battle = new TnwSiegeBattle(4, CommanderPresent: false, CommanderBattleRating: 0,
            TnwForceComposition.Minor, ZoneModifierApplies: false, IsGibraltar: false);

        TnwSiegeStats stats = TnwSiegeExactStats.Calculate(battle);

        Assert.Equal(0, stats.CommanderLostProbability);
    }

    [Fact]
    public void ExactStats_LargerBesiegingForce_FallsMoreOftenThanSmallerForce()
    {
        var weak = new TnwSiegeBattle(1, false, 0, TnwForceComposition.Minor, false, false);
        var strong = new TnwSiegeBattle(10, true, 4, TnwForceComposition.MajorityFrench, false, false);

        TnwSiegeStats weakStats = TnwSiegeExactStats.Calculate(weak);
        TnwSiegeStats strongStats = TnwSiegeExactStats.Calculate(strong);

        Assert.True(strongStats.FortressFallsProbability > weakStats.FortressFallsProbability);
    }

    [Fact]
    public void ExactStats_GibraltarNeedsMoreSixesAndTakesLongerToFall()
    {
        var normal = new TnwSiegeBattle(6, true, 3, TnwForceComposition.Power, false, IsGibraltar: false);
        var gibraltar = new TnwSiegeBattle(6, true, 3, TnwForceComposition.Power, false, IsGibraltar: true);

        TnwSiegeStats normalStats = TnwSiegeExactStats.Calculate(normal);
        TnwSiegeStats gibraltarStats = TnwSiegeExactStats.Calculate(gibraltar);

        Assert.True(gibraltarStats.FortressFallsProbability < normalStats.FortressFallsProbability);
        Assert.True(gibraltarStats.MeanRoundsTaken > normalStats.MeanRoundsTaken);
    }
}
