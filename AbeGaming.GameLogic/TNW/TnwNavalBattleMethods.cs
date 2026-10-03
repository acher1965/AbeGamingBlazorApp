using System.Collections.Concurrent;

namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// Round-by-round resolution of a naval battle (rules 13.4, 13.5). The pure steps
    /// (<see cref="ShoreBatteryFire"/>, <see cref="DiceForRound"/>, <see cref="ApplyHits"/>,
    /// <see cref="Verdict"/>, <see cref="Conclude"/>) are shared by the exact enumeration,
    /// the Monte Carlo simulation and the single roll, so all three agree by construction.
    /// </summary>
    public static class TnwNavalBattleMethods
    {
        private const int DieFaces = 6;

        public static TnwFleetState InitialState(TnwFleetComposition fleet) => new(fleet, FivesReceived: 0, HitsReceived: 0);

        /// <summary>
        /// Battle dice for one Fleet this Round (13.4): its Squadrons' dice (Refit ones one
        /// less, Danish ones one more under "Gallant Danes" - Admiral Fischer), minus one per
        /// "5" it has received, plus - Round 1 only - one die if the enemy failed to evade it.
        /// In a Port battle the Inactive side also rolls the shore batteries (13.5); assumption:
        /// the batteries are not a Fleet, so "5"s never reduce them.
        /// </summary>
        public static int DiceForRound(TnwNavalBattle battle, bool isActive, TnwFleetState state, int round)
        {
            bool fischer = isActive ? battle.ActiveHasFischer : battle.InactiveHasFischer;
            int baseDice = fischer ? state.Remaining.DiceWithFischer : state.Remaining.Dice;
            int dice = Math.Max(0, baseDice - state.FivesReceived);

            bool evasionDie = isActive ? battle.ActiveGetsEvasionDie : battle.InactiveGetsEvasionDie;
            if (round == 1 && evasionDie)
                dice++;

            if (!isActive)
                dice += battle.Location.ShoreBatteryDice();

            return dice;
        }

        /// <summary>
        /// Applies the "6"s and "5"s rolled against a Fleet (13.4). Each "6" sinks one
        /// Squadron of the owner's choice, split as evenly as possible across nations: among
        /// the nations that have lost fewest Squadrons, the owner sinks the Squadron that rolls
        /// the fewest dice (Refit ones first) - an assumption, matching the rulebook's example
        /// where the first loss is a Spanish rather than a French Squadron. Sixes beyond the
        /// last Squadron have no effect. Each "5" costs the Fleet one die in later Rounds.
        /// "Gallant Danes" (Admiral Fischer) voids one "6" against the Fleet, once per battle,
        /// before anything else is applied.
        /// </summary>
        public static TnwFleetState ApplyHits(TnwFleetComposition initial, TnwFleetState state, int sixes, int fives, bool fischerActive = false)
        {
            bool sixVoided = state.FischerSixVoided;
            if (fischerActive && !sixVoided && sixes > 0)
            {
                sixes--;
                sixVoided = true;
            }

            // The allocation is deterministic, so the Fleet after k sinkings is precomputed once
            // per starting Fleet; this keeps the exact enumeration and Monte Carlo fast.
            TnwFleetComposition[] afterSinkings = LossSequence(initial);
            int sunkSoFar = initial.TotalSquadrons - state.Remaining.TotalSquadrons;
            int sunk = Math.Min(afterSinkings.Length - 1, sunkSoFar + sixes);

            return new TnwFleetState(afterSinkings[sunk], state.FivesReceived + fives, state.HitsReceived + sixes + fives, sixVoided);
        }

        private const int MaxCachedLossSequences = 256;
        private static readonly ConcurrentDictionary<TnwFleetComposition, TnwFleetComposition[]> LossSequences = new();

        /// <summary>The Fleet after 0, 1, 2 ... sinkings, following the allocation rule in <see cref="ApplyHits"/>.</summary>
        private static TnwFleetComposition[] LossSequence(TnwFleetComposition initial)
        {
            if (LossSequences.TryGetValue(initial, out TnwFleetComposition[]? cached))
                return cached;

            if (LossSequences.Count >= MaxCachedLossSequences)
                LossSequences.Clear();

            TnwFleetComposition[] sequence = new TnwFleetComposition[initial.TotalSquadrons + 1];
            sequence[0] = initial;
            for (int sunk = 1; sunk < sequence.Length; sunk++)
                sequence[sunk] = SinkOne(initial, sequence[sunk - 1]);

            LossSequences[initial] = sequence;
            return sequence;
        }

        /// <summary>
        /// Pre-battle shore battery fire on the Active Fleet entering an enemy Port (13.5).
        /// Its hits also count in Round 1's casualty total ("one combined Round"), and its
        /// "5"s already reduce the Active Fleet's Round 1 dice.
        /// </summary>
        public static TnwFleetState ShoreBatteryFire(TnwNavalBattle battle, TnwFleetState active, int sixes, int fives) =>
            ApplyHits(battle.Active, active, sixes, fives, battle.ActiveHasFischer);

        private static bool Eliminated(TnwFleetComposition initial, TnwFleetState state) =>
            initial.TotalSquadrons > 0 && state.Remaining.TotalSquadrons == 0;

        /// <summary>
        /// Decides the battle after a Round, as for land battles (13.4, 11.32): a Fleet
        /// wiped out loses if the other is not; otherwise the Fleet that suffered more
        /// casualties (as rolled) loses. A tie after Round 1 means a second Round only if both
        /// Fleets still exist (13.5); otherwise, and after a second tie, the Active Fleet loses.
        /// </summary>
        public static TnwNavalRoundVerdict Verdict(TnwNavalBattle battle, TnwFleetState active, TnwFleetState inactive, int round)
        {
            bool activeEliminated = Eliminated(battle.Active, active);
            bool inactiveEliminated = Eliminated(battle.Inactive, inactive);

            if (activeEliminated && !inactiveEliminated)
                return TnwNavalRoundVerdict.InactiveWins;
            if (inactiveEliminated && !activeEliminated)
                return TnwNavalRoundVerdict.ActiveWins;
            if (active.HitsReceived > inactive.HitsReceived)
                return TnwNavalRoundVerdict.InactiveWins;
            if (inactive.HitsReceived > active.HitsReceived)
                return TnwNavalRoundVerdict.ActiveWins;

            bool bothFleetsExist = active.Remaining.TotalSquadrons > 0 && inactive.Remaining.TotalSquadrons > 0;
            return round == 1 && bothFleetsExist ? TnwNavalRoundVerdict.SecondRound : TnwNavalRoundVerdict.InactiveWins;
        }

        /// <summary>
        /// Applies the aftermath: if the Port loses a Port battle, the defending Fleet is
        /// eliminated (13.5). There are no routs in naval battles (13.4).
        /// </summary>
        public static TnwNavalBattleOutcome Conclude(TnwNavalBattle battle, TnwFleetState active, TnwFleetState inactive, int rounds, bool activeWins)
        {
            if (activeWins && battle.Location.IsPort())
                inactive = inactive with { Remaining = TnwFleetComposition.Empty };

            return new TnwNavalBattleOutcome(
                ActiveWins: activeWins,
                Rounds: rounds,
                ActiveSquadronsLost: battle.Active.TotalSquadrons - active.Remaining.TotalSquadrons,
                InactiveSquadronsLost: battle.Inactive.TotalSquadrons - inactive.Remaining.TotalSquadrons,
                ActiveEliminated: Eliminated(battle.Active, active),
                InactiveEliminated: Eliminated(battle.Inactive, inactive));
        }

        /// <summary>
        /// Resolves one battle with random dice. <paramref name="log"/>, when not null,
        /// receives each Round's dice (Round 0 is pre-battle shore battery fire).
        /// </summary>
        public static TnwNavalBattleOutcome Simulate(TnwNavalBattle battle, Random random, List<TnwNavalRoundLog>? log)
        {
            TnwFleetState active = InitialState(battle.Active);
            TnwFleetState inactive = InitialState(battle.Inactive);

            int shoreDice = battle.Location.ShoreBatteryDice();
            if (shoreDice > 0)
            {
                (int shoreSixes, int shoreFives) = RollSixesAndFives(random, shoreDice);
                log?.Add(new TnwNavalRoundLog(0, 0, 0, 0, shoreDice, shoreSixes, shoreFives));
                active = ShoreBatteryFire(battle, active, shoreSixes, shoreFives);
                if (active.Remaining.TotalSquadrons == 0)
                    return Conclude(battle, active, inactive, rounds: 0, activeWins: false);
            }

            for (int round = 1; ; round++)
            {
                int activeDice = DiceForRound(battle, isActive: true, active, round);
                int inactiveDice = DiceForRound(battle, isActive: false, inactive, round);
                (int activeSixes, int activeFives) = RollSixesAndFives(random, activeDice);
                (int inactiveSixes, int inactiveFives) = RollSixesAndFives(random, inactiveDice);
                log?.Add(new TnwNavalRoundLog(round, activeDice, activeSixes, activeFives, inactiveDice, inactiveSixes, inactiveFives));

                TnwFleetState newInactive = ApplyHits(battle.Inactive, inactive, activeSixes, activeFives, battle.InactiveHasFischer);
                active = ApplyHits(battle.Active, active, inactiveSixes, inactiveFives, battle.ActiveHasFischer);
                inactive = newInactive;

                TnwNavalRoundVerdict verdict = Verdict(battle, active, inactive, round);
                if (verdict != TnwNavalRoundVerdict.SecondRound)
                    return Conclude(battle, active, inactive, round, verdict == TnwNavalRoundVerdict.ActiveWins);
            }
        }

        /// <summary>Resolves one battle with random dice for a "Roll 1 Battle" result.</summary>
        public static TnwNavalBattleRollResult RollOnce(TnwNavalBattle battle)
        {
            List<TnwNavalRoundLog> log = [];
            TnwNavalBattleOutcome outcome = Simulate(battle, Random.Shared, log);
            return new TnwNavalBattleRollResult(outcome, log);
        }

        private static TnwFleetComposition SinkOne(TnwFleetComposition initial, TnwFleetComposition remaining)
        {
            TnwNavalNation? chosen = null;
            int chosenLosses = int.MaxValue;
            int chosenDice = int.MaxValue;

            foreach (TnwNavalNation nation in TnwNavalNationExtensions.All)
            {
                if (remaining.Total(nation) == 0)
                    continue;

                int losses = initial.Total(nation) - remaining.Total(nation);
                int cheapestDice = nation.SquadronDice(underRefit: remaining.Count(nation, true) > 0);
                if (losses < chosenLosses || (losses == chosenLosses && cheapestDice < chosenDice))
                {
                    chosen = nation;
                    chosenLosses = losses;
                    chosenDice = cheapestDice;
                }
            }

            TnwNavalNation sunk = chosen!.Value;
            bool sinkRefit = remaining.Count(sunk, true) > 0;
            return remaining.With(sunk, sinkRefit, remaining.Count(sunk, sinkRefit) - 1);
        }

        private static (int Sixes, int Fives) RollSixesAndFives(Random random, int dice)
        {
            int sixes = 0;
            int fives = 0;
            for (int i = 0; i < dice; i++)
            {
                int face = random.Next(1, DieFaces + 1);
                if (face == 6)
                    sixes++;
                else if (face == 5)
                    fives++;
            }
            return (sixes, fives);
        }
    }
}
