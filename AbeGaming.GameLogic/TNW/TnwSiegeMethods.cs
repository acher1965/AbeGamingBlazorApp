namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// Round-by-round resolution of a Siege (rule 12.2-12.3). One pure transition
    /// function (<see cref="ResolveRound"/>) is shared by the exact-stats enumeration
    /// (<see cref="TnwSiegeExactStats"/>) and the single-roll simulation
    /// (<see cref="RollOnce"/>), so both agree by construction.
    /// </summary>
    public static class TnwSiegeMethods
    {
        /// <summary>
        /// A Fortress's base strength (rule 12.3): both the number of dice it throws each
        /// Round, and the cumulative sixes the besieger needs to make it fall.
        /// </summary>
        public static int FortressStrength(bool isGibraltar) => isGibraltar ? 4 : 2;

        /// <summary>
        /// Units that roll this Round: the undisrupted Units available, capped at the
        /// Commander's Command Rating. Units beyond the cap wait outside the Army and
        /// replace its losses in later Rounds (rule 12.33).
        /// </summary>
        public static int RollingUnits(TnwSiegeBattle battle, int unitsAvailable) =>
            Math.Min(unitsAvailable, battle.CommandRating);

        /// <summary>
        /// Dice the Besieging Army rolls this Round, given how much of its force is
        /// still undisrupted (rule 11.2, 12.32, 12.33). Assumption: the nationality bonus
        /// applies at full value every Round - Land Battle's discretionary
        /// bonus-cancellation on excess disrupts (11.33) is not carried over to Sieges
        /// (see TNW-FEASIBILITY-2026-09-27.md §7).
        /// </summary>
        public static int BesiegerDiceThisRound(TnwSiegeBattle battle, int unitsAvailable, bool commanderAvailable)
        {
            int dice = RollingUnits(battle, unitsAvailable)
                + (commanderAvailable ? battle.CommanderBattleRating : 0)
                + battle.Composition.BonusDice()
                - (battle.ZoneModifierApplies ? 1 : 0);
            return Math.Max(0, dice);
        }

        /// <summary>
        /// The Besieging Army's state before Round 1: every Unit and the Commander
        /// undisrupted, no sixes accumulated yet.
        /// </summary>
        public static TnwSiegeState InitialState(TnwSiegeBattle battle) => new(
            UnitsAlive: battle.Units,
            UnitsAvailable: battle.Units,
            CommanderAlive: true,
            CommanderAvailable: true,
            CumulativeSixes: 0);

        /// <summary>
        /// Applies one Siege Round's dice results to <paramref name="state"/>.
        /// <paramref name="besiegerSixes"/> is the besieger's own attack roll (only sixes
        /// matter - rule 12.3: "5's have no effect against a Fortress"). <paramref
        /// name="fortressSixes"/>/<paramref name="fortressFives"/> are the Fortress's
        /// defensive fire on the besieger; kills take priority over disrupts when there
        /// isn't enough undisrupted capacity to record both (11.3), and - an assumption,
        /// since the rulebook does not state an order - excess casualties fall on Units
        /// before the Commander. Only the Units that rolled this Round can be hit; Units
        /// waiting beyond the Command Rating are safe until they step in (12.33).
        /// </summary>
        public static TnwSiegeRoundResult ResolveRound(
            TnwSiegeBattle battle,
            TnwSiegeState state,
            int round,
            int besiegerSixes,
            int fortressSixes,
            int fortressFives)
        {
            int strength = FortressStrength(battle.IsGibraltar);

            int rollingUnits = RollingUnits(battle, state.UnitsAvailable);
            int capacity = rollingUnits + (state.CommanderAvailable ? 1 : 0);
            int effectiveKills = Math.Min(fortressSixes, capacity);
            int effectiveDisrupts = Math.Min(fortressFives, capacity - effectiveKills);
            int sufferedThisRound = effectiveKills + effectiveDisrupts;

            int unitsKilled = Math.Min(effectiveKills, rollingUnits);
            bool commanderKilled = effectiveKills > unitsKilled;
            int unitsDisrupted = Math.Min(effectiveDisrupts, rollingUnits - unitsKilled);
            bool commanderDisrupted = !commanderKilled
                && effectiveDisrupts > unitsDisrupted
                && state.CommanderAvailable;

            var newState = new TnwSiegeState(
                UnitsAlive: state.UnitsAlive - unitsKilled,
                UnitsAvailable: state.UnitsAvailable - unitsKilled - unitsDisrupted,
                CommanderAlive: state.CommanderAlive && !commanderKilled,
                CommanderAvailable: state.CommanderAvailable && !commanderKilled && !commanderDisrupted,
                CumulativeSixes: state.CumulativeSixes + besiegerSixes);

            bool besiegersEliminated = newState.UnitsAlive == 0 && !newState.CommanderAlive;

            // 12.3: the Fortress does not fall if the besiegers were eliminated this same Round.
            bool fortressFalls = !besiegersEliminated && newState.CumulativeSixes >= strength;

            // 12.3: "may attack again ... if it has caused more casualties than it has suffered".
            // Assumption: the besieger always continues when the rules permit it (§7).
            bool canContinue = !besiegersEliminated && !fortressFalls
                && besiegerSixes > sufferedThisRound
                && (newState.UnitsAvailable > 0 || newState.CommanderAvailable);

            bool ended = besiegersEliminated || fortressFalls || !canContinue;

            // 12.31: an Overrun is fewer rounds than the Fortress's strength, or more sixes than it needed.
            bool overrun = fortressFalls && (round < strength || newState.CumulativeSixes > strength);

            return new TnwSiegeRoundResult(newState, ended, fortressFalls, overrun, besiegersEliminated, round);
        }

        /// <summary>
        /// Resolves one Siege attempt with actual random dice, round by round, for a
        /// single "Roll 1 Siege" result (mirrors FtP/PoG's single-battle-roll UX).
        /// </summary>
        public static TnwSiegeRollResult RollOnce(TnwSiegeBattle battle)
        {
            int strength = FortressStrength(battle.IsGibraltar);
            TnwSiegeState state = InitialState(battle);
            List<TnwSiegeRoundLog> log = [];
            TnwSiegeRoundResult result;

            int round = 1;
            do
            {
                int besiegerDice = BesiegerDiceThisRound(battle, state.UnitsAvailable, state.CommanderAvailable);
                int besiegerSixes = CountSixes(Dice.RollOneDieRepeatedly(besiegerDice));

                int fortressSixes = 0;
                int fortressFives = 0;
                foreach (int face in Dice.RollOneDieRepeatedly(strength))
                {
                    if (face == 6)
                        fortressSixes++;
                    else if (face == 5)
                        fortressFives++;
                }

                result = ResolveRound(battle, state, round, besiegerSixes, fortressSixes, fortressFives);
                log.Add(new TnwSiegeRoundLog(round, besiegerDice, besiegerSixes, strength, fortressSixes, fortressFives));
                state = result.State;
                round++;
            }
            while (!result.Ended);

            return new TnwSiegeRollResult(
                result.FortressFalls,
                result.Overrun,
                result.BesiegersEliminated,
                result.Round,
                battle.Units - state.UnitsAlive,
                !state.CommanderAlive,
                log);
        }

        private static int CountSixes(Span<int> dice)
        {
            int sixes = 0;
            foreach (int face in dice)
                if (face == 6)
                    sixes++;
            return sixes;
        }
    }
}
