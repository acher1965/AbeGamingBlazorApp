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

        public bool Alive => Stage != PoGUnitStage.Eliminated;

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
            PoGUnitStage.Reduced => (Unit.Type.ReducedLf, this with { Stage = HasReplacement ? PoGUnitStage.ReplacementFull : PoGUnitStage.Eliminated }),
            PoGUnitStage.ReplacementFull => (ReplacementType.FullLf, this with { Stage = PoGUnitStage.ReplacementReduced }),
            PoGUnitStage.ReplacementReduced => (ReplacementType.ReducedLf, this with { Stage = PoGUnitStage.Eliminated }),
            _ => null,
        };

        private bool HasReplacement => Unit.Type.Kind == PoGUnitKind.Army && Unit.ReserveCorpsAvailable;

        private PoGUnitType ReplacementType => PoGUnitCatalog.Get(Unit.Type.ReplacementCorps!);
    }
}
