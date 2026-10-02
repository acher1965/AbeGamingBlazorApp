namespace AbeGaming.GameLogic.PoG
{
    /// <summary>Display settings for one side of the war in the detailed calculator mode.</summary>
    /// <param name="Name">Display name, e.g. "Central Powers".</param>
    /// <param name="DefaultUnit">Unit type id used for new units and the starting setup of this side.</param>
    public record PoGFactionInfo(PoGFaction Faction, string Name, string DefaultUnit);
}
