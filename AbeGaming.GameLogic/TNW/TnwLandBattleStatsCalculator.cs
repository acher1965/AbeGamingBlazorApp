namespace AbeGaming.GameLogic.TNW
{
    /// <summary>Picks exact enumeration when it is affordable, Monte Carlo otherwise.</summary>
    public static class TnwLandBattleStatsCalculator
    {
        public static TnwLandBattleStats Calculate(TnwLandBattle battle, long exactWorkBudget = TnwLandBattleExactStats.DefaultWorkBudget)
        {
            return TnwLandBattleExactStats.TryCalculate(battle, exactWorkBudget, out TnwLandBattleStats? exact)
                ? exact!
                : TnwLandBattleMonteCarlo.Run(battle);
        }
    }
}
