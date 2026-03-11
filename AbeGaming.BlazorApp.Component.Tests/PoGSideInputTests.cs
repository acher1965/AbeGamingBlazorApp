using AbeGaming.GameLogic.PoG;
using AbeGamingBlazorApp.Components;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace AbeGaming.BlazorApp.Component.Tests;

public class PoGSideInputTests : BunitContext
{
    [Fact]
    public void OnFactorsChanged_ClampsToUpperBound()
    {
        int? emittedFactors = null;

        IRenderedComponent<PoGSideInput> cut = Render<PoGSideInput>(parameters => parameters
            .Add(p => p.SideName, "Attacker")
            .Add(p => p.Icon, "")
            .Add(p => p.FactorsInputId, "testFactors")
            .Add(p => p.Factors, 5)
            .Add(p => p.FactorsChanged, EventCallback.Factory.Create<int>(this, value => emittedFactors = value)));

        cut.Find("#testFactors").Change("999");

        Assert.Equal(16, emittedFactors);
    }

    [Fact]
    public void OnDRMChanged_ClampsToLowerBound()
    {
        int? emittedDrm = null;

        IRenderedComponent<PoGSideInput> cut = Render<PoGSideInput>(parameters => parameters
            .Add(p => p.SideName, "Defender")
            .Add(p => p.Icon, "")
            .Add(p => p.DRM, 0)
            .Add(p => p.DRMChanged, EventCallback.Factory.Create<int>(this, value => emittedDrm = value)));

        cut.FindAll("input[type='number']")[1].Change("-99");

        Assert.Equal(0, emittedDrm);
    }

    [Fact]
    public void OnFireTableChanged_EmitsSelectedValue()
    {
        FireTable? emitted = null;

        IRenderedComponent<PoGSideInput> cut = Render<PoGSideInput>(parameters => parameters
            .Add(p => p.FireTable, FireTable.Corps)
            .Add(p => p.FireTableChanged, EventCallback.Factory.Create<FireTable>(this, value => emitted = value)));

        cut.Find("select").Change("Army");

        Assert.Equal(FireTable.Army, emitted);
    }
}
