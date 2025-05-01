using System.Collections.Generic;
using Newtonsoft.Json;

namespace cBotBacktestAnalyzer.Models
{
    public class ParametersFile
    {
        [JsonProperty("Chart")]
        public Chart? Chart { get; set; }

        [JsonProperty("Parameters")]
        public Dictionary<string, string>? Parameters { get; set; }
    }

    public class Chart
    {
        [JsonProperty("Symbol")]
        public string? Symbol { get; set; }

        [JsonProperty("Period")]
        public string? Period { get; set; }
    }
} 