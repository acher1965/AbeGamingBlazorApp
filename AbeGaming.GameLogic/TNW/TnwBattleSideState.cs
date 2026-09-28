namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// One side's state during a Land Battle. Disrupted Units are out of the battle but
    /// not lost (11.44); kills are lasting. <see cref="KillsReceived"/> and
    /// <see cref="HitsReceived"/> count the results rolled against this side, uncapped:
    /// the victor is decided on rolled casualties (11.32), and Flag Overrun on rolled
    /// kills exceeding the pieces present (11.7).
    /// </summary>
    public readonly record struct TnwBattleSideState(
        int UnitsAlive,
        int UnitsUndisrupted,
        bool CommanderAlive,
        bool CommanderUndisrupted,
        int BonusDiceCancelled,
        int KillsReceived,
        int HitsReceived)
    {
        public int UnitsDisrupted => UnitsAlive - UnitsUndisrupted;

        /// <summary>True once every Unit and leader on this side has been killed.</summary>
        public bool Eliminated => UnitsAlive == 0 && !CommanderAlive;
    }

    /// <summary>What a Round's results mean for the battle (rule 11.32).</summary>
    public enum TnwRoundVerdict
    {
        AttackerWins,
        DefenderWins,
        SecondRound,
    }
}
