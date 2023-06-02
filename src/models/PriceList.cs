using System.Collections.Generic;
using Newtonsoft.Json;

namespace MgcPrxyDrftr.models
{
    public class PriceList
    {
        [JsonProperty("meta")]
        public Meta Meta { get; set; }

        [JsonProperty("data")]
        public Dictionary<string, string> Data { get; set; }
    }

    public class PriceData
    {
        
    }
}
