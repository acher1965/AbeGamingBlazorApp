namespace AbeGaming.GameLogic.PoG
{
    /// <summary>Where a unit is in its chain of step losses (12.4.2, 12.4.4).</summary>
    public enum PoGUnitStage
    {
        Full,
        Reduced,
        /// <summary>The Army was eliminated from its reduced side and replaced by a full Corps from the Reserve Box.</summary>
        ReplacementFull,
        ReplacementReduced,
        Eliminated,
    }

    /// <summary>A unit and its current stage during a detailed-mode combat.</summary>
    public readonly record struct PoGUnitState(PoGUnit Unit, PoGUnitStage Stage)
    {
        public static PoGUnitState Initial(PoGUnit unit) => new(unit, unit.Reduced ? PoGUnitStage.Reduced : PoGUnitStage.Full);

        /// <summary>
        /// Only for the 12.4.4.2 test: treat the Army as if a reduced Corps were in the Reserve
        /// Box when it has no full one.
        /// </summary>
        internal bool AssumeReducedReplacement { get; init; }

        public bool Alive => Stage != PoGUnitStage.Eliminated;

        /// <summary>The unit type currently on the map: the replacement Corps once it has replaced the Army.</summary>
        public PoGUnitType CurrentType => Stage is PoGUnitStage.ReplacementFull or PoGUnitStage.ReplacementReduced
            ? ReplacementType
            : Unit.Type;

        /// <summary>An Army without a replacement Corps that has been eliminated for good (12.4.4, 12.4.4.2).</summary>
        public bool IsPermanentlyEliminatedArmy => Stage == PoGUnitStage.Eliminated && Unit.Type.Kind == PoGUnitKind.Army && !HasReplacement;

        /// <summary>An Army still on the map with no replacement Corps in the Reserve Box.</summary>
        public bool IsArmyWithoutReplacement => IsArmy && !HasReplacement;

        /// <summary>An Army still on the map (full or reduced) - the side then fires on the Army table.</summary>
        public bool IsArmy => Stage is PoGUnitStage.Full or PoGUnitStage.Reduced && Unit.Type.Kind == PoGUnitKind.Army;

        /// <summary>A full strength unit, including a full replacement Corps (needed to force a retreat, 12.5.1).</summary>
        public bool IsFullStrength => Stage is PoGUnitStage.Full or PoGUnitStage.ReplacementFull;

        public int CombatFactors => Stage switch
        {
            PoGUnitStage.Full => Unit.Type.FullCf,
            PoGUnitStage.Reduced => Unit.Type.ReducedCf,
            PoGUnitStage.ReplacementFull => ReplacementType.FullCf,
            PoGUnitStage.ReplacementReduced => ReplacementType.ReducedCf,
            _ => 0,
        };

        /// <summary>Further step losses this unit can still take.</summary>
        public int StepsRemaining
        {
            get
            {
                int steps = 0;
                for (PoGUnitState state = this; state.NextStep() is (int _, PoGUnitState next); state = next)
                    steps++;
                return steps;
            }
        }

        /// <summary>
        /// The next step loss: the LF it provides (the LF of the side being removed, 12.4.2) and
        /// the resulting state; null once eliminated.
        /// </summary>
        public (int LossFactor, PoGUnitState Next)? NextStep() => Stage switch
        {
            PoGUnitStage.Full => (Unit.Type.FullLf, this with { Stage = PoGUnitStage.Reduced }),
            PoGUnitStage.Reduced => (Unit.Type.ReducedLf, this with { Stage = StageAfterReducedArmy }),
            PoGUnitStage.ReplacementFull => (ReplacementType.FullLf, this with { Stage = PoGUnitStage.ReplacementReduced }),
            PoGUnitStage.ReplacementReduced => (ReplacementType.ReducedLf, this with { Stage = PoGUnitStage.Eliminated }),
            _ => null,
        };

        private bool HasReplacement => Unit.Type.Kind == PoGUnitKind.Army && Unit.ReserveCorpsAvailable;

        private PoGUnitStage StageAfterReducedArmy =>
            HasReplacement ? PoGUnitStage.ReplacementFull
            : Unit.Type.Kind == PoGUnitKind.Army && AssumeReducedReplacement ? PoGUnitStage.ReplacementReduced
            : PoGUnitStage.Eliminated;

        private PoGUnitType ReplacementType => PoGUnitCatalog.Get(Unit.Type.ReplacementCorps!);
    }
}
