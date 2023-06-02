using System.Collections.Generic;
using Newtonsoft.Json;

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
