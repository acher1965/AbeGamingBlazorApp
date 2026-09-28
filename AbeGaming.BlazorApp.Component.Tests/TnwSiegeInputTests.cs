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

        Assert.Equal(30, emittedUnits);
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
    public void NoCommanderPresentCheckbox_BecauseOnlyArmiesCanSiege()
    {
        IRenderedComponent<TnwSiegeInput> cut = Render<TnwSiegeInput>(parameters => parameters
            .Add(p => p.Units, 4)
            .Add(p => p.CommandRating, 4));

        Assert.Empty(cut.FindAll("input[type=checkbox]"));
        Assert.Single(cut.FindAll("#tnwSiegeCommanderBattleRating"));
    }

    [Fact]
    public void CommandRatingSelect_OffersOnlyPrintedValues()
    {
        IRenderedComponent<TnwSiegeInput> cut = Render<TnwSiegeInput>(parameters => parameters
            .Add(p => p.CommandRating, 4));

        string[] options = cut.FindAll("#tnwSiegeCommandRating option").Select(o => o.TextContent).ToArray();

        Assert.Equal(["4", "6", "8"], options);
    }

    [Fact]
    public void OnCommandRatingChanged_EmitsSelectedValue()
    {
        int? emitted = null;

        IRenderedComponent<TnwSiegeInput> cut = Render<TnwSiegeInput>(parameters => parameters
            .Add(p => p.CommandRating, 4)
            .Add(p => p.CommandRatingChanged, EventCallback.Factory.Create<int>(this, value => emitted = value)));

        cut.Find("#tnwSiegeCommandRating").Change("8");

        Assert.Equal(8, emitted);
    }

    [Fact]
    public void OnCommanderBattleRatingChanged_ClampsToPrintedRange()
    {
        int? emittedRating = null;

        IRenderedComponent<TnwSiegeInput> cut = Render<TnwSiegeInput>(parameters => parameters
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
            .Add(p => p.Composition, TnwForceComposition.Power)
            .Add(p => p.CompositionChanged, EventCallback.Factory.Create<TnwForceComposition>(this, value => emitted = value)));

        cut.Find("#tnwSiegeComposition").Change(nameof(TnwForceComposition.MajorityFrench));

        Assert.Equal(TnwForceComposition.MajorityFrench, emitted);
    }
}
