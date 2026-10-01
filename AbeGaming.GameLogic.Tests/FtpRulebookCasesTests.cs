using AbeGaming.GameLogic.FtP;

namespace AbeGaming.GameLogic.Tests;

/// <summary>
/// Correctness tests taken from the 2024 rulebook (RulesAndTables/FTP_RULES_2024-FINAL-web.pdf):
/// its worked examples, the designer's play note on amphibious assaults (18.0), and one case
/// per rule fixed in the 2026-10-01 review (FTP-RULES-REVIEW-2026-10-01.md). Unlike the golden
/// values in FtpBattleStatsTests, these expected results come from the rules, not from the code.
/// Dice order for BattleResult: attacker, defender, attacker general casualty, defender general casualty.
/// </summary>
public class FtpRulebookCasesTests
{
    private static FtpBattle Battle(
        int attacker,
        int defender,
        bool fort = false,
        bool resourceOrCapital = false,
        bool interception = false,
        bool defenderLeader = true,
        int attackerDrm = 0,
        int defenderDrm = 0,
        int attackerElites = 0,
        int defenderElites = 0,
        bool attackerOos = false,
        bool defenderOos = false,
        FtpAmphibious? amphibious = null,
        bool divisionMove = false) =>
        new(resourceOrCapital, fort, interception, defenderLeader, attacker, defender, attackerDrm, defenderDrm,
            attackerElites, defenderElites, attackerOos, defenderOos, amphibious, divisionMove);

    private static readonly FtpAmphibious NavalMove = new(false, false, false, false, false, 0);
    private static readonly FtpAmphibious ArmyNavalMove = new(true, false, false, false, false, 0);
    private static readonly FtpAmphibious NavalMoveWithAdmiral = new(false, true, false, false, false, 0);

    // ---- Rulebook worked examples ----

    [Fact]
    public void Gettysburg_ComprehensiveExample()
    {
        // Lee 12 SPs attacks at +4; Meade 14 SPs intercepted: +5 generals, +2 interception, +1 elite.
        // Confederate rolls 4 (8 -> 4*), Union rolls 2 (10 -> 6). The Union rolled the modified 10:
        // Union loses a general on 1-3, the Confederacy on a 1. Reynolds dies; the Iron Brigade is lost.
        FtpBattle battle = Battle(12, 14, interception: true, attackerDrm: 4, defenderDrm: 5, defenderElites: 1);

        FTPBattleResult result = battle.BattleResult([4, 2, 2, 3]);

        Assert.Equal(BattleSize.Large, result.BattleSize);
        Assert.Equal(Winner.Defender, result.Winner);
        Assert.Equal(6, result.DamageToAttacker);
        Assert.Equal(4, result.DamageToDefender);
        Assert.True(result.Star);
        Assert.False(result.AttackerLeaderDeath);
        Assert.True(result.DefenderLeaderDeath);
        Assert.True(result.DefenderEliteLoss);
    }

    [Fact]
    public void Example1_ThomasVsLongstreet_AsteriskBreaksTheTie()
    {
        // Thomas 3 SPs (+2) attacks Longstreet 6 SPs (+3): 8 -> 2*, 7 -> 2. The attacker wins on the asterisk.
        FTPBattleResult result = Battle(3, 6, attackerDrm: 2, defenderDrm: 3).BattleResult([6, 4, 6, 6]);

        Assert.Equal(BattleSize.Medium, result.BattleSize);
        Assert.Equal(Winner.Attacker, result.Winner);
        Assert.Equal(2, result.DamageToDefender);
        Assert.Equal(2, result.DamageToAttacker);
        Assert.True(result.AttackerCanStay);
    }

    [Fact]
    public void Example2_ShermanAtLittleRock_ResourceSpaceCancelsTheAsterisk()
    {
        // Sherman 2 SPs (+3) attacks 1 SP in a fort at a Resource space: 7 -> 1*, Confederate 3 (+2) -> 1.
        FtpBattle battle = Battle(2, 1, fort: true, resourceOrCapital: true, attackerDrm: 3);

        FTPBattleResult result = battle.BattleResult([4, 3, 6, 6]);

        Assert.Equal(BattleSize.Small, result.BattleSize);
        Assert.False(result.Star);
        Assert.Equal(Winner.Defender, result.Winner);
        Assert.Equal(1, result.DamageToAttacker);
        Assert.Equal(1, result.DamageToDefender);
    }

