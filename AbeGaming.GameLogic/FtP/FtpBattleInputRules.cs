namespace AbeGaming.GameLogic.FtP
{
    public static class FtpBattleInputRules
    {
        public static int AttackerMaxSize(bool isAmphibious, bool amphibiousIsArmyMove)
        {
            if (!isAmphibious)
                return 15;

            return amphibiousIsArmyMove ? 9 : 3;
        }

        public static int DefenderMinSize(bool isAmphibious, bool fortPresent)
        {
            return isAmphibious && fortPresent ? 0 : 1;
        }

        public static int ClampAttackerSize(int value, bool isAmphibious, bool amphibiousIsArmyMove)
        {
            int maxSize = AttackerMaxSize(isAmphibious, amphibiousIsArmyMove);
            return Math.Clamp(value, 1, maxSize);
        }

        public static int ClampDefenderSize(int value, bool isAmphibious, bool fortPresent)
        {
            int minSize = DefenderMinSize(isAmphibious, fortPresent);
            return Math.Clamp(value, minSize, 30);
        }

        public static int ClampDrm(int value)
        {
            return Math.Clamp(value, 0, 9);
        }

        public static int ClampElites(int value)
        {
            return Math.Clamp(value, 0, 2);
        }

        public static int ClampAmphibiousAssaultLevel(int value)
        {
            return Math.Clamp(value, 0, 5);
        }
    }
}