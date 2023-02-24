using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MgcPrxyDrftr
{
    public class ApiCaller
    {
        //private const int WAIT_TIME = 100;
        private readonly HttpClient client = null;
        //private DateTime lastApiCall;

        //https://scryfall.com/docs/api

        public ApiCaller()
        {
            client = new HttpClient();
            client.BaseAddress = new Uri("https://api.scryfall.com");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<ScryfallApi.Client.Models.Card> GetCardByNameAsync(string cardName, string setCode = "")
        {
            ScryfallApi.Client.Models.Card card = null;
            var response = await client.GetAsync($"cards/named?exact={cardName}&set={setCode}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                card = System.Text.Json.JsonSerializer.Deserialize<ScryfallApi.Client.Models.Card>(json);
            }
            return card;
        }

        public async Task<ScryfallApi.Client.Models.Card> GetCardByNameAndLanguageAsync(string cardName, string language, string setCode = "")
        {
            ScryfallApi.Client.Models.Card card = null;
            //https://scryfall.com/search?order=released&q=lang%3Ade&unique=prints
            //https://api.scryfall.com/cards/search?q=name%3DHellkite+set%3DGRN+lang%3Dde
            var response = await client.GetAsync($"cards/search?q=name%3D{cardName}+set%3D{setCode}+lang%3D{language}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                card = System.Text.Json.JsonSerializer.Deserialize<ScryfallApi.Client.Models.Card>(json);
            }
            return card;
        }

        public async Task<ScryfallApi.Client.Models.Card> GetCardByMultiverseIdAsync(string multiverseid)
        {
            ScryfallApi.Client.Models.Card card = null;

            var response = await client.GetAsync($"cards/multiverse/{multiverseid}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                card = System.Text.Json.JsonSerializer.Deserialize<ScryfallApi.Client.Models.Card>(json);
            }
            return card;
        }
        public async Task<ScryfallApi.Client.Models.Card> GetCardByScryfallIdAsync(Guid scryfallGuid)
        {
            ScryfallApi.Client.Models.Card card = null;

            var response = await client.GetAsync($"cards/{scryfallGuid}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                card = System.Text.Json.JsonSerializer.Deserialize<ScryfallApi.Client.Models.Card>(json);
            }

            return card;
        }

        public async Task<List<ScryfallApi.Client.Models.Set>> GetSetsAsync()
        {
            //lastApiCall = DateTime.Now;
            List<ScryfallApi.Client.Models.Set> sets = null;
            var response = await client.GetAsync($"sets");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                sets = System.Text.Json.JsonSerializer.Deserialize<List<ScryfallApi.Client.Models.Set>>(json);
            }
            return sets;
        }
    }
}
