using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace cBotBacktestAnalyzer.Models
{
    public class BacktestData
    {
        [JsonProperty("backtestingSettings")]
        public BacktestingSettings? BacktestingSettings { get; set; }

        [JsonProperty("parameters")]
        public List<Parameter>? Parameters { get; set; }

        [JsonProperty("criteria")]
        public Criteria? Criteria { get; set; }

        [JsonProperty("optimizationMethod")]
        public string? OptimizationMethod { get; set; }

        [JsonProperty("cpuCores")]
        public int CpuCores { get; set; }

        [JsonProperty("testingPeriod")]
        public TestingPeriod? TestingPeriod { get; set; }

        [JsonProperty("autoSelectTheBestPass")]
        public bool AutoSelectTheBestPass { get; set; }

        [JsonProperty("results")]
        public Results? Results { get; set; }
    }

    public class BacktestingSettings
    {
        [JsonProperty("startingCapital")]
        public double StartingCapital { get; set; }

        [JsonProperty("commissions")]
        public Commissions? Commissions { get; set; }

        [JsonProperty("data")]
        public Data? Data { get; set; }

        [JsonProperty("spread")]
        public Spread? Spread { get; set; }
    }

    public class Commissions
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }
    }

    public class Data
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("csvFilePath")]
        public string? CsvFilePath { get; set; }

        [JsonProperty("preciseProfitCalculation")]
        public bool PreciseProfitCalculation { get; set; }
    }

    public class Spread
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("min")]
        public double Min { get; set; }

        [JsonProperty("max")]
        public double Max { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }
    }

    public class Parameter
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("value")]
        public string? Value { get; set; }

        [JsonProperty("parameterType")]
        public string? ParameterType { get; set; }

        [JsonProperty("metadata")]
        public Metadata? Metadata { get; set; }

        [JsonProperty("optimize")]
        public bool Optimize { get; set; }

        [JsonProperty("min")]
        public string? Min { get; set; }

        [JsonProperty("max")]
        public string? Max { get; set; }

        [JsonProperty("step")]
        public string? Step { get; set; }

        [JsonProperty("values")]
        public List<string>? Values { get; set; }
    }

    public class Metadata
    {
        [JsonProperty("minValue")]
        public double? MinValue { get; set; }

        [JsonProperty("maxValue")]
        public double? MaxValue { get; set; }

        [JsonProperty("step")]
        public double? Step { get; set; }

        [JsonProperty("defaultValue")]
        public object? DefaultValue { get; set; }

        [JsonProperty("parameterType")]
        public string? ParameterType { get; set; }

        [JsonProperty("propertyName")]
        public string? PropertyName { get; set; }

        [JsonProperty("friendlyName")]
        public string? FriendlyName { get; set; }

        [JsonProperty("groupName")]
        public string? GroupName { get; set; }

        [JsonProperty("isValueVisibleInTitle")]
        public bool IsValueVisibleInTitle { get; set; }

        [JsonProperty("enumValues")]
        public List<EnumValue>? EnumValues { get; set; }
    }

    public class EnumValue
    {
        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }
    }

    public class Criteria
    {
        [JsonProperty("standard")]
        public List<Criterion>? Standard { get; set; }

        [JsonProperty("custom")]
        public bool Custom { get; set; }
    }

    public class Criterion
    {
        [JsonProperty("criterion")]
        public string? CriterionName { get; set; }

        [JsonProperty("extremum")]
        public string? Extremum { get; set; }
    }

    public class TestingPeriod
    {
        [JsonProperty("startDate")]
        public long StartDate { get; set; }

        [JsonProperty("endDate")]
        public long EndDate { get; set; }
    }

    public class Results
    {
        [JsonProperty("elapsedTime")]
        public double ElapsedTime { get; set; }

        [JsonProperty("passes")]
        public List<Pass>? Passes { get; set; }
    }

    public class Pass
    {
        [JsonProperty("passId")]
        public int PassId { get; set; }

        [JsonProperty("status")]
        public string? Status { get; set; }

        [JsonProperty("fitness")]
        public double Fitness { get; set; }

        [JsonProperty("equity")]
        public double Equity { get; set; }

        [JsonProperty("balance")]
        public double Balance { get; set; }

        [JsonProperty("margin")]
        public double Margin { get; set; }

        [JsonProperty("freeMargin")]
        public double FreeMargin { get; set; }

        [JsonProperty("grossProfit")]
        public double GrossProfit { get; set; }

        [JsonProperty("netProfit")]
        public double NetProfit { get; set; }

        [JsonProperty("trades")]
        public int Trades { get; set; }

        [JsonProperty("winningTrades")]
        public int WinningTrades { get; set; }

        [JsonProperty("losingTrades")]
        public int LosingTrades { get; set; }

        [JsonProperty("profitFactor")]
        public double? ProfitFactor { get; set; }

        [JsonProperty("maxEquityDrawdownPercent")]
        public double MaxEquityDrawdownPercent { get; set; }

        [JsonProperty("maxBalanceDrawdownPercent")]
        public double MaxBalanceDrawdownPercent { get; set; }

        [JsonProperty("maxEquityDrawdownAbsolute")]
        public double MaxEquityDrawdownAbsolute { get; set; }

        [JsonProperty("maxBalanceDrawdownAbsolute")]
        public double MaxBalanceDrawdownAbsolute { get; set; }

        [JsonProperty("averageTrade")]
        public double? AverageTrade { get; set; }

        [JsonProperty("totalMarginCalculationType")]
        public string? TotalMarginCalculationType { get; set; }

        [JsonProperty("marginLevel")]
        public string? MarginLevel { get; set; }
    }
} 