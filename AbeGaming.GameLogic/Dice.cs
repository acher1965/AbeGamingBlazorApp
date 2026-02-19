using System;
using System.Collections.Generic;
using System.Text;

namespace AbeGaming.GameLogic
{
    public static class Dice
    {
        public static Span<int> RollOneDieRepeatedly(int count) => Random.Shared.GetItems(Dice.Die, count);

        public static readonly int[] Die = { 1, 2, 3, 4, 5, 6 };

        public static readonly (int black, int white)[,] TwoDice = new (int black, int white)[,]
        {
              { (1, 1), (1, 2), (1, 3), (1, 4), (1, 5), (1, 6) },
              { (2, 1), (2, 2), (2, 3), (2, 4), (2, 5), (2, 6) },
              { (3, 1), (3, 2), (3, 3), (3, 4), (3, 5), (3, 6) },
              { (4, 1), (4, 2), (4, 3), (4, 4), (4, 5), (4, 6) },
              { (5, 1), (5, 2), (5, 3), (5, 4), (5, 5), (5, 6) },
              { (6, 1), (6, 2), (6, 3), (6, 4), (6, 5), (6, 6) }
        };
        public static readonly (int black, int white, int red)[,,] ThreeDice = PrecalcThreeDice();

        private static (int black, int white, int red)[,,] PrecalcThreeDice()
        {
            var result = new (int black, int white, int red)[6, 6, 6];
            for (int black = 1; black <= 6; black++)
            {
                for (int white = 1; white <= 6; white++)
                {
                    for (int red = 1; red <= 6; red++)
                    {
                        result[black - 1, white - 1, red - 1] = (black, white, red);
                    }
                }
            }
            return result;
        }
    }
}
