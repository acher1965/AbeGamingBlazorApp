using AbeGaming.GameLogic.PoG;
using System.ComponentModel.Design;
using System.Drawing;

namespace AbeGaming.GameLogic.FtP
{
    public static partial class FtpBattleMethods
    {
        public static int DRM_fromRatio(this Ratio ratio) => ratio switch
        {
            Ratio.ThreeToOne => 2,
            Ratio.FourToOne => 3,
            Ratio.FiveToOnePlus or Ratio.TenToOnePlus => 4,
            _ => 0
        };

        public static (Ratio Ratio, bool InAttackerFavour) BattleRatio(this FtpBattle battle)
        {
            bool inAttackerFavour = battle.AttackerSize >= battle.DefenderSize;
            int larger = inAttackerFavour ? battle.AttackerSize : battle.DefenderSize;
            int smaller = inAttackerFavour ? battle.DefenderSize : battle.AttackerSize;

            Ratio ratio = smaller == 0 ? Ratio.TenToOnePlus : (larger / smaller) switch
            {
                >= 10 when inAttackerFavour => Ratio.TenToOnePlus,
                >= 5 => Ratio.FiveToOnePlus,
                >= 4 => Ratio.FourToOne,
                >= 3 => Ratio.ThreeToOne,
                _ => Ratio.Low
            };

            return (ratio, inAttackerFavour);
        }

        public static BattleSize Size(this FtpBattle battle) => (battle.AttackerSize + battle.DefenderSize) switch
        {
            <= 5 => BattleSize.Small,
            <= 19 => BattleSize.Medium,
            _ => BattleSize.Large
        };

        public static bool IsOverrun(this FtpBattle battle)
        {
            (Ratio ratio, bool inAttackerFavour) = battle.BattleRatio();
            return ratio == Ratio.TenToOnePlus && inAttackerFavour && !battle.FortPresent;
        }

        /// <summary>
        /// One battle outcome
        /// </summary>
        /// <param name="battle"></param>
        /// <param name="FourDiceRolls">
        /// First is attacker die roll,
        /// second is defender die roll,
        /// third is attacker leader death die roll (if needed),
        /// fourth is defender leader death die roll (if needed)
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static FTPBattleResult BattleResult(this FtpBattle battle, Span<int> FourDiceRolls)
        {
            if (FourDiceRolls.Length < 4)
                throw new ArgumentException($"{nameof(FtpBattleMethods)}.{BattleResult}(): 4 random numbers must be passed in",
                    nameof(FourDiceRolls));
            int attackerDieRoll = FourDiceRolls[0];
            int defenderDieRoll = FourDiceRolls[1];

            (int hitsToDefender, int hitsToAttacker, bool star, int defenderLeaderDeathTop, int attackerLeaderDeathTop) =
               battle.Outcome(attackerDieRoll, defenderDieRoll);

            Winner winner = Winner.Defender;
            bool attackerCanStay = false;

            if (hitsToDefender > hitsToAttacker
             || (hitsToDefender == hitsToAttacker && star))
            {
                winner = Winner.Attacker;
                attackerCanStay = true;
            }

            //final hits logic
            int finalHitsToDefender = Math.Min(hitsToDefender, 2 * battle.AttackerSize);
            int finalHitsToAttacker = Math.Min(hitsToAttacker, 2 * battle.DefenderSize);

            bool defenderWipedOut = finalHitsToDefender >= battle.DefenderSize;
            bool attackerWipedOut = finalHitsToAttacker >= battle.AttackerSize;

            if (defenderWipedOut && attackerWipedOut)
            {
                if (winner == Winner.Attacker)
                {
                    finalHitsToAttacker = battle.AttackerSize - 1;
                    attackerWipedOut = false;
                }
                else if (winner == Winner.Defender)
                {
                    finalHitsToDefender = battle.DefenderSize - 1;
                    defenderWipedOut = false;
                }
            }

            //continue moving logic
            bool attackerCanContinueMoving = false;
            if (defenderWipedOut && winner == Winner.Defender && !battle.FortPresent)
            {
                attackerCanStay = true;
                if (battle.AttackerSize >= 2 * battle.DefenderSize)
                    attackerCanContinueMoving = true;
            }
            if (winner == Winner.Attacker && (battle.AttackerSize >= 2 * battle.DefenderSize))
                attackerCanContinueMoving = true;

            //leader death logic            
            int? attackerLeaderDeathDieRoll = null;
            int? defenderLeaderDeathDieRoll = null;
            bool attackerLeaderDeath = false;
            bool defenderLeaderDeath = false;

            if (attackerLeaderDeathTop > 0)
            {
                attackerLeaderDeathDieRoll = FourDiceRolls[2];
                attackerLeaderDeath = attackerLeaderDeathDieRoll <= attackerLeaderDeathTop;
            }
            if (defenderLeaderDeathTop > 0)
            {
                defenderLeaderDeathDieRoll = FourDiceRolls[3];
                defenderLeaderDeath = defenderLeaderDeathDieRoll <= defenderLeaderDeathTop;
            }

            return new FTPBattleResult(winner,
                                       finalHitsToDefender,
                                       finalHitsToAttacker,
                                       attackerLeaderDeath,
                                       defenderLeaderDeath,
                                       attackerCanStay,
                                       attackerCanContinueMoving,
                                       battle.Size(),
                                       attackerDieRoll,
                                       defenderDieRoll,
                                       star,
                                       attackerLeaderDeathDieRoll,
                                       defenderLeaderDeathDieRoll,
                                       battle.IsOverrun());
        }

