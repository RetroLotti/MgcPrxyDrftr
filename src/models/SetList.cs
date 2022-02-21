using Newtonsoft.Json;
using MgcPrxyDrftr.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MgcPrxyDrftr.models
{
    public class SetList
    {
        [JsonProperty("meta")]
        public Meta Meta { get; set; }

        [JsonProperty("data")]
        public List<Set> Data { get; set; }
    }
}
