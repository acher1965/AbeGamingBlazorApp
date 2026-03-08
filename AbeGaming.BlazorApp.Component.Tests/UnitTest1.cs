using AbeGamingBlazorApp.Components;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace AbeGaming.BlazorApp.Component.Tests;

public class FtpSideInputTests : BunitContext
{
    [Fact]
    public void OnSizeChanged_AmphibiousNavalAssault_ClampsAttackerToThree()
    {
        int? emittedSize = null;

        IRenderedComponent<FtpSideInput> cut = Render<FtpSideInput>(parameters => parameters
            .Add(p => p.SideName, "Attacker")
            .Add(p => p.Icon, "")
            .Add(p => p.SpInputId, "testAttackerSp")
            .Add(p => p.IsAmphibious, true)
            .Add(p => p.IsArmyMove, false)
            .Add(p => p.MinSize, 1)
            .Add(p => p.MaxSize, 15)
            .Add(p => p.Size, 3)
            .Add(p => p.SizeChanged, EventCallback.Factory.Create<int>(this, value => emittedSize = value)));

        cut.Find("#testAttackerSp").Change("9");

        Assert.Equal(3, emittedSize);
    }

    [Fact]
    public void OnSizeChanged_AmphibiousArmyMove_ClampsAttackerToNine()
    {
        int? emittedSize = null;

        IRenderedComponent<FtpSideInput> cut = Render<FtpSideInput>(parameters => parameters
            .Add(p => p.SideName, "Attacker")
            .Add(p => p.Icon, "")
            .Add(p => p.SpInputId, "testAttackerSp")
            .Add(p => p.IsAmphibious, true)
            .Add(p => p.IsArmyMove, true)
            .Add(p => p.MinSize, 1)
            .Add(p => p.MaxSize, 15)
            .Add(p => p.Size, 3)
            .Add(p => p.SizeChanged, EventCallback.Factory.Create<int>(this, value => emittedSize = value)));

        cut.Find("#testAttackerSp").Change("12");

        Assert.Equal(9, emittedSize);
    }
}
