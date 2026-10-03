namespace AbeGaming.GameLogic.TNW
{
    public static class TnwNavalBattleInputRules
    {
        public static int ClampSquadrons(int value) => Math.Clamp(value, 0, TnwFleetComposition.MaxCount);

        /// <summary>
        /// The Active Fleet needs at least one Squadron; so does the Inactive Fleet at sea. A
        /// Port may be empty - its shore batteries still fight (13.5).
        /// </summary>
        public static bool IsBattleDefinitionConsistent(TnwNavalBattle battle, out string? errorMessage)
        {
            if (battle.Active.TotalSquadrons == 0)
            {
                errorMessage = "The Active Fleet needs at least one Squadron.";
                return false;
            }

            if (!battle.Location.IsPort() && battle.Inactive.TotalSquadrons == 0)
            {
                errorMessage = "At sea the Inactive Fleet needs at least one Squadron.";
                return false;
            }

            if (battle.Location.IsPort() && (battle.ActiveGetsEvasionDie || battle.InactiveGetsEvasionDie))
            {
                errorMessage = "Fleets in Port cannot evade, so there is no evasion die in a Port battle.";
                return false;
            }

            if (battle.ActiveHasFischer && battle.Active.Total(TnwNavalNation.Denmark) == 0)
            {
                errorMessage = "Admiral Fischer requires at least one Danish Squadron in the Active Fleet.";
                return false;
            }

            if (battle.InactiveHasFischer && battle.Inactive.Total(TnwNavalNation.Denmark) == 0)
            {
                errorMessage = "Admiral Fischer requires at least one Danish Squadron in the Inactive Fleet.";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
