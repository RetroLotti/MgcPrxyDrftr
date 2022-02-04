using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyDraftor.models
{
    public class SetNew
    {
        public Meta Meta { get; set; }
        public SetDataNew Data { get; set; }
    }

    public class SetDataNew : Data
    {
        [JsonProperty("baseSetSize")]
        public int BaseSetSize { get; set; }
        [JsonProperty("block")]
        public string Block { get; set; }
        [JsonProperty("booster")]
        public List<BoosterDefinition> Booster { get; set; }
    }

    public class BoosterDefinition
    {
        public object Default { get; set; }
    }

    public class SetBooster
    {

    }

    public class BoosterNew
    {
        [JsonProperty("contents")]
        public Dictionary<string, long> Contents { get; set; }
        [JsonProperty("weight")]
        public int Weight { get; set; }
    }
}
