namespace AbeGaming.GameLogic.PoG
{
    /// <summary>
    /// Combat Results Table (CRT) for Paths of Glory.
    /// 
    /// NOTE: This is a PLACEHOLDER implementation based on general knowledge of the game.
    /// The actual CRT values need to be verified against the official Paths of Glory rules.
    /// 
    /// PoG uses a differential CRT where:
    /// - Column is determined by: Attacker CF - Defender CF (modified by terrain, etc.)
    /// - Row is the die roll (1-6, modified by various factors)
    /// 
    /// Results key:
    /// - AE = Attacker Eliminated (all attacking units take a step loss)
    /// - AL = Attacker Loss (attacker loses steps)
    /// - EX = Exchange (both sides lose steps)
    /// - DL = Defender Loss (defender loses steps)
    /// - DR = Defender Routed (defender loses steps and must retreat)
    /// - BT = Breakthrough (defender loses, attacker may advance and attack again)
    /// </summary>
    public static class PoGCRT
    {
        /// <summary>
        /// Resolves combat and returns the result.
        /// </summary>
        public static PoGCombatResult ResolveCombat(PoGCombat combat, Random rng)
        {
            int dieRoll = rng.Next(1, 7);
            
            // Calculate combat differential
            int differential = CalculateDifferential(combat);
            
            // Calculate DRM
            int drm = CalculateDRM(combat);
            int modifiedRoll = Math.Clamp(dieRoll + drm, 1, 6);
            
            // Look up result on CRT
            var (resultType, attackerLosses, defenderLosses) = LookupCRT(differential, modifiedRoll, combat.CombatType);
            
            bool mustRetreat = resultType == CombatResultType.DefenderRouted || 
                               resultType == CombatResultType.Breakthrough;
            bool breakthrough = resultType == CombatResultType.Breakthrough;
            
            return new PoGCombatResult(
                resultType,
                attackerLosses,
                defenderLosses,
                mustRetreat,
                breakthrough,
                dieRoll,
                modifiedRoll,
                differential);
        }

        /// <summary>
        /// Calculates the combat differential (attacker CF - defender CF, modified by terrain).
        /// </summary>
        private static int CalculateDifferential(PoGCombat combat)
        {
            int attackerCF = combat.AttackerCombatFactors;
            int defenderCF = combat.DefenderCombatFactors;
            
            // Terrain modifiers (these are approximate - verify with rules)
            int terrainMod = combat.DefenderTerrain switch
            {
                Terrain.Clear => 0,
                Terrain.Forest => 1,
                Terrain.Marsh => 2,
                Terrain.Mountain => 2,
                Terrain.Trench => 3,
                Terrain.Fortress => 4,
                _ => 0
            };
            
            // Effective defender strength
            int effectiveDefenderCF = defenderCF + terrainMod;
            
            return attackerCF - effectiveDefenderCF;
        }

        /// <summary>
        /// Calculates die roll modifiers.
        /// </summary>
        private static int CalculateDRM(PoGCombat combat)
        {
            int drm = 0;
            
            // Gas attack (if not countered by gas masks)
            if (combat.AttackerHasGas && !combat.DefenderHasGasMasks)
                drm += 1;
            
            // Siege artillery against fortress
            if (combat.AttackerHasSiegeArtillery && combat.DefenderInFortress)
                drm += 1;
            
            // Air support
            if (combat.AttackerHasAirSupport && !combat.DefenderHasAirSupport)
                drm += 1;
            else if (combat.DefenderHasAirSupport && !combat.AttackerHasAirSupport)
                drm -= 1;
            
            // Leader modifiers
            drm += combat.AttackerLeaderDRM;
            drm -= combat.DefenderLeaderDRM;
            
            return drm;
        }

        /// <summary>
        /// Looks up the combat result on the CRT.
        /// 
        /// NOTE: These values are PLACEHOLDER approximations.
        /// The actual CRT needs to be verified against official rules.
        /// </summary>
        private static (CombatResultType result, int attackerLoss, int defenderLoss) LookupCRT(
            int differential, int roll, CombatType combatType)
        {
            // Clamp differential to reasonable range
            int column = Math.Clamp(differential, -4, 6);
            
            // Simplified CRT - NEEDS VERIFICATION WITH ACTUAL RULES
            // Fire Combat tends to cause fewer losses but less decisive results
            // Assault Combat is more decisive but riskier
            
            if (combatType == CombatType.FireCombat)
            {
                return (column, roll) switch
                {
                    // Very unfavorable (-4 or less)
                    ( <= -4, <= 3) => (CombatResultType.AttackerEliminated, 2, 0),
                    ( <= -4, <= 5) => (CombatResultType.AttackerLoss, 1, 0),
                    ( <= -4, _) => (CombatResultType.Exchange, 1, 1),
                    
                    // Unfavorable (-3 to -1)
                    ( <= -1, <= 2) => (CombatResultType.AttackerLoss, 1, 0),
                    ( <= -1, <= 4) => (CombatResultType.Exchange, 1, 1),
                    ( <= -1, _) => (CombatResultType.DefenderLoss, 0, 1),
                    
                    // Even (0 to 2)
                    ( <= 2, <= 2) => (CombatResultType.Exchange, 1, 1),
                    ( <= 2, <= 4) => (CombatResultType.DefenderLoss, 0, 1),
                    ( <= 2, _) => (CombatResultType.DefenderRouted, 0, 1),
                    
                    // Favorable (3+)
                    (_, <= 2) => (CombatResultType.DefenderLoss, 0, 1),
                    (_, <= 4) => (CombatResultType.DefenderRouted, 0, 1),
                    (_, _) => (CombatResultType.DefenderRouted, 0, 2),
                };
            }
            else // Assault
            {
                return (column, roll) switch
                {
                    // Very unfavorable (-4 or less)
                    ( <= -4, <= 4) => (CombatResultType.AttackerEliminated, 3, 0),
                    ( <= -4, _) => (CombatResultType.AttackerLoss, 2, 1),
                    
                    // Unfavorable (-3 to -1)
                    ( <= -1, <= 2) => (CombatResultType.AttackerLoss, 2, 0),
                    ( <= -1, <= 4) => (CombatResultType.Exchange, 1, 1),
                    ( <= -1, _) => (CombatResultType.DefenderLoss, 1, 2),
                    
                    // Even (0 to 2)
                    ( <= 2, <= 2) => (CombatResultType.Exchange, 1, 1),
                    ( <= 2, <= 4) => (CombatResultType.DefenderRouted, 0, 2),
                    ( <= 2, _) => (CombatResultType.Breakthrough, 0, 2),
                    
                    // Favorable (3+)
                    (_, <= 2) => (CombatResultType.DefenderRouted, 0, 2),
                    (_, <= 4) => (CombatResultType.Breakthrough, 0, 2),
                    (_, _) => (CombatResultType.Breakthrough, 0, 3),
                };
            }
        }
    }
}
