namespace AbeGamingBlazorApp.FtpBattle
{
    public static class FtpCRT
    {
        /// <summary>
        /// Calculates hits to defender and attacker based on the battle conditions and die rolls.
        /// </summary>
        /// <param name="battle"></param>
        /// <param name="attackerDieRoll"></param>
        /// <param name="defenderDieRoll"></param>
        /// <returns>star already takes ResourceOrCapital into account</returns>
        public static (int hitsToD, int hitsToA, bool star, int leaderDeathTopD, int leaderDeathTopA) Outcome(
            FtpLandBattle battle,
            int attackerDieRoll,
            int defenderDieRoll)
        {
            BattleSize battleSize = battle.Size();
            (Ratio ratio, bool inAttackerFavour) = battle.RatioDRM();

            if (battle.IsOverrun())
                return (battle.DefenderSize, 0, false, 0, 0);

            // Calculate attacker's DRM: leader DRM + elites + opponent OOS bonus
            int totalAttackerDRM = (inAttackerFavour ? (int)ratio : 0)
                + (battle.IsInterception ? 2 : 0)
                + battle.AttackerLeadersDRMIncludingCavalryIntelligence
                + battle.AttackerElitesCommitted
                + (battle.DefenderOOS ? 2 : 0);

            // Apply DRM to die roll and clamp to valid range (1-10+)
            int modifiedRollA = attackerDieRoll + totalAttackerDRM;
            int tableIndex = Math.Clamp(modifiedRollA - 1, 0, 9);

            int hitsToDefender = tablesHitToD[battleSize][tableIndex];
            bool star = !battle.ResourceOrCapital && tableStar[battleSize][tableIndex];

            int totalDefenderDRM = (inAttackerFavour ? 0 : (int)ratio)
                + (battle.FortPresent ? 2 : 0)
                + battle.DefenderLeadersDRMIncludingCavalryIntelligence
                + battle.DefenderElitesCommitted
                + (battle.IsInterception ? 2 : 0)
                + (battle.AttackerOOS ? 2 : 0);
            int modifiedRollD = defenderDieRoll + totalDefenderDRM;
            tableIndex = Math.Clamp(modifiedRollD - 1, 0, 9);
            int hitsToAttacker = tablesHitToA[battleSize][tableIndex];

            // Leaders death changes: determine leader death roll threshold
            // Rolled modified 10 or greater: Leader killed on 1-3
            // Rolled less than 10: Leader killed on 1
            int defenderLeaderDeathTop = (!inAttackerFavour && ratio > Ratio.Low)
                ?0
                :modifiedRollD >= 10
                    ? 3 
                    : modifiedRollA >= 10
                        ? 1                    : 0;
            int attackerLeaderDeathTop = (inAttackerFavour && ratio > Ratio.Low)
                ? 0
                : modifiedRollA >= 10
                    ? 3
                    : modifiedRollD >= 10
                        ? 1
                        : 0;

            return (hitsToDefender, hitsToAttacker, star, defenderLeaderDeathTop, attackerLeaderDeathTop);
        }

        // Def column: Attacker's Roll -> Defender's Result (hits to defender)
        // Index 0-9 = die roll 1-10+
        private static readonly Dictionary<BattleSize, int[]> tablesHitToD = new()
        {
            [BattleSize.Small] = [0, 0, 0, 1, 1, 1, 1, 1, 1, 1],
            [BattleSize.Medium] = [0, 1, 1, 1, 1, 2, 2, 2, 2, 3],
            [BattleSize.Large] = [1, 2, 2, 3, 3, 3, 4, 4, 5, 5]
        };
        // Att column: Defender's Roll -> Attacker's Result (hits to attacker)
        // Index 0-9 = die roll 1-10+
        private static readonly Dictionary<BattleSize, int[]> tablesHitToA = new()
        {
            [BattleSize.Small] = [0, 1, 1, 1, 1, 1, 1, 1, 1, 2],
            [BattleSize.Medium] = [1, 1, 1, 1, 1, 1, 2, 3, 3, 3],
            [BattleSize.Large] = [1, 2, 3, 3, 3, 4, 4, 4, 5, 6]
        };
        private static readonly Dictionary<BattleSize, bool[]> tableStar = new()
        {
            [BattleSize.Small] = [false, false, false, false, false, false, true, true, true, true],
            [BattleSize.Medium] = [false, false, false, false, false, false, true, true, true, true],
            [BattleSize.Large] = [false, false, false, false, false, false, false, true, true, true]
        };
    }
}
