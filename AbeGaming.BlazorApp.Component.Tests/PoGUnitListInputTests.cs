using AbeGaming.GameLogic;
using AbeGaming.GameLogic.PoG;
using AbeGamingBlazorApp.Components;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace AbeGaming.BlazorApp.Component.Tests;

public class PoGUnitListInputTests : BunitContext
{
    private EquatableList<PoGUnit>? _emitted;

    private IRenderedComponent<PoGUnitListInput> RenderList(
        EquatableList<PoGUnit> units,
        int maxUnits = PoGDetailedForces.MaxAttackers,
        int fortCf = 0,
        PoGFaction faction = PoGFaction.AlliedPowers) =>
        Render<PoGUnitListInput>(parameters => parameters
            .Add(p => p.Units, units)
            .Add(p => p.Faction, faction)
            .Add(p => p.MaxUnits, maxUnits)
            .Add(p => p.FortCf, fortCf)
            .Add(p => p.IdPrefix, "t")
            .Add(p => p.UnitsChanged, EventCallback.Factory.Create<EquatableList<PoGUnit>>(this, value => _emitted = value)));

    [Fact]
    public void Summary_ShowsCombatStrengthAndTable_ForTannenberg()
    {
        IRenderedComponent<PoGUnitListInput> attackers = RenderList(new([new PoGUnit("GE_ARMY"), new PoGUnit("GE_CORPS")]), faction: PoGFaction.CentralPowers);
        IRenderedComponent<PoGUnitListInput> defenders = RenderList(new([new PoGUnit("RU_CORPS")]), fortCf: 2);

        Assert.Contains("7", attackers.Find("#tSummary").TextContent);
        Assert.Contains("Army", attackers.Find("#tSummary").TextContent);
        Assert.Contains("3", defenders.Find("#tSummary").TextContent);
        Assert.Contains("Corps/Fort", defenders.Find("#tSummary").TextContent);
    }

    [Fact]
    public void ReserveCheckbox_OnlyForArmies()
    {
        IRenderedComponent<PoGUnitListInput> cut = RenderList(new([new PoGUnit("FR_ARMY"), new PoGUnit("FR_CORPS")]));

        Assert.Single(cut.FindAll("#tUnit0Reserve"));
        Assert.Empty(cut.FindAll("#tUnit1Reserve"));
    }

    [Fact]
    public void Add_Remove_Reduce_ChangeType_EmitNewLists()
    {
        IRenderedComponent<PoGUnitListInput> cut = RenderList(new([new PoGUnit("FR_ARMY")]));

        cut.Find("#tAddUnit").Click();
        Assert.Equal(2, _emitted!.Count);

        cut.Find("#tUnit0Reduced").Change(true);
        Assert.True(_emitted![0].Reduced);

        cut.Find("#tUnit0Type").Change("RU_CORPS");
        Assert.Equal("RU_CORPS", _emitted![0].TypeId);

        cut.Find("#tUnit0Remove").Click();
        Assert.Empty(_emitted!);
    }

    [Theory]
    [InlineData(PoGFaction.CentralPowers, "GE_ARMY", "FR_ARMY")]
    [InlineData(PoGFaction.AlliedPowers, "FR_ARMY", "GE_ARMY")]
    public void OnlyTheSidesOwnUnitsAreOffered_AndAddUsesItsDefault(PoGFaction faction, string own, string other)
    {
        IRenderedComponent<PoGUnitListInput> cut = RenderList(new([new PoGUnit(own)]), faction: faction);

        List<string?> offered = [.. cut.FindAll("#tUnit0Type option").Select(o => o.GetAttribute("value"))];
        Assert.Contains(own, offered);
        Assert.DoesNotContain(other, offered);
        Assert.Equal(PoGUnitCatalog.ForFaction(faction).Count, offered.Count);

        cut.Find("#tAddUnit").Click();
        Assert.Equal(own, _emitted![1].TypeId);
    }

    [Fact]
    public void AddButton_HiddenAtTheUnitLimit()
    {
        IRenderedComponent<PoGUnitListInput> cut = RenderList(
            new([new PoGUnit("FR_ARMY"), new PoGUnit("FR_ARMY"), new PoGUnit("FR_ARMY")]), maxUnits: PoGDetailedForces.MaxDefenders);

        Assert.Empty(cut.FindAll("#tAddUnit"));
    }
}
