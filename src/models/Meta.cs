using System;
using Newtonsoft.Json;

namespace MgcPrxyDrftr.models
{
    public class Meta
    {
        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
