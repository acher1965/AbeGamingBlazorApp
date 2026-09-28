namespace AbeGaming.GameLogic.TNW
{
    public static class TnwSiegeInputRules
    {
        /// <summary>A Commander's Command Rating is printed as 4, 6 or 8 (glossary, "Command Rating").</summary>
        public static IReadOnlyList<int> CommandRatings { get; } = [4, 6, 8];

        public static int ClampUnits(int value) => Math.Clamp(value, 0, 30);

        /// <summary>A leader's Battle Rating is printed as 1 to 4 (glossary, "Battle Rating").</summary>
        public static int ClampCommanderBattleRating(int value) => Math.Clamp(value, 1, 4);

        /// <summary>Snaps to the nearest printed Command Rating (4, 6 or 8).</summary>
        public static int ClampCommandRating(int value) =>
            CommandRatings.OrderBy(rating => Math.Abs(rating - value)).ThenBy(rating => rating).First();
    }
}
