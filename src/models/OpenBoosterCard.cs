using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MgcPrxyDrftr.models
{
    public class OpenBoosterCard
    {
        [JsonProperty("cards")]
        public string Uuid { get; set; }
        [JsonProperty("scryfallid")]
        public string Scryfallid { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("flavorText")]
        public string FlavorText { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("manaCost")]
        public string ManaCost { get; set; }
        [JsonProperty("side")]
        public string Side { get; set; }
        [JsonProperty("rarity")]
        public string Rarity { get; set; }
        [JsonProperty("layout")]
        public string Layout { get; set; }
        [JsonProperty("otherCards")]
        public List<OpenBoosterCard> OtherCards { get; set; }
    }
}
