using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using MgcPrxyDrftr.lib;
using MgcPrxyDrftr.models;
using MtgApiManager.Lib.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuestPDF.Infrastructure;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using TextCopy;
using Card = MgcPrxyDrftr.models.Card;
using H = MgcPrxyDrftr.lib.Helpers;
using Image = SixLabors.ImageSharp.Image;
using OpenBoosters.Api;

namespace MgcPrxyDrftr
{
    internal class Program
    {
        private static StateMachine StateMachine { get; set; }

        private static string BaseDirectory { get; } = ConfigurationManager.AppSettings["BaseDirectory"] ?? Environment.CurrentDirectory;    
        private static string JsonDirectory { get; } = ConfigurationManager.AppSettings["JsonDirectory"] ?? "json";
        private static string SetDirectory { get; } = ConfigurationManager.AppSettings["SetDirectory"] ?? "sets";
        private static string DeckDirectory { get; } = ConfigurationManager.AppSettings["DeckDirectory"] ?? "decks";
        private static string BoosterDirectory { get; } = ConfigurationManager.AppSettings["BoosterDirectory"] ?? "booster";
        private static string CacheDirectory { get; } = ConfigurationManager.AppSettings["CacheDirectory"] ?? "cache";
        private static string ScryfallCacheDirectory { get; } = ConfigurationManager.AppSettings["ScryfallCacheDirectory"] ?? "scryfall";
        private static string DraftDirectory { get; } = ConfigurationManager.AppSettings["DraftDirectory"] ?? "draft";
        private static string OutputDirectory { get; } = ConfigurationManager.AppSettings["OutputDirectory"] ?? "output";
        private static string FileDirectory { get; } = ConfigurationManager.AppSettings["FileDirectory"] ?? "files";
        private static string TemporaryDirectory { get; } = ConfigurationManager.AppSettings["TemporaryDirectory"] ?? "temporary";
        private static string ListDirectory { get; } = ConfigurationManager.AppSettings["ListDirectory"] ?? "lists";
        private static bool UseSetList { get; set; } = bool.Parse(ConfigurationManager.AppSettings["UseSetList"] ?? "true");
        private static bool IsWindows { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static bool IsCommandLineMode { get; set; }
        private static CommandLineOptions Options { get; set; }

        private static readonly SortedList<string, string> ReleaseTimelineSets = [];
        private static readonly SortedList<string, SetRoot> Sets = [];
        private static SortedList<string, Deck> Decks { get; } = [];
        private static readonly HashSet<string> SheetList = [];
        private static HashSet<string> AddedSheets { get; set; } = [];
        private static readonly Dictionary<string, List<string>> SetDependencies = [];

        public static readonly IMtgServiceProvider ServiceProvider = new MtgServiceProvider();
        [Obsolete("Obsolete")] private static readonly WebClient Client = new();
        private static readonly ApiCaller Api = new();
        private static Settings Settings { get; set; }

        // decks
        private static DeckList DeckList { get; set; }
        // sets
        private static SetList SetList { get; } = new();

        private static async Task Main(string[] args)
        {
            // set quest pdf license
            QuestPDF.Settings.License = LicenseType.Community;

            Options = Parser.Default.ParseArguments<CommandLineOptions>(args).Value;
            if (args.Length > 0) { IsCommandLineMode = true; }
            if (IsCommandLineMode) { await PrepareCommandLineModeAsync(args); return; }

#pragma warning disable CA1416
            if (IsWindows) { Console.SetWindowSize(136, 40); }
#pragma warning restore CA1416

            WriteHeader();

            Console.WriteLine(">> Pre-Clean-Up...");
            CleanFolders();

            Console.WriteLine(">> Checking directories...");
            CheckAllDirectories();

            Console.WriteLine(">> Reading settings...");
            Settings = new Settings();
            Settings.Load();

            // TODO: das auch
            //Console.WriteLine(">> Reading Prices...");
            //await LoadTodaysPriceList().ConfigureAwait(false);
            //await LoadPriceList().ConfigureAwait(false);

            // TODO: das will ich noch machen!
            //if (Settings.RunsForFirstTime)
            //{
            //    Console.WriteLine(">> It appears you are running MgcPrxyDrftr for the first time.");
            //    Console.WriteLine(">> All available set files will be downloaded from mtgjson.");

            //    // TODO: first get update for SetList file from mtgjson

            //    var tempList = Settings.SetsToLoad;

            //    DownloadAllSetFiles();

            //    //SetToSql(Sets, true);

            //    AnalyseAllSets();

            //    Settings.RunsForFirstTime = false;
            //    Settings.SetsToLoad = tempList;
            //    Settings.Save();
            //}

            //Console.WriteLine(">> Reading sets from disk...");
            //if (UseSetList) { await ReadAllConfiguredSets(Settings.SetsToLoad).ConfigureAwait(false); } else { ReadAllSets(); }
            //Console.WriteLine(">> Reading support sets from disk...");
            //await ReadAllConfiguredSets(Settings.SupportSetsToLoad);

            //Console.WriteLine(">> Reading decklist...");
            //await LoadDeckList().ConfigureAwait(false);
            //Console.WriteLine(">> Reading decks from disk...");
            //ReadAllDecks();

            Console.WriteLine(">> Starting...");
            Thread.Sleep(100);
            Console.Clear();

#if DEBUG
            // main loop
            _ = await EnterTheLoop();
            
            ////AnalyseAllSets();
            ////GenerateFixedBoosterSheetSqlUpdate();
            ////ResetAndCleanEverything();

            //// convert all given sets to sql inserts for mtgoa
            ////SetToSql(new SortedList<string, SetRoot>(){ { "LEA", Sets["LEA"]}, { "3ED", Sets["3ED"] }, { "ARN", Sets["ARN"] } }, false);
            ////SetToSql(Sets, true);
#else
            // start application loop
            _ = await EnterTheLoop();
#endif
        }

        private static async Task<OverallPriceList> LoadTodaysPriceList(bool forceDownload = false)
        {
            const string fileName = "AllPricesToday.json";
            FileInfo file = new(@$"{BaseDirectory}\{JsonDirectory}\{fileName}");

            if (Settings.LastPriceDownload < DateTime.Today)
            {
                Settings.LastPriceDownload = DateTime.Today;
                forceDownload = true;
            }

            // remove file if force download is used
            if (file.Exists && forceDownload) { file.Delete(); }

            // download file if it is missing or force download is used
            if (!file.Exists || forceDownload)
            {
                var valid = await H.DownloadAndValidateFile($"https://mtgjson.com/api/v5/{fileName}", $"https://mtgjson.com/api/v5/{fileName}.sha256", @$"{BaseDirectory}\{JsonDirectory}\");
                if (!valid)
                {
                    throw new Exception("Filechecksum is invalid!");
                }
            }

            var todaysPriceList = await File.ReadAllTextAsync(@$"{BaseDirectory}\{JsonDirectory}\{fileName}").ConfigureAwait(false);
            var list = JsonConvert.DeserializeObject<OverallPriceList>(todaysPriceList);

            Settings.Save();

            return list;
        }

        /// <summary>
        /// This is a method whose sole purpose is to handle cli calls of the program
        /// </summary>
        /// <param name="args"></param>
        private static async Task PrepareCommandLineModeAsync(string[] args)
        {
            if (args.Length == 0) { return; }

            if (!Options.Silent)
            {
                Console.WriteLine("Welcome to MgcPrxyDrftr!");
                Console.WriteLine("After some cleanup and preparation I'll be glad to start generating!");
                Console.WriteLine("");
            }

            CleanFolders();
            var targetDirectory =
                H.CheckDirectory(
                    @$"{BaseDirectory}\{OutputDirectory}\{DraftDirectory}\{DateTime.Now:yyyy-MM-ddTHH-mm-ss}\");

            // if there is a comma separated list of commands this step is called multiple times
            foreach (var boosterDraftDefinition in Options.Booster.Split(','))
            {
                _ = await DraftApi(boosterDraftDefinition, targetDirectory.FullName);
            }

            // open draft directory
            if (!IsWindows || Options.Silent) return;

            Process process = new();
            process.StartInfo.WorkingDirectory = $@"{targetDirectory.FullName}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = "explorer.exe";
            process.StartInfo.Arguments = $@"{targetDirectory.FullName}";
            process.Start();
        }

        // TODO: read set list to determine child sets
        private static async Task DetermineChildSets()
        {
            var mtgjsonSetList = await LoadSetList().ConfigureAwait(false);

            foreach (var set in mtgjsonSetList.Data
                         .Where(s => s.IsOnlineOnly == false && s.IsPartialPreview == false &&
                                     !s.Code.Equals("CON", StringComparison.OrdinalIgnoreCase) &&
                                     s.Type is SetType.Commander or SetType.Core or SetType.DraftInnovation
                                         or SetType.Expansion or SetType.Masterpiece or SetType.Masters)
                         .OrderBy(s => s.ReleaseDate))
            {
                // ignore if no parent is found
                if (string.IsNullOrEmpty(set.ParentCode)) continue;

                // get parent
                _ = ReadSingleSetWithUpdateCheck(set.ParentCode);

                // add dependency
                SetDependencies.TryAdd(set.ParentCode.ToUpper(), []);
                if (SetDependencies.TryGetValue(set.ParentCode, out var dependencyList)) { dependencyList.Add(set.Code); }

                // load child into list
                _ = ReadSingleSetWithUpdateCheck(set.Code);
            }
        }

        private static void DownloadAllSetFiles()
        {
            // TODO: implement fix for filename CON as it is a reserved keyword in windows
            foreach (var set in SetList.Data.Where(s =>
                         s.IsOnlineOnly == false && s.IsPartialPreview == false && !s.Code.Equals("CON", StringComparison.OrdinalIgnoreCase) && s.Type is SetType.Commander
                             or SetType.Core or SetType.DraftInnovation or SetType.Commander or SetType.Expansion
                             or SetType.Masterpiece or SetType.Masters).OrderBy(s => s.ReleaseDate))
            {
                try
                {
                    Console.WriteLine($"Downloading {set.Code.ToUpper()} ...");
                    _ = ReadSingleSetWithUpdateCheck(set.Code.ToUpper());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error ({set.Code}): {ex.Message}");
                }
            }
        }

        // removes all json files and clears the output
        // also resets the settings file
        private static void ResetAndCleanEverything()
        {
            Settings = new Settings();
            Settings.Save();

            DirectoryInfo outputDirectoryInfo = new(@$"{BaseDirectory}\{OutputDirectory}\");
            outputDirectoryInfo.Delete(true);

            DirectoryInfo cacheDirectoryInfo = new(@$"{BaseDirectory}\{CacheDirectory}\");
            cacheDirectoryInfo.Delete(true);

            DirectoryInfo tempDirectoryInfo = new(@$"{BaseDirectory}\{TemporaryDirectory}\");
            tempDirectoryInfo.Delete(true);

            DirectoryInfo jsonDirectoryInfo = new(@$"{BaseDirectory}\{JsonDirectory}\");
            jsonDirectoryInfo.Delete(true);
        }

        private static SortedDictionary<Guid, Card> ReadAllCards()
        {
            var list = new SortedDictionary<Guid, Card>();
            var dir = new DirectoryInfo(@$"{BaseDirectory}\{JsonDirectory}\{SetDirectory}\");
            foreach (var item in dir.GetFiles("*.json"))
            {
                var set = ReadSingleSet(item.Name[..item.Name.IndexOf('.')]);
                foreach(var card in set.Data.Cards)
                {
                    list.Add(card.Uuid, card);
                }
            }

            return list;
        }

        private static void AnalyseAllSets()
        {
            var dir = new DirectoryInfo(@$"{BaseDirectory}\{JsonDirectory}\{SetDirectory}\");
            foreach (var item in dir.GetFiles("*.json"))
            {
                Console.WriteLine($"Reading Set {item.Name}");
                _ = ReadSingleSet(item);
            }

            foreach (var code in ReleaseTimelineSets.Select(releaseTimelineSet => Sets[releaseTimelineSet.Value].Data.Code == "CON" ? "CON_" : Sets[releaseTimelineSet.Value].Data.Code))
            {
                AnalyseSet(code);
            }
        }

        private static void GenerateFixedBoosterSheetSqlUpdate()
        {
            ReadAllSets();

            foreach (var code in ReleaseTimelineSets.Select(releaseTimelineSet => Sets[releaseTimelineSet.Value].Data.Code == "CON" ? "CON_" : Sets[releaseTimelineSet.Value].Data.Code))
            {
                GetFixedBoosterSheets(code);
            }

            // TODO: add
            // ALTER TABLE `setBoosterSheets` ADD `sheetIsFixed` TINYINT(1) NOT NULL DEFAULT '0' AFTER `sheetIsFoil`;
            // at the end
        }

        private static void GetFixedBoosterSheets(string setCode)
        {
            var setFileContent= File.ReadAllText(@$"{BaseDirectory}\{JsonDirectory}\{SetDirectory}\{setCode.ToUpper()}.json");
            var setJsonObject = JObject.Parse(setFileContent);
            var boosterDataToken = setJsonObject.SelectToken("data")?.SelectToken("booster");
            var sqlUpdateString = new StringBuilder();

            var isPreview = setJsonObject.SelectToken("data")?.SelectToken("isPartialPreview")?.Value<bool>() ?? false;
            if (isPreview) { Console.WriteLine($"Skipping preview set {setCode}"); return; }

            if (boosterDataToken is null) return;

            Console.WriteLine($"Processing sheets for set {setCode}");

            foreach (var jToken in boosterDataToken)
            {
                var boosterName = ((JProperty)jToken).Name;

                foreach (var sheetName in from sheetToken in jToken.ToList()[0].SelectToken("sheets")!.ToList()
                         let isFixed = (sheetToken.ToList()[0].SelectToken("fixed") ?? false).Value<bool>()
                         let sheetName = ((JProperty)sheetToken).Name
                         where isFixed
                         select sheetName)
                {
                    sqlUpdateString.AppendLine($"update setBoosterSheets set sheetIsFixed = 1 where setCode = '{setCode}' and boosterName = '{boosterName}' and sheetName = '{sheetName}';");
                }
            }

            if (string.IsNullOrEmpty(sqlUpdateString.ToString())) return;

            using var writer = new StreamWriter(@$"{BaseDirectory}\output\update.sql", true);
            writer.WriteLine(sqlUpdateString);
        }

        private static void AnalyseSet(string setCode)
        {
            var file = File.ReadAllText(@$"{BaseDirectory}\{JsonDirectory}\{SetDirectory}\{setCode.ToUpper()}.json");
            var json = JObject.Parse(file);
            var foo = json.SelectToken("data")?.SelectToken("booster");
            var upgradeStringContents = new StringBuilder();
            var upgradeStringSheets = new StringBuilder();
            var sampleContent = new Contents();
            var newSheets = false;

            var isPreview = json.SelectToken("data")?.SelectToken("isPartialPreview")?.Value<bool>() ?? false;
            if (isPreview)
            {
                Console.WriteLine("Skipping preview set");
                return;
            }

            Console.WriteLine($"Processing set sheets {setCode}");

            if (foo is null) return;

            upgradeStringContents.AppendLine("namespace MgcPrxyDrftr.models { public partial class Contents { ");
            upgradeStringSheets.AppendLine("public partial class Sheets { ");

            foreach (var jToken in foo)
            {
                var attributeProperty = (JProperty)jToken;
                var attribute = foo[attributeProperty.Name];
                if (attribute == null) continue;
                foreach (var booster in attribute["boosters"]!)
                {
                    foreach (var jToken1 in booster["contents"]!)
                    {
                        var sheet = (JProperty)jToken1;
                        var sheetNameTitleCase = char.ToUpper(sheet.Name[0]) + sheet.Name[1..];
                        // check if sheet exists
                        var contentSheet = sampleContent.GetType().GetProperty(sheetNameTitleCase);

                        if (contentSheet != null || AddedSheets.Contains(sheetNameTitleCase) || sheetNameTitleCase.Equals("FoilUn-list")) continue;
                        newSheets = true;
                        AddedSheets.Add(sheetNameTitleCase);

                        upgradeStringContents.AppendLine($"public long? {sheetNameTitleCase} {{ get; set; }}");
                        upgradeStringSheets.AppendLine($"public Sheet {sheetNameTitleCase} {{ get; set; }}");
                    }
                }
            }

            upgradeStringContents.AppendLine(" } ");
            upgradeStringSheets.AppendLine(" } }");
            var bar1 = upgradeStringContents.ToString();
            var bar2 = upgradeStringSheets.ToString();

            var upgradeFileString = bar1 + bar2;

            if (!newSheets) return;
            using var writer = new StreamWriter(@$"{BaseDirectory}\models\upgrades\Upgrade{setCode.ToUpper()}_{Guid.NewGuid().ToString().ToLower()[..8]}.cs");
            writer.WriteLine(upgradeFileString);
        }

        private static void SetToSql(SortedList<string, SetRoot> sets, bool individualFiles = false)
        {
            var sb = new StringBuilder();
            var sbHeader = new StringBuilder();
            var boosterCounter = 0;
            var boosterBlueprintCounter = 0;
            var sheetCounter = 0;
            var setCounter = 0;
            var addedSheets = new List<string>();

            sbHeader.AppendLine("set character_set_server = 'utf8mb4';");
            sbHeader.AppendLine("set collation_server = 'utf8mb4_unicode_520_ci';");
            sbHeader.AppendLine("set names latin1;");
            //sbHeader.AppendLine("update rs_casino set maintenancesw = 1 where casinoname = 'Magic: Online Arena';");
            sbHeader.AppendLine("commit;");

            // handle all sets that have a least one booster
            foreach (var set in sets.Where(s => s.Value is { Data.Booster: not null }))
            {
                setCounter++;
                sb.AppendLine($"insert into rs_set (id, setcode, `name`, releasedate) values ({setCounter}, '{set.Value.Data.Code.ToUpper()}', '{set.Value.Data.Name.Replace("\'", "\\\'")}', '{set.Value.Data.ReleaseDate.ToString("yyyy-MM-dd")}');");
                sb.AppendLine("commit;");

                // add all cards of this set
                foreach (var item in set.Value.Data.Cards)
                {
                    //item.Colors
                    sb.AppendLine($"insert into rs_card (`name`, setid, scryfallid, mtgjsonid, scryfallimageuri, rarityid, colors, types, subtypes, supertypes) values ('{item.Name.Replace("\'", "\\\'")}', {setCounter}, '{item.Identifiers.ScryfallId}', '{item.Uuid.ToString()}', 'https://c1.scryfall.com/file/scryfall-cards/png/front/{item.Identifiers.ScryfallId.ToString()[0]}/{item.Identifiers.ScryfallId.ToString()[1]}/{item.Identifiers.ScryfallId}.png', (select id from rs_rarity where rarityname = '{item.Rarity}'), '{string.Join("", item.Colors.Select(s => s.ToString()).ToArray())}', '{string.Join(",", item.Types.Select(s => s.ToString().Replace("\'", "\\\'")).ToArray())}', '{string.Join(",", item.Subtypes.Select(s => s.ToString().Replace("\'", "\\\'")).ToArray())}', '{string.Join(",", item.Supertypes.Select(s => s.ToString().Replace("\'", "\\\'")).ToArray())}');");
                }

                // add sub sets
                if (SetDependencies.TryGetValue(set.Value.Data.Code, out var subSets))
                {
                    foreach (var card in subSets.Where(sub => Sets.ContainsKey(sub)).SelectMany(dep => Sets[dep].Data.Cards))
                    {
                        sb.AppendLine($"insert into rs_card (`name`, setid, scryfallid, mtgjsonid, scryfallimageuri, rarityid, colors, types, subtypes, supertypes) values ('{card.Name.Replace("\'", "\\\'")}', {setCounter}, '{card.Identifiers.ScryfallId}', '{card.Uuid.ToString()}', 'https://c1.scryfall.com/file/scryfall-cards/png/front/{card.Identifiers.ScryfallId.ToString()[0]}/{card.Identifiers.ScryfallId.ToString()[1]}/{card.Identifiers.ScryfallId}.png', (select id from rs_rarity where rarityname = '{card.Rarity}'), '{string.Join("", card.Colors.Select(s => s.ToString()).ToArray())}', '{string.Join(",", card.Types.Select(s => s.ToString().Replace("\'", "\\\'")).ToArray())}', '{string.Join(",", card.Subtypes.Select(s => s.ToString().Replace("\'", "\\\'")).ToArray())}', '{string.Join(",", card.Supertypes.Select(s => s.ToString().Replace("\'", "\\\'")).ToArray())}');");
                    }
                }
                sb.AppendLine("commit;");

                // iterate all available booster types
                var boosterList = set.Value.Data.Booster.GetType().GetProperties()
                    .Where(b => b.GetValue(set.Value.Data.Booster) != null);

                addedSheets = [];
                foreach (var boosterInfo in boosterList)
                {
                    var boosterInfoObject = (DefaultBooster)boosterInfo.GetValue(set.Value.Data.Booster, null);
                    var boosterName = $"{(boosterInfoObject!.Name ?? set.Value.Data.Name + " Booster").Replace("\'", "\\\'")}";

                    // TODO: skip some booster types for now
                    if (boosterName.Contains("Sample") || boosterName.Contains("Arena")) continue;

                    //sb.AppendLine($"insert into rs_magicproduct (productname, purchaseprice, setid) values ('{boosterName}', 230, {setCounter});");
                    //sb.AppendLine("commit;");

                    // create sheets
                    foreach (var sheet in boosterInfoObject!.Sheets.GetType().GetProperties()
                                 .Where(s => s.GetValue(boosterInfoObject.Sheets, null) != null))
                    {
                        var sheetObject = (Sheet)sheet.GetValue(boosterInfoObject.Sheets, null);

                        // check for existing sheet
                        if (addedSheets.Contains(sheet.Name)) continue;

                        sheetCounter++;
                        addedSheets.Add(sheet.Name);
                        sb.AppendLine($"insert into rs_sheet (id, setid, sheetname, totalweight) values ({sheetCounter}, {setCounter}, '{sheet.Name}', {sheetObject!.TotalWeight});");
                        sb.AppendLine("commit;");

                        // also check for a sheet named theList. if found we need to load subset PLST
                        if (sheet.Name.Equals("TheList"))
                        {
                            if (Sets.TryGetValue("PLST", out var theList))
                            {
                                foreach (var card in theList.Data.Cards)
                                {
                                    sb.AppendLine($"insert into rs_card (`name`, setid, scryfallid, mtgjsonid, scryfallimageuri, rarityid, colors, types, subtypes, supertypes) values ('{card.Name.Replace("\'", "\\\'")}', {setCounter}, '{card.Identifiers.ScryfallId}', '{card.Uuid.ToString()}', 'https://c1.scryfall.com/file/scryfall-cards/png/front/{card.Identifiers.ScryfallId.ToString()[0]}/{card.Identifiers.ScryfallId.ToString()[1]}/{card.Identifiers.ScryfallId}.png', (select id from rs_rarity where rarityname = '{card.Rarity}'), '{string.Join("", card.Colors.Select(s => s.ToString()).ToArray())}', '{string.Join(",", card.Types.Select(s => s.ToString().Replace("\'", "\\\'")).ToArray())}', '{string.Join(",", card.Subtypes.Select(s => s.ToString().Replace("\'", "\\\'")).ToArray())}', '{string.Join(",", card.Supertypes.Select(s => s.ToString().Replace("\'", "\\\'")).ToArray())}');");
                                }

                                sb.AppendLine("commit;");
                            }
                        }

                        foreach (var card in sheetObject!.Cards)
                        {
                            sb.AppendLine($"insert into rs_sheetcards (sheetid, cardid, cardweight) values ({sheetCounter}, (select id from rs_card where mtgjsonid = '{card.Key}' and setid = {setCounter}), {card.Value});");
                        }

                        sb.AppendLine("commit;");
                    }

                    boosterCounter++;
                    var boosterType = MapBoosterType(boosterInfoObject.Name);
                    sb.AppendLine($"insert into rs_booster (id, setid, totalboosterweight, boostername, boostertype) values ({boosterCounter}, {setCounter}, {boosterInfoObject.BoostersTotalWeight}, '{boosterName}', '{boosterType}');");
                    sb.AppendLine("commit;");

                    // now create booster as we needed the sheets first
                    foreach (var booster in boosterInfoObject.Boosters)
                    {
                        boosterBlueprintCounter++;
                        sb.AppendLine($"insert into rs_boosterblueprint (id, boosterid, boosterweight) values ({boosterBlueprintCounter}, {boosterCounter}, {booster.Weight});");
                        sb.AppendLine("commit;");

                        foreach (var sheet in booster.Contents.GetType().GetProperties().Where(s => s.GetValue(booster.Contents, null) != null))
                        {
                            var cardCount = (long)sheet.GetValue(booster.Contents, null)!;
                            
                            sb.AppendLine($"insert into rs_boosterblueprintsheets (boosterblueprintid, sheetid, cardcount) values ({boosterBlueprintCounter}, (select id from rs_sheet where sheetname = '{sheet.Name}' and setid = {setCounter}), {cardCount});");
                        }
                        sb.AppendLine("commit;");
                    }
                }

                // skip if big file is written at the end
                if (!individualFiles) continue;

                // write single set sql file
                using (var file = File.CreateText(@$"C:\dev\{set.Value.Data.Code.ToLower()}.sql"))
                {
                    file.Write(sbHeader);
                    file.Write(sb);
                    file.Close();
                }

                sb.Clear();
            }

            // write one big file
            if (individualFiles) return;
            {
                // write set sql file
                using var file = File.CreateText(@$"C:\dev\set_data.sql");
                file.Write(sbHeader);
                file.Write(sb);
                file.Close();
            }
        }

        private static void Write(string text, ConsoleColor backgroundColor = ConsoleColor.Black, ConsoleColor foregroundColor = ConsoleColor.White)
        {
            if (Console.BackgroundColor != backgroundColor) { Console.BackgroundColor = backgroundColor; }
            if (Console.ForegroundColor != foregroundColor) { Console.ForegroundColor = foregroundColor; }

            Console.Write(text);
        }

        private static void WriteLine(string text, ConsoleColor backgroundColor = ConsoleColor.Black, ConsoleColor foregroundColor = ConsoleColor.White)
        {
            if (Console.BackgroundColor != backgroundColor) { Console.BackgroundColor = backgroundColor; }
            if (Console.ForegroundColor != foregroundColor) { Console.ForegroundColor = foregroundColor; }

            Console.WriteLine(text);
        }

        private static void GenerateCubeDraftMini()
        {
            Console.Clear();

            DirectoryInfo draftDirectory = new(@$"{BaseDirectory}\{OutputDirectory}\{DraftDirectory}\{DateTime.Now:yyyy-MM-ddTHH-mm-ss}");
            draftDirectory.Create();

            for (var k = 0; k < 1; k++)
            {
                var guid = Guid.NewGuid();

                FileInfo fileInfo = new(@$"{draftDirectory.FullName}\{guid}.txt");
                var writer = fileInfo.AppendText();

                for (var i = 0; i < 36000; i++)
                {
                    Console.WriteLine($"{i + 1}/36000 [{k+1}]");

                    var dict = GenerateBoosterPlain("NEO");
                    writer.WriteLine($"{(dict.TryGetValue("C Red", out var value) ? value : "0")}|{(dict.TryGetValue("C Green", out var value1) ? value1 : "0")}|{(dict.TryGetValue("C Black", out var value2) ? value2 : "0")}|{(dict.TryGetValue("C White", out var value3) ? value3 : "0")}|{(dict.TryGetValue("C Blue", out var value4) ? value4 : "0")}|{(dict.TryGetValue("C .Else", out var value5) ? value5 : "0")}|{(dict.TryGetValue("U Red", out var value6) ? value6 : "0")}|{(dict.TryGetValue("U Green", out var value7) ? value7 : "0")}|{(dict.TryGetValue("U Black", out var value8) ? value8 : "0")}|{(dict.TryGetValue("U White", out var value9) ? value9 : "0")}|{(dict.TryGetValue("U Blue", out var value10) ? value10 : "0")}|{(dict.TryGetValue("U .Else", out var value11) ? value11 : "0")}|{(dict.TryGetValue("R/M", out var value12) ? value12 : "0")}|{(dict.TryGetValue("C/U", out var value13) ? value13 : "0")}");

                    Thread.Sleep(50);
                }
                writer.Close();
            }
        }

        private static void GenerateCubeDraftBooster()
        {
            Console.CursorVisible = false;

            const int startPositionTop = 10;
            const int startPositionLeft = 50;

            DirectoryInfo draftDirectory = new(@$"{BaseDirectory}\{OutputDirectory}\{DraftDirectory}\{DateTime.Now:yyyy-MM-ddTHH-mm-ss}");
            draftDirectory.Create();

            WriteHeader(false);

            do
            {
                var dict = GenerateBoosterPlain("NEO");
                var guid = Guid.NewGuid();

                // prepare string
                StringBuilder build = new();
                build.AppendLine("   R    G    B    W    U    E ");
                build.AppendLine("  ╔═╗  ╔═╗  ╔═╗  ╔═╗  ╔═╗  ╔═╗");
                build.AppendLine($"C ║{(dict.TryGetValue("C Red", out var value) ? value : "-")}║  ║{(dict.TryGetValue("C Green", out var value1) ? value1 : "-")}║  ║{(dict.TryGetValue("C Black", out var value2) ? value2 : "-")}║  ║{(dict.TryGetValue("C White", out var value3) ? value3 : "-")}║  ║{(dict.TryGetValue("C Blue", out var value4) ? value4 : "-")}║  ║{(dict.TryGetValue("C .Else", out var value5) ? value5 : "-")}║");
                build.AppendLine("  ╚═╝  ╚═╝  ╚═╝  ╚═╝  ╚═╝  ╚═╝");
                build.AppendLine("  ╔═╗  ╔═╗  ╔═╗  ╔═╗  ╔═╗  ╔═╗");
                build.AppendLine($"U ║{(dict.TryGetValue("U Red", out var value6) ? value6 : "-")}║  ║{(dict.TryGetValue("U Green", out var value7) ? value7 : "-")}║  ║{(dict.TryGetValue("U Black", out var value8) ? value8 : "-")}║  ║{(dict.TryGetValue("U White", out var value9) ? value9 : "-")}║  ║{(dict.TryGetValue("U Blue", out var value10) ? value10 : "-")}║  ║{(dict.TryGetValue("U .Else", out var value11) ? value11 : "-")}║");
                build.AppendLine("  ╚═╝  ╚═╝  ╚═╝  ╚═╝  ╚═╝  ╚═╝");
                build.AppendLine("R ╔═╗                         ");
                build.AppendLine($"/ ║{(dict.TryGetValue("R/M", out var value12) ? value12 : "-")}║                         ");
                build.AppendLine("M ╚═╝                         ");

                Console.Clear();

                Console.SetCursorPosition(startPositionLeft, startPositionTop);
                Write("   ");
                Write("R", ConsoleColor.Red);
                Write("    ");
                Write("G", ConsoleColor.DarkGreen);
                Write("    ");
                Write("B", ConsoleColor.DarkMagenta);
                Write("    ");
                Write("W", ConsoleColor.White, ConsoleColor.Black);
                Write("    ");
                Write("U", ConsoleColor.Blue);
                Write("    ");
                WriteLine("E", ConsoleColor.Yellow, ConsoleColor.Black);

                Console.SetCursorPosition(startPositionLeft, Console.CursorTop);
                Write("  ╔═╗  ", ConsoleColor.Black, ConsoleColor.Red);
                Write("╔═╗  ", ConsoleColor.Black, ConsoleColor.DarkGreen);
                Write("╔═╗  ", ConsoleColor.Black, ConsoleColor.DarkMagenta);
                Write("╔═╗  ");
                Write("╔═╗  ", ConsoleColor.Black, ConsoleColor.Blue);
                WriteLine("╔═╗", ConsoleColor.Black, ConsoleColor.Yellow);

                Console.SetCursorPosition(startPositionLeft, Console.CursorTop);
                Write("C ", ConsoleColor.Black, ConsoleColor.Gray);
                Write("║", ConsoleColor.Black, ConsoleColor.Red);
                Write(dict.TryGetValue("C Red", out var value13) ? value13.ToString() : "-", ConsoleColor.Red);
                Write("║  ", ConsoleColor.Black, ConsoleColor.Red);

                Write("║", ConsoleColor.Black, ConsoleColor.DarkGreen);
                Write(dict.TryGetValue("C Green", out var value14) ? value14.ToString() : "-", ConsoleColor.DarkGreen);
                Write("║  ", ConsoleColor.Black, ConsoleColor.DarkGreen);

                Write("║", ConsoleColor.Black, ConsoleColor.DarkMagenta);
                Write(dict.TryGetValue("C Black", out var value15) ? value15.ToString() : "-", ConsoleColor.DarkMagenta);
                Write("║  ", ConsoleColor.Black, ConsoleColor.DarkMagenta);

                Write("║");
                Write(dict.TryGetValue("C White", out var value16) ? value16.ToString() : "-", ConsoleColor.White, ConsoleColor.Black);
                Write("║  ");

                Write("║", ConsoleColor.Black, ConsoleColor.Blue);
                Write(dict.TryGetValue("C Blue", out var value17) ? value17.ToString() : "-", ConsoleColor.Blue);
                Write("║  ", ConsoleColor.Black, ConsoleColor.Blue);

                Write("║", ConsoleColor.Black, ConsoleColor.Yellow);
                Write(dict.TryGetValue("C .Else", out var value18) ? value18.ToString() : "-", ConsoleColor.Black, ConsoleColor.Yellow);
                WriteLine("║  ", ConsoleColor.Black, ConsoleColor.Yellow);

                Console.SetCursorPosition(startPositionLeft, Console.CursorTop);
                Write("  ╚═╝  ", ConsoleColor.Black, ConsoleColor.Red);
                Write("╚═╝  ", ConsoleColor.Black, ConsoleColor.DarkGreen);
                Write("╚═╝  ", ConsoleColor.Black, ConsoleColor.DarkMagenta);
                Write("╚═╝  ");
                Write("╚═╝  ", ConsoleColor.Black, ConsoleColor.Blue);
                WriteLine("╚═╝", ConsoleColor.Black, ConsoleColor.Yellow);

                Console.SetCursorPosition(startPositionLeft, Console.CursorTop);
                Write("  ╔═╗  ", ConsoleColor.Black, ConsoleColor.Red);
                Write("╔═╗  ", ConsoleColor.Black, ConsoleColor.DarkGreen);
                Write("╔═╗  ", ConsoleColor.Black, ConsoleColor.DarkMagenta);
                Write("╔═╗  ");
                Write("╔═╗  ", ConsoleColor.Black, ConsoleColor.Blue);
                WriteLine("╔═╗", ConsoleColor.Black, ConsoleColor.Yellow);

                Console.SetCursorPosition(startPositionLeft, Console.CursorTop);
                Write("U ");
                Write("║", ConsoleColor.Black, ConsoleColor.Red);
                Write(dict.TryGetValue("U Red", out var value19) ? value19.ToString() : "-", ConsoleColor.Red);
                Write("║  ", ConsoleColor.Black, ConsoleColor.Red);

                Write("║", ConsoleColor.Black, ConsoleColor.DarkGreen);
                Write(dict.TryGetValue("U Green", out var value20) ? value20.ToString() : "-", ConsoleColor.DarkGreen);
                Write("║  ", ConsoleColor.Black, ConsoleColor.DarkGreen);

                Write("║", ConsoleColor.Black, ConsoleColor.DarkMagenta);
                Write(dict.TryGetValue("U Black", out var value21) ? value21.ToString() : "-", ConsoleColor.DarkMagenta);
                Write("║  ", ConsoleColor.Black, ConsoleColor.DarkMagenta);

                Write("║");
                Write(dict.TryGetValue("U White", out var value22) ? value22.ToString() : "-", ConsoleColor.White, ConsoleColor.Black);
                Write("║  ");

                Write("║", ConsoleColor.Black, ConsoleColor.Blue);
                Write(dict.TryGetValue("U Blue", out var value23) ? value23.ToString() : "-", ConsoleColor.Blue);
                Write("║  ", ConsoleColor.Black, ConsoleColor.Blue);

                Write("║", ConsoleColor.Black, ConsoleColor.Yellow);
                Write(dict.TryGetValue("U .Else", out var value24) ? value24.ToString() : "-", ConsoleColor.Black, ConsoleColor.Yellow);
                WriteLine("║  ", ConsoleColor.Black, ConsoleColor.Yellow);

                Console.SetCursorPosition(startPositionLeft, Console.CursorTop);
                Write("  ╚═╝  ", ConsoleColor.Black, ConsoleColor.Red);
                Write("╚═╝  ", ConsoleColor.Black, ConsoleColor.DarkGreen);
                Write("╚═╝  ", ConsoleColor.Black, ConsoleColor.DarkMagenta);
                Write("╚═╝  ");
                Write("╚═╝  ", ConsoleColor.Black, ConsoleColor.Blue);
                WriteLine("╚═╝", ConsoleColor.Black, ConsoleColor.Yellow);

                Console.SetCursorPosition(startPositionLeft, Console.CursorTop);
                WriteLine("R ╔═╗                         ", ConsoleColor.Black, ConsoleColor.Yellow);
                Console.SetCursorPosition(startPositionLeft, Console.CursorTop);
                WriteLine($"/ ║{ (dict.TryGetValue("R/M", out var value25) ? value25 : "-")}║                         ", ConsoleColor.Black, ConsoleColor.Yellow);
                Console.SetCursorPosition(startPositionLeft, Console.CursorTop);
                Write("M", ConsoleColor.Black, ConsoleColor.DarkRed);
                WriteLine(" ╚═╝                         ", ConsoleColor.Black, ConsoleColor.Yellow);
                Console.SetCursorPosition(startPositionLeft, Console.CursorTop);
                WriteLine("                              ");
                Console.SetCursorPosition(startPositionLeft + 18, Console.CursorTop);
                Write($"ID: {guid.ToString().Split('-')[0]}");

                FileInfo fileInfo = new(@$"{draftDirectory.FullName}\{guid.ToString().Split('-')[0]}.txt");
                var writer = fileInfo.AppendText();
                writer.WriteLine(build.ToString());
                writer.Close();
            } while (Console.ReadKey().Key != ConsoleKey.X);
        }

        private static void WriteHeader(bool setCursor = true)
        {
            H.Write(" ****     ****   ********    ******        *******  *******   **     ** **    **       *******   *******   ******** ********** *******  ", 0, 1);
            H.Write("/**/**   **/**  **//////**  **////**      /**////**/**////** //**   ** //**  **       /**////** /**////** /**///// /////**/// /**////** ", 0, 2);
            H.Write("/**//** ** /** **      //  **    //       /**   /**/**   /**  //** **   //****        /**    /**/**   /** /**          /**    /**   /** ", 0, 3);
            H.Write("/** //***  /**/**         /**        *****/******* /*******    //***     //**    *****/**    /**/*******  /*******     /**    /*******  ", 0, 4);
            H.Write("/**  //*   /**/**    *****/**       ///// /**////  /**///**     **/**     /**   ///// /**    /**/**///**  /**////      /**    /**///**  ", 0, 5);
            H.Write("/**   /    /**//**  ////**//**    **      /**      /**  //**   ** //**    /**         /**    ** /**  //** /**          /**    /**  //** ", 0, 6);
            H.Write("/**        /** //********  //******       /**      /**   //** **   //**   /**         /*******  /**   //**/**          /**    /**   //**", 0, 7);
            H.Write("//         //   ////////    //////        //       //     // //     //    //          ///////   //     // //           //     //     // ", 0, 8);

            if(setCursor) { Console.SetCursorPosition(0, 10); }
        }
        
        // #############################################################
        // START
        // #############################################################
        private static async Task<int> EnterTheLoop()
        {
            StateMachine = new StateMachine();

            do
            {
                // clear the screen
                Console.Clear();

                // show header
                WriteHeader();

                // show current menu
                PrintMenu(StateMachine.CurrentState, 0, 10);

                // move cursor
                Console.SetCursorPosition(0, 23);
                Console.Write(">>> ");

                // read entered string
                var command = Console.ReadLine();
                var isCommand = command is { Length: 1 };

                if(isCommand)
                {
                    // move to next state
                    _ = StateMachine.MoveNext(command);

                    switch(command.ToLower())
                    {
                        case "c":
                            _ = ReadClipboardAndDownload();
                            break;
                        case "a":
                            switch (StateMachine.CurrentState)
                            {
                                case LoopState.DeckCreator:
                                {
                                    foreach(var deck in DeckList.Data)
                                    {
                                        Console.WriteLine(deck.Name);
                                    }
                                    Console.Write("Press any key to continue...");
                                    _ = Console.ReadKey();
                                    break;
                                }
                                case LoopState.Options:
                                {
                                    Settings.NewDraftMenu = !Settings.NewDraftMenu;
                                    Settings.Save();
                                    break;
                                }
                                case LoopState.Main:
                                case LoopState.BoosterDraft:
                                case LoopState.DeckManager:
                                case LoopState.SetManager:
                                case LoopState.RawListManager:
                                case LoopState.FolderPrint:
                                case LoopState.Exit:
                                default:
                                    break;
                            }

                            break;
                        case "l":
                            switch (StateMachine.CurrentState)
                            {
                                case LoopState.DeckCreator:
                                {
                                    foreach (var deck in Decks)
                                    {
                                        Console.WriteLine(deck.Value.Name);
                                    }
                                    Console.Write("Press any key to continue...");
                                    _ = Console.ReadKey();
                                    break;
                                }
                                case LoopState.SetManager:
                                {
                                    foreach (var set in Sets)
                                    {
                                        Console.WriteLine($"[{set.Value.Data.Code}]\t{set.Value.Data.Name}");
                                    }
                                    Console.Write("Press any key to continue...");
                                    _ = Console.ReadKey();
                                    break;
                                }
                                case LoopState.BoosterDraft:
                                {
                                    // get only sets that actually have boosters
                                    foreach (var set in Sets.Values.Where(s => s.Data.Booster is not null).ToList())
                                    {
                                        Console.WriteLine($"[{set.Data.Code}]\t{set.Data.Name}");
                                    }
                                    Console.Write("Press any key to continue...");
                                    _ = Console.ReadKey();
                                    break;
                                }
                                case LoopState.Main:
                                    break;
                                case LoopState.Options:
                                    break;
                                case LoopState.DeckManager:
                                    break;
                                case LoopState.RawListManager:
                                    break;
                                case LoopState.FolderPrint:
                                    break;
                                case LoopState.Exit:
                                    break;
                                case LoopState.PriceChecker:
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;
                        case "f":
                            if (StateMachine.CurrentState == LoopState.Options)
                            {
                                Settings.PrintFoils = !Settings.PrintFoils;
                                Settings.Save();
                            }
                            break;
                        case "e":
                            if (StateMachine.CurrentState == LoopState.Options)
                            {
                                Settings.DownloadBasicLands = !Settings.DownloadBasicLands;
                                Settings.Save();
                            }
                            break;
                        case "p":
                            if (StateMachine.CurrentState == LoopState.Options)
                            {
                                Settings.AutomaticPrinting = !Settings.AutomaticPrinting;
                                Settings.Save();
                            }
                            break;
                        case "d":
                            if (StateMachine.CurrentState == LoopState.Options)
                            {
                                Settings.PromptForDraftConfirmation = !Settings.PromptForDraftConfirmation;
                                Settings.Save();
                            }
                            break;
                        case "r":
                            if (StateMachine.CurrentState == LoopState.Options)
                            {
                                ResetAndCleanEverything();
                            }
                            break;
                    }
                }
                else
                {
                    // clear the screen
                    Console.Clear();

                    // handle entered string
                    switch (StateMachine.CurrentState)
                    {
                        case LoopState.DeckCreator:
                            throw new NotImplementedException("deck printing is not implemented");
                            //_ = await PrintDeck(command);
                            //break;
                        case LoopState.BoosterDraft:

                            var targetDirectory =
                                @$"{BaseDirectory}\{OutputDirectory}\{DraftDirectory}\{DateTime.Now:yyyy-MM-ddTHH-mm-ss}";

                            // if there is a comma separated list of commands this step is called multiple times
                            foreach (var singleCommand in command.Split(',').ToList())
                            {
                                //_ = await Draft(singleCommand);
                                _ = await DraftApi(singleCommand, targetDirectory);
                            }
                            break;
                        case LoopState.RawListManager:
                            throw new NotImplementedException("raw list printing is not implemented");
                            //_ = await PrintRawList(command);
                            //break;
                        case LoopState.FolderPrint:
                            _ = PrintDirectory(command);
                            break;
                        case LoopState.SetManager:
                            _ = AddSet(command);
                            break;
                        case LoopState.PriceChecker:
                            GetPrice(command);
                            break;
                        case LoopState.Main:
                            break;
                        case LoopState.Options:
                            break;
                        case LoopState.DeckManager:
                            break;
                        case LoopState.Exit:
                            break;
                    }
                }

            } while (StateMachine.CurrentState != LoopState.Exit);

            return 0;
        }

        private static void GetPrice(string cardName)
        {
            // clear the screen
            Console.Clear();

            // check price file
            _ = LoadPriceList();

            // search card (get uuid with name)

            // determine prices

            // show data
        }

        private static void PrintMenu(LoopState loopState, int startLeftPosition, int startTopPosition)
        {
            switch (loopState)
            {
                case LoopState.Main:
                    H.Write("D => Draft Booster", startLeftPosition, startTopPosition + 1);
                    //H.Write("E => Create Deck", startLeftPosition, startTopPosition + 2);
                    //H.Write("S => Add or Remove Sets", startLeftPosition, startTopPosition + 3);
                    //H.Write("R => Print Raw List", startLeftPosition, startTopPosition + 4);
                    //H.Write("C => Clipboard", startLeftPosition, startTopPosition + 5);
                    H.Write("F => Print Folder", startLeftPosition, startTopPosition + 6);
                    //H.Write("P => Price Checker", startLeftPosition, startTopPosition + 7);
                    H.Write("O => Options", startLeftPosition, startTopPosition + 8);
                    H.Write("X => Exit", startLeftPosition, startTopPosition + 10);
                    break;
                case LoopState.Options:
                    H.Write($"P => enable / disable automatic printing [{(Settings.AutomaticPrinting ? "enabled" : "disabled")}]", startLeftPosition, startTopPosition + 1);
                    H.Write($"E => enable / disable basic land download [{(Settings.DownloadBasicLands ? "enabled" : "disabled")}]", startLeftPosition, startTopPosition + 2);
                    H.Write($"D => enable / disable prompting for confirmation when drafting booster [{(Settings.PromptForDraftConfirmation ? "enabled" : "disabled")}]", startLeftPosition, startTopPosition + 3);
                    H.Write($"A => enable / disable alternative (new) draft menu [{(Settings.NewDraftMenu ? "enabled" : "disabled")}]", startLeftPosition, startTopPosition + 4);
                    H.Write($"F => enable / disable foil generation [{(Settings.PrintFoils ? "enabled" : "disabled")}]", startLeftPosition, startTopPosition + 5);
                    H.Write("R => reset all settings and folders !I mean it - everything will be deleted!", startLeftPosition, startTopPosition + 7);
                    H.Write("B => Back", startLeftPosition, startTopPosition + 11);
                    break;
                case LoopState.BoosterDraft:
                    //H.Write("A => List all sets", startLeftPosition, startTopPosition + 1);
                    //H.Write("L => List downloaded sets with booster", startLeftPosition, startTopPosition + 2);
                    H.Write("Format: {SetCode}|{HowManyBoosters}[|BoosterType]", startLeftPosition, startTopPosition + 6);
                    H.Write("B => Back", startLeftPosition, startTopPosition + 8);
                    break;
                case LoopState.DeckCreator:
                    H.Write("A => List all decks", startLeftPosition, startTopPosition + 1);
                    H.Write("L => List downloaded decks", startLeftPosition, startTopPosition + 2);
                    H.Write("B => Back", startLeftPosition, startTopPosition + 8);
                    break;
                case LoopState.DeckManager:
                    H.Write("C => Print Clipboard (later create a deck from it)", startLeftPosition, startTopPosition + 1);
                    H.Write("B => Back", startLeftPosition, startTopPosition + 8);
                    break;
                case LoopState.SetManager:
                    H.Write("Enter set code to add or remove it.", startLeftPosition, startTopPosition + 1);
                    //H.Write("A => Add Set", startLeftPosition, startTopPosition + 1);
                    //H.Write("R => Remove Set", startLeftPosition, startTopPosition + 2);
                    H.Write("L => List Sets", startLeftPosition, startTopPosition + 2);
                    H.Write("B => Back", startLeftPosition, startTopPosition + 8);
                    break;
                case LoopState.RawListManager:
                    H.Write("B => Back", startLeftPosition, startTopPosition + 8);
                    break;
                case LoopState.PriceChecker:
                    H.Write("B => Back", startLeftPosition, startTopPosition + 8);
                    break;
                case LoopState.FolderPrint:
                    break;
                case LoopState.Exit:
                    break;
                default:
                    break;
            }
        }
        // #############################################################
        // END
        // #############################################################

        // 1 [NEO] Ambitious Assault
        private static async Task<bool> ReadClipboardAndDownload()
        {
            Clipboard clipboard = new();

            // get new list id
            var guid = Guid.NewGuid();
            WriteLine("");

            var rawCardString = await clipboard.GetTextAsync().ConfigureAwait(false);
            if (rawCardString != null)
            {
                var cardList = rawCardString.Split("\r\n");

                // create directory
                DirectoryInfo directory = new(@$"{BaseDirectory}\{TemporaryDirectory}\{ListDirectory}\{guid}\");
                directory.Create();

                foreach (var card in cardList)
                {
                    // 1 [VOC] Azorius Signet
                    _ = int.TryParse(card[..1], out var cardCount);
                    var cardSet = card.Substring(card.IndexOf('[')+1, card.IndexOf(']') - card.IndexOf('[')-1);
                    var cardName = card[(card.IndexOf(']')+1)..].Trim();

                    var scryfallCard = await Api.GetCardByNameAsync(cardName, cardSet).ConfigureAwait(false);

                    for (var i = 0; i < cardCount; i++)
                    {
                        _ = await GetImage(scryfallCard, directory.FullName).ConfigureAwait(false);
                    }
                }

                // create pdf
                _ = H.CreatePdfDocument(guid, @$"{BaseDirectory}\{TemporaryDirectory}\{ListDirectory}");
            }

            // move file to output
            FileInfo file = new(@$"{BaseDirectory}\{TemporaryDirectory}\{ListDirectory}\{guid}\{guid}.pdf");
            if (file.Exists) { file.MoveTo($@"{BaseDirectory}\{OutputDirectory}\{ListDirectory}\clipboard_{guid}.pdf"); }

            WriteLine("Finished!");

            return true;
        }

        private static async Task LoadDeckList()
        {
            // TODO: check for new version of deck list

            FileInfo file = new(@$"{BaseDirectory}\{JsonDirectory}\DeckList.json");
            if(!file.Exists)
            {
                var valid = await H.DownloadAndValidateFile("https://mtgjson.com/api/v5/DeckList.json", "https://mtgjson.com/api/v5/DeckList.json.sha256", @$"{BaseDirectory}\{JsonDirectory}\").ConfigureAwait(false);
                if(!valid)
                {
                    throw new Exception("Filechecksum is invalid!");
                }
            }
            DeckList = JsonConvert.DeserializeObject<DeckList>(await File.ReadAllTextAsync(@$"{BaseDirectory}\{JsonDirectory}\DeckList.json").ConfigureAwait(false));
        }

        private static async Task<SetList> LoadSetList(bool forceDownload = false)
        {
            FileInfo file = new(@$"{BaseDirectory}\{JsonDirectory}\SetList.json");

            // remove file if force download is used
            if (file.Exists && forceDownload) { file.Delete(); }
            
            // download file if it is missing or force download is used
            if (!file.Exists || forceDownload)
            {
                var valid = await H.DownloadAndValidateFile("https://mtgjson.com/api/v5/SetList.json", "https://mtgjson.com/api/v5/SetList.json.sha256", @$"{BaseDirectory}\{JsonDirectory}\");
                if (!valid)
                {
                    throw new Exception("Filechecksum is invalid!");
                }
            }

            var setlist = await File.ReadAllTextAsync(@$"{BaseDirectory}\{JsonDirectory}\SetList.json").ConfigureAwait(false);
            return JsonConvert.DeserializeObject<SetList>(setlist);
        }

        private static async Task<OverallPriceList> LoadPriceList()
        {
            const string fileName = "AllPrices.json";

            // TODO: check for new version of set list

            FileInfo file = new(@$"{BaseDirectory}\{JsonDirectory}\{fileName}");
            if (!file.Exists)
            {
                var valid = await H.DownloadAndValidateFile($"https://mtgjson.com/api/v5/{fileName}", $"https://mtgjson.com/api/v5/{fileName}.sha256", @$"{BaseDirectory}\{JsonDirectory}\");
                if (!valid)
                {
                    throw new Exception("Filechecksum is invalid!");
                }
            }

            
            var completePriceList = await File.ReadAllTextAsync(@$"{BaseDirectory}\{JsonDirectory}\{fileName}").ConfigureAwait(false);
            var list = JsonConvert.DeserializeObject<OverallPriceList>(completePriceList);

            return list;
        }

        private static void ReadAllDecks() 
        {
            DirectoryInfo deckDirectory = new(@$"{BaseDirectory}\{JsonDirectory}\{DeckDirectory}\");
            if (!deckDirectory.Exists) return;
            var files = deckDirectory.GetFiles("*.json");
            foreach (var file in files)
            {
                var deck = H.ReadSingleDeck(file.FullName);
                Decks.Add($"{file.Name[..file.Name.IndexOf('.')]}_{deck.Data.Name}", deck.Data);
            }
            if (files.Length <= 0)
            {
                Console.WriteLine("No Deck-Files found!");
            }
        }

        private static void CheckAllDirectories()
        {
            H.CheckDirectory(@$"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\MgcPrxyDrftr\");

            H.CheckDirectory(@$"{BaseDirectory}\{CacheDirectory}\{ScryfallCacheDirectory}\");

            H.CheckDirectory(@$"{BaseDirectory}\{FileDirectory}\");

            H.CheckDirectory(@$"{BaseDirectory}\{JsonDirectory}\{DeckDirectory}\");
            H.CheckDirectory(@$"{BaseDirectory}\{JsonDirectory}\{SetDirectory}\");

            H.CheckDirectory(@$"{BaseDirectory}\{OutputDirectory}\{DeckDirectory}\");
            H.CheckDirectory(@$"{BaseDirectory}\{OutputDirectory}\{DraftDirectory}\");
            H.CheckDirectory(@$"{BaseDirectory}\{OutputDirectory}\{ListDirectory}\");

            H.CheckDirectory(@$"{BaseDirectory}\{TemporaryDirectory}\{BoosterDirectory}\");
            H.CheckDirectory(@$"{BaseDirectory}\{TemporaryDirectory}\{DeckDirectory}\");
            H.CheckDirectory(@$"{BaseDirectory}\{TemporaryDirectory}\{DraftDirectory}\");
            H.CheckDirectory(@$"{BaseDirectory}\{TemporaryDirectory}\{ListDirectory}\");
        }

        private static void ReadAllSets()
        {
            DirectoryInfo setDirectory = new(@$"{BaseDirectory}\{JsonDirectory}\{SetDirectory}\");
            if (!setDirectory.Exists) return;
            var files = setDirectory.GetFiles("*.json");
            foreach (var file in files)
            {
                Console.WriteLine($"Reading {file.Name} ...");
                _ = ReadSingleSet(file);
            }
            if(files.Length <= 0)
            {
                Console.WriteLine("No Set-Files found!");
            }
        }

        private static async Task ReadAllConfiguredSets(List<string> setsToLoad)
        {
            if(setsToLoad is not { Count: > 0 })
            {
                Console.WriteLine("No sets configured!");
            }
            else
            {
                Console.WriteLine(">> Checking for updates...");
                // check if an update is available
                foreach (var set in setsToLoad)
                {
                    Console.WriteLine($"> Checking {set}");
                    _ = await Settings.CheckLastUpdate(set, @$"{BaseDirectory}\{JsonDirectory}", SetDirectory).ConfigureAwait(false);
                }

                Console.WriteLine(">> Reading files...");
                foreach (var set in setsToLoad)
                {
                    FileInfo file = new(@$"{BaseDirectory}\{JsonDirectory}\{SetDirectory}\{set}.json");
                    // force reread when file does no longer exist
                    // TODO: I forgot why I did it that way and now I am too afraid to ask
                    if(!file.Exists) { Settings.LastUpdatesList[set] = DateTime.Now.AddDays(-2); Settings.Save(); }
                    _ = await Settings.CheckLastUpdate(set, $@"{BaseDirectory}\{JsonDirectory}", SetDirectory).ConfigureAwait(false);
                    Console.WriteLine($"> Reading {set}");
                    _ = ReadSingleSet(set);
                }
            }
        }

        private static SetRoot ReadSingleSetWithUpdateCheck(string setCode)
        {
            _ = Settings.CheckLastUpdate(setCode, @$"{BaseDirectory}\{JsonDirectory}", SetDirectory);
            return ReadSingleSet(setCode);
        }

        private static SetRoot ReadSingleSet(string setCode)
        {
            FileInfo fileInfo = new(@$"{BaseDirectory}\{JsonDirectory}\{SetDirectory}\{setCode.ToUpper()}.json");

            return ReadSingleSet(fileInfo);
        }

        private static SetRoot ReadSingleSet(FileInfo fileInfo)
        {
            var txt = File.ReadAllText(fileInfo.ToString());
            var o = JsonConvert.DeserializeObject<SetRoot>(txt);

            var json = JObject.Parse(txt);
            if(json.SelectToken("data")?.SelectToken("booster") != null && json.SelectToken("data")?.SelectToken("booster")?.SelectToken("default") != null)
            {
                foreach (var item in json.SelectToken("data")?.SelectToken("booster")?.SelectToken("default")?.SelectToken("boosters")!)
                {
                    foreach (var item1 in item.SelectToken("contents")!)
                    {
                        var name = ((JProperty)item1).Name;
                        if (!SheetList.TryGetValue(name, out _))
                        {
                            SheetList.Add(name);
                        }
                    }
                }
            }

            Sets.TryAdd(o.Data.Code, o);

            if (!ReleaseTimelineSets.ContainsValue(o.Data.Code))
            {
                ReleaseTimelineSets.Add(o.Data.ReleaseDate.ToString("yyyy-MM-dd") + o.Data.Code, o.Data.Code);
            }
                
            return o;
        }

        private static SortedDictionary<string, int> GenerateBoosterPlain(string setCode)
        {
            _ = ReadSingleSet(setCode);

            List<Guid> boosterCards = [];
            List<CardIdentifiers> boosterCardIdentifier = [];
            var set = Sets[setCode.ToUpper()];

            // create Hashtable for cards identified by scryfall id
            SortedDictionary<Guid, Card> cards = [];
            foreach (var item in set.Data.Cards) { if (!cards.ContainsKey(item.Uuid) && item.Side is null or Side.A) cards.Add(item.Uuid, item); }

            // check for available booster blueprints
            if (set.Data.Booster == null || set.Data.Booster.Default.Boosters.Count == 0) return null;

            // determine booster blueprint
            Dictionary<Contents, float> blueprint = [];
            foreach (var item in set.Data.Booster.Default.Boosters) { blueprint.Add(item.Contents, item.Weight / (float)set.Data.Booster.Default.BoostersTotalWeight); }
            var booster = blueprint.RandomElementByWeight(e => e.Value);

            // determine booster contents
            foreach (var sheet in booster.Key.GetType().GetProperties().Where(s => s.GetValue(booster.Key, null) != null))
            {
                // how many cards should be added for this sheet
                var cardCount = (long)sheet.GetValue(booster.Key, null)!;

                // name of the sheet
                var sheetName = sheet.Name;

                // temporary sheet
                Dictionary<Guid, float> temporarySheet = [];

                // get the actual sheet
                var actualSheetReflection = set.Data.Booster.Default.Sheets.GetType().GetProperties().First(s => s.GetValue(set.Data.Booster.Default.Sheets, null) != null && s.Name.ToLower().Equals(sheetName.ToLower()));
                var actualSheet = ((Sheet)actualSheetReflection.GetValue(set.Data.Booster.Default.Sheets));

                // add all cards to a temporary list with correct weight
                foreach (var item in actualSheet!.Cards)
                {
                    temporarySheet.Add(Guid.Parse(item.Key), item.Value / (float)actualSheet.TotalWeight);
                }

                // get cards from sheet and add to booster
                for (var i = 0; i < cardCount; i++)
                {
                    // reset card id
                    Guid card;

                    // prevent added duplicate cards
                    do { card = temporarySheet.RandomElementByWeight(e => e.Value).Key; } while (boosterCards.Contains(card));

                    // add card to booster
                    boosterCards.Add(card);
                }
            }

            // just for some fun
            var s = string.Empty;
            List<string> generalCards = [];
            SortedDictionary<string, int> generalCardDictionary = [];
            for (var i = 0; i < boosterCards.Count; i++)
            {
                var colorIdent = string.Empty;

                if (cards[boosterCards[i]].ColorIdentity.Count == 1)
                {
                    foreach (var item in cards[boosterCards[i]].ColorIdentity)
                    {
                        switch (item.ToString())
                        {
                            case "B":
                                colorIdent += "Black";
                                break;
                            case "U":
                                colorIdent += "Blue";
                                break;
                            case "R":
                                colorIdent += "Red";
                                break;
                            case "G":
                                colorIdent += "Green";
                                break;
                            case "W":
                                colorIdent += "White";
                                break;
                        }
                    }
                }
                //else if (cards[boosterCards[i]].ColorIdentity.Count > 1)
                //{
                //    colorIdent += "Multi";
                //}
                else
                {
                    //colorIdent += "Clrls";
                    colorIdent += ".Else";
                }

                if(cards[boosterCards[i]].Rarity == Rarity.Rare || cards[boosterCards[i]].Rarity == Rarity.Mythic)
                {
                    if (!generalCardDictionary.TryAdd("R/M", 1))
                    {
                        generalCardDictionary["R/M"]++;
                    }
                }
                else if(cards[boosterCards[i]].OtherFaceIds != null && cards[boosterCards[i]].SetCode.ToUpper().Equals("NEO") && (cards[boosterCards[i]].Rarity == Rarity.Common || cards[boosterCards[i]].Rarity == Rarity.Uncommon))
                {
                    if (!generalCardDictionary.TryAdd("C/U", 1))
                    {
                        generalCardDictionary["C/U"]++;
                    }
                }
                else
                {
                    if (!generalCardDictionary.ContainsKey($"{cards[boosterCards[i]].Rarity.ToString()[..1]} {colorIdent}"))
                    {
                        generalCardDictionary.Add($"{cards[boosterCards[i]].Rarity.ToString()[..1]} {colorIdent}", 1);
                    }
                    else
                    {
                        generalCardDictionary[$"{cards[boosterCards[i]].Rarity.ToString()[..1]} {colorIdent}"]++;
                    }
                }
            }

            s = generalCardDictionary.Aggregate(s, (current, item) => current + $"{item.Key}\t{item.Value}\n");

            //return s;
            return generalCardDictionary;
        }

        private static List<Card> GenerateBooster(string setCode, IReadOnlyCollection<string> additionalSetCodes = null, Enumerators.BoosterType boosterType = Enumerators.BoosterType.Play)
        {
            List<Guid> boosterCards = [];
            List<CardIdentifiers> boosterCardIdentifier = [];
            var set = Sets[setCode.ToUpper()];

            // create Hashtable for cards identified by scryfall id
            SortedDictionary<Guid, Card> cards = [];
            foreach (var item in set.Data.Cards.Where(item => !cards.ContainsKey(item.Uuid) && item.Side is null or Side.A)) { cards.Add(item.Uuid, item); }

            // check for available booster blueprints
            //if (set.Data.Booster == null || set.Data.Booster.Default.Boosters.Count == 0) { return null; }
            if (set.Data.Booster == null) { return null; }

            // check if there is only one booster type. If that's the case use this as "default"
            // separate determination of total weight as a set can also have only one booster type like collector
            var validProperties = set.Data.Booster.GetType().GetProperties().Count(p => p.GetValue(set.Data.Booster) != null);
            DefaultBooster dynamicBooster = null;

            // TODO: use "priority list" of booster types if given or default was not found
            // I guess "default" is not used anymore in newer sets
            // maybe "draft" -> "play" -> "default" -> exception

            // name of booster to generate
            var type = Enum.GetName(boosterType) ?? "Play";
            dynamicBooster = (DefaultBooster)set.Data.Booster.GetType().GetProperty(type)!.GetValue(set.Data.Booster, null) ?? (DefaultBooster)set.Data.Booster.GetType().GetProperty("Play")!.GetValue(set.Data.Booster, null);

            // determine booster blueprint
            var blueprint = dynamicBooster!.Boosters.ToDictionary(item => item.Contents, item => item.Weight / (float)dynamicBooster.BoostersTotalWeight);
            var booster = blueprint.RandomElementByWeight(e => e.Value);
            
            // determine booster contents
            foreach (var sheet in booster.Key.GetType().GetProperties().Where(s => s.GetValue(booster.Key, null) != null))
            {
                // how many cards should be added for this sheet
                var cardCount = (long)sheet.GetValue(booster.Key, null)!;

                // name of the sheet
                var sheetName = sheet.Name;

                // get the actual sheet
                var actualSheetReflection = dynamicBooster.Sheets.GetType().GetProperties().First(s => s.GetValue(dynamicBooster.Sheets, null) != null && s.Name.ToLower().Equals(sheetName.ToLower()));
                var actualSheet = ((Sheet)actualSheetReflection.GetValue(dynamicBooster.Sheets));

                // add all cards to a temporary list with correct weight
                if (actualSheet == null) continue;
                var temporarySheet = actualSheet.Cards.ToDictionary(item => Guid.Parse(item.Key), item => item.Value / (float)actualSheet.TotalWeight);

                // differentiate between fixed and random boosters
                if (actualSheet.Fixed)
                {
                    // for this sheet add all cards 
                    foreach (var cardKeyValue in actualSheet.Cards)
                    {
                        if (cardKeyValue.Value > 1)
                        {
                            for (var i = 0; i < cardKeyValue.Value; i++)
                            {
                                boosterCards.Add(new Guid(cardKeyValue.Key));
                            }
                        }
                        else
                        {
                            boosterCards.Add(new Guid(cardKeyValue.Key));
                        }
                    }
                }
                else
                {
                    // get cards from sheet and add to booster
                    for (var i = 0; i < cardCount; i++)
                    {
                        // reset card id
                        Guid card;

                        // add cards
                        do { card = temporarySheet.RandomElementByWeight(e => e.Value).Key; } while (boosterCards.Contains(card));

                        // add card to booster
                        boosterCards.Add(card);
                    }
                }
            }

            // if there are addtional sets given they will only be used to get their cards like BRO needs BRR
            if (additionalSetCodes == null) return boosterCards.Select(t => cards[t]).ToList();
            
            foreach (var item in additionalSetCodes.Select(addSetCode => Sets[addSetCode.ToUpper()]).SelectMany(addSet => addSet.Data.Cards.Where(item => !cards.ContainsKey(item.Uuid) && item.Side is null or Side.A)))
            {
                cards.Add(item.Uuid, item);
            }

            return boosterCards.Select(t => cards[t]).ToList();
        }

        //private static async Task<object> PrintDeck(string deckName)
        //{
        //    // individual deck guid
        //    var guid = Guid.NewGuid();
            
        //    // create folder
        //    DirectoryInfo directory = new(@$"{BaseDirectory}\{TemporaryDirectory}\{DeckDirectory}\{guid}\");
        //    directory.Create();

        //    // try to get deck from the list of already downloaded decks
        //    var deck = Decks.FirstOrDefault(x => x.Key.Contains(deckName)).Value;

        //    // deck not found
        //    if (deck == null)
        //    {
        //        // check if it is a legitimate deck
        //        var deckFromList = DeckList.Data.Find(x => x.Name.ToLowerInvariant().Contains(deckName.ToLowerInvariant()) || x.FileName.ToLowerInvariant().Contains(deckName.ToLowerInvariant()));

        //        if(deckFromList != null)
        //        {
        //            // download and validate deck and checksum files
        //            var isValid = await H.DownloadAndValidateFile($"https://mtgjson.com/api/v5/decks/{deckFromList.FileName}.json", $"https://mtgjson.com/api/v5/decks/{deckFromList.FileName}.json.sha256", @$"{BaseDirectory}\{JsonDirectory}\{DeckDirectory}\");

        //            // when file is valid
        //            if(isValid)
        //            {
        //                // read json 
        //                var localDeck = JsonConvert.DeserializeObject<DeckRoot>(await File.ReadAllTextAsync(@$"{BaseDirectory}\{JsonDirectory}\{DeckDirectory}\{deckFromList.FileName}.json").ConfigureAwait(false));
        //                deck = localDeck.Data;
                        
        //                // add it to the list of local decks
        //                Decks.Add($"{deckFromList.FileName.ToLowerInvariant()}_{deck.Name.ToLowerInvariant()}", deck);
        //            }
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }

        //    // download all cards from mainboard and sideboard
        //    foreach (var card in deck!.MainBoard) 
        //    {
        //        //// check for language card
        //        //if(!Language.Equals("en"))
        //        //{
        //        //    var d = await api.GetCardByNameAndLanguageAsync(card.Name, Language, card.SetCode);
        //        //}

        //        for (var i = 0; i < card.Count; i++)
        //        {
        //            // TODO check basix lands download setting
        //            //if ((Settings.DownloadBasicLands && card.Types.Contains(models.Type.Land) && card.Supertypes.Contains(models.Supertype.Basic)))
        //            //{
        //            //    _ = await GetImage(card.Identifiers, @$"{BaseDirectory}\{OutputDirectory}\{DeckDirectory}\{guid}\");
        //            //}
        //            _ = await GetImage(card.Identifiers, @$"{BaseDirectory}\{TemporaryDirectory}\{DeckDirectory}\{guid}\");
        //        }
        //    }
        //    foreach (var card in deck.SideBoard)
        //    {
        //        for (var i = 0; i < card.Count; i++)
        //        {
        //            _ = await GetImage(card.Identifiers, @$"{BaseDirectory}\{TemporaryDirectory}\{DeckDirectory}\{guid}\");
        //        }
        //    }

        //    // create pdf
        //    var proc = CreatePdf(guid, @$"{BaseDirectory}\{TemporaryDirectory}\{DeckDirectory}\", Settings.AutomaticPrinting);
        //    if (proc.ExitCode != 0) return true;
        //    FileInfo file = new(@$"{BaseDirectory}\{ScriptDirectory}\{DefaultScriptName}.pdf");
        //    if (file.Exists) { file.MoveTo($@"{BaseDirectory}\{OutputDirectory}\{DeckDirectory}\{deck.Name.Replace(' ', '_').ToLowerInvariant()}_{guid}.pdf"); }

        //    return true;
        //}

        ///// <summary>
        ///// prints all cards from the file
        ///// Format has to be one card per line
        ///// 1 Wasteland|MPR
        ///// {count} {Name}|{SetCode}
        ///// Large lists (>90 cards) are split up into multiple files
        ///// </summary>
        ///// <param name="listFileName">Filename that should be used</param>
        ///// <returns></returns>
        //private static async Task<object> PrintRawList(string listFileName)
        //{
        //    // get new list id
        //    var guid = Guid.NewGuid();
        //    var subGuid = Guid.NewGuid();

        //    // create directory
        //    DirectoryInfo directory = new(@$"{BaseDirectory}\{TemporaryDirectory}\{ListDirectory}\{guid}\");
        //    directory.Create();

        //    // read all lines
        //    var lines = await File.ReadAllLinesAsync(@$"{BaseDirectory}\{FileDirectory}\{listFileName}");
        //    var isLargeList = lines.Length > 90;
        //    var lineCounter = 0;

        //    directory = new DirectoryInfo(@$"{BaseDirectory}\{TemporaryDirectory}\{ListDirectory}\{guid}\{subGuid}\");
        //    directory.Create();

        //    foreach (var line in lines)
        //    {
        //        // 1 Azorius Signet|VOC
        //        _ = int.TryParse(line[..1], out var cardCount);
        //        var cardName = line[2..].Split('|')[0];
        //        var cardSet = line[2..].Split('|')[1];

        //        var card = await Api.GetCardByNameAsync(cardName, cardSet);
        //        if (card == null) continue;

        //        lineCounter++;
        //        if (lineCounter == 91)
        //        {
        //            subGuid = Guid.NewGuid();
        //            directory = new DirectoryInfo(@$"{BaseDirectory}\{TemporaryDirectory}\{ListDirectory}\{guid}\{subGuid}\");
        //            directory.Create();
        //            lineCounter = 1;
        //        }
                    
        //        _ = await GetImage(card, directory.FullName);
        //    }

        //    DirectoryInfo directoryInfo = new(@$"{BaseDirectory}\{TemporaryDirectory}\{ListDirectory}\{guid}\");
        //    var count = 1;

        //    foreach (var dir in directoryInfo.GetDirectories())
        //    {
        //        // create pdf
        //        var proc = CreatePdf(@$"{dir.FullName}", Settings.AutomaticPrinting);
        //        if (proc.ExitCode == 0)
        //        {
        //            FileInfo file = new(@$"{BaseDirectory}\{ScriptDirectory}\{DefaultScriptName}.pdf");
        //            if (file.Exists) { file.MoveTo($@"{BaseDirectory}\{OutputDirectory}\{ListDirectory}\{listFileName.Replace(' ', '_').ToLowerInvariant()}_{guid}_{count}.pdf"); }
        //        }
        //        count++;
        //    }

        //    return true;
        //}

        /// <summary>
        /// Create pdf from all cards in the given directory
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        private static bool PrintDirectory(string directoryPath)
        {
            // create pdf
            //H.CreatePdfDocumentSpire(@$"{directoryPath}", $"{Guid.NewGuid()}.pdf");
            //H.CreatePdfDocumentQuest(@$"{directoryPath}", $"{Guid.NewGuid()}.pdf");

            //FileInfo file = new(@$"{BaseDirectory}\{ScriptDirectory}\{DefaultScriptNameNoGuid}.pdf");
            //if (file.Exists) { file.MoveTo($@"{BaseDirectory}\{OutputDirectory}\{ListDirectory}\folder_{Guid.NewGuid()}.pdf"); }
            return true;
        }

        private static async Task<bool> DraftToSql(string draftString)
        {
            var setService = ServiceProvider.GetSetService();
            var setCode = draftString.Split('|')[0];
            var boosterCountParam = draftString.Split('|')[1];
            var sb = new StringBuilder();

            var set = (await setService.FindAsync(setCode)).Value;
            Settings.LastGeneratedSet = setCode;
            ReadSingleSetWithUpdateCheck(set.Code);
            Settings.AddSet(set.Code);
            Settings.Save();
            int boosterCount = int.TryParse(boosterCountParam, out boosterCount) ? boosterCount : 1;
            Console.CursorVisible = false;

            for (var i = 1; i <= boosterCount; i++)
            {
                sb.AppendLine($"insert into rs_booster (setid) values ((select id from rs_set where setcode = '{setCode}'));");

                Console.Clear();
                Console.WriteLine($"Generating booster {i}/{boosterCount}...");

                // get a booster
                var booster = GenerateBooster(set.Code);

                foreach (var card in booster) 
                {
                    sb.AppendLine($"insert into rs_boostercards (boosterid, cardid) values ((select max(id) from rs_booster), (select cardid from rs_card where mtgjsonid = '{card.Uuid}'));");
                }
            }

            // update booster count just for fun
            Settings.UpdateBoosterCount(1);

            return true;
        }

        private static bool AddSet(string setCode)
        {
            // read and download if necessary
            var setRoot = ReadSingleSetWithUpdateCheck(setCode.ToUpper());

            if (setRoot.Data.IsOnlineOnly || setRoot.Data.IsPartialPreview || setRoot.Data.Booster == null)
            {
                // ask user if adding is ok when online only or it is in preview or has no booster
                Console.Write("[");
                Console.ForegroundColor = !setRoot.Data.IsOnlineOnly ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write("IsNotOnlineOnly");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("] [");
                Console.ForegroundColor = !setRoot.Data.IsPartialPreview ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write("IsNotPartialPreview");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("] [");
                Console.ForegroundColor = setRoot.Data.Booster != null ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write("HasBooster");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("]\n");

                Console.Write("Continue anyway? (yes|no)[default:n] ");
                var key = Console.ReadKey(false);
                if (key.Key is ConsoleKey.N or ConsoleKey.Enter) { return false; }
            }

            // add new set
            Settings.AddSet(setCode.ToUpper());
            
            // check dependencies 
            if (SetDependencies.ContainsKey(setCode.ToUpper()))
            {
                foreach (var supportSet in SetDependencies[setCode.ToUpper()])
                {
                    Settings.AddSupportSet(supportSet);
                }
            }

            // save all settings
            Settings.Save();

            // analyse and add new upgrade sheet file
            AnalyseSet(setCode.ToUpper());

            return true;
        }

        private static Enumerators.BoosterType MapBoosterType(char boosterType)
        {
            return boosterType switch
            {
                'a' => Enumerators.BoosterType.Arena,
                'c' => Enumerators.BoosterType.Collector,
                'd' => Enumerators.BoosterType.Draft,
                'j' => Enumerators.BoosterType.Jumpstart,
                'p' => Enumerators.BoosterType.Play,
                's' => Enumerators.BoosterType.Set,
                't' => Enumerators.BoosterType.Tournament,
                _ => Enumerators.BoosterType.Play
            };
        }

        private static Enumerators.BoosterType MapBoosterType(string boosterName)
        {
            return boosterName switch
            {
                not null when boosterName.Contains("arena", StringComparison.InvariantCultureIgnoreCase) => Enumerators.BoosterType.Arena,
                not null when boosterName.Contains("box topper", StringComparison.InvariantCultureIgnoreCase) => Enumerators.BoosterType.BoxTopper,
                not null when boosterName.Contains("collector sample", StringComparison.InvariantCultureIgnoreCase) => Enumerators.BoosterType.CollectorSample,
                not null when boosterName.Contains("collector", StringComparison.InvariantCultureIgnoreCase) => Enumerators.BoosterType.Collector,
                not null when boosterName.Contains("draft", StringComparison.InvariantCultureIgnoreCase) => Enumerators.BoosterType.Draft,
                not null when boosterName.Contains("jumpstart", StringComparison.InvariantCultureIgnoreCase) => Enumerators.BoosterType.Jumpstart,
                not null when boosterName.Contains("play", StringComparison.InvariantCultureIgnoreCase) => Enumerators.BoosterType.Play,
                not null when boosterName.Contains("set", StringComparison.InvariantCultureIgnoreCase) => Enumerators.BoosterType.Set,
                not null when boosterName.Contains("tournament", StringComparison.InvariantCultureIgnoreCase) => Enumerators.BoosterType.Tournament,
                _ => Enumerators.BoosterType.Draft,
            };
        }

        private static async Task SimpleDraft(string setCode, int numberOfBoosters, Enumerators.BoosterType boosterType, IReadOnlyCollection<string> additionalSetCollection)
        {
            DirectoryInfo draftDirectory = new(@$"{BaseDirectory}\{OutputDirectory}\{DraftDirectory}\{DateTime.Now:yyyy-MM-ddTHH-mm-ss}");
            draftDirectory.Create();

            for (var i = 1; i <= numberOfBoosters; i++)
            {
                Console.WriteLine($"{Enum.GetName(boosterType)} Booster {i}/{numberOfBoosters}");

                // get a booster
                var booster = GenerateBooster(setCode.ToUpper(), additionalSetCollection, boosterType);

                // new booster guid 
                var boosterGuid = Guid.NewGuid();

                // create directory
                DirectoryInfo boosterDirectory = new(@$"{BaseDirectory}\{TemporaryDirectory}\{BoosterDirectory}\{boosterGuid}\");
                if (!boosterDirectory.Exists) { boosterDirectory.Create(); }

                // load images
                foreach (var card in booster) { await GetImage(card.Identifiers, boosterDirectory.FullName); }

                // generate and move pdf file
                _ = H.CreatePdfDocument(boosterGuid, @$"{BaseDirectory}\{TemporaryDirectory}\{BoosterDirectory}");
                FileInfo file = new(@$"{BaseDirectory}\{TemporaryDirectory}\{BoosterDirectory}\{boosterGuid}\{boosterGuid}.pdf");
                if (file.Exists) { file.MoveTo($@"{draftDirectory}\{setCode.ToLower()}_{boosterGuid}.pdf"); }

                Console.WriteLine($@"File {draftDirectory}\{boosterGuid}.pdf created.");
            }
        }

        /// <summary>
        /// Draft boosters from given set but from the open boosters api
        /// </summary>
        /// <param name="draftString">set + count + [boostertype] i.e. MH3|36[|p/play]</param>
        /// <param name="targetDirectory">if booster from multiple sets are created this is the target directory</param>
        /// <returns></returns>
        private static async Task<bool> DraftApi(string draftString, string targetDirectory = "")
        {
            var setService = ServiceProvider.GetSetService();
            var setCode = draftString.Split('|')[0];
            var boosterCountParam = draftString.Split('|')[1];
            var boosterType = draftString.Split('|').Length == 2
                ? Enumerators.BoosterType.Play
                : MapBoosterType(draftString.Split('|')[2].ToCharArray()[0]);
            var set = (await setService.FindAsync(setCode)).Value;
            int boosterCount = int.TryParse(boosterCountParam, out boosterCount) ? boosterCount : 1;

            if (Settings != null)
            {
                Settings.LastGeneratedSet = set?.Code ?? setCode.ToUpper();
                Settings.AddSet(set?.Code ?? setCode.ToUpper());
                Settings.Save();

                if (Settings.PromptForDraftConfirmation && !Options.Silent)
                {
                    Console.WriteLine(
                        $"Generating {boosterCount} {(boosterCount == 1 ? "booster" : "boosters")} of this set \"{set?.Name}\".");
                    Console.CursorVisible = false;
                    Console.Write("Press any key to start generating.");
                    Console.ReadKey();
                }
            }

            // create new draft folder
            var draftDirectory = targetDirectory != string.Empty ? new DirectoryInfo(targetDirectory) : new DirectoryInfo(@$"{BaseDirectory}\{OutputDirectory}\{DraftDirectory}\{DateTime.Now:yyyy-MM-ddTHH-mm-ss}");
            if (!draftDirectory.Exists) { draftDirectory.Create(); }

            // create all boosters at once and iterate them afterwards
            using var client = new Client("418fed9c-1aa0-4628-87d0-45ec41af03f5");
            var boosterResult = await client.GenerateBoosterAsync(set?.Code ?? setCode.ToUpper(), boosterType, boosterCount);

            // create all pdfs
            foreach (var booster in boosterResult.BoosterBox?.Booster!)
            {
                // new booster guid 
                var boosterGuid = Guid.NewGuid();

                // create directory
                DirectoryInfo boosterDirectory = new(@$"{BaseDirectory}\{TemporaryDirectory}\{BoosterDirectory}\{boosterGuid}\");
                if (!boosterDirectory.Exists) { boosterDirectory.Create(); }

                if (!Options.Silent)
                {
                    Console.WriteLine("\nDownloading images...");
                    Console.WriteLine("".PadRight(Console.WindowWidth, '═'));
                }

                // load images
                foreach (var card in booster.Cards) { await GetImage(card, boosterDirectory.FullName); }

                if (Options.Mode == RunModes.Pdf && Options.Single == false)
                {
                    // create pdf
                    if (!Options.Silent)
                    {
                        Console.WriteLine("\nCreating pdf file...");
                        Console.WriteLine("".PadRight(Console.WindowWidth, '═'));
                    }
                    _ = H.CreatePdfDocument(boosterGuid, @$"{BaseDirectory}\{TemporaryDirectory}\{BoosterDirectory}", Settings is not null && Settings.PrintFoils);

                    FileInfo file = new(@$"{BaseDirectory}\{TemporaryDirectory}\{BoosterDirectory}\{boosterGuid}.pdf");

                    if (file.Exists) { file.MoveTo($@"{draftDirectory}\{setCode.ToLower()}_{Enum.GetName(boosterType)?.ToLowerInvariant()}_{boosterGuid}.pdf"); }

                    if (!Options.Silent)
                    {
                        Console.WriteLine($@"File {draftDirectory}\{boosterGuid}.pdf created");
                        Console.WriteLine("".PadRight(Console.WindowWidth, '═'));
                    }
                }

                if (IsCommandLineMode) continue;

                // update booster count just for fun
                Settings?.UpdateBoosterCount(1);
                Console.Clear();
            }

            if (Options.Mode == RunModes.Pdf && Options.Single)
            {
                var directory = new DirectoryInfo(@$"{BaseDirectory}\{TemporaryDirectory}\{BoosterDirectory}");
                var cards = new List<string>();

                // gather all files for one large file
                foreach (var imageDirectory in directory.GetDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    cards.AddRange(imageDirectory.GetFiles("*.png").Select(imageFile => imageFile.FullName));
                    cards.AddRange(Directory.GetFiles(@$"{imageDirectory.FullName}\foil\", "*.png"));
                }
                
                // create pdf
                _ = H.CreatePdfDocumentQuest(cards, $@"{setCode.ToLower()}_{Enum.GetName(boosterType)?.ToLowerInvariant()}_{Guid.NewGuid()}.pdf", draftDirectory.FullName);
            }

            // resort images for images generation for photo printing
            if (Options.Mode == RunModes.Photo)
            {
                List<Image> images = [];
                var photoFileCounter = 1;
                var directory = new DirectoryInfo(@$"{BaseDirectory}\{TemporaryDirectory}\{BoosterDirectory}");

                foreach (var imageDirectory in directory.GetDirectories("*", SearchOption.AllDirectories))
                {
                    foreach (var imageFile in imageDirectory.GetFiles())
                    {
                        if(images.Count < 4)
                        {
                            try
                            {
                                images.Add(await Image.LoadAsync(imageFile.FullName));
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Error: {imageFile.FullName}");
                            }

                            if (images.Count < 4) continue;
                        }

                        // save image
                        CreateGridImage(images, photoFileCounter, draftDirectory);
                        photoFileCounter++;
                    }
                }

                if (images.Count is > 0)
                {
                    // save last image
                    CreateGridImage(images, photoFileCounter, draftDirectory);
                }
            }

            //// open draft directory
            //if (!IsWindows || Options.Silent) return true;

            //Process process = new();
            //process.StartInfo.WorkingDirectory = $@"{draftDirectory}";
            //process.StartInfo.UseShellExecute = false;
            //process.StartInfo.FileName = "explorer.exe";
            //process.StartInfo.Arguments = $@"{draftDirectory}";
            //process.Start();

            return true;
        }

        public static void CreateGridImage(List<Image> images, int imageIndex, DirectoryInfo targetOutputDirectory)
        {
            if (images == null || images.Count == 0)
            {
                throw new ArgumentException("The list of images must contain at least one image.", nameof(images));
            }

            const int margin = 12;
            const int pixelPerCentimeter = 60;

            const int photoWidthInCentimeter = 13;
            const int photoHeightInCentimeter = 18;

            const int cardWidth = (photoWidthInCentimeter * pixelPerCentimeter - margin * 2);
            const int cardHeight = (photoHeightInCentimeter * pixelPerCentimeter - margin * 2);

            // Load the four images from PNG files
            var image1 = images[0];
            var image2 = images.Count >= 2 ? images[1] : null;
            var image3 = images.Count >= 3 ? images[2] : null;
            var image4 = images.Count == 4 ? images[3] : null;

            // Create a new image with double the width and height to fit a 2x2 grid
            // 60 ppcm == 152 dpi
            using var gridImage = new Image<Rgba32>(photoWidthInCentimeter * pixelPerCentimeter * 2, photoHeightInCentimeter * pixelPerCentimeter * 2, SixLabors.ImageSharp.Color.Magenta);

            // Draw the images onto the grid image
            gridImage.Mutate(ctx =>
            {
                image1.Mutate(x => x.Resize(cardWidth, cardHeight));
                image2?.Mutate(x => x.Resize(cardWidth, cardHeight));
                image3?.Mutate(x => x.Resize(cardWidth, cardHeight));
                image4?.Mutate(x => x.Resize(cardWidth, cardHeight));

                ctx.DrawImage(image1, new Point(margin * 2, margin * 2), 1);
                if (image2 != null) ctx.DrawImage(image2, new Point(cardWidth + margin * 2, margin * 2), 1);
                if (image3 != null) ctx.DrawImage(image3, new Point(margin * 2, cardHeight + margin * 2), 1);
                if (image4 != null) ctx.DrawImage(image4, new Point(cardWidth + margin * 2, cardHeight + margin * 2), 1);

                //ctx.Resize(new Size(photoWidthInCentimeter * pixelPerCentimeter, photoHeightInCentimeter * pixelPerCentimeter));
            });

            // Save the resulting image as a PNG file
            gridImage.Save($@"{targetOutputDirectory}{imageIndex.ToString().PadLeft(8, '0')}.png");

            // TODO: check if images are disposed correctly
            images.Clear();
        }

        /// <summary>
        /// Draft boosters from given set
        /// </summary>
        /// <param name="draftString">set + count + [boostertype] i.e. NEO|6[|c]</param>
        /// <returns></returns>
        private static async Task<bool> Draft(string draftString)
        {
            var setService = ServiceProvider.GetSetService();
            var setCode = draftString.Split('|')[0];
            var boosterCountParam = draftString.Split('|')[1];
            var boosterType = draftString.Split('|').Length == 2
                ? Enumerators.BoosterType.Default
                : MapBoosterType(draftString.Split('|')[2].ToCharArray()[0]);

            var set = (await setService.FindAsync(setCode).ConfigureAwait(false)).Value;
            Settings.LastGeneratedSet = set?.Code ?? setCode.ToUpper();
            ReadSingleSetWithUpdateCheck(set?.Code ?? setCode.ToUpper());
            Settings.AddSet(set?.Code ?? setCode.ToUpper());
            Settings.Save();
            int boosterCount = int.TryParse(boosterCountParam, out boosterCount) ? boosterCount : 1;
            if (Settings.PromptForDraftConfirmation)
            {
                Console.WriteLine(
                    $"Generating {boosterCount} {(boosterCount == 1 ? "booster" : "boosters")} of this set \"{set?.Name}\".");
                Console.CursorVisible = false;
                Console.Write("Press any key to start generating.");
                Console.ReadKey();
            }

            // create new draft folder
            DirectoryInfo draftDirectory = new(@$"{BaseDirectory}\{OutputDirectory}\{DraftDirectory}\{DateTime.Now:yyyy-MM-ddTHH-mm-ss}");
            if (!draftDirectory.Exists) { draftDirectory.Create(); }

            for (var i = 1; i <= boosterCount; i++)
            {
                Console.Clear();
                Console.WriteLine($"Generating booster {i}/{boosterCount}...");

                // get a booster
                var booster = GenerateBooster(set?.Code ?? setCode.ToUpper(), Settings.SupportSetsToLoad, boosterType);

                // new booster guid 
                var boosterGuid = Guid.NewGuid();

                // create directory
                DirectoryInfo boosterDirectory = new(@$"{BaseDirectory}\{TemporaryDirectory}\{BoosterDirectory}\{boosterGuid}\");
                if (!boosterDirectory.Exists) { boosterDirectory.Create(); }

                Console.WriteLine("Downloading images...");
                Console.WriteLine("".PadRight(Console.WindowWidth, '═'));

                // load images
                foreach (var card in booster) { await GetImage(card.Identifiers, boosterDirectory.FullName); }

                _ = H.CreatePdfDocument(boosterGuid, @$"{BaseDirectory}\{TemporaryDirectory}\{BoosterDirectory}");

                FileInfo file = new(@$"{BaseDirectory}\{TemporaryDirectory}\{BoosterDirectory}\{boosterGuid}\{boosterGuid}.pdf");
                
                if (file.Exists) { file.MoveTo($@"{draftDirectory}\{setCode.ToLower()}_{boosterGuid}.pdf"); }
                
                Console.WriteLine("".PadRight(Console.WindowWidth, '═'));
                Console.WriteLine($@"File {draftDirectory}\{boosterGuid}.pdf created.");
                Console.WriteLine("".PadRight(Console.WindowWidth, '═'));

                // update booster count just for fun
                Settings.UpdateBoosterCount(1);

                try
                {
                    // cleanup
                    if (IsWindows) { boosterDirectory.Delete(true); }
                }
                catch (Exception)
                {
                    // ignore - will be cleaned up when the application is starting again
                }
                
                Console.Clear();
            }

            // open draft directory
            if (!IsWindows) return true;
            Process process = new();
            process.StartInfo.WorkingDirectory = $@"{draftDirectory}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = "explorer.exe";
            process.StartInfo.Arguments = $@"{draftDirectory}";
            process.Start();

            return true;
        }

        //private static async Task<object> Draft()
        //{
        //    var setService = ServiceProvider.GetSetService();
        //    ISet set = null;
        //    do
        //    {
        //        Console.Clear();
        //        Console.WriteLine("╔══════ RetroLottis Magic The Gathering Proxy Generator ═══════╗");
        //        Console.WriteLine("╠═════╦════════════╦═══════════════════════════════════════════╣");
        //        Console.WriteLine("╠═════╬════════════╬═══════════════════════════════════════════╣");
        //        foreach (var item in ReleaseTimelineSets) { Console.WriteLine($"║ {item.Value} ║ {DateTime.Parse(item.Key[..10]):dd.MM.yyyy} ║ {Sets[item.Value].Data.Name,-41} ║"); }
        //        Console.WriteLine("╚═════╩════════════╩═══════════════════════════════════════════╝");
        //        var noValidSetFound = true;
        //        do
        //        {
        //            Console.WriteLine("");
        //            Console.Write($"Which set shall be used? [{Settings.LastGeneratedSet}]> ");
        //            var setCode = Console.ReadLine().ToUpper();
        //            if(string.IsNullOrEmpty(setCode) && !string.IsNullOrEmpty(Settings.LastGeneratedSet))
        //            {
        //                Console.WriteLine($"Using last used set {Settings.LastGeneratedSet}.");
        //                setCode = Settings.LastGeneratedSet;
        //            }
        //            set = (await setService.FindAsync(setCode)).Value;

        //            // save last used set
        //            Settings.LastGeneratedSet = setCode;
        //            Settings.Save();
                    
        //            Console.WriteLine("");
        //            if (set == null)
        //            {
        //                Console.WriteLine($"The given input [{setCode}] is no valid set code.");
        //                Console.Write("Press any key to continue.");
        //            }
        //            else
        //            {
        //                noValidSetFound = false;
        //            }
        //        } while (noValidSetFound);

        //        Console.WriteLine($"Chosen set: {set.Name}");
        //        Console.WriteLine("Reading set file...");
        //        ReadSingleSetWithUpdateCheck(set.Code);
                
        //        Settings.AddSet(set.Code);
        //        Settings.Save();

        //        Console.WriteLine("");
        //        Console.Write("How many boosters shall be created? [1] > ");
        //        var count = Console.ReadLine();
        //        int boosterCount = int.TryParse(count, out boosterCount) ? boosterCount : 1;
        //        Console.WriteLine("");
        //        Console.WriteLine($"Generating {boosterCount} {(boosterCount == 1 ? "booster" : "boosters")} of this set \"{set.Name}\".");
        //        Console.CursorVisible = false;
        //        Console.Write("Press any key to start generating.");
        //        Console.ReadKey();

        //        Console.Clear();

        //        // create new draft folder
        //        DirectoryInfo draftDirectory = new(@$"{BaseDirectory}\{OutputDirectory}\{DraftDirectory}\{DateTime.Now:yyyy-MM-ddTHH-mm-ss}");
        //        if(!draftDirectory.Exists) { draftDirectory.Create(); }

        //        for (var i = 1; i <= boosterCount; i++)
        //        {
        //            Console.WriteLine($"Generating booster {i}/{boosterCount}...");

        //            // get a booster
        //            var booster = GenerateBooster(set.Code);

        //            // new booster guid 
        //            var boosterGuid = Guid.NewGuid();

        //            // create directory
        //            DirectoryInfo boosterDirectory = new(@$"{BaseDirectory}\{TemporaryDirectory}\{BoosterDirectory}\{boosterGuid}\");
        //            if (!boosterDirectory.Exists) { boosterDirectory.Create(); }

        //            Console.WriteLine("Downloading images...");
        //            Console.WriteLine("".PadRight(Console.WindowWidth, '═'));

        //            // load images
        //            foreach (var card in booster) { await GetImage(card.Identifiers, boosterDirectory.FullName); }

        //            if (IsWindows)
        //            {
        //                var proc = CreatePdf(boosterGuid,  @$"{BaseDirectory}\\\{OutputDirectory}\\{BoosterDirectory}", Settings.AutomaticPrinting);

        //                if (proc.ExitCode == 0)
        //                {
        //                    FileInfo file = new(@$"{BaseDirectory}\{ScriptDirectory}\{DefaultScriptName}.pdf");
        //                    if (file.Exists)
        //                    {
        //                        file.MoveTo($@"{draftDirectory}\{set.Code.ToLower()}_{boosterGuid}.pdf");
        //                    }
        //                    Console.WriteLine("".PadRight(Console.WindowWidth, '═'));
        //                    Console.WriteLine($@"File {draftDirectory}\{boosterGuid}.pdf created.");
        //                    Console.WriteLine("".PadRight(Console.WindowWidth, '═'));
        //                }
        //                else
        //                {
        //                    Console.WriteLine("".PadRight(Console.WindowWidth, '═'));
        //                    Console.WriteLine("Booster creation failed...");
        //                }
        //            }
        //            else
        //            {
        //                Console.WriteLine("Skipping PDF-file generation...");
        //            }

        //            // update booster count just for fun
        //            Settings.UpdateBoosterCount(1);

        //            // cleanup
        //            if (IsWindows) { boosterDirectory.Delete(true); }
        //            Console.Clear();
        //        }

        //        Console.WriteLine("");
        //        Console.WriteLine("All boosters created.");
        //        Console.WriteLine("Press any key to generate more boosters.");
        //        Console.Write("To exit the application press [x].");

        //    } while (Console.ReadKey().Key != ConsoleKey.X);
            
        //    return true;
        //}

        private static async Task<bool> GetImage(string absoluteDownloadUri, string imageName, string imageExtension, string cacheDirectory, string targetBoosterDirectory, string face = "front")
        {
            // get unique file name guid
            var fileName = $"{Guid.NewGuid()}.png";

            // check for image
            FileInfo file = new(@$"{cacheDirectory}\{face}\{imageName[..1]}\{imageName.Substring(1, 1)}\{imageName}.{imageExtension}");
            if (file.Exists) { _ = file.CopyTo($"{targetBoosterDirectory}{fileName}"); return true; }

            // check target directory
            DirectoryInfo directoryInfo = new(@$"{cacheDirectory}\{face}\{imageName[..1]}\{imageName.Substring(1, 1)}\");
            if(!directoryInfo.Exists) { directoryInfo.Create(); }

            try
            {
                // download if not present
#pragma warning disable CS0618 // Type or member is obsolete
                await Client.DownloadFileTaskAsync(absoluteDownloadUri, @$"{cacheDirectory}\{face}\{imageName[..1]}\{imageName.Substring(1, 1)}\{imageName}.{imageExtension}");
#pragma warning restore CS0618 // Type or member is obsolete
            }
            catch (WebException webException)
            {
                Console.WriteLine(webException.Message);
            }

            // copy to booster directory
            FileInfo newFile = new(@$"{cacheDirectory}\{face}\{imageName[..1]}\{imageName.Substring(1, 1)}\{imageName}.{imageExtension}");
            if (!newFile.Exists) return false;
            _ = newFile.CopyTo($"{targetBoosterDirectory}{fileName}"); return true;
        }

        private static async Task GetImage(OpenBoosterCard card, string targetDirectory)
        {
            // skip if side b is an adventure
            if (card.Side is not "a" && card.Layout is "adventure" or "split") return;

            // determine wether to download front or back cards
            var face = card.Side is null or "a" ? "front" : "back";

            // build png url
            var imageUrl = $@"https://cards.scryfall.io/png/{face}/{card.Scryfallid[..1]}/{card.Scryfallid[1..2]}/{card.Scryfallid}.png";
            var currentColor = Console.ForegroundColor;

            // set font color according to rarity
            Console.ForegroundColor = GetColorByRarity(card.Rarity);
            if (!Options.Silent) Console.WriteLine($"Downloading {card.Name} ...");

            if (card.Foil)
            {
                targetDirectory += @"foil\";
                H.CheckDirectory(targetDirectory);
            }

            // download image if not in cache
            await GetImage(imageUrl, card.Scryfallid, "png", @$"{BaseDirectory}\{CacheDirectory}\{ScryfallCacheDirectory}", targetDirectory, face);

            // reset original font color
            Console.ForegroundColor = currentColor;

            // get other faces
            if (card.OtherCards is null) return;
            foreach (var otherCard in card.OtherCards) await GetImage(otherCard, targetDirectory);
        }

        private static async Task<bool> GetImage(ScryfallApi.Client.Models.Card card, string targetDirectory)
        {
            var currentColor = Console.ForegroundColor;
            
            Console.ForegroundColor = GetColorByRarity(card.Rarity);
            Console.WriteLine($"Downloading {card.Name} ...");

            // check if images are present
            if (card.ImageUris != null)
            {
                _ = await GetImage(card.ImageUris["png"].AbsoluteUri, card.Id.ToString(), "png", @$"{BaseDirectory}\{CacheDirectory}\{ScryfallCacheDirectory}", targetDirectory);
            }
            else
            {
                if (card.CardFaces != null)
                {
                    foreach (var item in card.CardFaces)
                    {
                        // get image uri
                        var uri = item.ImageUris["png"].AbsoluteUri;

                        // get card face
                        var face = uri.Contains("front") ? "front" : "back";

                        // download image
                        _ = await GetImage(uri, $"{card.Id}_{face}", "png", @$"{BaseDirectory}\{CacheDirectory}\{ScryfallCacheDirectory}", targetDirectory);
                    }
                }
            }
            Console.ForegroundColor = currentColor;

            return true;
        }

        private static async Task<bool> GetImage(CardIdentifiers cardIdentifiers, string targetDirectory)
        {
            // get scryfall card
            var scryfallCard = await Api.GetCardByScryfallIdAsync(cardIdentifiers.ScryfallId);
            
            return await GetImage(scryfallCard, targetDirectory);
        }
        private static async Task<bool> GetImage(string scryfallId, string targetDirectory)
        {
            // get scryfall card
            var scryfallCard = await Api.GetCardByScryfallIdAsync(scryfallId);

            return await GetImage(scryfallCard, targetDirectory);
        }

        private static ConsoleColor GetColorByRarity(string rarity)
        {
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

        private static void CleanFolders()
        {
            DirectoryInfo temporaryDirectory = new(@$"{BaseDirectory}\{TemporaryDirectory}\");
            DeleteDirectories(temporaryDirectory, "*.*");

            DirectoryInfo jsonDirectory = new(@$"{BaseDirectory}\{JsonDirectory}\");
            DeleteDirectories(jsonDirectory, "*.sha256");
        }

        private static void DeleteDirectories(DirectoryInfo directory, string filePattern)
        {
            DeleteFilesInDirectory(directory, filePattern);

            if (!directory.Exists) return;
            foreach (var subDirectory in directory.GetDirectories()) { DeleteDirectories(subDirectory, filePattern); }

            if (directory.GetFiles().Length == 0) directory.Delete();
        }

        private static void DeleteFilesInDirectory(DirectoryInfo directory, string filePattern)
        {
            if (!directory.Exists) return;
            foreach (var file in directory.GetFiles(filePattern))
            {
                try
                {
                    file.Delete();
                }
                catch (IOException) { }
            }
        }
    }
}
