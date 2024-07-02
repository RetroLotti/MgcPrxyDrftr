using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MgcPrxyDrftr.models
{
    public class OpenBooster
    {
        [JsonProperty("cards")]
        public List<OpenBoosterCard> Cards { get; set; }
    }
}
