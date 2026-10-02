using AbeGamingBlazorApp.Layout;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AbeGaming.BlazorApp.Component.Tests;

public class NavMenuTests : BunitContext
{
    private static readonly string[] TnwLinks = ["tnwbattle", "tnwnaval", "tnwsiege"];

    private static List<string> TnwHrefs(IRenderedComponent<NavMenu> cut) =>
        [.. cut.FindAll("a.nav-link").Select(a => a.GetAttribute("href") ?? string.Empty).Where(h => TnwLinks.Contains(h))];

    [Fact]
    public void TnwPages_AreOneGroup_ClosedOutsideTnw_OpenedByItsButton()
    {
        IRenderedComponent<NavMenu> cut = Render<NavMenu>();

        Assert.Empty(TnwHrefs(cut));
        Assert.Equal("false", cut.Find("#navTnwToggle").GetAttribute("aria-expanded"));

        cut.Find("#navTnwToggle").Click();

        Assert.Equal(TnwLinks, TnwHrefs(cut));
        Assert.Equal("true", cut.Find("#navTnwToggle").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void TnwGroup_OpensByItself_OnATnwPage()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        IRenderedComponent<NavMenu> cut = Render<NavMenu>();

        navigation.NavigateTo("tnwsiege");

        cut.WaitForAssertion(() => Assert.Equal(TnwLinks, TnwHrefs(cut)));
    }

    [Fact]
    public void MainLayout_UsesTheCompactNav_WhileAPageAsksForIt()
    {
        LayoutState state = new();
        Services.AddSingleton(state);
        IRenderedComponent<MainLayout> cut = Render<MainLayout>();

        Assert.DoesNotContain("compact-nav-landscape", cut.Find(".page").ClassList);

        state.CompactNavOnLandscapePhone = true;
        cut.WaitForAssertion(() => Assert.Contains("compact-nav-landscape", cut.Find(".page").ClassList));

        state.CompactNavOnLandscapePhone = false;
        cut.WaitForAssertion(() => Assert.DoesNotContain("compact-nav-landscape", cut.Find(".page").ClassList));
    }
}
