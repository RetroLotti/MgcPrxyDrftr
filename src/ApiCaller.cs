using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using ScryfallApi.Client.Models;

namespace MgcPrxyDrftr
{
    public class ApiCaller
    {
        //private const int WAIT_TIME = 100;
        private readonly HttpClient client;
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

        public async Task<Card> GetCardByNameAsync(string cardName, string setCode = "")
        {
            Card card = null;
            var response = await client.GetAsync($"cards/named?exact={cardName}&set={setCode}").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                card = JsonSerializer.Deserialize<Card>(json);
            }
            return card;
        }

        public async Task<Card> GetCardByNameAndLanguageAsync(string cardName, string language, string setCode = "")
        {
            Card card = null;
            //https://scryfall.com/search?order=released&q=lang%3Ade&unique=prints
            //https://api.scryfall.com/cards/search?q=name%3DHellkite+set%3DGRN+lang%3Dde
            var response = await client.GetAsync($"cards/search?q=name%3D{cardName}+set%3D{setCode}+lang%3D{language}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                card = JsonSerializer.Deserialize<Card>(json);
            }
            return card;
        }

        public async Task<Card> GetCardByMultiverseIdAsync(string multiverseid)
        {
            Card card = null;

            var response = await client.GetAsync($"cards/multiverse/{multiverseid}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                card = JsonSerializer.Deserialize<Card>(json);
            }
            return card;
        }
        public async Task<Card> GetCardByScryfallIdAsync(Guid scryfallGuid)
        {
            Card card = null;

            var response = await client.GetAsync($"cards/{scryfallGuid}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                card = JsonSerializer.Deserialize<Card>(json);
            }

            return card;
        }

        public async Task<List<Set>> GetSetsAsync()
        {
            //lastApiCall = DateTime.Now;
            List<Set> sets = null;
            var response = await client.GetAsync("sets");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                sets = JsonSerializer.Deserialize<List<Set>>(json);
            }
            return sets;
        }
    }
}
