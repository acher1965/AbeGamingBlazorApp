using AbeGaming.GameLogic.TNW;
using AbeGamingBlazorApp.Components;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace AbeGaming.BlazorApp.Component.Tests;

public class TnwFleetInputTests : BunitContext
{
    private TnwFleetComposition? _emitted;

    private IRenderedComponent<TnwFleetInput> RenderFleet(TnwFleetComposition fleet, bool evasionAllowed = true) =>
        Render<TnwFleetInput>(parameters => parameters
            .Add(p => p.IdPrefix, "test")
            .Add(p => p.Fleet, fleet)
            .Add(p => p.EvasionAllowed, evasionAllowed)
            .Add(p => p.FleetChanged, EventCallback.Factory.Create<TnwFleetComposition>(this, value => _emitted = value)));

    [Fact]
    public void ShowsOneRowPerNavalNation()
    {
        IRenderedComponent<TnwFleetInput> cut = RenderFleet(TnwFleetComposition.Empty);

        Assert.Equal(TnwNavalNationExtensions.All.Count, cut.FindAll("tbody tr").Count);
    }

    [Fact]
    public void OnSquadronsChanged_ClampsToMaximum()
    {
        IRenderedComponent<TnwFleetInput> cut = RenderFleet(TnwFleetComposition.Empty);

        cut.Find("#testBritainSquadrons").Change("99");

        Assert.Equal(TnwFleetComposition.MaxCount, _emitted!.Value.Total(TnwNavalNation.Britain));
    }

    [Fact]
    public void OnRefitChanged_CannotExceedSquadrons()
    {
        IRenderedComponent<TnwFleetInput> cut = RenderFleet(TnwFleetComposition.Empty.WithSquadrons(TnwNavalNation.France, 2, 0));

        cut.Find("#testFranceRefit").Change("5");

        Assert.Equal(2, _emitted!.Value.Count(TnwNavalNation.France, underRefit: true));
        Assert.Equal(2, _emitted.Value.Total(TnwNavalNation.France));
    }

    [Fact]
    public void LoweringSquadrons_KeepsRefitWithinTheNewTotal()
    {
        IRenderedComponent<TnwFleetInput> cut = RenderFleet(TnwFleetComposition.Empty.WithSquadrons(TnwNavalNation.Spain, 3, 3));

        cut.Find("#testSpainSquadrons").Change("1");

        Assert.Equal(1, _emitted!.Value.Total(TnwNavalNation.Spain));
        Assert.Equal(1, _emitted.Value.Count(TnwNavalNation.Spain, underRefit: true));
    }

    [Fact]
    public void EvasionCheckbox_DisabledWhenNotAllowed()
    {
        IRenderedComponent<TnwFleetInput> cut = RenderFleet(TnwFleetComposition.Empty, evasionAllowed: false);

        Assert.True(cut.Find("#testEvasionDie").HasAttribute("disabled"));
    }
}
