namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// Input for a Siege of a Fortress (rule 12). Only a single Army may Siege (12.2), so a
    /// Commander is always present. <paramref name="Units"/> is every Unit available in the
    /// Duchy (the Army plus any other Armies of the same nationality with it): at most
    /// <paramref name="CommandRating"/> of them roll each Round, and the excess replace the
    /// Army's disrupted/eliminated Units in later Rounds (12.33). Armies are single-nationality,
    /// so <paramref name="Composition"/> applies to the whole force.
    /// </summary>
    /// <param name="Units">Undisrupted Units available, excluding the Commander.</param>
    /// <param name="CommandRating">The Commander's Command Rating (4, 6 or 8): the most Units that roll per Round.</param>
    /// <param name="CommanderBattleRating">The Commander's Battle Rating (1 to 4): bonus dice while he is undisrupted.</param>
    /// <param name="Composition">Nationality of the Army, for the nationality bonus dice (11.21).</param>
    /// <param name="ZoneModifierApplies">Fortress-Port whose owner controls an adjacent Zone: one less die (12.32).</param>
    /// <param name="IsGibraltar">Gibraltar has strength 4 instead of 2 (12.3).</param>
    public record TnwSiegeBattle(
        int Units,
        int CommandRating,
        int CommanderBattleRating,
        TnwForceComposition Composition,
        bool ZoneModifierApplies,
        bool IsGibraltar);
}
