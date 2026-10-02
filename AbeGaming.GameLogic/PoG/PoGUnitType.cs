using System.Text.Json.Serialization;

namespace AbeGaming.GameLogic.PoG
{
    /// <summary>
    /// One kind of Paths of Glory combat unit: its CF and LF on the full and the reduced side.
    /// </summary>
    /// <param name="Nation">Display group; AUS, CND and PT Corps are grouped with Britain (12.1.11.2).</param>
    /// <param name="ReplacementCorps">
    /// For an Army, the id of the Corps that replaces it when it is eliminated from its reduced
    /// side, if one is in the Reserve Box (12.4.4, 12.4.4.3); null for a Corps.
    /// </param>
    /// <param name="AttackerLossPriority">
    /// Rank in the 12.4.5 loss priority list when attacking (1 takes the first loss before 2, and
    /// so on; equal ranks are the owner's choice); null if the unit is not on the list.
    /// </param>
    public record PoGUnitType(
        string Id,
        [property: JsonRequired] PoGFaction Faction,
        string Nation,
        string Name,
        PoGUnitKind Kind,
        int FullCf,
        int FullLf,
        int ReducedCf,
        int ReducedLf,
        string? ReplacementCorps,
        int? AttackerLossPriority = null);
}
