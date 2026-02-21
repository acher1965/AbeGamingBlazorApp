namespace AbeGaming.GameLogic.FtP
{
    public record FtpStats(
    BattleSize BattleSize,
    double AttackerWinProbability,
    double DefenderWinProbability,
    HitStats HitsStats,
    double AttackerLeaderDeathProbability,
    double DefenderLeaderDeathProbability,
    double StarResultProbability,
    double AttackerEliteLossProbability,
    double DefenderEliteLossProbability);
}
