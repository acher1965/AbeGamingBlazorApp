using AbeGaming.GameLogic.TNW;
using AbeGamingBlazorApp.Components;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace AbeGaming.BlazorApp.Component.Tests;

public class TnwSiegeInputTests : BunitContext
{
    [Fact]
    public void OnUnitsChanged_ClampsToUpperBound()
    {
        int? emittedUnits = null;

        IRenderedComponent<TnwSiegeInput> cut = Render<TnwSiegeInput>(parameters => parameters
            .Add(p => p.Units, 4)
            .Add(p => p.UnitsChanged, EventCallback.Factory.Create<int>(this, value => emittedUnits = value)));

        cut.Find("#tnwSiegeUnits").Change("999");

        Assert.Equal(20, emittedUnits);
    }

    [Fact]
    public void OnUnitsChanged_ClampsBelowZeroToZero()
    {
        int? emittedUnits = null;

        IRenderedComponent<TnwSiegeInput> cut = Render<TnwSiegeInput>(parameters => parameters
            .Add(p => p.Units, 4)
            .Add(p => p.UnitsChanged, EventCallback.Factory.Create<int>(this, value => emittedUnits = value)));

        cut.Find("#tnwSiegeUnits").Change("-5");

        Assert.Equal(0, emittedUnits);
    }

    [Fact]
    public void CommanderBattleRatingInput_OnlyRendersWhenCommanderPresent()
    {
        IRenderedComponent<TnwSiegeInput> cut = Render<TnwSiegeInput>(parameters => parameters
            .Add(p => p.Units, 4)
            .Add(p => p.CommanderPresent, false));

        Assert.Empty(cut.FindAll("#tnwSiegeCommanderBattleRating"));

        cut.Render(parameters => parameters.Add(p => p.CommanderPresent, true));

        Assert.Single(cut.FindAll("#tnwSiegeCommanderBattleRating"));
    }

    [Fact]
    public void OnCommanderBattleRatingChanged_ClampsToPrintedRange()
    {
        int? emittedRating = null;

        IRenderedComponent<TnwSiegeInput> cut = Render<TnwSiegeInput>(parameters => parameters
            .Add(p => p.Units, 4)
            .Add(p => p.CommanderPresent, true)
            .Add(p => p.CommanderBattleRating, 2)
            .Add(p => p.CommanderBattleRatingChanged, EventCallback.Factory.Create<int>(this, value => emittedRating = value)));

        cut.Find("#tnwSiegeCommanderBattleRating").Change("9");

        Assert.Equal(4, emittedRating);
    }

    [Fact]
    public void OnCompositionChanged_EmitsSelectedValue()
    {
        TnwForceComposition? emitted = null;

        IRenderedComponent<TnwSiegeInput> cut = Render<TnwSiegeInput>(parameters => parameters
            .Add(p => p.Units, 4)
            .Add(p => p.Composition, TnwForceComposition.Power)
            .Add(p => p.CompositionChanged, EventCallback.Factory.Create<TnwForceComposition>(this, value => emitted = value)));

        cut.Find("select").Change(nameof(TnwForceComposition.MajorityFrench));

        Assert.Equal(TnwForceComposition.MajorityFrench, emitted);
    }
}
