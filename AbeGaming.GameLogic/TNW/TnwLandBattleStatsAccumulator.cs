namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// Accumulates weighted battle outcomes into <see cref="TnwLandBattleStats"/>. Shared by
    /// the exact enumeration (weight = outcome probability) and the Monte Carlo simulation
    /// (weight = 1 per trial), so both report the same quantities in the same way.
    /// </summary>
    internal sealed class TnwLandBattleStatsAccumulator
    {
        private readonly SideAccumulator _attacker;
        private readonly SideAccumulator _defender;
        private double _total;
        private double _attackerWins;
        private double _secondRound;

        public TnwLandBattleStatsAccumulator(TnwLandBattle battle)
        {
            _attacker = new SideAccumulator(battle.Attacker.Units);
            _defender = new SideAccumulator(battle.Defender.Units);
        }

        public void Add(in TnwLandBattleOutcome outcome, double weight)
        {
            _total += weight;
            if (outcome.AttackerWins)
                _attackerWins += weight;
            if (outcome.Rounds == 2)
                _secondRound += weight;

            SideAccumulator victor = outcome.AttackerWins ? _attacker : _defender;
            SideAccumulator loser = outcome.AttackerWins ? _defender : _attacker;
            if (outcome.FlagOverrun)
                victor.FlagOverrun += weight;
            victor.Resource += weight * outcome.ResourceChance;
            if (outcome.LoserRouted)
                loser.Routed += weight;

            _attacker.Add(outcome.AttackerUnitsLost, outcome.AttackerCommanderKilled, outcome.AttackerEliminated, weight);
            _defender.Add(outcome.DefenderUnitsLost, outcome.DefenderCommanderKilled, outcome.DefenderEliminated, weight);
        }

        public TnwLandBattleStats Build(bool isExact, int monteCarloTrials)
        {
            double attackerWins = _attackerWins / _total;
            return new TnwLandBattleStats(
                AttackerWinProbability: attackerWins,
                DefenderWinProbability: 1 - attackerWins,
                SecondRoundProbability: _secondRound / _total,
                Attacker: _attacker.Build(_total),
                Defender: _defender.Build(_total),
                IsExact: isExact,
                MonteCarloTrials: monteCarloTrials);
        }

        private sealed class SideAccumulator(int units)
        {
            private readonly double[] _unitsLost = new double[units + 1];
            private double _commanderKilled;
            private double _eliminated;

            public double Routed { get; set; }
            public double FlagOverrun { get; set; }
            public double Resource { get; set; }

            public void Add(int unitsLost, bool commanderKilled, bool eliminated, double weight)
            {
                _unitsLost[unitsLost] += weight;
                if (commanderKilled)
                    _commanderKilled += weight;
                if (eliminated)
                    _eliminated += weight;
            }

            public TnwLandBattleSideStats Build(double total)
            {
                double mean = 0;
                double meanOfSquares = 0;
                Dictionary<int, double> distribution = [];
                for (int lost = 0; lost < _unitsLost.Length; lost++)
                {
                    if (_unitsLost[lost] == 0)
                        continue;
                    double probability = _unitsLost[lost] / total;
                    distribution[lost] = probability;
                    mean += lost * probability;
                    meanOfSquares += (double)lost * lost * probability;
                }

                return new TnwLandBattleSideStats(
                    MeanUnitsLost: mean,
                    StdDevUnitsLost: Math.Sqrt(Math.Max(0, meanOfSquares - mean * mean)),
                    UnitsLostDistribution: distribution,
                    RoutedProbability: Routed / total,
                    EliminatedProbability: _eliminated / total,
                    CommanderKilledProbability: _commanderKilled / total,
                    FlagOverrunProbability: FlagOverrun / total,
                    ResourceGainProbability: Resource / total);
            }
        }
    }
}
