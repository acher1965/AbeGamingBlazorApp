using AbeGaming.GameLogic.TNW;
using AbeGamingBlazorApp.Components;
using Bunit;

namespace AbeGaming.BlazorApp.Component.Tests;

public class TnwLandBattleStatsDisplayTests : BunitContext
{
    [Fact]
    public void ExactStats_ShowWinProbabilitiesAndLossBadges()
    {
        TnwLandBattle battle = new(
            new TnwBattleSide(4, 2, TnwForceComposition.Power, 0),
            new TnwBattleSide(3, 1, TnwForceComposition.Power, 0),
            TnwTerrain.None, 0, false, false);
        TnwLandBattleStats stats = TnwLandBattleStatsCalculator.Calculate(battle);

        IRenderedComponent<TnwLandBattleStatsDisplay> cut = Render<TnwLandBattleStatsDisplay>(parameters => parameters
            .Add(p => p.Stats, stats)
            .Add(p => p.Title, "Exact Stats"));

        Assert.Contains("Attacker Wins", cut.Markup);
        Assert.Contains("Defender Wins", cut.Markup);
        Assert.NotEmpty(cut.FindAll(".dist-badge"));
        Assert.Contains(cut.FindAll(".info-tip-content"), e => e.GetAttribute("title") == TnwLandBattleStatsDisplay.SecondRoundTip);
    }
}
