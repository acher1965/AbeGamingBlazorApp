using AbeGaming.GameLogic.TNW;
using AbeGamingBlazorApp.Components;
using Bunit;

namespace AbeGaming.BlazorApp.Component.Tests;

public class TnwNavalBattleStatsDisplayTests : BunitContext
{
    [Fact]
    public void ExactStats_ShowWinProbabilitiesAndLossBadges()
    {
        TnwNavalBattle battle = new(
            TnwFleetComposition.Empty.WithSquadrons(TnwNavalNation.Britain, 2, 0),
            TnwFleetComposition.Empty.WithSquadrons(TnwNavalNation.France, 2, 0),
            TnwNavalBattleLocation.OpenSea, false, false);
        TnwNavalBattleStats stats = TnwNavalBattleStatsCalculator.Calculate(battle);

        IRenderedComponent<TnwNavalBattleStatsDisplay> cut = Render<TnwNavalBattleStatsDisplay>(parameters => parameters
            .Add(p => p.Stats, stats)
            .Add(p => p.Title, "Exact Stats"));

        Assert.Contains("Active Wins", cut.Markup);
        Assert.Contains("Inactive Wins", cut.Markup);
        Assert.NotEmpty(cut.FindAll(".dist-badge"));
        Assert.Contains(cut.FindAll(".info-tip-content"), e => e.GetAttribute("title") == TnwNavalBattleStatsDisplay.SecondRoundTip);
    }
}
