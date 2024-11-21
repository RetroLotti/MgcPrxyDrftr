using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using MgcPrxyDrftr.models;
using Card = ScryfallApi.Client.Models.Card;
using Set = ScryfallApi.Client.Models.Set;
using Newtonsoft.Json;

namespace MgcPrxyDrftr
{
    public class ApiCaller
    {
        //private const int WAIT_TIME = 100;
        private readonly HttpClient _client = new() { BaseAddress = new Uri("https://api.scryfall.com") };
        private readonly HttpClient _openBoostersClient = new() { BaseAddress = new Uri("http://82.165.127.227/") };
        //private DateTime lastApiCall;

        //https://scryfall.com/docs/api

        public ApiCaller()
        {
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            _openBoostersClient.DefaultRequestHeaders.Accept.Clear();
            _openBoostersClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<OpenBoosterBox> GenerateBooster(string setCode, BoosterType boosterType, int amount)
        {
            var response = await _openBoostersClient.GetAsync($"boosters.php?s={setCode}&b={Enum.GetName(boosterType)!.ToLower()}&a={amount}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<OpenBoosterBox>(json);
        }

        public async Task<Card> GetCardByNameAsync(string cardName, string setCode = "")
        {
            Card card = null;
            var response = await _client.GetAsync($"cards/named?exact={cardName}&set={setCode}").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            card = System.Text.Json.JsonSerializer.Deserialize<Card>(json);
            return card;
        }

        public async Task<Card> GetCardByNameAndLanguageAsync(string cardName, string language, string setCode = "")
        {
            Card card = null;
            //https://scryfall.com/search?order=released&q=lang%3Ade&unique=prints
            //https://api.scryfall.com/cards/search?q=name%3DHellkite+set%3DGRN+lang%3Dde
            var response = await _client.GetAsync($"cards/search?q=name%3D{cardName}+set%3D{setCode}+lang%3D{language}");
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            card = System.Text.Json.JsonSerializer.Deserialize<Card>(json);
            return card;
        }

        public async Task<Card> GetCardByMultiverseIdAsync(string multiverseid)
        {
            Card card = null;

            var response = await _client.GetAsync($"cards/multiverse/{multiverseid}");
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            card = System.Text.Json.JsonSerializer.Deserialize<Card>(json);
            return card;
        }

        public async Task<Card> GetCardByScryfallIdAsync(Guid scryfallGuid)
        {
            return await GetCardByScryfallIdAsync(scryfallGuid.ToString());
        }
        public async Task<Card> GetCardByScryfallIdAsync(string scryfallGuid)
        {
            Card card = null;

            var response = await _client.GetAsync($"cards/{scryfallGuid}");
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            card = System.Text.Json.JsonSerializer.Deserialize<Card>(json);

            return card;
        }

        public async Task<List<Set>> GetSetsAsync()
        {
            //lastApiCall = DateTime.Now;
            List<Set> sets = null;
            var response = await _client.GetAsync("sets");
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            sets = System.Text.Json.JsonSerializer.Deserialize<List<Set>>(json);
            return sets;
        }
    }
}
