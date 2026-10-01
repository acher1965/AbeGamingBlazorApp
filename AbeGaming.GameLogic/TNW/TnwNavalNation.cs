namespace AbeGaming.GameLogic.TNW
{
    /// <summary>The nations that have Squadrons (rule 13.4 dice table).</summary>
    public enum TnwNavalNation
    {
        Britain,
        France,
        Denmark,
        Sweden,
        Russia,
        Spain,
        Ottoman,
    }

    public static class TnwNavalNationExtensions
    {
        /// <summary>Every nation with Squadrons, in display order.</summary>
        public static IReadOnlyList<TnwNavalNation> All { get; } = Enum.GetValues<TnwNavalNation>();

        /// <summary>
        /// Battle dice per Squadron (rule 13.4): three for British, two for French, Danish or
        /// Swedish, one for Russian, Turkish or Spanish.
        /// </summary>
        public static int DicePerSquadron(this TnwNavalNation nation) => nation switch
        {
            TnwNavalNation.Britain => 3,
            TnwNavalNation.France or TnwNavalNation.Denmark or TnwNavalNation.Sweden => 2,
            _ => 1,
        };

        /// <summary>Battle dice for one Squadron; a Squadron under Refit rolls one less, to no less than 0 (13.4).</summary>
        public static int SquadronDice(this TnwNavalNation nation, bool underRefit) =>
            Math.Max(0, nation.DicePerSquadron() - (underRefit ? 1 : 0));

        public static string DisplayName(this TnwNavalNation nation) => nation switch
        {
            TnwNavalNation.Ottoman => "Ottoman Turks",
            _ => nation.ToString(),
        };
    }
}
