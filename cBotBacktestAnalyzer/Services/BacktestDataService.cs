using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using cBotBacktestAnalyzer.Models;
using Newtonsoft.Json;

namespace cBotBacktestAnalyzer.Services
{
    public class BacktestDataService
    {
        private BacktestData? _backtestData;
        private Dictionary<int, Dictionary<string, string>> _passParameters = new Dictionary<int, Dictionary<string, string>>();
        private List<CombinedPassData> _combinedPassData = new List<CombinedPassData>();
        private string _backtestFolderPath = string.Empty;

        public BacktestData? BacktestData => _backtestData;
        public List<CombinedPassData> CombinedPassData => _combinedPassData;
        public List<Parameter> OptimizableParameters => _backtestData?.Parameters?.Where(p => p.Optimize).ToList() ?? new List<Parameter>();
        public bool IsDataLoaded => _backtestData != null;

        public async Task<bool> LoadBacktestDataAsync(string filePath)
        {
            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                _backtestData = JsonConvert.DeserializeObject<BacktestData>(json);

                if (_backtestData != null)
                {
                    _backtestFolderPath = Path.Combine(
                        Path.GetDirectoryName(filePath) ?? string.Empty,
                        Path.GetFileNameWithoutExtension(filePath));

                    await LoadAllPassParametersAsync();
                    CreateCombinedPassData();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading backtest data: {ex.Message}");
                return false;
            }
        }

