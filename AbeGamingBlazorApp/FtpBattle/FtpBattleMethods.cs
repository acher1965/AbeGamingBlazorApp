namespace AbeGamingBlazorApp.FtpBattle
{
    public static partial class FtpBattleMethods
    {
        public static (Ratio Ratio, bool InAttackerFavour) RatioDRM(this FtpLandBattle battle)
        {
            bool inAttackerFavour = battle.AttackerSize >= battle.DefenderSize;
            int larger = inAttackerFavour ? battle.AttackerSize : battle.DefenderSize;
            int smaller = inAttackerFavour ? battle.DefenderSize : battle.AttackerSize;

            Ratio ratio = smaller == 0 ? Ratio.TenToOnePlus : (larger / smaller) switch
            {
                >= 10 => Ratio.TenToOnePlus,
                >= 5 => Ratio.FiveToOnePlus,
                >= 4 => Ratio.FourToOne,
                >= 3 => Ratio.ThreeToOne,
                _ => Ratio.Low
            };

            return (ratio, inAttackerFavour);
        }
        public static BattleSize Size(this FtpLandBattle battle) => (battle.AttackerSize + battle.DefenderSize) switch
        {
            <= 5 => BattleSize.Small,
            <= 19 => BattleSize.Medium,
            _ => BattleSize.Large
        };

        public static FTPBattleResult BattleResult(FtpLandBattle battle, Random rng)
        {
            int attackerDieRoll = rng.Next(1, 7);
            int defenderDieRoll = rng.Next(1, 7);

            (int hitsToDefender, int hitsToAttacker, bool star, int defenderLeaderTop, int attackerLeaderTop) =
                FtpCRT.Outcome(battle, attackerDieRoll, defenderDieRoll);

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
                    attackerWipedOut = false;
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

            if (attackerLeaderTop > 0)
            {
                attackerLeaderDeathDieRoll = rng.Next(1, 7);
                attackerLeaderDeath = attackerLeaderDeathDieRoll <= attackerLeaderTop;
            }
            if (defenderLeaderTop > 0)
            {
                defenderLeaderDeathDieRoll = rng.Next(1, 7);
                defenderLeaderDeath = defenderLeaderDeathDieRoll <= defenderLeaderTop;
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
                                       defenderLeaderDeathDieRoll);
        }
    }
}
