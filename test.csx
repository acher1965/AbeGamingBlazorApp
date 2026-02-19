using AbeGaming.GameLogic.FtP;
var battle = new FtpBattle(false, true, true, false, 5, 5, 0, 0, 0, 0, false, false, false);
Console.WriteLine($"Battle: {battle}");
var stats = battle.ExactStats();
Console.WriteLine($"ExactStats - AWin: {stats.AttackerWinProbability:P2}, DWin: {stats.DefenderWinProbability:P2}");
Console.WriteLine($"MeanHtoA: {stats.HitsStats.MeanHtoA:F2}, MeanHtoD: {stats.HitsStats.MeanHtoD:F2}");
