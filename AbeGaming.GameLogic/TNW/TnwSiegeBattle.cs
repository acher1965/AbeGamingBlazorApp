namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// Input for a Siege of a Fortress (rule 12): the Besieging Army's own composition,
    /// plus the properties of the Fortress being attacked. Only a single, non-reinforced
    /// Army is modeled (12.33's mid-siege reserve replacement is out of scope).
    /// </summary>
    public record TnwSiegeBattle(
        int Units,
        bool CommanderPresent,
        int CommanderBattleRating,
        TnwForceComposition Composition,
        bool ZoneModifierApplies,
        bool IsGibraltar);
}
