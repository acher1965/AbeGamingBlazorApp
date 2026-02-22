using AbeGaming.GameLogic.FtP;

namespace AbeGaming.GameLogic.FtP
{
    public record FtpAmphibious(
        bool IsArmyMove,
        bool Admiral,
        bool Hunley,
        bool Ironclad,
        bool Torpedoes,
        int AmphibiousAssaultLevel)
    {
        /// <summary>
        /// Returns an FtpAmphibious with all default values (all false, level 0)
        /// </summary>
        public static FtpAmphibious Empty { get; } = new(false, false, false, false, false, 0);
    }
}
