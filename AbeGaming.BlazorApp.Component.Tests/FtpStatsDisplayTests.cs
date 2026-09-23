using AbeGaming.GameLogic;
using AbeGaming.GameLogic.FtP;
using AbeGamingBlazorApp.Components;
using AngleSharp.Dom;
using Bunit;

namespace AbeGaming.BlazorApp.Component.Tests;

public class FtpStatsDisplayTests : BunitContext
{
    /// <summary>Stats where every icon-only statistic is non-zero, so all of them are rendered.</summary>
    private static FtpStats AllIconsStats() => new(
        BattleSize.Medium,
        AttackerWinProbability: 0.8,
        DefenderWinProbability: 0.2,
        HitsStats: new HitStats(
            MeanHtoD: 2.0, StdDevHtoD: 0.6, MeanHtoA: 1.2, StdDevHtoA: 0.4,
            HitsToD_Prblty: new Dictionary<int, double> { [1] = 0.25, [2] = 0.75 },
            HitsToA_Prblty: new Dictionary<int, double> { [1] = 0.8, [2] = 0.2 }),
        AttackerLeaderDeathProbability: 0.08,
        DefenderLeaderDeathProbability: 0.03,
        StarResultProbability: 0.67,
        AttackerEliteLossProbability: 0,
        DefenderEliteLossProbability: 0,
        AttackerCanStayProbability: 0.8,
        AttackerCanContinueProbability: 0.4);

    private IRenderedComponent<FtpStatsDisplay> RenderDisplay() =>
        Render<FtpStatsDisplay>(parameters => parameters
            .Add(p => p.Stats, AllIconsStats())
            .Add(p => p.Title, "Exact Stats"));

    [Theory]
    [InlineData(FtpStatsDisplay.StarTip)]
    [InlineData(FtpStatsDisplay.CanStayTip)]
    [InlineData(FtpStatsDisplay.CanContinueTip)]
    [InlineData(FtpStatsDisplay.AttackerLeaderDeathTip)]
    [InlineData(FtpStatsDisplay.DefenderLeaderDeathTip)]
    public void IconStatistics_HaveHoverTooltips(string expectedTip)
    {
        IRenderedComponent<FtpStatsDisplay> cut = RenderDisplay();

        IReadOnlyList<IElement> triggers = cut.FindAll(".info-tip-content")
            .Where(e => e.GetAttribute("title") == expectedTip)
            .ToList();

        Assert.Single(triggers);
    }

    [Fact]
    public void IconStatistic_Tap_ShowsPopoverWithExplanation()
    {
        IRenderedComponent<FtpStatsDisplay> cut = RenderDisplay();

        IElement starTrigger = cut.FindAll(".info-tip")
            .First(e => e.QuerySelector(".info-tip-content")?.GetAttribute("title") == FtpStatsDisplay.StarTip);
        starTrigger.Click();

        Assert.Equal(FtpStatsDisplay.StarTip, cut.Find(".info-popover").TextContent);
    }

    [Fact]
    public void LossDistributionBadges_HaveHoverTooltips()
    {
        IRenderedComponent<FtpStatsDisplay> cut = RenderDisplay();

        List<string?> titles = cut.FindAll(".dist-badge").Select(e => e.GetAttribute("title")).ToList();

        Assert.Equal(4, titles.Count);
        Assert.All(titles, t => Assert.StartsWith("Loses ", t));
    }

    [Fact]
    public void InfoTip_WithoutChildContent_StillRendersInfoIcon()
    {
        IRenderedComponent<InfoTip> cut = Render<InfoTip>(parameters => parameters
            .Add(p => p.Text, "Some help"));

        Assert.Equal("Some help", cut.Find(".info-icon").GetAttribute("title"));
        Assert.Empty(cut.FindAll(".info-tip-content"));
    }
}
