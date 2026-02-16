namespace AbeGamingBlazorApp.FtpBattle
{
    public record FTPBattleResult(Winner Winner,
        double DamageToDefender,
        double DamageToAttacker,
        bool AttackerLeaderDeath,
        bool DefenderLeaderDeath,
        bool AttackerCanStay,
        bool AttackerCanContinueMoving,
        BattleSize BattleSize);
}
