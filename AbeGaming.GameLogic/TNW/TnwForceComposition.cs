namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// Nationality-mix bonus dice for a Battle or Siege dice pool (rule 11.21):
    /// each side normally gets one extra die, unless it is majority-French (two
    /// extra dice) or majority-Minor (no extra dice).
    /// </summary>
    public enum TnwForceComposition
    {
        /// <summary>At least half Minor-nation Units/leaders: no bonus die.</summary>
        Minor,

        /// <summary>A normal force (not majority-French, not majority-Minor): +1 bonus die.</summary>
        Power,

        /// <summary>More than half French Units/leaders: +2 bonus dice.</summary>
        MajorityFrench,
    }
}
