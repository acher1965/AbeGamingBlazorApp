namespace AbeGamingBlazorApp.FtpBattle
{
    public record FTPBattleResult(
        Winner Winner,
        int DamageToDefender,
        int DamageToAttacker,
        bool AttackerLeaderDeath,
        bool DefenderLeaderDeath,
        bool AttackerCanStay,
        bool AttackerCanContinueMoving,
        BattleSize BattleSize,
        int AttackerDieRoll,
        int DefenderDieRoll,
        bool Star,
        int? AttackerLeaderDeathDieRoll,
        int? DefenderLeaderDeathDieRoll);
}
