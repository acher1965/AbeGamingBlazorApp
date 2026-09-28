namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// Input for a naval battle (rule 13.4). The Active Fleet is the one moving: at sea
    /// either Fleet may have intercepted, but a tie after two Rounds goes against the
    /// Active Fleet. In a Port battle the Inactive Fleet is the one in the Port and may be
    /// empty - the shore batteries fight anyway (13.5).
    /// </summary>
    /// <param name="ActiveGetsEvasionDie">The Inactive Fleet failed to evade the Active Fleet's Patrol: +1 Active die in Round 1 (13.33).</param>
    /// <param name="InactiveGetsEvasionDie">The Active Fleet failed to evade an interception: +1 Inactive die in Round 1 (13.33).</param>
    public record TnwNavalBattle(
        TnwFleetComposition Active,
        TnwFleetComposition Inactive,
        TnwNavalBattleLocation Location,
        bool ActiveGetsEvasionDie,
        bool InactiveGetsEvasionDie);
}
