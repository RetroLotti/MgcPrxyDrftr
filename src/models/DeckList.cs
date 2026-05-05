using Newtonsoft.Json;
using System.Collections.Generic;

namespace MgcPrxyDrftr.models
{
    public class DeckList
    {
        [JsonProperty("meta")]
        public Meta Meta { get; set; }

        [JsonProperty("data")]
        public List<Deck> Data { get; set; }
    }
}
