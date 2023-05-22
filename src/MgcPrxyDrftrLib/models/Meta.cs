using Newtonsoft.Json;
using System;

namespace MgcPrxyDrftrLib.models
{
    public class Meta
    {
        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
