namespace AbeGaming.GameLogic.TNW
{
    public static class TnwForceCompositionExtensions
    {
        /// <summary>Nationality bonus dice for a force of this composition (rule 11.21).</summary>
        public static int BonusDice(this TnwForceComposition composition) => composition switch
        {
            TnwForceComposition.Minor => 0,
            TnwForceComposition.Power => 1,
            TnwForceComposition.MajorityFrench => 2,
            _ => 0,
        };
    }
}
