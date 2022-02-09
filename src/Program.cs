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
using System.Threading;
using System.Threading.Tasks;
using H = ProxyDraftor.lib.Helpers;

namespace ProxyDraftor
{
    class Program
    {
#if DEBUG
        //public static string BaseDirectory { get; set; } = @"c:\dev\git\ProxyDraftor\src";
        public static string BaseDirectory { get; set; } = @"C:\Users\Affenbande\source\repos\RetroLotti\MagicTheGatheringProxyDrafter\src";
#else
        public static string BaseDirectory { get; set; } = Environment.CurrentDirectory;
#endif
        private static string JsonDirectory { get; set; } = ConfigurationManager.AppSettings["JsonDirectory"];
        private static string JsonSetDirectory { get; set; } = ConfigurationManager.AppSettings["JsonSetDirectory"];
        private static string BoosterDirectory { get; set; } = ConfigurationManager.AppSettings["BoosterDirectory"];
        private static string ImageCacheDirectory { get; set; } = ConfigurationManager.AppSettings["ImageCacheDirectory"];
        private static string ScryfallCacheDirectory { get; set; } = ConfigurationManager.AppSettings["ScryfallCacheDirectory"];
        private static string ScriptDirectory { get; set; } = ConfigurationManager.AppSettings["ScriptDirectory"];
        private static string DraftDirectory { get; set; } = ConfigurationManager.AppSettings["DraftDirectory"];
        private static string DefaultScriptName { get; set; } = ConfigurationManager.AppSettings["DefaultScriptName"];
        private static string NanDeckPath { get; set; } = ConfigurationManager.AppSettings["NanDeckPath"];
        private static string LastGeneratedSet { get; set; } = ConfigurationManager.AppSettings["LastGeneratedSet"];
        private static bool AutomaticPrinting { get; set; } = bool.Parse(ConfigurationManager.AppSettings["AutomaticPrinting"]);

        private static readonly SortedList<string, string> releaseTimelineSets = new();
        private static readonly SortedList<string, models.Set> sets = new();
        
        private static readonly IMtgServiceProvider serviceProvider = new MtgServiceProvider();
        private static readonly WebClient client = new();
        private static readonly ApiCaller api = new();

        static async Task Main()
        {
            H.Write("╔═", 0, 0);
            H.Write("Bereite Applikation vor", (Console.WindowWidth / 2) - ("Bereite Applikation vor".Length / 2), 0);
            H.Write("═╗", Console.WindowWidth - "═╗".Length, 0);
            H.Write("╚", 0, 1);
            H.Write("".PadRight(Console.WindowWidth - 2, '═'), 1, 1);
            H.Write("╝", Console.WindowWidth - 1, 1);
            Console.SetCursorPosition(0, 2);

            Console.WriteLine(">> Prüfe Verzeichnisse...");
            CheckAllDirectories();

            Console.WriteLine(">> Lese Sets...");
            ReadAllSets();

            Console.WriteLine(">> Halte nach NanDeck ausschau...");
            H.CheckNanDeck(NanDeckPath);

            Console.WriteLine(">> Starte...");
            Thread.Sleep(666);
            Console.Clear();

            // start main loop
            await Draft();
        }

