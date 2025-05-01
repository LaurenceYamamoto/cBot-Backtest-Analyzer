using System.Collections.Generic;

namespace cBotBacktestAnalyzer.Models
{
    public class AnalysisResult
    {
        public string? ParameterName { get; set; }
        public string? ParameterType { get; set; }
        public List<ParameterValueAnalysis> ValueAnalyses { get; set; } = new List<ParameterValueAnalysis>();
    }

    public class ParameterValueAnalysis
    {
        public string? ParameterValue { get; set; }
        public double NetProfitAverage { get; set; }
        public double MaxBalanceDrawdownPercentAverage { get; set; }
        public int PassCount { get; set; }
        
        // Additional metrics
        public double GrossProfitAverage { get; set; }
        public double ProfitFactorAverage { get; set; }
        public double AverageTradeAverage { get; set; }
        public double MaxEquityDrawdownPercentAverage { get; set; }
        public double MaxBalanceDrawdownAbsoluteAverage { get; set; }
        public double MaxEquityDrawdownAbsoluteAverage { get; set; }
        
        // 新しい指標を追加
        public double FitnessAverage { get; set; }
        public double EquityAverage { get; set; }
        public double BalanceAverage { get; set; }
        public double TradesAverage { get; set; }
        public double WinningTradesAverage { get; set; }
        public double LosingTradesAverage { get; set; }
        public double WinningRateAverage { get; set; }  // WinningTrades/Trades (%)
    }

    public class HeatMapData
    {
        public string? Parameter1Name { get; set; }
        public string? Parameter2Name { get; set; }
        public List<string> Parameter1Values { get; set; } = new List<string>();
        public List<string> Parameter2Values { get; set; } = new List<string>();
        public double[,]? NetProfitMap { get; set; }
        public double[,]? MaxBalanceDrawdownPercentMap { get; set; }
        public int[,]? PassCountMap { get; set; }

        // 新しい指標のマップを追加
        public double[,]? FitnessMap { get; set; }
        public double[,]? EquityMap { get; set; }
        public double[,]? BalanceMap { get; set; }
        public double[,]? TradesMap { get; set; }
        public double[,]? WinningTradesMap { get; set; }
        public double[,]? LosingTradesMap { get; set; }
        public double[,]? WinningRateMap { get; set; }  // WinningTrades/Trades (%)
        public double[,]? ProfitFactorMap { get; set; }
        public double[,]? GrossProfitMap { get; set; }
        public double[,]? MaxEquityDrawdownPercentMap { get; set; }
        public double[,]? MaxEquityDrawdownAbsoluteMap { get; set; }
        public double[,]? MaxBalanceDrawdownAbsoluteMap { get; set; }
        public double[,]? AverageTradeMap { get; set; }
    }

    public class CombinedPassData
    {
        public int PassId { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        public double NetProfit { get; set; }
        public double MaxBalanceDrawdownPercent { get; set; }
        
        // Additional metrics
        public double GrossProfit { get; set; }
        public double? ProfitFactor { get; set; }
        public double? AverageTrade { get; set; }
        public double MaxEquityDrawdownPercent { get; set; }
        public double MaxBalanceDrawdownAbsolute { get; set; }
        public double MaxEquityDrawdownAbsolute { get; set; }
        
        // 新しい指標を追加
        public double Fitness { get; set; }
        public double Equity { get; set; }
        public double Balance { get; set; }
        public int Trades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        
        // 計算プロパティ
        public double WinningRate => Trades > 0 ? (double)WinningTrades / Trades * 100 : 0;
    }
} 