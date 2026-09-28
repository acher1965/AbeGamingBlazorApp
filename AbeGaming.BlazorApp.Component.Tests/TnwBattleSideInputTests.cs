using AbeGaming.GameLogic.TNW;
using AbeGamingBlazorApp.Components;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace AbeGaming.BlazorApp.Component.Tests;

public class TnwBattleSideInputTests : BunitContext
{
    private IRenderedComponent<TnwBattleSideInput> RenderSide(Action<ComponentParameterCollectionBuilder<TnwBattleSideInput>>? extra = null) =>
        Render<TnwBattleSideInput>(parameters =>
        {
            parameters.Add(p => p.IdPrefix, "test").Add(p => p.Units, 4);
            extra?.Invoke(parameters);
        });

    [Fact]
    public void OnUnitsChanged_ClampsToUpperBound()
    {
        int? emitted = null;
        IRenderedComponent<TnwBattleSideInput> cut = RenderSide(p =>
            p.Add(x => x.UnitsChanged, EventCallback.Factory.Create<int>(this, value => emitted = value)));

        cut.Find("#testUnits").Change("99");

        Assert.Equal(30, emitted);
    }

    [Fact]
    public void CommanderSelect_OffersNoneAndPrintedRatings()
    {
        IRenderedComponent<TnwBattleSideInput> cut = RenderSide();

        string[] values = cut.FindAll("#testCommander option").Select(o => o.GetAttribute("value") ?? "").ToArray();

        Assert.Equal(["0", "1", "2", "3", "4"], values);
    }

    [Fact]
    public void OnCommanderChanged_EmitsRating()
    {
        int? emitted = null;
        IRenderedComponent<TnwBattleSideInput> cut = RenderSide(p =>
            p.Add(x => x.CommanderBattleRatingChanged, EventCallback.Factory.Create<int>(this, value => emitted = value)));

        cut.Find("#testCommander").Change("3");

        Assert.Equal(3, emitted);
    }

    [Fact]
    public void OnEventDiceChanged_ClampsBothWays()
    {
        int? emitted = null;
        IRenderedComponent<TnwBattleSideInput> cut = RenderSide(p =>
            p.Add(x => x.EventDiceChanged, EventCallback.Factory.Create<int>(this, value => emitted = value)));

        cut.Find("#testEventDice").Change("-9");
        Assert.Equal(-5, emitted);

        cut.Find("#testEventDice").Change("9");
        Assert.Equal(5, emitted);
    }

    [Fact]
    public void OnCompositionChanged_EmitsSelectedValue()
    {
        TnwForceComposition? emitted = null;
        IRenderedComponent<TnwBattleSideInput> cut = RenderSide(p =>
            p.Add(x => x.CompositionChanged, EventCallback.Factory.Create<TnwForceComposition>(this, value => emitted = value)));

        cut.Find("#testComposition").Change(nameof(TnwForceComposition.Minor));

        Assert.Equal(TnwForceComposition.Minor, emitted);
    }
}