        private async Task LoadAllPassParametersAsync()
        {
            _passParameters.Clear();

            if (_backtestData?.Results?.Passes == null)
                return;

            foreach (var pass in _backtestData.Results.Passes.Where(p => p.Status == "Completed"))
            {
                string parameterFilePath = Path.Combine(_backtestFolderPath, pass.PassId.ToString(), "parameters.cbotset");
                if (File.Exists(parameterFilePath))
                {
                    try
                    {
                        string json = await File.ReadAllTextAsync(parameterFilePath);
                        var parametersFile = JsonConvert.DeserializeObject<ParametersFile>(json);
                        
                        if (parametersFile?.Parameters != null)
                        {
                            _passParameters[pass.PassId] = parametersFile.Parameters;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading parameters for pass {pass.PassId}: {ex.Message}");
                    }
                }
            }
        }

        private void CreateCombinedPassData()
        {
            _combinedPassData.Clear();

            if (_backtestData?.Results?.Passes == null)
                return;

            foreach (var pass in _backtestData.Results.Passes.Where(p => p.Status == "Completed"))
            {
                if (_passParameters.TryGetValue(pass.PassId, out var parameters))
                {
                    _combinedPassData.Add(new CombinedPassData
                    {
                        PassId = pass.PassId,
                        Parameters = parameters,
                        NetProfit = pass.NetProfit,
                        MaxBalanceDrawdownPercent = pass.MaxBalanceDrawdownPercent,
                        GrossProfit = pass.GrossProfit,
                        ProfitFactor = pass.ProfitFactor,
                        AverageTrade = pass.AverageTrade,
                        MaxEquityDrawdownPercent = pass.MaxEquityDrawdownPercent,
                        MaxBalanceDrawdownAbsolute = pass.MaxBalanceDrawdownAbsolute,
                        MaxEquityDrawdownAbsolute = pass.MaxEquityDrawdownAbsolute,
                        Fitness = pass.Fitness,
                        Equity = pass.Equity,
                        Balance = pass.Balance,
                        Trades = pass.Trades,
                        WinningTrades = pass.WinningTrades,
                        LosingTrades = pass.LosingTrades
                    });
                }
            }
        }

        public List<AnalysisResult> AnalyzeParameters(FilterCriteria? filterCriteria = null, bool useMedian = false)
        {
            var results = new List<AnalysisResult>();

            if (_backtestData == null || _combinedPassData.Count == 0)
                return results;

            if (OptimizableParameters == null)
                return results;

            var filteredPassData = _combinedPassData;
            if (filterCriteria != null)
            {
                filteredPassData = _combinedPassData
                    .Where(cp => filterCriteria.PassesFilter(cp))
                    .ToList();
            }

            foreach (var parameter in OptimizableParameters)
            {
                if (parameter.Name == null)
                    continue;

                var result = new AnalysisResult
                {
                    ParameterName = parameter.Metadata?.FriendlyName ?? parameter.Name,
                    ParameterType = parameter.ParameterType
                };

                var uniqueValues = filteredPassData
                    .Where(cp => cp.Parameters.ContainsKey(parameter.Name))
                    .Select(cp => cp.Parameters[parameter.Name])
                    .Distinct()
                    .ToList();

                foreach (var value in uniqueValues)
                {
                    var passesWithValue = filteredPassData
                        .Where(cp => cp.Parameters.ContainsKey(parameter.Name) && cp.Parameters[parameter.Name] == value)
                        .ToList();

                    if (passesWithValue.Count > 0)
                    {
                        var analysis = new ParameterValueAnalysis
                        {
                            ParameterValue = value,
                            PassCount = passesWithValue.Count
                        };
                        
                        if (useMedian)
                        {
                            analysis.NetProfitAverage = CalculateMedian(passesWithValue, p => p.NetProfit);
                            analysis.MaxBalanceDrawdownPercentAverage = CalculateMedian(passesWithValue, p => p.MaxBalanceDrawdownPercent);
                            analysis.GrossProfitAverage = CalculateMedian(passesWithValue, p => p.GrossProfit);
                            analysis.ProfitFactorAverage = CalculateMedian(passesWithValue, p => p.ProfitFactor ?? 0);
                            analysis.AverageTradeAverage = CalculateMedian(passesWithValue, p => p.AverageTrade ?? 0);
                            analysis.MaxEquityDrawdownPercentAverage = CalculateMedian(passesWithValue, p => p.MaxEquityDrawdownPercent);
                            analysis.MaxBalanceDrawdownAbsoluteAverage = CalculateMedian(passesWithValue, p => p.MaxBalanceDrawdownAbsolute);
                            analysis.MaxEquityDrawdownAbsoluteAverage = CalculateMedian(passesWithValue, p => p.MaxEquityDrawdownAbsolute);
                            analysis.FitnessAverage = CalculateMedian(passesWithValue, p => p.Fitness);
                            analysis.EquityAverage = CalculateMedian(passesWithValue, p => p.Equity);
                            analysis.BalanceAverage = CalculateMedian(passesWithValue, p => p.Balance);
                            analysis.TradesAverage = CalculateMedian(passesWithValue, p => p.Trades);
                            analysis.WinningTradesAverage = CalculateMedian(passesWithValue, p => p.WinningTrades);
                            analysis.LosingTradesAverage = CalculateMedian(passesWithValue, p => p.LosingTrades);
                            analysis.WinningRateAverage = CalculateMedian(passesWithValue, p => p.WinningRate);
                        }
                        else
                        {
                            analysis.NetProfitAverage = passesWithValue.Average(p => p.NetProfit);
                            analysis.MaxBalanceDrawdownPercentAverage = passesWithValue.Average(p => p.MaxBalanceDrawdownPercent);
                            analysis.GrossProfitAverage = passesWithValue.Average(p => p.GrossProfit);
                            analysis.ProfitFactorAverage = passesWithValue.Average(p => p.ProfitFactor ?? 0);
                            analysis.AverageTradeAverage = passesWithValue.Average(p => p.AverageTrade ?? 0);
                            analysis.MaxEquityDrawdownPercentAverage = passesWithValue.Average(p => p.MaxEquityDrawdownPercent);
                            analysis.MaxBalanceDrawdownAbsoluteAverage = passesWithValue.Average(p => p.MaxBalanceDrawdownAbsolute);
                            analysis.MaxEquityDrawdownAbsoluteAverage = passesWithValue.Average(p => p.MaxEquityDrawdownAbsolute);
                            analysis.FitnessAverage = passesWithValue.Average(p => p.Fitness);
                            analysis.EquityAverage = passesWithValue.Average(p => p.Equity);
                            analysis.BalanceAverage = passesWithValue.Average(p => p.Balance);
                            analysis.TradesAverage = passesWithValue.Average(p => p.Trades);
                            analysis.WinningTradesAverage = passesWithValue.Average(p => p.WinningTrades);
                            analysis.LosingTradesAverage = passesWithValue.Average(p => p.LosingTrades);
                            analysis.WinningRateAverage = passesWithValue.Average(p => p.WinningRate);
                        }

                        result.ValueAnalyses.Add(analysis);
                    }
                }

                if (parameter.ParameterType == "Double" || parameter.ParameterType == "Integer")
                {
                    result.ValueAnalyses = result.ValueAnalyses
                        .OrderBy(va => 
                        {
                            if (va.ParameterValue != null && double.TryParse(va.ParameterValue, out double val))
                                return val;
                            return 0;
                        })
                        .ToList();
                }
                else
                {
                    result.ValueAnalyses = result.ValueAnalyses
                        .OrderBy(va => va.ParameterValue)
                        .ToList();
                }

                results.Add(result);
            }

            return results;
        }

        public HeatMapData CreateHeatMap(string? parameter1Name, string? parameter2Name, FilterCriteria? filterCriteria = null, bool useMedian = false)
        {
            var result = new HeatMapData
            {
                Parameter1Name = parameter1Name,
                Parameter2Name = parameter2Name
            };

            if (_backtestData == null || _combinedPassData.Count == 0 || parameter1Name == null || parameter2Name == null)
                return result;

            var filteredPassData = _combinedPassData;
            if (filterCriteria != null)
            {
                filteredPassData = _combinedPassData
                    .Where(cp => filterCriteria.PassesFilter(cp))
                    .ToList();
            }

            var parameter1Values = filteredPassData
                .Where(cp => cp.Parameters.ContainsKey(parameter1Name))
                .Select(cp => cp.Parameters[parameter1Name])
                .Distinct()
                .ToList();

            var parameter2Values = filteredPassData
                .Where(cp => cp.Parameters.ContainsKey(parameter2Name))
                .Select(cp => cp.Parameters[parameter2Name])
                .Distinct()
                .ToList();

            var parameter1 = _backtestData.Parameters?.FirstOrDefault(p => p.Name == parameter1Name);
            var parameter2 = _backtestData.Parameters?.FirstOrDefault(p => p.Name == parameter2Name);

            if (parameter1?.ParameterType == "Double" || parameter1?.ParameterType == "Integer")
            {
                parameter1Values = parameter1Values
                    .OrderBy(v => 
                    {
                        if (double.TryParse(v, out double val))
                            return val;
                        return 0;
                    })
                    .ToList();
            }
            else
            {
                parameter1Values = parameter1Values.OrderBy(v => v).ToList();
            }

            if (parameter2?.ParameterType == "Double" || parameter2?.ParameterType == "Integer")
            {
                parameter2Values = parameter2Values
                    .OrderBy(v => 
                    {
                        if (double.TryParse(v, out double val))
                            return val;
                        return 0;
                    })
                    .ToList();
            }
            else
            {
                parameter2Values = parameter2Values.OrderBy(v => v).ToList();
            }

            result.Parameter1Values = parameter1Values;
            result.Parameter2Values = parameter2Values;
            
            int rows = parameter1Values.Count;
            int cols = parameter2Values.Count;
            
            result.NetProfitMap = new double[rows, cols];
            result.MaxBalanceDrawdownPercentMap = new double[rows, cols];
            result.PassCountMap = new int[rows, cols];
            
            result.FitnessMap = new double[rows, cols];
            result.EquityMap = new double[rows, cols];
            result.BalanceMap = new double[rows, cols];
            result.TradesMap = new double[rows, cols];
            result.WinningTradesMap = new double[rows, cols];
            result.LosingTradesMap = new double[rows, cols];
            result.WinningRateMap = new double[rows, cols];
            result.ProfitFactorMap = new double[rows, cols];
            result.GrossProfitMap = new double[rows, cols];
            result.MaxEquityDrawdownPercentMap = new double[rows, cols];
            result.MaxEquityDrawdownAbsoluteMap = new double[rows, cols];
            result.MaxBalanceDrawdownAbsoluteMap = new double[rows, cols];
            result.AverageTradeMap = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result.NetProfitMap[i, j] = double.NaN;
                    result.MaxBalanceDrawdownPercentMap[i, j] = double.NaN;
                    result.FitnessMap[i, j] = double.NaN;
                    result.EquityMap[i, j] = double.NaN;
                    result.BalanceMap[i, j] = double.NaN;
                    result.TradesMap[i, j] = double.NaN;
                    result.WinningTradesMap[i, j] = double.NaN;
                    result.LosingTradesMap[i, j] = double.NaN;
                    result.WinningRateMap[i, j] = double.NaN;
                    result.ProfitFactorMap[i, j] = double.NaN;
                    result.GrossProfitMap[i, j] = double.NaN;
                    result.MaxEquityDrawdownPercentMap[i, j] = double.NaN;
                    result.MaxEquityDrawdownAbsoluteMap[i, j] = double.NaN;
                    result.MaxBalanceDrawdownAbsoluteMap[i, j] = double.NaN;
                    result.AverageTradeMap[i, j] = double.NaN;
                    result.PassCountMap[i, j] = 0;
                }
            }

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    var passesWithValues = filteredPassData
                        .Where(cp => 
                            cp.Parameters.ContainsKey(parameter1Name) && 
                            cp.Parameters[parameter1Name] == parameter1Values[i] &&
                            cp.Parameters.ContainsKey(parameter2Name) && 
                            cp.Parameters[parameter2Name] == parameter2Values[j])
                        .ToList();

