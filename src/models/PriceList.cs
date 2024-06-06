using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MgcPrxyDrftr.models
{
    public class OverallPriceList
    {
        [JsonProperty("meta")]
        public Meta Meta { get; set; }

        [JsonProperty("data")]
        public Dictionary<string, PriceFormats> Data { get; set; }
    }

    public class PriceFormats
    {
        public MagicPlatform Mtgo { get; set; }
        public MagicPlatform Paper { get; set; }
    }

    public class MagicPlatform
    {
        public PriceList CardHoarder { get; set; }
        public PriceList CardKingdom { get; set; }
        public PriceList CardMarket { get; set; }
        public PriceList CardSphere { get; set; }
        public PriceList TcgPlayer { get; set; }
    }

    public class PriceList
    {
        public PricePoint Buylist { get; set; }
        public PricePoint Retail { get; set; }
        public string Currency { get; set; }
    }

    public class PricePoint
    {
        public Dictionary<string, decimal> Foil { get; set; }
        public Dictionary<string, decimal> Normal { get; set; }
    }
}
