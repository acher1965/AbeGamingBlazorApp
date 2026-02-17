namespace AbeGaming.GameLogic.PoG
{
    /// <summary>
    /// Combat Results Table (CRT) for Paths of Glory.
    /// 
    /// NOTE: This is a PLACEHOLDER implementation based on general knowledge of the game.
    /// </summary>
    public static class PoGCRT
    {
        public static PoGCombatResult Outcome(PoGCombat combat, Random rng)
        {
            int attackerDieRoll = rng.Next(1, 7);
            int defenderDieRoll = rng.Next(1, 7);

            return new PoGCombatResult(0, 0, 0, attackerDieRoll, defenderDieRoll, 1, 1, 0);
        }
    }
}
