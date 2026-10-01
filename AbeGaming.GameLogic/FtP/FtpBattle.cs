namespace AbeGaming.GameLogic.FtP
{
    /// <param name="IsDivisionMove">
    /// The attacker is a Division move (1-3 SPs, no general). Only an Army or Corps move can
    /// continue moving after winning a battle (rules 5.73, 7.32).
    /// </param>
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
        FtpAmphibious? Amphibious,
        bool IsDivisionMove = false);
}
