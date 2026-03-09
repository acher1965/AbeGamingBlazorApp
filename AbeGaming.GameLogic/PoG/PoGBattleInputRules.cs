namespace AbeGaming.GameLogic.PoG
{
    public static class PoGBattleInputRules
    {
        public static int ClampFactors(int value) => Math.Clamp(value, 0, 50);

        public static int ClampDrm(int value) => Math.Clamp(value, -9, 9);

        public static int ClampDieRoll(int value) => Math.Clamp(value, 1, 6);

        public static int ClampModifiedDieRoll(int value) => Math.Clamp(value, 1, 6);

        public static int ClampTrench(int value) => Math.Clamp(value, 0, 2);

        public static int ClampFlankAttackDrm(int value) => Math.Clamp(value, 0, 5);
    }
}
