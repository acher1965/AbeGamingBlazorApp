namespace AbeGaming.GameLogic
{
    public record HitStats(
        double MeanHtoD,
        double StdDevHtoD,
        double MeanHtoA,
        double StdDevHtoA,
        Dictionary<int, double> HitsToD_Prblty,
        Dictionary<int, double> HitsToA_Prblty)
    {
        public static implicit operator (double MeanHtoD, double StdDevHtoD, double MeanHtoA, double StdDevHtoA, Dictionary<int, double> HitsToD_Prblty, Dictionary<int, double> HitsToA_Prblty)(HitStats value)
        {
            return (value.MeanHtoD, value.StdDevHtoD, value.MeanHtoA, value.StdDevHtoA, value.HitsToD_Prblty, value.HitsToA_Prblty);
        }

        public static implicit operator HitStats((double MeanHtoD, double StdDevHtoD, double MeanHtoA, double StdDevHtoA, Dictionary<int, double> HitsToD_Prblty, Dictionary<int, double> HitsToA_Prblty) value)
        {
            return new HitStats(value.MeanHtoD, value.StdDevHtoD, value.MeanHtoA, value.StdDevHtoA, value.HitsToD_Prblty, value.HitsToA_Prblty);
        }
    }
}
