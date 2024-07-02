using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MgcPrxyDrftr.models
{
    public class OpenBoosterBox
    {
        [JsonProperty("set")]
        public string Set { get; set; }
        [JsonProperty("Product")]
        public string Product { get; set; }
        [JsonProperty("Booster")]
        public List<OpenBooster> Booster { get; set; }
    }
}
