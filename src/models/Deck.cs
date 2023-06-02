using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MgcPrxyDrftr.models
{
    public class DeckRoot
    {
        [JsonProperty("meta")]
        public Meta Meta { get; set; }
        [JsonProperty("data")]
        public Deck Data { get; set; }
    }

    public class Deck
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("fileName")]
        public string FileName { get; set; }

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