        private static void CheckAllDirectories()
        {
            H.CheckDirectory(@$"{BaseDirectory}\{JsonDirectory}\{JsonSetDirectory}\");
            H.CheckDirectory(@$"{BaseDirectory}\{BoosterDirectory}\");
            H.CheckDirectory(@$"{BaseDirectory}\{ScriptDirectory}\");
            H.CheckDirectory(@$"{BaseDirectory}\{DraftDirectory}\");
            H.CheckDirectory(@$"{BaseDirectory}\{ImageCacheDirectory}\");
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
                    Console.WriteLine("Keine Set-Dateien gefunden!");
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
                    // reset card id
                    Guid card = Guid.Empty;

                    // prevent added duplicate cards
                    do { card = temporarySheet.RandomElementByWeight(e => e.Value).Key; } while (boosterCards.Contains(card));
                    
                    // add card to booster
                    boosterCards.Add(card);
                }
            }

            // balance colors
            // TODO: if a sheet has balanceColors == true this has to be considered ==> BUT HOW

            // get scryfall id for card determination later on
            for (int i = 0; i < boosterCards.Count; i++) { boosterCardIdentifier.Add(cards[boosterCards[i]].Identifiers); boosterCards[i] = cards[boosterCards[i]].Identifiers.ScryfallId; }

            return boosterCardIdentifier;
        }

        static async Task<bool> MainLoop()
        {
            string title = "RetroLottis Magic The Gathering Proxy Generator";
            Console.CursorVisible = false;

            do
            {
                H.Write("╔═", 0, 0);
                H.Write(title, (Console.WindowWidth / 2) - (title.Length / 2), 0);
                H.Write("═╗", Console.WindowWidth - "═╗".Length, 0);

                H.Write("╠═══╦══════════╦═══════════════════════════════════════════════╝", 0, 1);
                H.Write("╠ D ╬ Draft    ╣", 0, 2);
                H.Write("╠ O ╬ Optionen ╣", 0, 3);
                H.Write("╟───╫──────────╢", 0, 4);
                H.Write("╠ X ╬ Beenden  ╣", 0, 5);
                H.Write("╚═══╩══════════╝", 0, 6);

            } while (Console.ReadKey().Key != ConsoleKey.X);

            return true;
        }

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
                    Console.Write($"Welches Set soll verwendet werden? [{LastGeneratedSet}]> ");
                    var setCode = Console.ReadLine();
                    if(string.IsNullOrEmpty(setCode) && !string.IsNullOrEmpty(LastGeneratedSet))
                    {
                        Console.WriteLine($"Benutze zuletzt verwendetes Set {LastGeneratedSet}.");
                        setCode = LastGeneratedSet;
                    }
                    set = (await setService.FindAsync(setCode)).Value;
                    Console.WriteLine("");
                    if (set == null)
                    {
                        Console.WriteLine($"Die Eingabe [{setCode}] ist kein gültiger Set-Code.");
                        Console.Write("Beliebige Taste zum fortfahren drücken.");
                    }
                    else
                    {
                        noValidSetFound = false;
                    }
                } while (noValidSetFound);

                // save last used set
                ConfigurationManager.AppSettings["LastGeneratedSet"] = set.Code.ToUpper();
                
                Console.WriteLine($"Ausgewähltes Set: {set.Name}");
                Console.WriteLine("");
                Console.Write("Wie viele Booster sollen erstellt werden? [1] > ");
                var count = Console.ReadLine();
                int boosterCount = int.TryParse(count, out boosterCount) ? boosterCount : 1;
                Console.WriteLine("");
                Console.WriteLine($"Es {(boosterCount == 1 ? "wird" : "werden")} {boosterCount} Booster der Erweiterung \"{set.Name}\" erstellt.");
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
                    var booster = GenerateBooster(set.Code);

                    // new booster guid 
                    var boosterGuid = Guid.NewGuid();

                    // create directory
                    DirectoryInfo boosterDirectory = new(@$"{BaseDirectory}\{BoosterDirectory}\{boosterGuid}\");
                    if (!boosterDirectory.Exists) { boosterDirectory.Create(); }

                    Console.WriteLine("Lade Bilder herunter...");
                    Console.WriteLine("".PadRight(Console.WindowWidth, '═'));

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
                    proc.StartInfo.Arguments = $"/c {NanDeckPath} \"{BaseDirectory}\\{ScriptDirectory}\\{DefaultScriptName}.nde\" /[guid]={boosterGuid} /[boosterfolder]={BaseDirectory}\\{BoosterDirectory} /createpdf";
                    if (AutomaticPrinting)
                    {
                        proc.StartInfo.Arguments += " /print";
                    }
                    proc.EnableRaisingEvents = true;
                    proc.Start();
                    proc.WaitForExit();

                    if(proc.ExitCode == 0)
                    {
                        FileInfo file = new(@$"{BaseDirectory}\{ScriptDirectory}\{DefaultScriptName}.pdf");
                        if (file.Exists)
                        {
                            file.MoveTo($@"{draftDirectory}\{boosterGuid}.pdf");
                        }
                        Console.WriteLine("".PadRight(Console.WindowWidth, '═'));
                        Console.WriteLine($@"Datei {draftDirectory}\{boosterGuid}.pdf erstellt.");
                        Console.WriteLine("".PadRight(Console.WindowWidth, '═'));
                    } 
                    else
                    {
                        Console.WriteLine("".PadRight(Console.WindowWidth, '═'));
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

        private static async Task<bool> GetImage(string absoluteDownloadUri, string imageName, string imageExtension, string cacheDirectory, string targetBoosterDirectory)
        {
            // check for image
            FileInfo file = new(@$"{cacheDirectory}\{imageName}.{imageExtension}");
            if (file.Exists) { file.CopyTo(targetBoosterDirectory + file.Name); return true; }

            // download if not present
            await client.DownloadFileTaskAsync(absoluteDownloadUri, @$"{cacheDirectory}\{imageName}.{imageExtension}");

            // copy to booster directory
            FileInfo newFile = new(@$"{cacheDirectory}\{imageName}.{imageExtension}");
            if (newFile.Exists) { newFile.CopyTo(targetBoosterDirectory + newFile.Name); return true; }

            return false;
        }

        private static async Task<bool> GetImage(models.CardIdentifiers cardIdentifiers, string boosterDirectory)
        {
            // get scryfall card
            var scryfallCard = await api.GetCardByScryfallIdAsync(cardIdentifiers.ScryfallId);

            Console.WriteLine($"Lade {scryfallCard.Name} herunter...");

            // check if images are present
            if(scryfallCard.ImageUris != null)
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

            return true;
        }
    }
}
