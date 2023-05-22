using Newtonsoft.Json;
using System.Collections.Generic;

namespace MgcPrxyDrftrLib.models
{
    public class SetList
    {
        [JsonProperty("meta")]
        public Meta Meta { get; set; }

        [JsonProperty("data")]
        public List<Set> Data { get; set; }
    }
}
