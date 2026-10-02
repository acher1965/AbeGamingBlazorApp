namespace AbeGaming.GameLogic.PoG
{
    /// <summary>Helpers for <see cref="PoGFaction"/>.</summary>
    public static class PoGFactionExtensions
    {
        /// <summary>The other side of the war.</summary>
        public static PoGFaction Opponent(this PoGFaction faction) =>
            faction == PoGFaction.CentralPowers ? PoGFaction.AlliedPowers : PoGFaction.CentralPowers;
    }
}
