namespace AbeGaming.GameLogic.TNW
{
    public static class TnwSiegeInputRules
    {
        public static int ClampUnits(int value) => Math.Clamp(value, 0, 20);

        /// <summary>A leader's Battle Rating is printed as 1 to 4 (glossary, "Battle Rating").</summary>
        public static int ClampCommanderBattleRating(int value) => Math.Clamp(value, 1, 4);
    }
}
