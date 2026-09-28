namespace AbeGaming.GameLogic.TNW
{
    /// <summary>Picks exact enumeration when it is affordable, Monte Carlo otherwise.</summary>
    public static class TnwNavalBattleStatsCalculator
    {
        public static TnwNavalBattleStats Calculate(TnwNavalBattle battle, long exactWorkBudget = TnwNavalBattleExactStats.DefaultWorkBudget)
        {
            return TnwNavalBattleExactStats.TryCalculate(battle, exactWorkBudget, out TnwNavalBattleStats? exact)
                ? exact!
                : TnwNavalBattleMonteCarlo.Run(battle);
        }
    }
}