        public static FtpStats ExactStats(this FtpBattle battle)
        {
            List<FTPBattleResult> results = new List<FTPBattleResult>(36 * 36);
            int[] fourDiceRolls = [0, 0, 1, 1];
            int[] hitsToAVector = new int[36];
            int[] hitsToDVector = new int[36];
            int totalAttackerWins = 0;
            int totalDefenderWins = 0;
            int totalStars = 0;
            int totalAttackerLeaderDeaths = 0;
            int totalDefenderLeaderDeaths = 0;
            BattleSize size = battle.Size();
            int i = 0;
            foreach ((int black, int white) in Dice.TwoDice)
            {
                fourDiceRolls[0] = black;
                fourDiceRolls[1] = white;
                FTPBattleResult r = battle.BattleResult(fourDiceRolls);
                results.Add(r);
                hitsToAVector[i] = r.DamageToAttacker;
                hitsToDVector[i] = r.DamageToDefender;
                totalAttackerWins += r.Winner == Winner.Attacker ? 1 : 0;
                totalDefenderWins += r.Winner == Winner.Defender ? 1 : 0;
                totalStars += r.Star ? 1 : 0;
                totalAttackerLeaderDeaths += r.AttackerLeaderDeath ? 1 : 0;
                totalDefenderLeaderDeaths += r.DefenderLeaderDeath ? 1 : 0;
                i++;
            }
            (double MeanToA, double StdDevToA) = IntArrayStatHelpers.MeanAndStdDevVectorised(hitsToAVector);
            (double MeanToD, double StdDevToD) = IntArrayStatHelpers.MeanAndStdDevVectorised(hitsToDVector);

            double[] distributionHtoA = IntArrayStatHelpers.CalculateDistribution(hitsToAVector, FtpCRT.MaxHitsToA[size]);
            double[] distributionHtoD = IntArrayStatHelpers.CalculateDistribution(hitsToDVector, FtpCRT.MaxHitsToD[size]);

            HitStats hitsStats = (MeanToD, StdDevToD,
                MeanToA, StdDevToA,
                distributionHtoD.Index().ToDictionary(x => x.Index, x => x.Item),
                distributionHtoA.Index().ToDictionary(x => x.Index, x => x.Item));
            return new FtpStats(
                size,
                (double)totalAttackerWins / 36,
                (double)totalDefenderWins / 36,
                hitsStats,
                (double)totalAttackerLeaderDeaths / 36,
                (double)totalDefenderLeaderDeaths / 36,
                (double)totalStars / 36);
        }
    }
}
