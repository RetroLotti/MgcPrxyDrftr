using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyDraftor.models
{
    public class Deck
    {
        [JsonProperty("meta")]
        public Meta Meta { get; set; }
        [JsonProperty("data")]
        public DeckData Data { get; set; }
    }

    public class DeckData
    {
        [JsonProperty("code")]
        public string Code { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("releaseDate")]
        public DateTime ReleaseDate { get; set; }
        [JsonProperty("commander")]
        public List<Card> Commander { get; set; }
        [JsonProperty("mainBoard")]
        public List<Card> MainBoard { get; set; }
        [JsonProperty("sideBoard")]
        public List<Card> SideBoard { get; set; }
    }
}
