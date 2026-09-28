namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// Accumulates weighted naval battle outcomes into <see cref="TnwNavalBattleStats"/>.
    /// Shared by the exact enumeration (weight = outcome probability) and the Monte Carlo
    /// simulation (weight = 1 per trial).
    /// </summary>
    internal sealed class TnwNavalBattleStatsAccumulator
    {
        private readonly SideAccumulator _active;
        private readonly SideAccumulator _inactive;
        private double _total;
        private double _activeWins;
        private double _secondRound;

        public TnwNavalBattleStatsAccumulator(TnwNavalBattle battle)
        {
            _active = new SideAccumulator(battle.Active.TotalSquadrons);
            _inactive = new SideAccumulator(battle.Inactive.TotalSquadrons);
        }

        public void Add(in TnwNavalBattleOutcome outcome, double weight)
        {
            _total += weight;
            if (outcome.ActiveWins)
                _activeWins += weight;
            if (outcome.Rounds == 2)
                _secondRound += weight;

            _active.Add(outcome.ActiveSquadronsLost, outcome.ActiveEliminated, weight);
            _inactive.Add(outcome.InactiveSquadronsLost, outcome.InactiveEliminated, weight);
        }

        public TnwNavalBattleStats Build(bool isExact, int monteCarloTrials)
        {
            double activeWins = _activeWins / _total;
            return new TnwNavalBattleStats(
                ActiveWinProbability: activeWins,
                InactiveWinProbability: 1 - activeWins,
                SecondRoundProbability: _secondRound / _total,
                Active: _active.Build(_total),
                Inactive: _inactive.Build(_total),
                IsExact: isExact,
                MonteCarloTrials: monteCarloTrials);
        }

        private sealed class SideAccumulator(int squadrons)
        {
            private readonly double[] _lost = new double[squadrons + 1];
            private double _eliminated;

            public void Add(int lost, bool eliminated, double weight)
            {
                _lost[lost] += weight;
                if (eliminated)
                    _eliminated += weight;
            }

            public TnwNavalSideStats Build(double total)
            {
                double mean = 0;
                double meanOfSquares = 0;
                Dictionary<int, double> distribution = [];
                for (int lost = 0; lost < _lost.Length; lost++)
                {
                    if (_lost[lost] == 0)
                        continue;
                    double probability = _lost[lost] / total;
                    distribution[lost] = probability;
                    mean += lost * probability;
                    meanOfSquares += (double)lost * lost * probability;
                }

                return new TnwNavalSideStats(
                    MeanSquadronsLost: mean,
                    StdDevSquadronsLost: Math.Sqrt(Math.Max(0, meanOfSquares - mean * mean)),
                    SquadronsLostDistribution: distribution,
                    EliminatedProbability: _eliminated / total);
            }
        }
    }
}
