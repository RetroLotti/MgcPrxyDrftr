﻿using System.Collections.Generic;
using Newtonsoft.Json;

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
