namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// Round-by-round resolution of a Land Battle (rule 11). The pure steps
    /// (<see cref="DiceForRound"/>, <see cref="ApplyHits"/>, <see cref="Verdict"/>,
    /// <see cref="Conclude"/>) are shared by the exact enumeration
    /// (<see cref="TnwLandBattleExactStats"/>), the Monte Carlo simulation
    /// (<see cref="TnwLandBattleMonteCarlo"/>) and the single roll (<see cref="RollOnce"/>),
    /// so all three agree by construction.
    /// </summary>
    public static class TnwLandBattleMethods
    {
        /// <summary>A victor inflicting at least this many more casualties than he suffered routs the loser (11.5).</summary>
        public const int RoutMargin = 3;

        private const int DieFaces = 6;

        public static TnwBattleSideState InitialState(TnwBattleSide side) => new(
            UnitsAlive: side.Units,
            UnitsUndisrupted: side.Units,
            CommanderAlive: side.HasCommander,
            CommanderUndisrupted: side.HasCommander,
            BonusDiceCancelled: 0,
            KillsReceived: 0,
            HitsReceived: 0);

        /// <summary>
        /// Battle dice for one side this Round (rule 11.2): one per undisrupted Unit, the
        /// Commander's Battle Rating while undisrupted, the nationality bonus less any dice
        /// cancelled by excess disrupts (11.33), and - first Round only - event dice, the
        /// defender's terrain bonus (forfeited if any evasion failed, 11.22) and the
        /// attacker's +1 per failed enemy evasion (10.2). A side with no pieces left rolls nothing.
        /// </summary>
        public static int DiceForRound(TnwLandBattle battle, bool isAttacker, TnwBattleSideState state, int round)
        {
            if (state.Eliminated)
                return 0;

            TnwBattleSide side = isAttacker ? battle.Attacker : battle.Defender;
            int commanderDice = !state.CommanderUndisrupted || (!isAttacker && battle.DefenderWithoutArmyGroup)
                ? 0
                : side.CommanderBattleRating;

            int dice = state.UnitsUndisrupted
                + commanderDice
                + Math.Max(0, side.Composition.BonusDice() - state.BonusDiceCancelled);

            if (round == 1)
            {
                dice += side.EventDice;
                if (isAttacker)
                    dice += battle.FailedEvasions;
                else if (battle.FailedEvasions == 0)
                    dice += battle.Terrain.DefenderBonusDice();
            }

            return Math.Max(0, dice);
        }

        /// <summary>
        /// Applies the kills ("6") and disrupts ("5") rolled against a side (rules 11.3,
        /// 11.33). Kills are applied first, so they take priority when there are too few
        /// pieces to absorb every result. Assumptions (the owner chooses, and the rules don't
        /// say how): kills fall on already-disrupted Units first, then undisrupted Units, and
        /// on the Commander only when no Unit is left. Disrupts fall on undisrupted Units;
        /// the first excess disrupts the Commander, and each further excess cancels one
        /// nationality bonus die.
        /// </summary>
        public static TnwBattleSideState ApplyHits(TnwBattleSideState state, int kills, int disrupts)
        {
            int killsLeft = kills;
            int killedDisrupted = Math.Min(killsLeft, state.UnitsDisrupted);
            killsLeft -= killedDisrupted;
            int killedUndisrupted = Math.Min(killsLeft, state.UnitsUndisrupted);
            killsLeft -= killedUndisrupted;
            bool commanderKilled = killsLeft > 0 && state.CommanderAlive;

            int unitsAlive = state.UnitsAlive - killedDisrupted - killedUndisrupted;
            int unitsUndisrupted = state.UnitsUndisrupted - killedUndisrupted;
            bool commanderAlive = state.CommanderAlive && !commanderKilled;
            bool commanderUndisrupted = state.CommanderUndisrupted && !commanderKilled;

            int disruptsLeft = disrupts;
            int newlyDisrupted = Math.Min(disruptsLeft, unitsUndisrupted);
            disruptsLeft -= newlyDisrupted;
            unitsUndisrupted -= newlyDisrupted;
            if (disruptsLeft > 0 && commanderUndisrupted)
            {
                commanderUndisrupted = false;
                disruptsLeft--;
            }

            return new TnwBattleSideState(
                UnitsAlive: unitsAlive,
                UnitsUndisrupted: unitsUndisrupted,
                CommanderAlive: commanderAlive,
                CommanderUndisrupted: commanderUndisrupted,
                BonusDiceCancelled: state.BonusDiceCancelled + disruptsLeft,
                KillsReceived: state.KillsReceived + kills,
                HitsReceived: state.HitsReceived + kills + disrupts);
        }

        /// <summary>
        /// Decides the battle after a Round (rule 11.32): a side wiped out by kills loses if
        /// the other side is not; otherwise the side that suffered more casualties (kills plus
        /// disrupts, as rolled) loses. A tie after Round 1 means a second Round; a tie after
        /// Round 2 means the Active Formation retreats, i.e. the attacker loses.
        /// </summary>
        public static TnwRoundVerdict Verdict(TnwBattleSideState attacker, TnwBattleSideState defender, int round)
        {
            if (attacker.Eliminated && !defender.Eliminated)
                return TnwRoundVerdict.DefenderWins;
            if (defender.Eliminated && !attacker.Eliminated)
                return TnwRoundVerdict.AttackerWins;
            if (attacker.HitsReceived > defender.HitsReceived)
                return TnwRoundVerdict.DefenderWins;
            if (defender.HitsReceived > attacker.HitsReceived)
                return TnwRoundVerdict.AttackerWins;
            return round == 1 ? TnwRoundVerdict.SecondRound : TnwRoundVerdict.DefenderWins;
        }

        /// <summary>
        /// Applies the battle's aftermath once the victor is known: rout (11.5) eliminates
        /// the loser's disrupted Units and leaders; a defender that cannot retreat is
        /// eliminated (11.42); Flag Overrun (11.7) when the kills rolled against the loser
        /// exceed its pieces, unless both sides are eliminated. Assumption: a losing attacker
        /// can always retreat to a Duchy it came from, without Attrition (11.4, 11.41).
        /// </summary>
        public static TnwLandBattleOutcome Conclude(
            TnwLandBattle battle,
            TnwBattleSideState attacker,
            TnwBattleSideState defender,
            int rounds,
            bool attackerWins)
        {
            TnwBattleSide loserSide = attackerWins ? battle.Defender : battle.Attacker;
            TnwBattleSideState winner = attackerWins ? attacker : defender;
            TnwBattleSideState loser = attackerWins ? defender : attacker;

            bool routed = loser.HitsReceived - winner.HitsReceived >= RoutMargin;
            if (routed)
                loser = loser with { UnitsAlive = loser.UnitsUndisrupted, CommanderAlive = loser.CommanderAlive && loser.CommanderUndisrupted };

            if (attackerWins && battle.DefenderCannotRetreat)
                loser = loser with { UnitsAlive = 0, UnitsUndisrupted = 0, CommanderAlive = false, CommanderUndisrupted = false };

            bool bothEliminated = winner.Eliminated && loser.Eliminated;
            bool flagOverrun = !bothEliminated && loser.KillsReceived > loserSide.Pieces;
            double resourceChance = routed && loserSide.HasCommander
                ? (double)loserSide.CommanderBattleRating / DieFaces
                : 0;

            TnwBattleSideState attackerFinal = attackerWins ? winner : loser;
            TnwBattleSideState defenderFinal = attackerWins ? loser : winner;

            return new TnwLandBattleOutcome(
                AttackerWins: attackerWins,
                Rounds: rounds,
                AttackerUnitsLost: battle.Attacker.Units - attackerFinal.UnitsAlive,
                DefenderUnitsLost: battle.Defender.Units - defenderFinal.UnitsAlive,
                AttackerCommanderKilled: battle.Attacker.HasCommander && !attackerFinal.CommanderAlive,
                DefenderCommanderKilled: battle.Defender.HasCommander && !defenderFinal.CommanderAlive,
                LoserRouted: routed,
                AttackerEliminated: attackerFinal.Eliminated,
                DefenderEliminated: defenderFinal.Eliminated,
                FlagOverrun: flagOverrun,
                ResourceChance: resourceChance);
        }

        /// <summary>
        /// Pre-battle shore battery fire on an Amphibious Assault's landing attacker (13.7, via
        /// 13.5), once, before Round 1. Its casualties count in Round 1's total and reduce the
        /// attacker's Round 1 dice (via <see cref="ApplyHits"/>); unlike a naval Port battle, the
        /// batteries do not fire again once the Land Battle itself begins.
        /// </summary>
        public static TnwBattleSideState ShoreBatteryFire(TnwBattleSideState attacker, int sixes, int fives) =>
            ApplyHits(attacker, sixes, fives);

        /// <summary>
        /// Resolves one battle with random dice. <paramref name="log"/>, when not null,
        /// receives each Round's dice (Round 0 is pre-battle shore battery fire on an Amphibious
        /// Assault's attacker, if any).
        /// </summary>
        public static TnwLandBattleOutcome Simulate(TnwLandBattle battle, Random random, List<TnwLandBattleRoundLog>? log)
        {
            TnwBattleSideState attacker = InitialState(battle.Attacker);
            TnwBattleSideState defender = InitialState(battle.Defender);

            int shoreDice = battle.AmphibiousLanding.ShoreBatteryDice();
            if (shoreDice > 0)
            {
                (int shoreSixes, int shoreFives) = RollSixesAndFives(random, shoreDice);
                log?.Add(new TnwLandBattleRoundLog(0, 0, 0, 0, shoreDice, shoreSixes, shoreFives));
                attacker = ShoreBatteryFire(attacker, shoreSixes, shoreFives);
                if (attacker.Eliminated)
                    return Conclude(battle, attacker, defender, rounds: 0, attackerWins: false);
            }

            for (int round = 1; ; round++)
            {
                int attackerDice = DiceForRound(battle, isAttacker: true, attacker, round);
                int defenderDice = DiceForRound(battle, isAttacker: false, defender, round);
                (int attackerSixes, int attackerFives) = RollSixesAndFives(random, attackerDice);
                (int defenderSixes, int defenderFives) = RollSixesAndFives(random, defenderDice);
                log?.Add(new TnwLandBattleRoundLog(round, attackerDice, attackerSixes, attackerFives, defenderDice, defenderSixes, defenderFives));

                TnwBattleSideState newDefender = ApplyHits(defender, attackerSixes, attackerFives);
                attacker = ApplyHits(attacker, defenderSixes, defenderFives);
                defender = newDefender;

                TnwRoundVerdict verdict = Verdict(attacker, defender, round);
                if (verdict != TnwRoundVerdict.SecondRound)
                    return Conclude(battle, attacker, defender, round, verdict == TnwRoundVerdict.AttackerWins);
            }
        }

        /// <summary>Resolves one battle with random dice for a "Roll 1 Battle" result, including the rout Resource roll.</summary>
        public static TnwLandBattleRollResult RollOnce(TnwLandBattle battle)
        {
            List<TnwLandBattleRoundLog> log = [];
            TnwLandBattleOutcome outcome = Simulate(battle, Random.Shared, log);

            int? resourceDieRoll = null;
            bool resourceGained = false;
            if (outcome.ResourceChance > 0)
            {
                TnwBattleSide loserSide = outcome.AttackerWins ? battle.Defender : battle.Attacker;
                resourceDieRoll = Random.Shared.Next(1, DieFaces + 1);
                resourceGained = resourceDieRoll <= loserSide.CommanderBattleRating;
            }

            return new TnwLandBattleRollResult(outcome, log, resourceDieRoll, resourceGained);
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
