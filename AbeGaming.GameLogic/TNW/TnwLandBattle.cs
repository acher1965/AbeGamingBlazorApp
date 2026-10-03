namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// One side of a Land Battle (rule 11). Unlike a Siege there is no Command Rating cap:
    /// every piece in the Duchy fights (9.7), and leaders other than the Commander count as
    /// one Unit each (11.2).
    /// </summary>
    /// <param name="Units">Units present, including non-Commander leaders, excluding the Commander.</param>
    /// <param name="CommanderBattleRating">The Commander's printed Battle Rating (1 to 4), or 0 if there is no leader.</param>
    /// <param name="Composition">Nationality mix, for the nationality bonus dice (11.21).</param>
    /// <param name="EventDice">Battle event dice for the first Round (+/-); events affect only one Round (11.1).</param>
    public record TnwBattleSide(
        int Units,
        int CommanderBattleRating,
        TnwForceComposition Composition,
        int EventDice)
    {
        public bool HasCommander => CommanderBattleRating > 0;

        /// <summary>All Units and leaders on this side, including the Commander.</summary>
        public int Pieces => Units + (HasCommander ? 1 : 0);
    }

    /// <summary>
    /// Input for a Land Battle (rule 11). The attacker is the Active Formation.
    /// </summary>
    /// <param name="Terrain">Terrain the attacker crossed; defender's first-Round bonus (11.22). Not applicable to an Amphibious Assault - a landing crosses no rough/pass/marsh line.</param>
    /// <param name="FailedEvasions">Failed defender evasion attempts: +1 attacker die each in the first Round, and the defender forfeits any terrain bonus (10.2, 11.22).</param>
    /// <param name="DefenderCannotRetreat">A defender that loses and cannot retreat is eliminated (11.42), an Overrun (11.6).</param>
    /// <param name="DefenderWithoutArmyGroup">Defending Armies not formed into an Army Group defend with a Commander Battle Rating of 0, but the printed rating still counts if routed (9.7).</param>
    /// <param name="AmphibiousLanding">
    /// If the attacker arrived by Amphibious Assault (13.7), the Port it landed in: its shore
    /// batteries fire once at the attacker before Round 1, and no further times once the battle
    /// itself begins (unlike a naval Port battle, 13.5).
    /// </param>
    public record TnwLandBattle(
        TnwBattleSide Attacker,
        TnwBattleSide Defender,
        TnwTerrain Terrain,
        int FailedEvasions,
        bool DefenderCannotRetreat,
        bool DefenderWithoutArmyGroup,
        TnwAmphibiousLanding AmphibiousLanding = TnwAmphibiousLanding.None);
}