                    if (passesWithValues.Count > 0)
                    {
                        if (useMedian)
                        {
                            result.NetProfitMap[i, j] = CalculateMedian(passesWithValues, p => p.NetProfit);
                            result.MaxBalanceDrawdownPercentMap[i, j] = CalculateMedian(passesWithValues, p => p.MaxBalanceDrawdownPercent);
                            result.FitnessMap[i, j] = CalculateMedian(passesWithValues, p => p.Fitness);
                            result.EquityMap[i, j] = CalculateMedian(passesWithValues, p => p.Equity);
                            result.BalanceMap[i, j] = CalculateMedian(passesWithValues, p => p.Balance);
                            result.TradesMap[i, j] = CalculateMedian(passesWithValues, p => p.Trades);
                            result.WinningTradesMap[i, j] = CalculateMedian(passesWithValues, p => p.WinningTrades);
                            result.LosingTradesMap[i, j] = CalculateMedian(passesWithValues, p => p.LosingTrades);
                            result.WinningRateMap[i, j] = CalculateMedian(passesWithValues, p => p.WinningRate);
                            result.ProfitFactorMap[i, j] = CalculateMedian(passesWithValues, p => p.ProfitFactor ?? 0);
                            result.GrossProfitMap[i, j] = CalculateMedian(passesWithValues, p => p.GrossProfit);
                            result.MaxEquityDrawdownPercentMap[i, j] = CalculateMedian(passesWithValues, p => p.MaxEquityDrawdownPercent);
                            result.MaxEquityDrawdownAbsoluteMap[i, j] = CalculateMedian(passesWithValues, p => p.MaxEquityDrawdownAbsolute);
                            result.MaxBalanceDrawdownAbsoluteMap[i, j] = CalculateMedian(passesWithValues, p => p.MaxBalanceDrawdownAbsolute);
                            result.AverageTradeMap[i, j] = CalculateMedian(passesWithValues, p => p.AverageTrade ?? 0);
                        }
                        else
                        {
                            result.NetProfitMap[i, j] = passesWithValues.Average(p => p.NetProfit);
                            result.MaxBalanceDrawdownPercentMap[i, j] = passesWithValues.Average(p => p.MaxBalanceDrawdownPercent);
                            result.FitnessMap[i, j] = passesWithValues.Average(p => p.Fitness);
                            result.EquityMap[i, j] = passesWithValues.Average(p => p.Equity);
                            result.BalanceMap[i, j] = passesWithValues.Average(p => p.Balance);
                            result.TradesMap[i, j] = passesWithValues.Average(p => p.Trades);
                            result.WinningTradesMap[i, j] = passesWithValues.Average(p => p.WinningTrades);
                            result.LosingTradesMap[i, j] = passesWithValues.Average(p => p.LosingTrades);
                            result.WinningRateMap[i, j] = passesWithValues.Average(p => p.WinningRate);
                            result.ProfitFactorMap[i, j] = passesWithValues.Average(p => p.ProfitFactor ?? 0);
                            result.GrossProfitMap[i, j] = passesWithValues.Average(p => p.GrossProfit);
                            result.MaxEquityDrawdownPercentMap[i, j] = passesWithValues.Average(p => p.MaxEquityDrawdownPercent);
                            result.MaxEquityDrawdownAbsoluteMap[i, j] = passesWithValues.Average(p => p.MaxEquityDrawdownAbsolute);
                            result.MaxBalanceDrawdownAbsoluteMap[i, j] = passesWithValues.Average(p => p.MaxBalanceDrawdownAbsolute);
                            result.AverageTradeMap[i, j] = passesWithValues.Average(p => p.AverageTrade ?? 0);
                        }
                        
                        result.PassCountMap[i, j] = passesWithValues.Count;
                    }
                }
            }

            return result;
        }

        private double CalculateMedian<T>(List<T> items, Func<T, double> selector)
        {
            if (items.Count == 0)
                return 0;
            
            var sortedValues = items.Select(selector).OrderBy(v => v).ToList();
            
            if (sortedValues.Count % 2 == 0)
            {
                int midIndex = sortedValues.Count / 2;
                return (sortedValues[midIndex - 1] + sortedValues[midIndex]) / 2.0;
            }
            else
            {
                return sortedValues[sortedValues.Count / 2];
            }
        }
    }
} 