    [Fact]
    public void SampleTurn_FortPulaski_UngarrisonedFortLosesTheTie()
    {
        // 2 SPs assault the ungarrisoned Fort Pulaski: Union 1 (+4 = 5), Confederate 2 (+2 = 4).
        // Each side's result is 1, the attacker survives, so the attacker wins and occupies the fort.
        FTPBattleResult result = Battle(2, 0, fort: true, amphibious: NavalMove).BattleResult([1, 2, 6, 6]);

        Assert.Equal(Winner.Attacker, result.Winner);
        Assert.Equal(1, result.DamageToAttacker);
        Assert.True(result.AttackerCanStay);
    }

    // ---- Play note 18.0: amphibious assaults on forts ----

    [Fact]
    public void PlayNote_UngarrisonedFort_ZeroOne_InvasionSucceeds()
    {
        // The Admiral (+2) cancels the fort (+2), so the fort can roll a 0 result.
        FTPBattleResult result = Battle(1, 0, fort: true, amphibious: NavalMoveWithAdmiral).BattleResult([1, 1, 6, 6]);

        Assert.Equal(0, result.DamageToAttacker);
        Assert.True(result.AttackerCanStay);
    }

    [Theory]
    [InlineData(1, false)] // 1 Union SP: no survivor, so 6.81 does not apply.
    [InlineData(2, true)]  // 2 Union SPs: a survivor wins the tie (6.81).
    public void PlayNote_UngarrisonedFort_OneOne(int unionSps, bool succeeds)
    {
        FTPBattleResult result = Battle(unionSps, 0, fort: true, amphibious: NavalMove).BattleResult([1, 1, 6, 6]);

        Assert.False(result.Star);
        Assert.Equal(succeeds, result.AttackerCanStay);
    }

