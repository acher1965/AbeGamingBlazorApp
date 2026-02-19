namespace AbeGaming.GameLogic.FtP
{
    public record FtpBattle(
        bool ResourceOrCapital,
        bool FortPresent,
        bool IsInterception,
        bool IsDefenderLeaderPresent,
        int AttackerSize,
        int DefenderSize,
        int AttackerLeadersDRMIncludingCavalryIntelligence,
        int DefenderLeadersDRMIncludingCavalryIntelligence,
        int AttackerElitesCommitted,
        int DefenderElitesCommitted,
        bool AttackerOOS,
        bool DefenderOOS,
        bool IsAmphibious);
}
