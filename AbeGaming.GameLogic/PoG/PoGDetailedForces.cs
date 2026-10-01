namespace AbeGaming.GameLogic.PoG
{
    /// <summary>
    /// Both sides' units in a detailed-mode battle. When present on a <see cref="PoGBattle"/>,
    /// each side's Combat Strength and Fire Table come from its units (plus the fort for the
    /// defender), and losses are taken step by step from the units (12.4).
    /// </summary>
    public record PoGDetailedForces(EquatableList<PoGUnit> Attackers, EquatableList<PoGUnit> Defenders)
    {
        public const int MaxAttackers = 9;
        public const int MaxDefenders = 3;

        public PoGDetailedForces(IEnumerable<PoGUnit> attackers, IEnumerable<PoGUnit> defenders)
            : this(new EquatableList<PoGUnit>(attackers), new EquatableList<PoGUnit>(defenders))
        {
        }
    }
}
