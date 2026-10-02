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
        /// to destroy it (12.4.6). When the Loss Number cannot be met but could have been with a
        /// reduced Corps in the Reserve Box, an Army without one is eliminated for good if
        /// possible (12.4.4.2). Where several allocations fulfil the same amount, the owner's
        /// choice is assumed to be, in order: the best expected return fire if this side still
        /// fires this combat (<paramref name="returnFire"/>), keeping a full strength unit, keeping
        /// the most steps, keeping the most CF.
        /// </summary>
        /// <param name="attackerLossPriority">
        /// True for the attacker: the first step comes from the highest unit on the 12.4.5 list
        /// that can take it without exceeding the Loss Number, even if that means not meeting
        /// the Loss Number exactly (12.4.5 takes precedence over 12.4.3).
        /// </param>
        public PoGSideForce TakeLosses(int lossNumber, Func<PoGSideForce, double>? returnFire = null, bool attackerLossPriority = false)
        {
            if (lossNumber <= 0)
                return this;

            List<(int LossFactor, PoGSideForce Force)> firstLosses = attackerLossPriority ? PriorityFirstLosses(lossNumber) : [];
            if (firstLosses.Count == 0)
                return BestAllocation(lossNumber, returnFire).Force;

            // Equal priorities (e.g. MEF and RU CAU) are the owner's choice.
            IEnumerable<Allocation> options = firstLosses.Select(first =>
            {
                Allocation rest = first.Force.BestAllocation(lossNumber - first.LossFactor, returnFire);
                return rest with { Points = rest.Points + first.LossFactor };
            });
            return Rank(options, returnFire).First().Force;
        }

        /// <summary>
        /// 12.4.5: the possible first steps from the highest-priority units that can take a step
        /// without exceeding the Loss Number; empty if none is on the list.
        /// </summary>
        private List<(int LossFactor, PoGSideForce Force)> PriorityFirstLosses(int lossNumber)
        {
            List<(int Priority, int Index, int LossFactor, PoGUnitState Next)> eligible = [];
            for (int i = 0; i < Units.Count; i++)
            {
                PoGUnitState unit = Units[i];
                if (unit.Alive && unit.CurrentType.AttackerLossPriority is int priority
                    && unit.NextStep() is (int lf, PoGUnitState next) && lf <= lossNumber)
                    eligible.Add((priority, i, lf, next));
            }

            if (eligible.Count == 0)
                return [];

            int top = eligible.Min(e => e.Priority);
            return [.. eligible
                .Where(e => e.Priority == top)
                .Select(e => (e.LossFactor, this with { Units = new EquatableList<PoGUnitState>(Units.Select((u, i) => i == e.Index ? e.Next : u)) }))];
        }

        /// <summary>The owner's best allocation of a Loss Number under 12.4.3, 12.4.4.2 and 12.4.6.</summary>
        private Allocation BestAllocation(int lossNumber, Func<PoGSideForce, double>? returnFire)
        {
            if (lossNumber <= 0)
                return new Allocation(0, this, false);

            List<Allocation> allocations = Allocations(lossNumber);
            int maxPoints = allocations.Max(a => a.Points);
            List<Allocation> candidates = [.. allocations.Where(a => a.Points == maxPoints)];

            // 12.4.4.2: only a tie-break among the allocations allowed by 12.4.3.
            if (maxPoints < lossNumber && candidates.Any(a => a.ArmyLostForGood) && CouldMeetWithAReducedReserveCorps(lossNumber))
                candidates = [.. candidates.Where(a => a.ArmyLostForGood)];

            return Rank(candidates, returnFire).First();
        }

        /// <summary>
        /// True if the Loss Number could be met exactly had one of the Armies without a
        /// replacement had a reduced Corps in the Reserve Box (12.4.4.2).
        /// </summary>
        private bool CouldMeetWithAReducedReserveCorps(int lossNumber)
        {
            for (int i = 0; i < Units.Count; i++)
            {
                if (!Units[i].IsArmyWithoutReplacement)
                    continue;

                PoGSideForce hypothetical = this with
                {
                    Units = new EquatableList<PoGUnitState>(Units.Select((u, j) => j == i ? u with { AssumeReducedReplacement = true } : u)),
                };
                if (hypothetical.Allocations(lossNumber).Max(a => a.Points) == lossNumber)
                    return true;
            }
            return false;
        }

        private static IOrderedEnumerable<Allocation> Rank(IEnumerable<Allocation> allocations, Func<PoGSideForce, double>? returnFire) =>
            allocations
                .OrderByDescending(a => a.Points)
                .ThenByDescending(a => returnFire?.Invoke(a.Force) ?? 0)
                .ThenByDescending(a => a.Force.HasFullStrengthUnit)
                .ThenByDescending(a => a.Force.StepsRemaining)
                .ThenByDescending(a => a.Force.CombatFactors);

        /// <summary>
        /// Every distinct way to take up to <paramref name="lossNumber"/> loss points, including
        /// destroying the fort once no unit is left (12.4.6).
        /// </summary>
        private List<Allocation> Allocations(int lossNumber)
        {
            // Dynamic programme over units, keyed by what decides the outcome: loss points
            // taken, CF left, an Army still present, any unit alive, an Army eliminated for good.
            Dictionary<(int Points, int Cf, bool Army, bool Alive, bool ArmyLost), Candidate> best = new()
            {
                [(0, 0, false, false, false)] = new Candidate([], 0, 0),
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

                Dictionary<(int Points, int Cf, bool Army, bool Alive, bool ArmyLost), Candidate> nextBest = [];
                foreach (((int points, int cf, bool army, bool alive, bool armyLost), Candidate candidate) in best)
                {
                    foreach ((int lossFactor, PoGUnitState state) in options)
                    {
                        if (points + lossFactor > lossNumber)
                            continue;

                        (int, int, bool, bool, bool) key = (
                            points + lossFactor,
                            cf + state.CombatFactors,
                            army || state.IsArmy,
                            alive || state.Alive,
                            armyLost || state.IsPermanentlyEliminatedArmy);
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

            List<Allocation> finals = [];
            foreach (((int points, int _, bool _, bool alive, bool armyLost), Candidate candidate) in best)
            {
                PoGSideForce kept = this with { Units = new EquatableList<PoGUnitState>(candidate.States) };
                finals.Add(new Allocation(points, kept, armyLost));
                if (!alive && FortIntact && FortCf > 0 && points + FortCf <= lossNumber)
                    finals.Add(new Allocation(points + FortCf, kept with { FortIntact = false }, armyLost));
            }
            return finals;
        }

        /// <summary>One way to take a Loss Number: the points taken and the force left.</summary>
        private sealed record Allocation(int Points, PoGSideForce Force, bool ArmyLostForGood);

        private sealed record Candidate(List<PoGUnitState> States, int FullStrengthUnits, int Steps)
        {
            public bool IsBetterThan(Candidate other) =>
                (FullStrengthUnits > 0, Steps, FullStrengthUnits).CompareTo((other.FullStrengthUnits > 0, other.Steps, other.FullStrengthUnits)) > 0;
        }
    }
}
