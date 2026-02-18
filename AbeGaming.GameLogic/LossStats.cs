using System;
using System.Collections.Generic;
using System.Text;

namespace AbeGaming.GameLogic
{
    //TODO use in FtpMonteCarloResult as well (?)
    public record LossStats(
        double MeanHtoA, 
        double StdDevHtoA, 
        double MeanHtoD, 
        double StdDevHtoD,
        Dictionary<int, double> HitsD_Prblty,
        Dictionary<int, double> HitsA_Prblty)
    {
        public static implicit operator (double MeanHtoA, double StdDevHtoA, double MeanHtoD, double StdDevHtoD, Dictionary<int, double> HitsD_Prblty, Dictionary<int, double> HitsA_Prblty)(LossStats value)
        {
            return (value.MeanHtoA, value.StdDevHtoA, value.MeanHtoD, value.StdDevHtoD, value.HitsD_Prblty, value.HitsA_Prblty);
        }

        public static implicit operator LossStats((double MeanHtoA, double StdDevHtoA, double MeanHtoD, double StdDevHtoD, Dictionary<int, double> HitsD_Prblty, Dictionary<int, double> HitsA_Prblty) value)
        {
            return new LossStats(value.MeanHtoA, value.StdDevHtoA, value.MeanHtoD, value.StdDevHtoD, value.HitsD_Prblty, value.HitsA_Prblty);
        }
    }
}
