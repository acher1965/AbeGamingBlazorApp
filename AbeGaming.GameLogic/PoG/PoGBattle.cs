namespace AbeGaming.GameLogic.PoG
{
    public record PoGBattle(
        BattleSideInfo Attacker,
        BattleSideInfo Defender,
        Terrain Terrain,
        FortressLevel FortressLevel,
        int Trench,
        bool AttemptFlankAttack = false,
        int FlankAttackDrm = 0,
        bool IsAllAttackersInSinai = false);
}
