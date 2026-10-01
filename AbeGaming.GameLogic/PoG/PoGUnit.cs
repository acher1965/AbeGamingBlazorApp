namespace AbeGaming.GameLogic.PoG
{
    /// <summary>One combat unit in a detailed-mode battle.</summary>
    /// <param name="TypeId">The unit type, see <see cref="PoGUnitCatalog"/>.</param>
    /// <param name="Reduced">The unit is on its reduced side.</param>
    /// <param name="ReserveCorpsAvailable">
    /// For an Army: a full strength replacement Corps is in the Reserve Box (12.4.4). Ignored for a Corps.
    /// </param>
    public readonly record struct PoGUnit(string TypeId, bool Reduced = false, bool ReserveCorpsAvailable = true)
    {
        public PoGUnitType Type => PoGUnitCatalog.Get(TypeId);
    }
}
