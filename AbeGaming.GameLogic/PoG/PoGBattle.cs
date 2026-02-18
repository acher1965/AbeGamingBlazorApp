namespace AbeGaming.GameLogic.PoG
{
    public record PoGBattle(
        BattleSideInfo Attacker,
        BattleSideInfo Defender,
        Terrain Terrain,
        FortressLevel FortressLevel,
        int Trench);
}
