using MtgApiManager.Lib.Service;
using Newtonsoft.Json;
using ProxyDraftor.lib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using H = ProxyDraftor.lib.Helpers;

namespace ProxyDraftor
{
    class Program
    {
        public static string BaseDirectory { get; set; } = ConfigurationManager.AppSettings["BaseDirectory"] ?? Environment.CurrentDirectory;    
        private static string JsonDirectory { get; set; } = ConfigurationManager.AppSettings["JsonDirectory"] ?? "json";
        private static string JsonSetDirectory { get; set; } = ConfigurationManager.AppSettings["JsonSetDirectory"] ?? "sets";
        private static string JsonDeckDirectory { get; set; } = ConfigurationManager.AppSettings["JsonDeckDirectory"] ?? "decks";
        private static string BoosterDirectory { get; set; } = ConfigurationManager.AppSettings["BoosterDirectory"] ?? "booster";
        private static string ImageCacheDirectory { get; set; } = ConfigurationManager.AppSettings["ImageCacheDirectory"] ?? "images";
        private static string ScryfallCacheDirectory { get; set; } = ConfigurationManager.AppSettings["ScryfallCacheDirectory"] ?? "scryfall";
        private static string ScriptDirectory { get; set; } = ConfigurationManager.AppSettings["ScriptDirectory"] ?? "scripts";
        private static string DraftDirectory { get; set; } = ConfigurationManager.AppSettings["DraftDirectory"] ?? "draft";
        private static string DefaultScriptName { get; set; } = ConfigurationManager.AppSettings["DefaultScriptName"];
        private static string NanDeckPath { get; set; } = ConfigurationManager.AppSettings["NanDeckPath"];
        private static bool UseSetList { get; set; } = bool.Parse(ConfigurationManager.AppSettings["UseSetList"]);
        private static bool IsWindows { get; set; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private static readonly SortedList<string, string> releaseTimelineSets = new();
        private static readonly SortedList<string, models.Set> sets = new();
        
        private static readonly IMtgServiceProvider serviceProvider = new MtgServiceProvider();
        private static readonly WebClient client = new();
        private static readonly ApiCaller api = new();
        private static models.Settings Settings { get; set; }

        // decks
        private static Dictionary<string, string> DeckList { get; set; }
        private static Dictionary<string, string> AllDecks { get; set; }

        static async Task Main()
        {
            H.Write("╔═", 0, 0);
            H.Write("Preparing application", (Console.WindowWidth / 2) - ("Preparing application".Length / 2), 0);
            H.Write("═╗", Console.WindowWidth - "═╗".Length, 0);
            H.Write("╚", 0, 1);
            H.Write("".PadRight(Console.WindowWidth - 2, '═'), 1, 1);
            H.Write("╝", Console.WindowWidth - 1, 1);
            Console.SetCursorPosition(0, 2);

            Console.WriteLine(">> Checking directories...");
            CheckAllDirectories();

            Settings = H.LoadSettings(@$"{BaseDirectory}\{JsonDirectory}\settings.json");

            Console.WriteLine(">> Reading sets...");
            if (UseSetList) { ReadAllConfiguredSets(); } else { ReadAllSets(); }

            if(IsWindows)
            {
                Console.WriteLine(">> Looking for nanDeck...");
                H.CheckNanDeck(NanDeckPath);
            }
            else
            {
                Console.WriteLine(">> nanDeck disabled");
            }

            var ok1 = H.DownloadAndValidateFile("https://mtgjson.com/api/v5/DeckList.json", "https://mtgjson.com/api/v5/DeckList.json.sha256", @$"{BaseDirectory}\{JsonDirectory}\");
            var list = JsonConvert.DeserializeObject<models.DeckList>(File.ReadAllText(@$"{BaseDirectory}\{JsonDirectory}\DeckList.json"));
            var deck1 = list.Data.Find(x => x.Name.Contains("Kazz"));
            var ok2 = H.DownloadAndValidateFile($"https://mtgjson.com/api/v5/decks/{deck1.FileName}.json", $"https://mtgjson.com/api/v5/decks/{deck1.FileName}.json.sha256", @$"{BaseDirectory}\{JsonDirectory}\{JsonDeckDirectory}\");
            var deck2 = JsonConvert.DeserializeObject<models.DeckRoot>(File.ReadAllText(@$"{BaseDirectory}\{JsonDirectory}\{JsonDeckDirectory}\{deck1.FileName}.json"));


            Console.WriteLine(">> Starting...");
            Thread.Sleep(666);
            Console.Clear();

            // start drafting loop
            await Draft();
        }

        private static void CheckAllDirectories()
        {
            H.CheckDirectory(@$"{BaseDirectory}\{JsonDirectory}\{JsonSetDirectory}\");
            H.CheckDirectory(@$"{BaseDirectory}\{BoosterDirectory}\");
            H.CheckDirectory(@$"{BaseDirectory}\{ScriptDirectory}\");
            H.CheckDirectory(@$"{BaseDirectory}\{DraftDirectory}\");
            H.CheckDirectory(@$"{BaseDirectory}\{ImageCacheDirectory}\{ScryfallCacheDirectory}\");
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
                if(files.Length <= 0)
                {
                    Console.WriteLine("No Set-Files found!");
                }
            }
        }

        private static void ReadAllConfiguredSets()
        {
            if(Settings.SetsToLoad.Count <= 0)
            {
                Console.WriteLine("No sets configured!");
            }
            else
            {
                foreach (var set in Settings.SetsToLoad)
                {
                    H.CheckLastUpdate(set, @$"{BaseDirectory}\{JsonDirectory}", Settings, JsonSetDirectory);
                    _ = ReadSingleSet(set);
                }
            }
            
        }

        static models.Set ReadSingleSetWithUpdateCheck(string setCode)
        {
            H.CheckLastUpdate(setCode, @$"{BaseDirectory}\{JsonDirectory}", Settings, JsonSetDirectory);
            return ReadSingleSet(setCode);
        }

        static models.Set ReadSingleSet(string setCode)
        {
            FileInfo fileInfo = new(@$"{BaseDirectory}\{JsonDirectory}\{JsonSetDirectory}\{setCode.ToUpper()}.json");
            return ReadSingleSet(fileInfo);
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

        static List<models.CardIdentifiers> GenerateBooster(string setCode)
        {
            List<Guid> boosterCards = new();
            List<models.CardIdentifiers> boosterCardIdentifier = new();
            var set = sets[setCode.ToUpper()];

            // create Hashtable for cards identified by scryfall id
            SortedDictionary<Guid, models.Card> cards = new();
            foreach (var item in set.Data.Cards) { if(!cards.ContainsKey(item.Uuid) && (item.Side == null || item.Side == models.Side.A)) cards.Add(item.Uuid, item); }

            // check for available booster blueprints
            if (set.Data.Booster == null || set.Data.Booster.Default.Boosters.Count == 0)
            {
                return null;
            }

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
                    // reset card id
                    Guid card = Guid.Empty;

                    // prevent added duplicate cards
                    do { card = temporarySheet.RandomElementByWeight(e => e.Value).Key; } while (boosterCards.Contains(card));
                    
                    // add card to booster
                    boosterCards.Add(card);
                }
            }

            // get scryfall id for card determination later on
            for (int i = 0; i < boosterCards.Count; i++) { boosterCardIdentifier.Add(cards[boosterCards[i]].Identifiers); boosterCards[i] = cards[boosterCards[i]].Identifiers.ScryfallId; }

            return boosterCardIdentifier;
        }

        // START

        private static LoopState AppState { get; set; }

        static async void EnterTheLoop()
        {
            Console.Clear();
            AppState = LoopState.Main;


        }

        static async void PrintMenu()
        {
            switch (AppState)
            {
                case LoopState.Main:
                    H.Write("B => Draft Booster", 0, 0);
                    H.Write("D => Create Deck", 0, 1);
                    H.Write("S => Add or Remove Sets", 0, 2);
                    H.Write("C => Clean Up", 0, 3);
                    H.Write("O => Options", 0, 4);
                    H.Write("X => Exit", 0, 6);
                    break;
                case LoopState.Options:
                    H.Write("P => enable / disable automatic printing", 0, 0);
                    break;
                case LoopState.BoosterDraft:
                    break;
                case LoopState.DeckCreator:
                    break;
                case LoopState.SetManager:
                    break;
                default:
                    break;
            }
        }

        public enum LoopState
        {
            Main,
            Options,
            BoosterDraft,
            DeckCreator,
            SetManager
        }

        // END

        //static async Task<object> PrintDeck(string deckName) 
        //{
        //    if(AllDecks == null)
        //    {
        //        AllDecks = new();
        //        H.ReadSingleDeck(deckName);
        //    }
        //}


        static async Task<object> Draft()
        {
            ISetService setService = serviceProvider.GetSetService();
            MtgApiManager.Lib.Model.ISet set = null;
            do
            {
                Console.Clear();
                Console.WriteLine("╔══════ RetroLottis Magic The Gathering Proxy Generator ═══════╗");
                Console.WriteLine("╠═════╦════════════╦═══════════════════════════════════════════╣");
                Console.WriteLine("╠═════╬════════════╬═══════════════════════════════════════════╣");
                foreach (var item in releaseTimelineSets) { Console.WriteLine($"║ {item.Value} ║ {DateTime.Parse(item.Key[..10]):dd.MM.yyyy} ║ {sets[item.Value].Data.Name.PadRight(41, ' ')} ║"); }
                Console.WriteLine("╚═════╩════════════╩═══════════════════════════════════════════╝");
                bool noValidSetFound = true;
                do
                {
                    Console.WriteLine("");
                    Console.Write($"Which set shall be used? [{Settings.LastGeneratedSet}]> ");
                    var setCode = Console.ReadLine().ToUpper();
                    if(string.IsNullOrEmpty(setCode) && !string.IsNullOrEmpty(Settings.LastGeneratedSet))
                    {
                        Console.WriteLine($"Using last used set {Settings.LastGeneratedSet}.");
                        setCode = Settings.LastGeneratedSet;
                    }
                    set = (await setService.FindAsync(setCode)).Value;

                    // save last used set
                    Settings.LastGeneratedSet = setCode;
                    H.SaveSettings(Settings, $@"{BaseDirectory}\{JsonDirectory}\settings.json");
                    
                    Console.WriteLine("");
                    if (set == null)
                    {
                        Console.WriteLine($"The given input [{setCode}] is no valid set code.");
                        Console.Write("Press any key to continue.");
                    }
                    else
                    {
                        noValidSetFound = false;
                    }
                } while (noValidSetFound);

                Console.WriteLine($"Chosen set: {set.Name}");
                Console.WriteLine($"Reading set file...");
                ReadSingleSetWithUpdateCheck(set.Code);
                
                Settings.AddSet(set.Code);
                H.SaveSettings(Settings, @$"{BaseDirectory}\{JsonDirectory}\settings.json");
                
                Console.WriteLine("");
                Console.Write("How many boosters shall be created? [1] > ");
                var count = Console.ReadLine();
                int boosterCount = int.TryParse(count, out boosterCount) ? boosterCount : 1;
                Console.WriteLine("");
                Console.WriteLine($"Generating {boosterCount} {(boosterCount == 1 ? "booster" : "boosters")} of this set \"{set.Name}\".");
                Console.CursorVisible = false;
                Console.Write("Press any key to start generating.");
                Console.ReadKey();

                Console.Clear();

                // create new draft folder
                DirectoryInfo draftDirectory = new(@$"{BaseDirectory}\{DraftDirectory}\{DateTime.Now:yyyy-MM-ddTHH-mm-ss}");
                if(!draftDirectory.Exists) { draftDirectory.Create(); }

                for (int i = 1; i <= boosterCount; i++)
                {
                    Console.WriteLine($"Generating booster {i}/{boosterCount}...");

                    // get a booster
                    var booster = GenerateBooster(set.Code);

                    // new booster guid 
                    var boosterGuid = Guid.NewGuid();

                    // create directory
                    DirectoryInfo boosterDirectory = new(@$"{BaseDirectory}\{BoosterDirectory}\{boosterGuid}\");
                    if (!boosterDirectory.Exists) { boosterDirectory.Create(); }

                    Console.WriteLine("Downloading images...");
                    Console.WriteLine("".PadRight(Console.WindowWidth, '═'));

                    // load images
                    foreach (var card in booster) { await GetImage(card, boosterDirectory.FullName); }

                    //// copy magic back once
                    //FileInfo backFile = new(@$"{BaseDirectory}\images\mtg.back.png");
                    //if(backFile.Exists) { backFile.CopyTo(@$"{boosterDirectory.FullName}\{backFile.Name}"); }

                    if (IsWindows)
                    {
                        Console.WriteLine("Creating PDF-file via nanDECK...");

                        // prepare pdf with nandeck
                        Process proc = new();
                        proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        proc.StartInfo.CreateNoWindow = true;
                        proc.StartInfo.FileName = "cmd.exe";
                        proc.StartInfo.Arguments = $"/c {NanDeckPath} \"{BaseDirectory}\\{ScriptDirectory}\\{DefaultScriptName}.nde\" /[guid]={boosterGuid} /[boosterfolder]={BaseDirectory}\\{BoosterDirectory} /createpdf";
                        if (Settings.AutomaticPrinting)
                        {
                            proc.StartInfo.Arguments += " /print";
                        }
                        proc.EnableRaisingEvents = true;
                        proc.Start();
                        proc.WaitForExit();

                        if (proc.ExitCode == 0)
                        {
                            FileInfo file = new(@$"{BaseDirectory}\{ScriptDirectory}\{DefaultScriptName}.pdf");
                            if (file.Exists)
                            {
                                file.MoveTo($@"{draftDirectory}\{set.Code.ToLower()}_{boosterGuid}.pdf");
                            }
                            Console.WriteLine("".PadRight(Console.WindowWidth, '═'));
                            Console.WriteLine($@"File {draftDirectory}\{boosterGuid}.pdf created.");
                            Console.WriteLine("".PadRight(Console.WindowWidth, '═'));
                        }
                        else
                        {
                            Console.WriteLine("".PadRight(Console.WindowWidth, '═'));
                            Console.WriteLine("Booster creation failed...");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Skipping PDF-file generation...");
                    }

                    H.UpdateBoosterCount(Settings, @$"{BaseDirectory}\{JsonDirectory}\settings.json", 1);

                    // cleanup
                    if (IsWindows) { boosterDirectory.Delete(true); }
                    Console.Clear();
                }

                Console.WriteLine("");
                Console.WriteLine("All boosters created.");
                Console.WriteLine("Press any key to generate more boosters.");
                Console.Write("To exit the application press [x].");

            } while (Console.ReadKey().Key != ConsoleKey.X);
            
            return true;
        }

        private static async Task<bool> GetImage(string absoluteDownloadUri, string imageName, string imageExtension, string cacheDirectory, string targetBoosterDirectory)
        {
            // check for image
            FileInfo file = new(@$"{cacheDirectory}\{imageName[..1]}\{imageName.Substring(1, 1)}\{imageName}.{imageExtension}");
            if (file.Exists) { _ = file.CopyTo(targetBoosterDirectory + file.Name); return true; }

            // check target directory
            DirectoryInfo directoryInfo = new(@$"{cacheDirectory}\{imageName[..1]}\{imageName.Substring(1, 1)}\");
            if(!directoryInfo.Exists) { directoryInfo.Create(); }

            // download if not present
            await client.DownloadFileTaskAsync(absoluteDownloadUri, @$"{cacheDirectory}\{imageName[..1]}\{imageName.Substring(1, 1)}\{imageName}.{imageExtension}");

            // copy to booster directory
            FileInfo newFile = new(@$"{cacheDirectory}\{imageName[..1]}\{imageName.Substring(1, 1)}\{imageName}.{imageExtension}");
            if (newFile.Exists) { _ = newFile.CopyTo(targetBoosterDirectory + newFile.Name); return true; }

            return false;
        }

        private static ConsoleColor GetColorByRarity(string rarity)
        {
            //Console.Write($"[{rarity}]");
            return rarity switch
            {
                "common" => ConsoleColor.Gray,
                "uncommon" => ConsoleColor.White,
                "rare" => ConsoleColor.Yellow,
                "mythic" => ConsoleColor.Red,
                "land" => ConsoleColor.DarkYellow,
                "special" => ConsoleColor.Magenta,
                "bonus" => ConsoleColor.Magenta,
                _ => ConsoleColor.Gray,
            };
        }

        private static async Task<bool> GetImage(models.CardIdentifiers cardIdentifiers, string boosterDirectory)
        {
            // get scryfall card
            var scryfallCard = await api.GetCardByScryfallIdAsync(cardIdentifiers.ScryfallId);
            var currentColor = Console.ForegroundColor;

            Console.ForegroundColor = GetColorByRarity(scryfallCard.Rarity);
            Console.WriteLine($"Lade {scryfallCard.Name} herunter...");

            // check if images are present
            if (scryfallCard.ImageUris != null)
            {
                _ = await GetImage(scryfallCard.ImageUris["png"].AbsoluteUri, scryfallCard.Id.ToString(), "png", @$"{BaseDirectory}\{ImageCacheDirectory}\{ScryfallCacheDirectory}", boosterDirectory);
            }
            else
            {
                if (scryfallCard.CardFaces != null)
                {
                    foreach (var item in scryfallCard.CardFaces)
                    {
                        // get image uri
                        var uri = item.ImageUris["png"].AbsoluteUri;

                        // get card face
                        var face = uri.Contains("front") ? "front" : "back";

                        // download image
                        _ = await GetImage(uri, $"{scryfallCard.Id}_{face}", "png", @$"{BaseDirectory}\{ImageCacheDirectory}\{ScryfallCacheDirectory}", boosterDirectory);
                    }
                }
            }
            Console.ForegroundColor = currentColor;

            return true;
        }
    }
}
