namespace AbeGaming.GameLogic.PoG
{
    /// <summary>
    /// One side's units (and, for the defender, its fort) during a detailed-mode combat.
    /// Assumption: a fort's LF equals its printed CF (the fort values are not in the rules files).
    /// </summary>
    public record PoGSideForce(EquatableList<PoGUnitState> Units, int FortCf, bool FortIntact)
    {
        public static PoGSideForce FromUnits(IEnumerable<PoGUnit> units, int fortCf = 0) =>
            new(new EquatableList<PoGUnitState>(units.Select(PoGUnitState.Initial)), fortCf, fortCf > 0);

        /// <summary>Combat Strength: the units' CF plus an intact fort's CF (12.2.3).</summary>
        public int CombatFactors => Units.Sum(u => u.CombatFactors) + (FortIntact ? FortCf : 0);

        /// <summary>The Army table if any Army is firing, otherwise the Corps/Fort table (12.2.8).</summary>
        public FireTable FireTable => Units.Any(u => u.IsArmy) ? FireTable.Army : FireTable.Corps;

        public bool AnyUnitAlive => Units.Any(u => u.Alive);

        public bool HasFullStrengthUnit => Units.Any(u => u.IsFullStrength);

        public int StepsRemaining => Units.Sum(u => u.StepsRemaining);

        /// <summary>True while there is anything left to fire: a unit (even with 0 CF, 12.1.7) or an intact fort.</summary>
        public bool CanFire => AnyUnitAlive || (FortIntact && FortCf > 0);

        /// <summary>
        /// Takes a Loss Number (12.4.3): exactly if any combination of step losses reaches it,
        /// otherwise as much as possible without exceeding it. The fort takes a loss only once
        /// every unit (including replacement Corps) is gone and enough of the Loss Number remains
        /// to destroy it (12.4.6). Where several allocations fulfil the same amount, the owner's
        /// choice is assumed to be, in order: the best expected return fire if this side still
        /// fires this combat (<paramref name="returnFire"/>), keeping a full strength unit, keeping
        /// the most steps, keeping the most CF.
        /// </summary>
        public PoGSideForce TakeLosses(int lossNumber, Func<PoGSideForce, double>? returnFire = null)
        {
            if (lossNumber <= 0)
                return this;

            // Dynamic programme over units, keyed by what decides the outcome: loss points
            // taken, CF left, an Army still present, any unit alive.
            Dictionary<(int Points, int Cf, bool Army, bool Alive), Candidate> best = new()
            {
                [(0, 0, false, false)] = new Candidate([], 0, 0),
            };

            foreach (PoGUnitState unit in Units)
            {
                List<(int LossFactor, PoGUnitState State)> options = [(0, unit)];
                int taken = 0;
                for (PoGUnitState state = unit; state.NextStep() is (int lf, PoGUnitState next); state = next)
                {
                    taken += lf;
                    options.Add((taken, next));
                }

                Dictionary<(int Points, int Cf, bool Army, bool Alive), Candidate> nextBest = [];
                foreach (((int points, int cf, bool army, bool alive), Candidate candidate) in best)
                {
                    foreach ((int lossFactor, PoGUnitState state) in options)
                    {
                        if (points + lossFactor > lossNumber)
                            continue;

                        (int, int, bool, bool) key = (points + lossFactor, cf + state.CombatFactors, army || state.IsArmy, alive || state.Alive);
                        Candidate extended = new(
                            [.. candidate.States, state],
                            candidate.FullStrengthUnits + (state.IsFullStrength ? 1 : 0),
                            candidate.Steps + state.StepsRemaining);
                        if (!nextBest.TryGetValue(key, out Candidate? existing) || extended.IsBetterThan(existing))
                            nextBest[key] = extended;
                    }
                }
                best = nextBest;
            }

            List<(int Points, PoGSideForce Force, Candidate Candidate)> finals = [];
            foreach (((int points, int _, bool _, bool alive), Candidate candidate) in best)
            {
                PoGSideForce kept = this with { Units = new EquatableList<PoGUnitState>(candidate.States) };
                finals.Add((points, kept, candidate));
                if (!alive && FortIntact && FortCf > 0 && points + FortCf <= lossNumber)
                    finals.Add((points + FortCf, kept with { FortIntact = false }, candidate));
            }

            int maxPoints = finals.Max(f => f.Points);
            return finals
                .Where(f => f.Points == maxPoints)
                .OrderByDescending(f => returnFire?.Invoke(f.Force) ?? 0)
                .ThenByDescending(f => f.Candidate.FullStrengthUnits > 0)
                .ThenByDescending(f => f.Candidate.Steps)
                .ThenByDescending(f => f.Force.CombatFactors)
                .First()
                .Force;
        }

        private sealed record Candidate(List<PoGUnitState> States, int FullStrengthUnits, int Steps)
        {
            public bool IsBetterThan(Candidate other) =>
                (FullStrengthUnits > 0, Steps, FullStrengthUnits).CompareTo((other.FullStrengthUnits > 0, other.Steps, other.FullStrengthUnits)) > 0;
        }
    }
}
