using System;
using System.Collections.Generic;

namespace cBotBacktestAnalyzer.Models
{
    public class FilterCriteria
    {
        public List<NumericFilterCriterion> NumericCriteria { get; set; } = new List<NumericFilterCriterion>();
        public List<EnumFilterCriterion> EnumCriteria { get; set; } = new List<EnumFilterCriterion>();
        public List<ParameterFilterCriterion> ParameterCriteria { get; set; } = new List<ParameterFilterCriterion>();
        
        public bool PassesFilter(CombinedPassData pass)
        {
            // すべての数値基準を満たすか確認
            foreach (var criterion in NumericCriteria)
            {
                if (!criterion.PassesCriterion(pass))
                {
                    return false;
                }
            }
            
            // すべての列挙型基準を満たすか確認
            foreach (var criterion in EnumCriteria)
            {
                if (!criterion.PassesCriterion(pass))
                {
                    return false;
                }
            }
            
            // すべてのパラメータ基準を満たすか確認
            foreach (var criterion in ParameterCriteria)
            {
                if (!criterion.PassesCriterion(pass))
                {
                    return false;
                }
            }
            
            // すべての基準を満たした場合
            return true;
        }
    }
    
    public class NumericFilterCriterion
    {
        public string MetricName { get; set; } = string.Empty;
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        public bool IsMinInclusive { get; set; } = true;   // デフォルトでは最小値を含む
        public bool IsMaxInclusive { get; set; } = false;  // デフォルトでは最大値を含まない
        
        public bool PassesCriterion(CombinedPassData pass)
        {
            double value = GetMetricValue(pass, MetricName);
            
            // 最小値と最大値の両方が設定されていない場合はフィルタリングしない
            if (!MinValue.HasValue && !MaxValue.HasValue)
            {
                return true;
            }
            
            // 最小値のみ設定されている場合
            if (MinValue.HasValue && !MaxValue.HasValue)
            {
                return IsMinInclusive ? value >= MinValue.Value : value > MinValue.Value;
            }
            
            // 最大値のみ設定されている場合
            if (!MinValue.HasValue && MaxValue.HasValue)
            {
                return IsMaxInclusive ? value <= MaxValue.Value : value < MaxValue.Value;
            }
            
            // 両方設定されている場合
            bool passesMin = IsMinInclusive ? value >= MinValue!.Value : value > MinValue!.Value;
            bool passesMax = IsMaxInclusive ? value <= MaxValue!.Value : value < MaxValue!.Value;
            return passesMin && passesMax;
        }
        
        private double GetMetricValue(CombinedPassData pass, string metricName)
        {
            return metricName switch
            {
                "Net Profit" => pass.NetProfit,
                "Max Balance Drawdown (%)" => pass.MaxBalanceDrawdownPercent * 100, // パーセントに変換
                "Profit Factor" => pass.ProfitFactor ?? 0,
                "Average Trade" => pass.AverageTrade ?? 0,
                "Max Equity Drawdown (%)" => pass.MaxEquityDrawdownPercent * 100, // パーセントに変換
                "Max Balance Drawdown (Absolute)" => pass.MaxBalanceDrawdownAbsolute,
                "Max Equity Drawdown (Absolute)" => pass.MaxEquityDrawdownAbsolute,
                "Fitness" => pass.Fitness,
                "Equity" => pass.Equity,
                "Balance" => pass.Balance,
                "Trades" => pass.Trades,
                "Winning Trades" => pass.WinningTrades,
                "Losing Trades" => pass.LosingTrades,
                "Winning Rate (%)" => pass.WinningRate,
                _ => 0
            };
        }
    }
    
    public class ParameterFilterCriterion
    {
        public string ParameterName { get; set; } = string.Empty;
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        public bool IsMinInclusive { get; set; } = true;   // デフォルトでは最小値を含む
        public bool IsMaxInclusive { get; set; } = false;  // デフォルトでは最大値を含まない
        
        public bool PassesCriterion(CombinedPassData pass)
        {
            // パラメータが存在するか確認
            if (!pass.Parameters.TryGetValue(ParameterName, out string? paramValue) || paramValue == null)
            {
                return false;
            }
            
            // パラメータを数値に変換
            if (!double.TryParse(paramValue, out double value))
            {
                return false;
            }
            
            // 最小値と最大値の両方が設定されていない場合はフィルタリングしない
            if (!MinValue.HasValue && !MaxValue.HasValue)
            {
                return true;
            }
            
            // 最小値のみ設定されている場合
            if (MinValue.HasValue && !MaxValue.HasValue)
            {
                return IsMinInclusive ? value >= MinValue.Value : value > MinValue.Value;
            }
            
            // 最大値のみ設定されている場合
            if (!MinValue.HasValue && MaxValue.HasValue)
            {
                return IsMaxInclusive ? value <= MaxValue.Value : value < MaxValue.Value;
            }
            
            // 両方設定されている場合
            bool passesMin = IsMinInclusive ? value >= MinValue!.Value : value > MinValue!.Value;
            bool passesMax = IsMaxInclusive ? value <= MaxValue!.Value : value < MaxValue!.Value;
            return passesMin && passesMax;
        }
    }
    
    public class EnumFilterCriterion
    {
        public string ParameterName { get; set; } = string.Empty;
        public List<string> SelectedValues { get; set; } = new List<string>();
        
        public bool PassesCriterion(CombinedPassData pass)
        {
            // 選択された値がない場合はフィルタリングしない
            if (SelectedValues.Count == 0)
            {
                return true;
            }
            
            // パラメータが存在するか確認
            if (!pass.Parameters.TryGetValue(ParameterName, out string? value) || value == null)
            {
                return false;
            }
            
            // 選択された値のいずれかに一致するか確認
            return SelectedValues.Contains(value);
        }
    }
} 