    [Theory]
    [InlineData(1, false)] // "7.34 does not apply, but 7.33 does, since there is a surviving defending zero SP"
    [InlineData(2, true)]
    public void PlayNote_UngarrisonedFort_OneOneStar(int unionSps, bool succeeds)
    {
        FTPBattleResult result = Battle(unionSps, 0, fort: true, amphibious: NavalMove).BattleResult([3, 1, 6, 6]);

        Assert.True(result.Star);
        Assert.Equal(1, result.DamageToAttacker);
        Assert.Equal(succeeds, result.AttackerCanStay);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void PlayNote_GarrisonedFort_OneOneStar(int unionSps, bool succeeds)
    {
        FTPBattleResult result = Battle(unionSps, 1, fort: true, attackerDrm: 3, amphibious: NavalMove).BattleResult([4, 1, 6, 6]);

        Assert.True(result.Star);
        Assert.Equal(1, result.DamageToAttacker);
        Assert.Equal(1, result.DamageToDefender);
        Assert.Equal(succeeds, result.AttackerCanStay);
    }

    [Fact]
    public void PlayNote_GarrisonedFort_TwoAny_InvasionFails()
    {
        // The fort side rolls a modified 10 (6 +2 amphibious +2 general): result 2.
        FTPBattleResult result = Battle(2, 1, fort: true, defenderDrm: 2, amphibious: NavalMove).BattleResult([1, 6, 6, 6]);

        Assert.Equal(Winner.Defender, result.Winner);
        Assert.False(result.AttackerCanStay);
    }

    [Fact]
    public void PlayNote_NoFort_DefenderWinsButIsEliminated_UnionStillRetreats()
    {
        // 6.42 overrides 7.33: 2 Union SPs vs 1 CSA SP, 1-1 tie, no asterisk.
        FTPBattleResult result = Battle(2, 1, amphibious: NavalMove).BattleResult([4, 2, 6, 6]);

        Assert.Equal(Winner.Defender, result.Winner);
        Assert.Equal(1, result.DamageToDefender);
        Assert.False(result.AttackerCanStay);
    }

    // ---- Rules fixed in the 2026-10-01 review ----

    [Fact]
    public void Rule7_34_BothEliminated_TieWithoutAsterisk_BothRetainOneSp()
    {
        FTPBattleResult result = Battle(1, 1).BattleResult([4, 2, 6, 6]);

        Assert.Equal(Winner.Defender, result.Winner);
        Assert.Equal(0, result.DamageToAttacker);
        Assert.Equal(0, result.DamageToDefender);
    }

    [Fact]
    public void Rule7_34_BothEliminated_WinnerRetainsOneSp()
    {
        // 1 vs 1 with an asterisk: the attacker wins and keeps its SP, the defender is eliminated.
        FTPBattleResult result = Battle(1, 1, attackerDrm: 3).BattleResult([4, 2, 6, 6]);

        Assert.True(result.Star);
        Assert.Equal(Winner.Attacker, result.Winner);
        Assert.Equal(0, result.DamageToAttacker);
        Assert.Equal(1, result.DamageToDefender);
    }

    [Fact]
    public void Rule7_34_StillAppliesToLandBattlesAtAFort()
    {
        // The play note's "surviving zero-SP fort" exception is for amphibious assaults only.
        FTPBattleResult result = Battle(1, 1, fort: true, attackerDrm: 5).BattleResult([4, 1, 6, 6]);

        Assert.True(result.Star);
        Assert.Equal(Winner.Attacker, result.Winner);
        Assert.Equal(0, result.DamageToAttacker);
    }

    [Fact]
    public void Rule7_33_AttackerWinsButIsEliminated_NeitherStaysNorContinues()
    {
        // 3 SPs vs 17: 5* against 4, but the attacker loses all 3 SPs.
        FTPBattleResult result = Battle(3, 17, attackerDrm: 6).BattleResult([3, 3, 6, 6]);

        Assert.Equal(Winner.Attacker, result.Winner);
        Assert.Equal(3, result.DamageToAttacker);
        Assert.False(result.AttackerCanStay);
        Assert.False(result.AttackerCanContinueMoving);
    }

    [Fact]
    public void Rule7_71_AttackerOutOfSupply_NoDefenderGeneralCasualtyRoll()
    {
        FTPBattleResult result = Battle(5, 5, defenderDrm: 2, attackerOos: true).BattleResult([1, 6, 1, 1]);

        Assert.Null(result.DefenderLeaderDeathDieRoll);
        Assert.NotNull(result.AttackerLeaderDeathDieRoll);
    }

    [Fact]
    public void Rule7_72_DefenderOutOfSupply_NoAttackerGeneralCasualtyRoll()
    {
        FTPBattleResult result = Battle(5, 5, attackerDrm: 2, defenderOos: true).BattleResult([6, 1, 1, 1]);

        Assert.Null(result.AttackerLeaderDeathDieRoll);
        Assert.NotNull(result.DefenderLeaderDeathDieRoll);
    }

    [Fact]
    public void Rule7_82_EliteLostOnlyIfTwoOrMoreSpLossesAreTaken()
    {
        // 6 SP Army vs an ungarrisoned fort: CRT result 2, but the fort can inflict only 1 SP (7.31).
        FTPBattleResult result = Battle(6, 0, fort: true, attackerElites: 1, amphibious: ArmyNavalMove).BattleResult([1, 5, 6, 6]);

        Assert.Equal(1, result.DamageToAttacker);
        Assert.False(result.AttackerEliteLoss);
    }

    // ---- Decisions taken in the 2026-10-01 review ----

    [Fact]
    public void Rule5_72_Overrun_AttackerTenToOne_DefenderEliminated()
    {
        FTPBattleResult result = Battle(10, 1).BattleResult([1, 6, 6, 6]);

        Assert.True(result.Overrun);
        Assert.Equal(Winner.Attacker, result.Winner);
        Assert.Equal(1, result.DamageToDefender);
        Assert.Equal(0, result.DamageToAttacker);
        Assert.True(result.AttackerCanContinueMoving);
    }

    [Fact]
    public void Rule5_72_Overrun_DefenderTenToOne_AttackerEliminated()
    {
        FTPBattleResult result = Battle(1, 10).BattleResult([6, 1, 6, 6]);

        Assert.True(result.Overrun);
        Assert.Equal(Winner.Defender, result.Winner);
        Assert.Equal(1, result.DamageToAttacker);
        Assert.Equal(0, result.DamageToDefender);
        Assert.False(result.AttackerCanStay);
        Assert.Null(result.AttackerLeaderDeathDieRoll);
        Assert.Null(result.DefenderLeaderDeathDieRoll);
    }

    [Fact]
    public void Rule5_72_FortPreventsTheOverrun_EitherWay()
    {
        Assert.False(Battle(10, 1, fort: true).IsOverrun());
        Assert.False(Battle(1, 10, fort: true).IsOverrun());
        Assert.True(Battle(1, 10).IsOverrun());
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Rule7_32_OnlyArmyOrCorpsMovesContinue(bool divisionMove, bool canContinue)
    {
        // 6 SPs beat 3 (3* against 1) at 2-1: an Army or Corps may move on, a Division may not.
        FTPBattleResult result = Battle(6, 3, attackerDrm: 3, divisionMove: divisionMove).BattleResult([6, 1, 6, 6]);

        Assert.Equal(Winner.Attacker, result.Winner);
        Assert.True(result.AttackerCanStay);
        Assert.Equal(canContinue, result.AttackerCanContinueMoving);
    }

    [Fact]
    public void Rule5_72_OverrunningDivisionMayStillContinue()
    {
        // An overrun is not a battle, so 7.32's Army/Corps condition does not apply.
        FTPBattleResult result = Battle(10, 1, divisionMove: true).BattleResult([1, 6, 6, 6]);

        Assert.True(result.AttackerCanContinueMoving);
    }
}
