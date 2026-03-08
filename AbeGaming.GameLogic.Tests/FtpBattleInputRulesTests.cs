using AbeGaming.GameLogic.FtP;

namespace AbeGaming.GameLogic.Tests
{
    public class FtpBattleInputRulesTests
    {
        [Theory]
        [InlineData(false, false, 15)]
        [InlineData(true, true, 9)]
        [InlineData(true, false, 3)]
        public void AttackerMaxSize_ReturnsExpectedValue(bool isAmphibious, bool isArmyMove, int expected)
        {
            int result = FtpBattleInputRules.AttackerMaxSize(isAmphibious, isArmyMove);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(9, true, false, 3)]
        [InlineData(9, true, true, 9)]
        [InlineData(25, false, false, 15)]
        [InlineData(0, true, false, 1)]
        public void ClampAttackerSize_AppliesBounds(int value, bool isAmphibious, bool isArmyMove, int expected)
        {
            int result = FtpBattleInputRules.ClampAttackerSize(value, isAmphibious, isArmyMove);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ClampAttackerSize_WhenAmphibiousArmyMoveTurnsOff_ClampsFromNineToThree()
        {
            int attackerSize = FtpBattleInputRules.ClampAttackerSize(9, isAmphibious: true, amphibiousIsArmyMove: true);
            attackerSize = FtpBattleInputRules.ClampAttackerSize(attackerSize, isAmphibious: true, amphibiousIsArmyMove: false);

            Assert.Equal(3, attackerSize);
        }

        [Theory]
        [InlineData(true, true, 0)]
        [InlineData(true, false, 1)]
        [InlineData(false, true, 1)]
        public void DefenderMinSize_ReturnsExpectedValue(bool isAmphibious, bool fortPresent, int expected)
        {
            int result = FtpBattleInputRules.DefenderMinSize(isAmphibious, fortPresent);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ClampDefenderSize_WhenFortTurnsOffInAmphibious_ClampsFromZeroToOne()
        {
            int defenderSize = FtpBattleInputRules.ClampDefenderSize(0, isAmphibious: true, fortPresent: true);
            defenderSize = FtpBattleInputRules.ClampDefenderSize(defenderSize, isAmphibious: true, fortPresent: false);

            Assert.Equal(1, defenderSize);
        }

        [Theory]
        [InlineData(-2, 0)]
        [InlineData(5, 5)]
        [InlineData(9, 9)]
        [InlineData(12, 9)]
        public void ClampDrm_AppliesBounds(int value, int expected)
        {
            int result = FtpBattleInputRules.ClampDrm(value);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, 0)]
        [InlineData(2, 2)]
        [InlineData(4, 2)]
        public void ClampElites_AppliesBounds(int value, int expected)
        {
            int result = FtpBattleInputRules.ClampElites(value);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, 0)]
        [InlineData(5, 5)]
        [InlineData(8, 5)]
        public void ClampAmphibiousAssaultLevel_AppliesBounds(int value, int expected)
        {
            int result = FtpBattleInputRules.ClampAmphibiousAssaultLevel(value);
            Assert.Equal(expected, result);
        }
    }
}
