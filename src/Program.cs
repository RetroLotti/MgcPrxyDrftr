using MtgApiManager.Lib.Service;
using Newtonsoft.Json;
using ProxyDraftor.lib;
using ScryfallApi.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyDraftor
{
    class Program
    {
#if DEBUG
        public static string BaseDirectory { get; set; } = @"c:\dev\git\ProxyDraftor\src";
#else
        public static string BaseDirectory { get; set; } = Environment.CurrentDirectory;
#endif
        private static string JsonDirectory { get; set; } = ConfigurationManager.AppSettings["JsonDirectory"];
        private static string JsonSetDirectory { get; set; } = ConfigurationManager.AppSettings["JsonSetDirectory"];
        private static string BoosterDirectory { get; set; } = ConfigurationManager.AppSettings["BoosterDirectory"];
        private static string ScriptDirectory { get; set; } = ConfigurationManager.AppSettings["ScriptDirectory"];
        private static string DraftDirectory { get; set; } = ConfigurationManager.AppSettings["DraftDirectory"];
        private static string DefaultScriptName { get; set; } = ConfigurationManager.AppSettings["DefaultScriptName"];
        //private static string DefaultImageType { get; set; } = ConfigurationManager.AppSettings["DefaultImageType"];
        //private static string DefaultCardLanguage { get; set; } = ConfigurationManager.AppSettings["DefaultCardLanguage"];
        private static string NanDeckFullPath { get; set; } = ConfigurationManager.AppSettings["NanDeckFullPath"];
        //private static string[] SetsToLoad { get; set; } = ConfigurationManager.AppSettings["SetsToLoad"].Split(" ");

        private static readonly SortedList<string, string> releaseTimelineSets = new();
        private static readonly SortedList<string, models.Set> sets = new();
        
        private static readonly IMtgServiceProvider serviceProvider = new MtgServiceProvider();
        private static readonly ISetService setService = serviceProvider.GetSetService();
        private static readonly ICardService cardService = serviceProvider.GetCardService();
        private static readonly WebClient client = new();
        private static readonly ApiCaller api = new();

        //static async Task Main(/*string[] args*/)
        static async Task Main()
        {
            Console.WriteLine("╔═                   Preparing Application                    ═╗");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

            Console.WriteLine(">> Checking directories...");
            CheckAllDirectories();

            Console.WriteLine(">> Reading sets...");
            ReadAllSets();

            Console.WriteLine(">> Looking for NanDeck...");
            CheckNanDeck();

            Console.WriteLine(">> Launching application...");
            Thread.Sleep(1000);
            Console.Clear();

            // start main loop
            await Draft();
        }

        private static void CheckDirectory(string path)
        {
            DirectoryInfo dir = new(path);
            if(!dir.Exists) { dir.Create(); }
        }

        private static void CheckAllDirectories()
        {
            CheckDirectory(@$"{BaseDirectory}\{JsonDirectory}\{JsonSetDirectory}\");
            CheckDirectory(@$"{BaseDirectory}\{BoosterDirectory}\");
            CheckDirectory(@$"{BaseDirectory}\{ScriptDirectory}\");
            CheckDirectory(@$"{BaseDirectory}\{DraftDirectory}\");
        }

        private static void ReadAllSets()
        {
            DirectoryInfo setDirectory = new(@$"{BaseDirectory}\{JsonDirectory}\{JsonSetDirectory}\");
            if(setDirectory.Exists)
            {
                var files = setDirectory.GetFiles("*.json");
                foreach (var file in files)
                {
                    _ = ReadSingleSet(file);
                }
            }
        }

        static models.Set ReadSingleSet(FileInfo fileInfo)
        {
            var txt = File.ReadAllText(fileInfo.FullName);
            var o = JsonConvert.DeserializeObject<models.Set>(txt);

            if (!sets.ContainsKey(o.Data.Code))
            {
                sets.Add(o.Data.Code, o);
            }

            if (!releaseTimelineSets.ContainsValue(o.Data.Code))
            {
                releaseTimelineSets.Add(o.Data.ReleaseDate.ToString("yyyy-MM-dd") + o.Data.Code, o.Data.Code);
            }
                
            return o;
        }

        static models.Deck ReadSingleDeck(string file)
        {
            return JsonConvert.DeserializeObject<models.Deck>(File.ReadAllText(file));
        }

        static List<models.CardIdentifiers> GenerateBooster(string setCode)
        {
            List<Guid> boosterCards = new();
            List<models.CardIdentifiers> boosterCardIdentifier = new();
            var set = sets[setCode.ToUpper()];

            // create Hashtable for cards identified by scryfall id
            SortedDictionary<Guid, models.Card> cards = new();
            foreach (var item in set.Data.Cards) { if(!cards.ContainsKey(item.Uuid) && (item.Side == null || item.Side == models.Side.A)) cards.Add(item.Uuid, item); }

            // determine booster blueprint
            Dictionary<models.Contents, float> blueprint = new();
            foreach (var item in set.Data.Booster.Default.Boosters) { blueprint.Add(item.Contents, (float)item.Weight / (float)set.Data.Booster.Default.BoostersTotalWeight); }
            var booster = blueprint.RandomElementByWeight(e => e.Value);

            // REMOVE
#warning "I need to remove this"
            if (setCode.ToLower().Equals("vma"))
            {
                // just for Vintage Masters I "fixed" it to "Special"-Booster to grant everybody Power Nine Cards
                booster = new(set.Data.Booster.Default.Boosters[0].Contents, set.Data.Booster.Default.Boosters[0].Weight);
            }

            // determine booster contents
            foreach (var sheet in booster.Key.GetType().GetProperties().Where(s => s.GetValue(booster.Key, null) != null))
            {
                // how many cards should be added for this sheet
                long cardCount = (long)sheet.GetValue(booster.Key, null);

                // name of the sheet
                string sheetName = sheet.Name;

                // temporary sheet
                Dictionary<Guid, float> temporarySheet = new();

                // get the actual sheet
                var actualSheetReflection = set.Data.Booster.Default.Sheets.GetType().GetProperties().First(s => s.GetValue(set.Data.Booster.Default.Sheets, null) != null && s.Name.ToLower().Equals(sheetName.ToLower()));
                var actualSheet = ((models.Sheet)actualSheetReflection.GetValue(set.Data.Booster.Default.Sheets));

                // add all cards to a temporary list with correct weight
                foreach (var item in actualSheet.Cards) 
                {
                    temporarySheet.Add(Guid.Parse(item.Key), (float)item.Value / (float)actualSheet.TotalWeight); 
                }

                // get cards from sheet and add to booster
                for (int i = 0; i < cardCount; i++)
                {
                    boosterCards.Add(temporarySheet.RandomElementByWeight(e => e.Value).Key);
                }
            }

            // balance colors
            // TODO: if a sheet has balanceColors == true this has to be considered ==> BUT HOW

            // get scryfall id for card determination later on
            for (int i = 0; i < boosterCards.Count; i++) { boosterCardIdentifier.Add(cards[boosterCards[i]].Identifiers); boosterCards[i] = cards[boosterCards[i]].Identifiers.ScryfallId; }

            return boosterCardIdentifier;
        }

        static void CheckNanDeck()
        {
            FileInfo nandeck = new(NanDeckFullPath);
            if(!nandeck.Exists)
            {
                Console.WriteLine("NanDECK wurde nicht gefunden. Bitte geben die den Ort manuell an!");
                Console.Write("> ");
                NanDeckFullPath = Console.ReadLine();
                ConfigurationManager.AppSettings["NanDeckFullPath"] = NanDeckFullPath;
            }
        }

        //static string RemoveNoise(string input)
        //{
        //    input = Regex.Replace(input, @"\r\n?|\n", string.Empty); // no more NewLine stuff
        //    return input.Replace(" ", string.Empty)
        //        .Replace(@"""", string.Empty);
        //}

        static async Task<bool> Draft()
        {
            do
            {
                Console.Clear();
                Console.WriteLine("╔═      RetroLottis Magic The Gathering Proxy Generator       ═╗");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
                Console.WriteLine("");
                Console.WriteLine("Verfügbare Sets");
                Console.WriteLine("╔═════╦════════════╦═══════════════════════════════════════════╗");
                foreach (var item in releaseTimelineSets) { Console.WriteLine($"║ {item.Value} ║ {DateTime.Parse(item.Key.Substring(0, 10)):dd.MM.yyyy} ║ {sets[item.Value].Data.Name.PadRight(41, ' ')} ║"); }
                Console.WriteLine("╚═════╩════════════╩═══════════════════════════════════════════╝");
                Console.WriteLine("");
                Console.Write("Welches Set soll verwendet werden? > ");
                var setCode = Console.ReadLine();
                var set = await setService.FindAsync(setCode);
                Console.WriteLine("");
                if(set.Value == null)
                {
                    Console.WriteLine($"Die Eingabe [{setCode}] ist kein gültiger Set-Code.");
                    Console.Write("Beliebige Taste zum fortfahren drücken.");
                    continue;
                }
                Console.WriteLine($"Gewähltes Set: {set.Value.Name}");
                Console.WriteLine("");
                Console.Write("Wie viele Booster sollen erstellt werden? > ");
                var count = Console.ReadLine();
                int boosterCount = int.Parse(count);
                Console.WriteLine("");
                Console.WriteLine($"Es {(boosterCount == 1 ? "wird" : "werden")} {boosterCount} Booster der Erweiterung \"{set.Value.Name}\" erstellt.");
                Console.CursorVisible = false;
                Console.Write("Beliebige Taste zum starten drücken!");
                Console.ReadKey();

                Console.Clear();

                // create new draft folder
                DirectoryInfo draftDirectory = new(@$"{BaseDirectory}\{DraftDirectory}\{DateTime.Now:yyyy-MM-ddTHH-mm-ss}");
                if(!draftDirectory.Exists) { draftDirectory.Create(); }

                for (int i = 1; i <= boosterCount; i++)
                {
                    Console.WriteLine($"Generiere Booster {i}/{boosterCount}...");

                    // get a booster
                    var booster = GenerateBooster(set.Value.Code);

                    // new booster guid 
                    var boosterGuid = Guid.NewGuid();

                    // create directory
                    DirectoryInfo boosterDirectory = new(@$"{BaseDirectory}\{BoosterDirectory}\{boosterGuid}\");
                    if (!boosterDirectory.Exists) { boosterDirectory.Create(); }

                    Console.WriteLine("Lade Bilder herunter...");
                    Console.WriteLine("=======================================================================================");

                    // load images
                    foreach (var card in booster) { await GetImage(card, boosterDirectory.FullName); }

                    // copy magic back once
                    FileInfo backFile = new(@$"{BaseDirectory}\images\mtg.back.png");
                    if(backFile.Exists) { backFile.CopyTo(@$"{boosterDirectory.FullName}\{backFile.Name}"); }

                    Console.WriteLine("Erstelle PDF-Datei via nanDECK...");

                    // prepare pdf with nandeck
                    Process proc = new();
                    proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.FileName = "cmd.exe";
                    proc.StartInfo.Arguments = $"/c {NanDeckFullPath} \"{BaseDirectory}\\{ScriptDirectory}\\{DefaultScriptName}.nde\" /[guid]={boosterGuid} /[boosterfolder]={BaseDirectory}\\{BoosterDirectory} /createpdf";
                    proc.EnableRaisingEvents = true;
                    proc.Start();
                    proc.WaitForExit();
                    var exitCode = proc.ExitCode;

                    if(proc.ExitCode == 0)
                    {
                        FileInfo file = new(@$"{BaseDirectory}\{ScriptDirectory}\{DefaultScriptName}.pdf");
                        if (file.Exists)
                        {
                            file.MoveTo($@"{draftDirectory}\{boosterGuid}.pdf");
                        }
                        Console.WriteLine("=======================================================================================");
                        Console.WriteLine($@"Datei {draftDirectory}\{boosterGuid}.pdf erstellt.");
                        Console.WriteLine("=======================================================================================");
                    } 
                    else
                    {
                        Console.WriteLine("=======================================================================================");
                        Console.WriteLine("Erzeugen des Boosters fehlgeschlagen...");
                    }

                    // cleanup
                    boosterDirectory.Delete(true);
                    Console.Clear();
                }

                Console.WriteLine("");
                Console.WriteLine("Alle Booster wurden erstellt.");
                Console.WriteLine("Um weitere Booster zu erstellen bitte eine beliebige Taste drücken.");
                Console.Write("Zum beenden [x] drücken!");
            } while (Console.ReadKey().Key != ConsoleKey.X);
            
            return true;
        }

        private static async Task<bool> GetImage(models.CardIdentifiers cardIdentifiers, string boosterDirectory)
        {
            //DirectoryInfo localImageFolder = new("");
            //// Dictionary<string, Uri> scryfallImageUris


            //// check for local image
            //FileInfo file = new();

            // get scryfall card
            var scryfallCard = await api.GetCardByScryfallIdAsync(cardIdentifiers.ScryfallId);

            Console.WriteLine($"Lade {scryfallCard.Name} herunter...");

            // check if images are present
            if(scryfallCard.ImageUris != null)
            {
                // download image
                await client.DownloadFileTaskAsync(scryfallCard.ImageUris["png"].AbsoluteUri, boosterDirectory + Guid.NewGuid().ToString() + ".png");
            }
            else
            {
                if (scryfallCard.CardFaces != null)
                {
                    foreach (var item in scryfallCard.CardFaces)
                    {
                        // download image
                        await client.DownloadFileTaskAsync(item.ImageUris["png"].AbsoluteUri, boosterDirectory + Guid.NewGuid().ToString() + ".png");
                    }
                }
            }

            //{
            //    //magicthegathering.io
            //    var cardFallback = await cardService.FindAsync((int)cardIdentifiers.MultiverseId);

            //    if (cardFallback.IsSuccess)
            //    {
            //        // download image
            //        await client.DownloadFileTaskAsync(cardFallback.Value.ImageUrl, boosterDirectory + Guid.NewGuid().ToString() + ".png");
            //    }
            //    else
            //    { // gatherer

            //        // download image
            //       // await client.DownloadFileTaskAsync(scryfallCard.ImageUris["png"].AbsoluteUri, boosterDirectory + Guid.NewGuid().ToString() + ".png");
            //    }
            //}

            return true;
        }

        //private static void GeneratePdf(string boosterPath)
        //{
        //    var ImageFiles = System.IO.Directory.EnumerateFiles(boosterPath).Where(f => f.EndsWith(".png"));
        //    ImageToPdfConverter.ImageToPdf(ImageFiles).SaveAs(@"C:\booster\composite.pdf");
        //}
    }

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

        public async Task<ScryfallApi.Client.Models.Card> GetCardByMultiverseIdAsync(string multiverseid)
        {
            //var currentTime = DateTime.Now;
            //lastApiCall = DateTime.Now;

            ScryfallApi.Client.Models.Card card = null;

            HttpResponseMessage response = await client.GetAsync($"cards/multiverse/{multiverseid}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                card = System.Text.Json.JsonSerializer.Deserialize<ScryfallApi.Client.Models.Card>(json);
            }
            return card;
        }
        public async Task<ScryfallApi.Client.Models.Card> GetCardByScryfallIdAsync(Guid scryfallGuid)
        {
            //var currentTime = DateTime.Now;
            //lastApiCall = DateTime.Now;

            ScryfallApi.Client.Models.Card card = null;

            HttpResponseMessage response = await client.GetAsync($"cards/{scryfallGuid}");
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
            HttpResponseMessage response = await client.GetAsync($"sets");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                sets = System.Text.Json.JsonSerializer.Deserialize<List<ScryfallApi.Client.Models.Set>>(json);
            }
            return sets;
        }
    }
}
