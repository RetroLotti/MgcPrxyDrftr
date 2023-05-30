﻿using System;
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
using MgcPrxyDrftr.lib;
using MgcPrxyDrftr.models;
using MtgApiManager.Lib.Model;
using MtgApiManager.Lib.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TextCopy;
using H = MgcPrxyDrftr.lib.Helpers;

namespace MgcPrxyDrftr
{
    internal class Program
    {
        private static StateMachine StateMachine { get; set; }

        private static string BaseDirectory { get; set; } = ConfigurationManager.AppSettings["BaseDirectory"] ?? Environment.CurrentDirectory;    
        private static string JsonDirectory { get; set; } = ConfigurationManager.AppSettings["JsonDirectory"] ?? "json";
        private static string SetDirectory { get; set; } = ConfigurationManager.AppSettings["SetDirectory"] ?? "sets";
        private static string DeckDirectory { get; set; } = ConfigurationManager.AppSettings["DeckDirectory"] ?? "decks";
        private static string BoosterDirectory { get; set; } = ConfigurationManager.AppSettings["BoosterDirectory"] ?? "booster";
        private static string CacheDirectory { get; set; } = ConfigurationManager.AppSettings["CacheDirectory"] ?? "cache";
        private static string ScryfallCacheDirectory { get; set; } = ConfigurationManager.AppSettings["ScryfallCacheDirectory"] ?? "scryfall";
        private static string ScriptDirectory { get; set; } = ConfigurationManager.AppSettings["ScriptDirectory"] ?? "scripts";
        private static string DraftDirectory { get; set; } = ConfigurationManager.AppSettings["DraftDirectory"] ?? "draft";
        private static string OutputDirectory { get; set; } = ConfigurationManager.AppSettings["OutputDirectory"] ?? "output";
        private static string FileDirectory { get; set; } = ConfigurationManager.AppSettings["FileDirectory"] ?? "files";
        private static string TemporaryDirectory { get; set; } = ConfigurationManager.AppSettings["TemporaryDirectory"] ?? "temporary";
        private static string ListDirectory { get; set; } = ConfigurationManager.AppSettings["ListDirectory"] ?? "lists";
        private static string DefaultScriptName { get; set; } = ConfigurationManager.AppSettings["DefaultScriptName"];
        private static string DefaultScriptNameNoGuid { get; set; } = ConfigurationManager.AppSettings["DefaultScriptNameNoGuid"];
        private static string NanDeckPath { get; set; } = ConfigurationManager.AppSettings["NanDeckPath"];
        private static bool UseSetList { get; set; } = bool.Parse(ConfigurationManager.AppSettings["UseSetList"] ?? "true");
        private static bool IsWindows { get; set; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        

        private static readonly SortedList<string, string> ReleaseTimelineSets = new();
        private static readonly SortedList<string, SetRoot> Sets = new();
        private static SortedList<string, Deck> Decks { get; set; } = new();
        private static readonly HashSet<string> SheetList = new();
        private static HashSet<string> AddedSheets { get; set; } = new ();

        private static readonly IMtgServiceProvider ServiceProvider = new MtgServiceProvider();
        [Obsolete("Obsolete")] private static readonly WebClient Client = new();
        private static readonly ApiCaller Api = new();
        private static Settings Settings { get; set; }

        private static string Language { get; set; } = "de";

        // decks
        private static DeckList DeckList { get; set; }
        // sets
        private static SetList SetList { get; set; }
        // cards
        private static SortedDictionary<Guid, Card> OmniCardList { get; set; } = new(); 

        private static async Task Main()
        {
#pragma warning disable CA1416 // Validate platform compatibility
            if (IsWindows) { Console.SetWindowSize(136, 40); }
#pragma warning restore CA1416 // Validate platform compatibility

            WriteHeader();

            Console.WriteLine(">> Pre-Clean-Up...");
            CleanFolders();

            Console.WriteLine(">> Checking directories...");
            CheckAllDirectories();

            Console.WriteLine(">> Reading settings...");
            Settings = new Settings();
            Settings.Load();

            Console.WriteLine(">> Reading setlist...");
            LoadSetList();
            Console.WriteLine(">> Reading sets from disk...");
            if (UseSetList) { await ReadAllConfiguredSets(Settings.SetsToLoad); } else { ReadAllSets(); }
            Console.WriteLine(">> Reading support sets from disk...");
            await ReadAllConfiguredSets(Settings.SupportSetsToLoad);

            Console.WriteLine(">> Reading decklist...");
            LoadDeckList();
            Console.WriteLine(">> Reading decks from disk...");
            ReadAllDecks();

            if (IsWindows)
            {
                Console.WriteLine(">> Looking for nanDeck...");
                H.CheckNanDeck(NanDeckPath);
            }
            else
            {
                Console.WriteLine(">> nanDeck disabled");
            }

            Console.WriteLine(">> Starting...");
            Thread.Sleep(666);
            Console.Clear();

#if DEBUG
            //OmniCardList = ReadAllCards();

            ////GenerateCubeDraftBooster();
            ////GenerateCubeDraftMini();

            //ReadFilteredSets();

            //ResetAndCleanEverything();

            // main loop
            _ = await EnterTheLoop();

            // Magic: Online Arena
            //_ = await DraftToSql("ARN|60");
            //_ = await DraftToSql("LEB|36");

            // convert all given sets to sql inserts for mtgoa
            //string foo = SetToSql();
#else
            // start application loop
            _ = await EnterTheLoop();
#endif
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
                var set = ReadSingleSet(item.Name[..item.Name.IndexOf(".", StringComparison.Ordinal)]);
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

        private static void AnalyseSet(string setCode)
        {
            var file = File.ReadAllText(@$"{BaseDirectory}\{JsonDirectory}\{SetDirectory}\{setCode.ToUpper()}.json");
            var json = JObject.Parse(file);
            var foo = json.SelectToken("data")?.SelectToken("booster");
            var upgradeStringContents = new StringBuilder();
            var upgradeStringSheets = new StringBuilder();
            var sampleContent = new Contents();
            var newSheets = false;

            var isPreview = json.SelectToken("data")?.SelectToken("isPartialPreview")?.Value<bool>();
            if (isPreview is null or true)
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

                        if (contentSheet != null || AddedSheets.Contains(sheetNameTitleCase)) continue;
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
            using var writer = new StreamWriter(@$"{BaseDirectory}\models\upgrades\Upgrade{setCode.ToUpper()}.cs");
            writer.WriteLine(upgradeFileString);
        }

        private static string SetToSql()
        {
            var sb = new StringBuilder();
            var boosterBlueprintCounter = 0;
            var sheetCounter = 0;
            var setCounter = 0;

            foreach (var set in Sets.Where(x => x.Value.Data.Code == "LEA" || x.Value.Data.Code == "ARN" || x.Value.Data.Code == "DRK" || x.Value.Data.Code == "LEG" || x.Value.Data.Code == "ATQ" || x.Value.Data.Code == "BRO" || x.Value.Data.Code == "NEO" || x.Value.Data.Code == "3RD"))
            //foreach (var set in sets)
            {
                setCounter++;
                sb.AppendLine($"insert into rs_set (id, setcode, `name`, releasedate) values ({setCounter}, '{set.Value.Data.Code.ToUpper()}', '{set.Value.Data.Name.Replace("\'", "\\\'")}', '{set.Value.Data.ReleaseDate.ToString("yyyy-MM-dd")}');");
                sb.AppendLine($"insert into rs_magicproduct (productname, purchaseprice, setid) values ('{set.Value.Data.Name.Replace("\'", "\\\'")} Booster', 230, {setCounter});");

                // add all cards of this set
                foreach (var item in set.Value.Data.Cards)
                {
                    //item.Colors
                    sb.AppendLine($"insert into rs_card (`name`, setid, scryfallid, mtgjsonid, scryfallimageuri, rarityid, colors, types, subtypes, supertypes) values ('{item.Name.Replace("\'", "\\\'")}', {setCounter}, '{item.Identifiers.ScryfallId}', '{item.Uuid.ToString()}', '{$"https://c1.scryfall.com/file/scryfall-cards/png/front/{item.Identifiers.ScryfallId.ToString()[0]}/{item.Identifiers.ScryfallId.ToString()[1]}/{item.Identifiers.ScryfallId}.png"}', (select id from rs_rarity where rarityname = '{item.Rarity}'), '{string.Join("", item.Colors.Select(s => s.ToString()).ToArray())}', '{string.Join(",", item.Types.Select(s => s.ToString()).ToArray())}', '{string.Join(",", item.Subtypes.Select(s => s.ToString()).ToArray())}', '{string.Join(",", item.Supertypes.Select(s => s.ToString()).ToArray())}');");
                }

                // if set is BRO then add BRR cards
                if(set.Value.Data.Code == "BRO")
                {
                    foreach (var card in Sets["BRR"].Data.Cards)
                    {
                        sb.AppendLine($"insert into rs_card (`name`, setid, scryfallid, mtgjsonid, scryfallimageuri, rarityid, colors, types, subtypes, supertypes) values ('{card.Name.Replace("\'", "\\\'")}', {setCounter}, '{card.Identifiers.ScryfallId}', '{card.Uuid.ToString()}', '{$"https://c1.scryfall.com/file/scryfall-cards/png/front/{card.Identifiers.ScryfallId.ToString()[0]}/{card.Identifiers.ScryfallId.ToString()[1]}/{card.Identifiers.ScryfallId}.png"}', (select id from rs_rarity where rarityname = '{card.Rarity}'), '{string.Join("", card.Colors.Select(s => s.ToString()).ToArray())}', '{string.Join(",", card.Types.Select(s => s.ToString()).ToArray())}', '{string.Join(",", card.Subtypes.Select(s => s.ToString()).ToArray())}', '{string.Join(",", card.Supertypes.Select(s => s.ToString()).ToArray())}');");
                    }
                }

                sb.AppendLine("commit;");
                sb.AppendLine($"update rs_set set boostertotalweight = {set.Value.Data.Booster.Default.BoostersTotalWeight} where setcode = '{set.Value.Data.Code}';");
                sb.AppendLine("commit;");

                foreach (var sheet in set.Value.Data.Booster.Default.Sheets.GetType().GetProperties().Where(s => s.GetValue(set.Value.Data.Booster.Default.Sheets, null) != null))
                {
                    long cardCount = (((Sheet)sheet.GetValue(set.Value.Data.Booster.Default.Sheets, null))!).Cards.Count;
                    var sheetName = sheet.Name;
                    var actualSheetReflection = set.Value.Data.Booster.Default.Sheets.GetType().GetProperties().First(s => s.GetValue(set.Value.Data.Booster.Default.Sheets, null) != null && s.Name.ToLower().Equals(sheetName.ToLower()));
                    var actualSheet = ((Sheet)actualSheetReflection.GetValue(set.Value.Data.Booster.Default.Sheets));

                    sheetCounter++;
                    sb.AppendLine($"insert into rs_sheet (id, setid, sheetname, totalweight) values ({sheetCounter}, {setCounter}, '{sheetName}', {actualSheet.TotalWeight});");

                    foreach (var card in actualSheet.Cards)
                    {
                        sb.AppendLine($"insert into rs_sheetcards (sheetid, cardid, cardweight) values ({sheetCounter}, (select id from rs_card where mtgjsonid = '{card.Key}'), {card.Value});");
                    }

                    sb.AppendLine("commit;");
                }

                foreach (var booster in set.Value.Data.Booster.Default.Boosters)
                {
                    boosterBlueprintCounter++;
                    sb.AppendLine($"insert into rs_boosterblueprint (id, setid, boosterweight) values ({boosterBlueprintCounter}, {setCounter}, {booster.Weight});");

                    foreach (var sheet in booster.Contents.GetType().GetProperties().Where(s => s.GetValue(booster.Contents, null) != null))
                    {
                        var cardCount = (long)sheet.GetValue(booster.Contents, null)!;
                        var sheetName = sheet.Name;
                        //var actualSheetReflection = set.Value.Data.Booster.Default.Sheets.GetType().GetProperties().First(s => s.GetValue(set.Value.Data.Booster.Default.Sheets, null) != null && s.Name.ToLower().Equals(sheetName.ToLower()));
                        //var actualSheet = ((models.Sheet)actualSheetReflection.GetValue(set.Value.Data.Booster.Default.Sheets));
                        
                        //sheetCounter++;
                        sb.AppendLine($"insert into rs_boosterblueprintsheets (boosterblueprintid, sheetid, cardcount) values ({boosterBlueprintCounter}, (select id from rs_sheet where sheetname = '{sheetName}' and setid = {setCounter}), {cardCount});");

                        sb.AppendLine("commit;");
                    }
                }
            }

            return sb.ToString();
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
                    writer.WriteLine($"{(dict.ContainsKey("C Red") ? dict["C Red"] : "0")}|{(dict.ContainsKey("C Green") ? dict["C Green"] : "0")}|{(dict.ContainsKey("C Black") ? dict["C Black"] : "0")}|{(dict.ContainsKey("C White") ? dict["C White"] : "0")}|{(dict.ContainsKey("C Blue") ? dict["C Blue"] : "0")}|{(dict.ContainsKey("C .Else") ? dict["C .Else"] : "0")}|{(dict.ContainsKey("U Red") ? dict["U Red"] : "0")}|{(dict.ContainsKey("U Green") ? dict["U Green"] : "0")}|{(dict.ContainsKey("U Black") ? dict["U Black"] : "0")}|{(dict.ContainsKey("U White") ? dict["U White"] : "0")}|{(dict.ContainsKey("U Blue") ? dict["U Blue"] : "0")}|{(dict.ContainsKey("U .Else") ? dict["U .Else"] : "0")}|{(dict.ContainsKey("R/M") ? dict["R/M"] : "0")}|{(dict.ContainsKey("C/U") ? dict["C/U"] : "0")}");

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
                build.AppendLine($"C ║{(dict.ContainsKey("C Red") ? dict["C Red"] : "-")}║  ║{(dict.ContainsKey("C Green") ? dict["C Green"] : "-")}║  ║{(dict.ContainsKey("C Black") ? dict["C Black"] : "-")}║  ║{(dict.ContainsKey("C White") ? dict["C White"] : "-")}║  ║{(dict.ContainsKey("C Blue") ? dict["C Blue"] : "-")}║  ║{(dict.ContainsKey("C .Else") ? dict["C .Else"] : "-")}║");
                build.AppendLine("  ╚═╝  ╚═╝  ╚═╝  ╚═╝  ╚═╝  ╚═╝");
                build.AppendLine("  ╔═╗  ╔═╗  ╔═╗  ╔═╗  ╔═╗  ╔═╗");
                build.AppendLine($"U ║{(dict.ContainsKey("U Red") ? dict["U Red"] : "-")}║  ║{(dict.ContainsKey("U Green") ? dict["U Green"] : "-")}║  ║{(dict.ContainsKey("U Black") ? dict["U Black"] : "-")}║  ║{(dict.ContainsKey("U White") ? dict["U White"] : "-")}║  ║{(dict.ContainsKey("U Blue") ? dict["U Blue"] : "-")}║  ║{(dict.ContainsKey("U .Else") ? dict["U .Else"] : "-")}║");
                build.AppendLine("  ╚═╝  ╚═╝  ╚═╝  ╚═╝  ╚═╝  ╚═╝");
                build.AppendLine("R ╔═╗                         ");
                build.AppendLine($"/ ║{(dict.ContainsKey("R/M") ? dict["R/M"] : "-")}║                         ");
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
                Write(dict.ContainsKey("C Red") ? dict["C Red"].ToString() : "-", ConsoleColor.Red);
                Write("║  ", ConsoleColor.Black, ConsoleColor.Red);

                Write("║", ConsoleColor.Black, ConsoleColor.DarkGreen);
                Write(dict.ContainsKey("C Green") ? dict["C Green"].ToString() : "-", ConsoleColor.DarkGreen);
                Write("║  ", ConsoleColor.Black, ConsoleColor.DarkGreen);

                Write("║", ConsoleColor.Black, ConsoleColor.DarkMagenta);
                Write(dict.ContainsKey("C Black") ? dict["C Black"].ToString() : "-", ConsoleColor.DarkMagenta);
                Write("║  ", ConsoleColor.Black, ConsoleColor.DarkMagenta);

                Write("║");
                Write(dict.ContainsKey("C White") ? dict["C White"].ToString() : "-", ConsoleColor.White, ConsoleColor.Black);
                Write("║  ");

                Write("║", ConsoleColor.Black, ConsoleColor.Blue);
                Write(dict.ContainsKey("C Blue") ? dict["C Blue"].ToString() : "-", ConsoleColor.Blue);
                Write("║  ", ConsoleColor.Black, ConsoleColor.Blue);

                Write("║", ConsoleColor.Black, ConsoleColor.Yellow);
                Write(dict.ContainsKey("C .Else") ? dict["C .Else"].ToString() : "-", ConsoleColor.Black, ConsoleColor.Yellow);
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
                Write(dict.ContainsKey("U Red") ? dict["U Red"].ToString() : "-", ConsoleColor.Red);
                Write("║  ", ConsoleColor.Black, ConsoleColor.Red);

                Write("║", ConsoleColor.Black, ConsoleColor.DarkGreen);
                Write(dict.ContainsKey("U Green") ? dict["U Green"].ToString() : "-", ConsoleColor.DarkGreen);
                Write("║  ", ConsoleColor.Black, ConsoleColor.DarkGreen);

                Write("║", ConsoleColor.Black, ConsoleColor.DarkMagenta);
                Write(dict.ContainsKey("U Black") ? dict["U Black"].ToString() : "-", ConsoleColor.DarkMagenta);
                Write("║  ", ConsoleColor.Black, ConsoleColor.DarkMagenta);

                Write("║");
                Write(dict.ContainsKey("U White") ? dict["U White"].ToString() : "-", ConsoleColor.White, ConsoleColor.Black);
                Write("║  ");

                Write("║", ConsoleColor.Black, ConsoleColor.Blue);
                Write(dict.ContainsKey("U Blue") ? dict["U Blue"].ToString() : "-", ConsoleColor.Blue);
                Write("║  ", ConsoleColor.Black, ConsoleColor.Blue);

                Write("║", ConsoleColor.Black, ConsoleColor.Yellow);
                Write(dict.ContainsKey("U .Else") ? dict["U .Else"].ToString() : "-", ConsoleColor.Black, ConsoleColor.Yellow);
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
                WriteLine($"/ ║{ (dict.ContainsKey("R/M") ? dict["R/M"] : "-")}║                         ", ConsoleColor.Black, ConsoleColor.Yellow);
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
                Console.SetCursorPosition(0, 21);
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
                                default:
                                    throw new ArgumentOutOfRangeException();
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
                            _ = await PrintDeck(command);
                            break;
                        case LoopState.BoosterDraft:
                            _ = await Draft(command);
                            break;
                        case LoopState.RawListManager:
                            _ = await PrintRawList(command);
                            break;
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
                    H.Write("E => Create Deck", startLeftPosition, startTopPosition + 2);
                    H.Write("S => Add or Remove Sets", startLeftPosition, startTopPosition + 3);
                    H.Write("R => Print Raw List", startLeftPosition, startTopPosition + 4);
                    H.Write("C => Clipboard", startLeftPosition, startTopPosition + 5);
                    H.Write("F => Print Folder", startLeftPosition, startTopPosition + 6);
                    H.Write("P => Price Checker", startLeftPosition, startTopPosition + 7);
                    H.Write("O => Options", startLeftPosition, startTopPosition + 8);
                    H.Write("X => Exit", startLeftPosition, startTopPosition + 10);
                    break;
                case LoopState.Options:
                    H.Write($"P => enable / disable automatic printing [{(Settings.AutomaticPrinting ? "enabled" : "disabled")}]", startLeftPosition, startTopPosition + 1);
                    H.Write($"E => enable / disable basic land download [{(Settings.DownloadBasicLands ? "enabled" : "disabled")}]", startLeftPosition, startTopPosition + 2);
                    H.Write($"D => enable / disable prompting for confirmation where drafting booster [{(Settings.PromptForDraftConfirmation ? "enabled" : "disabled")}]", startLeftPosition, startTopPosition + 3);
                    H.Write($"A => enable / disable alternative (new) draft menu [{(Settings.NewDraftMenu ? "enabled" : "disabled")}]", startLeftPosition, startTopPosition + 4);
                    H.Write("B => Back", startLeftPosition, startTopPosition + 8);
                    break;
                case LoopState.BoosterDraft:
                    H.Write("A => List all sets", startLeftPosition, startTopPosition + 1);
                    H.Write("L => List downloaded sets with booster", startLeftPosition, startTopPosition + 2);
                    //H.Write("G => Create general draft booster", startLeftPosition, startTopPosition + 3);
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
            //Guid subGuid = Guid.NewGuid();
            
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
                var proc = CreatePdf(directory.FullName, Settings.AutomaticPrinting);
                if (proc.ExitCode != 0) return true;
            }

            FileInfo file = new(@$"{BaseDirectory}\{ScriptDirectory}\{DefaultScriptName}.pdf");
            if (file.Exists) { file.MoveTo($@"{BaseDirectory}\{OutputDirectory}\{ListDirectory}\clipboard_{guid}.pdf"); }

            return true;
        }

        private static async void LoadDeckList()
        {
            // TODO: check for new version of deck list

            FileInfo file = new(@$"{BaseDirectory}\{JsonDirectory}\DeckList.json");
            if(!file.Exists)
            {
                var valid = await H.DownloadAndValidateFile("https://mtgjson.com/api/v5/DeckList.json", "https://mtgjson.com/api/v5/DeckList.json.sha256", @$"{BaseDirectory}\{JsonDirectory}\");
                if(!valid)
                {
                    throw new Exception("Filechecksum is invalid!");
                }
            }
            DeckList = JsonConvert.DeserializeObject<DeckList>(await File.ReadAllTextAsync(@$"{BaseDirectory}\{JsonDirectory}\DeckList.json"));
        }

        private static async void LoadSetList()
        {
            // TODO: check for new version of set list

            FileInfo file = new(@$"{BaseDirectory}\{JsonDirectory}\SetList.json");
            if (!file.Exists)
            {
                var valid = await H.DownloadAndValidateFile("https://mtgjson.com/api/v5/SetList.json", "https://mtgjson.com/api/v5/SetList.json.sha256", @$"{BaseDirectory}\{JsonDirectory}\");
                if (!valid)
                {
                    throw new Exception("Filechecksum is invalid!");
                }
            }
            SetList = JsonConvert.DeserializeObject<SetList>(await File.ReadAllTextAsync(@$"{BaseDirectory}\{JsonDirectory}\SetList.json"));
        }

        private static async Task<PriceList> LoadPriceList()
        {
            // TODO: check for new version of set list

            FileInfo file = new(@$"{BaseDirectory}\{JsonDirectory}\Anpm audit llPrices.json");
            if (!file.Exists)
            {
                var valid = await H.DownloadAndValidateFile("https://mtgjson.com/api/v5/AllPrices.json", "https://mtgjson.com/api/v5/AllPrices.json.sha256", @$"{BaseDirectory}\{JsonDirectory}\");
                if (!valid)
                {
                    throw new Exception("Filechecksum is invalid!");
                }
            }

            //var bar = JsonConvert.DeserializeObject<PriceList>(File.ReadAllText(@$"{BaseDirectory}\{JsonDirectory}\AllPrices.json"));
            var bar = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(@$"{BaseDirectory}\{JsonDirectory}\AllPrices.json"));

            //return JsonConvert.DeserializeObject<PriceList>(await File.ReadAllTextAsync(@$"{BaseDirectory}\{JsonDirectory}\PriceList.json"));
            return null;
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

            //if (!fileInfo.Exists)
            //{
            //    Settings.CheckSetFile(setCode, @$"{BaseDirectory}\{JsonDirectory}\", SetDirectory);


            //    //var isValid = H.DownloadAndValidateFile($"https://mtgjson.com/api/v5/decks/{deckFromList.FileName}.json", $"https://mtgjson.com/api/v5/decks/{deckFromList.FileName}.json.sha256", @$"{BaseDirectory}\{JsonDirectory}\{DeckDirectory}\");
            //}

            return ReadSingleSet(fileInfo);
        }

        private static SetRoot ReadSingleSet(FileInfo fileInfo)
        {
            var txt = File.ReadAllText(fileInfo.FullName);
            var o = JsonConvert.DeserializeObject<SetRoot>(txt);

            var json = JObject.Parse(txt);
            if(json.SelectToken("data").SelectToken("booster") != null && json.SelectToken("data").SelectToken("booster").SelectToken("default") != null)
            {
                foreach (var item in json.SelectToken("data").SelectToken("booster")?.SelectToken("default").SelectToken("boosters")!)
                {
                    foreach (var item1 in item.SelectToken("contents"))
                    {
                        var name = ((JProperty)item1).Name;
                        if (!SheetList.TryGetValue(name, out _))
                        {
                            SheetList.Add(name);
                        }
                    }
                }
            }

            if (!Sets.ContainsKey(o.Data.Code))
            {
                Sets.Add(o.Data.Code, o);
            }

            if (!ReleaseTimelineSets.ContainsValue(o.Data.Code))
            {
                ReleaseTimelineSets.Add(o.Data.ReleaseDate.ToString("yyyy-MM-dd") + o.Data.Code, o.Data.Code);
            }
                
            return o;
        }

        //static string GenerateBoosterPlain(string setCode)
        private static SortedDictionary<string, int> GenerateBoosterPlain(string setCode)
        {
            _ = ReadSingleSet(setCode);

            List<Guid> boosterCards = new();
            List<CardIdentifiers> boosterCardIdentifier = new();
            var set = Sets[setCode.ToUpper()];

            // create Hashtable for cards identified by scryfall id
            SortedDictionary<Guid, Card> cards = new();
            foreach (var item in set.Data.Cards) { if (!cards.ContainsKey(item.Uuid) && (item.Side == null || item.Side == Side.A)) cards.Add(item.Uuid, item); }

            // check for available booster blueprints
            if (set.Data.Booster == null || set.Data.Booster.Default.Boosters.Count == 0)
            {
                return null;
            }

            // determine booster blueprint
            Dictionary<Contents, float> blueprint = new();
            foreach (var item in set.Data.Booster.Default.Boosters) { blueprint.Add(item.Contents, item.Weight / (float)set.Data.Booster.Default.BoostersTotalWeight); }
            var booster = blueprint.RandomElementByWeight(e => e.Value);

            // determine booster contents
            foreach (var sheet in booster.Key.GetType().GetProperties().Where(s => s.GetValue(booster.Key, null) != null))
            {
                // how many cards should be added for this sheet
                var cardCount = (long)sheet.GetValue(booster.Key, null);

                // name of the sheet
                var sheetName = sheet.Name;

                // temporary sheet
                Dictionary<Guid, float> temporarySheet = new();

                // get the actual sheet
                var actualSheetReflection = set.Data.Booster.Default.Sheets.GetType().GetProperties().First(s => s.GetValue(set.Data.Booster.Default.Sheets, null) != null && s.Name.ToLower().Equals(sheetName.ToLower()));
                var actualSheet = ((Sheet)actualSheetReflection.GetValue(set.Data.Booster.Default.Sheets));

                // add all cards to a temporary list with correct weight
                foreach (var item in actualSheet.Cards)
                {
                    temporarySheet.Add(Guid.Parse(item.Key), item.Value / (float)actualSheet.TotalWeight);
                }

                // get cards from sheet and add to booster
                for (var i = 0; i < cardCount; i++)
                {
                    // reset card id
                    var card = Guid.Empty;

                    // prevent added duplicate cards
                    do { card = temporarySheet.RandomElementByWeight(e => e.Value).Key; } while (boosterCards.Contains(card));

                    // add card to booster
                    boosterCards.Add(card);
                }
            }

            // just for some fun
            var s = string.Empty;
            List<string> generalCards = new();
            SortedDictionary<string, int> generalCardDictionary = new();
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
                    if (generalCardDictionary.ContainsKey("R/M"))
                    {
                        generalCardDictionary["R/M"]++;
                    }
                    else
                    {
                        generalCardDictionary.Add("R/M", 1);
                    }
                }
                else if(cards[boosterCards[i]].OtherFaceIds != null && cards[boosterCards[i]].SetCode.ToUpper().Equals("NEO") && (cards[boosterCards[i]].Rarity == Rarity.Common || cards[boosterCards[i]].Rarity == Rarity.Uncommon))
                {
                    if (generalCardDictionary.ContainsKey("C/U"))
                    {
                        generalCardDictionary["C/U"]++;
                    }
                    else
                    {
                        generalCardDictionary.Add("C/U", 1);
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

            foreach (var item in generalCardDictionary)
            {
                s += $"{item.Key}\t{item.Value}\n";
            }

            //return s;
            return generalCardDictionary;
        }

        private static List<Card> GenerateBooster(string setCode, IReadOnlyCollection<string> additionalSetCodes = null, BoosterType boosterType = BoosterType.Default)
        {
            List<Guid> boosterCards = new();
            List<CardIdentifiers> boosterCardIdentifier = new();
            var set = Sets[setCode.ToUpper()];

            // create Hashtable for cards identified by scryfall id
            SortedDictionary<Guid, Card> cards = new();
            foreach (var item in set.Data.Cards.Where(item => !cards.ContainsKey(item.Uuid) && item.Side is null or Side.A)) { cards.Add(item.Uuid, item); }

            // check for available booster blueprints
            //if (set.Data.Booster == null || set.Data.Booster.Default.Boosters.Count == 0) { return null; }
            if (set.Data.Booster == null) { return null; }

            // name of booster to generate
            var type = Enum.GetName(boosterType) ?? "Default";
            var dynamicBooster = (DefaultBooster)set.Data.Booster.GetType().GetProperty(type)!.GetValue(set.Data.Booster, null);

            // determine booster blueprint
            var blueprint = dynamicBooster!.Boosters.ToDictionary(item => item.Contents, item => item.Weight / (float)set.Data.Booster.Default.BoostersTotalWeight);
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

                // get cards from sheet and add to booster
                for (var i = 0; i < cardCount; i++)
                {
                    // reset card id
                    var card = Guid.Empty;

                    // prevent added duplicate cards
                    // exclude jumpstart as they may? contain duplicates???
                    // TODO: check this assumption
                    if (boosterType is BoosterType.Jumpstart or BoosterType.Tournament)
                    {
                        card = temporarySheet.RandomElementByWeight(e => e.Value).Key;
                    }
                    else
                    {
                        do { card = temporarySheet.RandomElementByWeight(e => e.Value).Key; } while (boosterCards.Contains(card));
                    }
                    
                    // add card to booster
                    boosterCards.Add(card);
                }
            }

            // if there are addtional sets given they will only be used to get their cards like BRO needs BRR
            if (additionalSetCodes != null)
            {
                foreach (var item in additionalSetCodes.Select(addSetCode => Sets[addSetCode.ToUpper()]).SelectMany(addSet => addSet.Data.Cards.Where(item => !cards.ContainsKey(item.Uuid) && item.Side is null or Side.A)))
                {
                    cards.Add(item.Uuid, item);
                }
            }

            return boosterCards.Select(t => cards[t]).ToList();
        }

        private static async Task<object> PrintDeck(string deckName)
        {
            // individual deck guid
            var guid = Guid.NewGuid();
            
            // create folder
            DirectoryInfo directory = new(@$"{BaseDirectory}\{TemporaryDirectory}\{DeckDirectory}\{guid}\");
            directory.Create();

            // try to get deck from the list of already downloaded decks
            var deck = Decks.FirstOrDefault(x => x.Key.Contains(deckName)).Value;

            // deck not found
            if (deck == null)
            {
                // check if it is a legitimate deck
                var deckFromList = DeckList.Data.Find(x => x.Name.ToLowerInvariant().Contains(deckName.ToLowerInvariant()) || x.FileName.ToLowerInvariant().Contains(deckName.ToLowerInvariant()));

                if(deckFromList != null)
                {
                    // download and validate deck and checksum files
                    var isValid = await H.DownloadAndValidateFile($"https://mtgjson.com/api/v5/decks/{deckFromList.FileName}.json", $"https://mtgjson.com/api/v5/decks/{deckFromList.FileName}.json.sha256", @$"{BaseDirectory}\{JsonDirectory}\{DeckDirectory}\");

                    // when file is valid
                    if(isValid)
                    {
                        // read json 
                        var localDeck = JsonConvert.DeserializeObject<DeckRoot>(await File.ReadAllTextAsync(@$"{BaseDirectory}\{JsonDirectory}\{DeckDirectory}\{deckFromList.FileName}.json").ConfigureAwait(false));
                        deck = localDeck.Data;
                        
                        // add it to the list of local decks
                        Decks.Add($"{deckFromList.FileName.ToLowerInvariant()}_{deck.Name.ToLowerInvariant()}", deck);
                    }
                }
                else
                {
                    return false;
                }
            }

            // download all cards from mainboard and sideboard
            foreach (var card in deck!.MainBoard) 
            {
                //// check for language card
                //if(!Language.Equals("en"))
                //{
                //    var d = await api.GetCardByNameAndLanguageAsync(card.Name, Language, card.SetCode);
                //}

                for (var i = 0; i < card.Count; i++)
                {
                    // TODO check basix lands download setting
                    //if ((Settings.DownloadBasicLands && card.Types.Contains(models.Type.Land) && card.Supertypes.Contains(models.Supertype.Basic)))
                    //{
                    //    _ = await GetImage(card.Identifiers, @$"{BaseDirectory}\{OutputDirectory}\{DeckDirectory}\{guid}\");
                    //}
                    _ = await GetImage(card.Identifiers, @$"{BaseDirectory}\{TemporaryDirectory}\{DeckDirectory}\{guid}\");
                }
            }
            foreach (var card in deck.SideBoard)
            {
                for (var i = 0; i < card.Count; i++)
                {
                    _ = await GetImage(card.Identifiers, @$"{BaseDirectory}\{TemporaryDirectory}\{DeckDirectory}\{guid}\");
                }
            }

            // create pdf
            var proc = CreatePdf(guid, @$"{BaseDirectory}\{TemporaryDirectory}\{DeckDirectory}\", Settings.AutomaticPrinting);
            if (proc.ExitCode != 0) return true;
            FileInfo file = new(@$"{BaseDirectory}\{ScriptDirectory}\{DefaultScriptName}.pdf");
            if (file.Exists) { file.MoveTo($@"{BaseDirectory}\{OutputDirectory}\{DeckDirectory}\{deck.Name.Replace(' ', '_').ToLowerInvariant()}_{guid}.pdf"); }

            return true;
        }

        /// <summary>
        /// prints all cards from the file
        /// Format has to be one card per line
        /// 1 Wasteland|MPR
        /// {count} {Name}|{SetCode}
        /// Large lists (>90 cards) are split up into multiple files
        /// </summary>
        /// <param name="listFileName">Filename that should be used</param>
        /// <returns></returns>
        private static async Task<object> PrintRawList(string listFileName)
        {
            // get new list id
            var guid = Guid.NewGuid();
            var subGuid = Guid.NewGuid();

            // create directory
            DirectoryInfo directory = new(@$"{BaseDirectory}\{TemporaryDirectory}\{ListDirectory}\{guid}\");
            directory.Create();

            // read all lines
            var lines = File.ReadAllLines(@$"{BaseDirectory}\{FileDirectory}\{listFileName}");
            var isLargeList = lines.Length > 90;
            var lineCounter = 0;

            directory = new(@$"{BaseDirectory}\{TemporaryDirectory}\{ListDirectory}\{guid}\{subGuid}\");
            directory.Create();

            foreach (var line in lines)
            {
                // 1 Azorius Signet|VOC
                _ = int.TryParse(line[..1], out var cardCount);
                var cardName = line[2..].Split('|')[0];
                var cardSet = line[2..].Split('|')[1];

                var card = await Api.GetCardByNameAsync(cardName, cardSet);
                if (card != null)
                {
                    lineCounter++;
                    if (lineCounter == 91)
                    {
                        subGuid = Guid.NewGuid();
                        directory = new(@$"{BaseDirectory}\{TemporaryDirectory}\{ListDirectory}\{guid}\{subGuid}\");
                        directory.Create();
                        lineCounter = 1;
                    }
                    
                    _ = await GetImage(card, directory.FullName);
                }
            }

            DirectoryInfo directoryInfo = new(@$"{BaseDirectory}\{TemporaryDirectory}\{ListDirectory}\{guid}\");
            var count = 1;

            foreach (var dir in directoryInfo.GetDirectories())
            {
                // create pdf
                var proc = CreatePdf(@$"{dir.FullName}", Settings.AutomaticPrinting);
                if (proc.ExitCode == 0)
                {
                    FileInfo file = new(@$"{BaseDirectory}\{ScriptDirectory}\{DefaultScriptName}.pdf");
                    if (file.Exists) { file.MoveTo($@"{BaseDirectory}\{OutputDirectory}\{ListDirectory}\{listFileName.Replace(' ', '_').ToLowerInvariant()}_{guid}_{count}.pdf"); }
                }
                count++;
            }

            return true;
        }

        /// <summary>
        /// Create pdf from all cards in the given directory
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        private static bool PrintDirectory(string directoryPath)
        {
            // create pdf
            var proc = CreatePdf(@$"{directoryPath}", Settings.AutomaticPrinting);
            if (proc.ExitCode == 0)
            {
                FileInfo file = new(@$"{BaseDirectory}\{ScriptDirectory}\{DefaultScriptNameNoGuid}.pdf");
                if (file.Exists) { file.MoveTo($@"{BaseDirectory}\{OutputDirectory}\{ListDirectory}\folder_{Guid.NewGuid()}.pdf"); }
            }
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
            Settings.Save();

            // analyse and add new upgrade sheet file
            AnalyseSet(setCode.ToUpper());

            return true;
        }

        private static BoosterType MapBoosterType(string boosterType)
        {
            return boosterType switch
            {
                "c" => BoosterType.Collector,
                "d" => BoosterType.Default,
                "j" => BoosterType.Jumpstart,
                "s" => BoosterType.Set,
                "a" => BoosterType.Arena,
                "t" => BoosterType.Tournament,
                _ => BoosterType.Default
            };
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
                ? BoosterType.Default
                : MapBoosterType(draftString.Split('|')[2]);

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

                var foo = H.CreatePdfDocument(boosterGuid, @$"{BaseDirectory}\{TemporaryDirectory}\{BoosterDirectory}");

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
                    // ignore will be cleaned up when the application is starting again
                }
                
                Console.Clear();
            }

            return true;
        }

        private static async Task<object> Draft()
        {
            var setService = ServiceProvider.GetSetService();
            ISet set = null;
            do
            {
                Console.Clear();
                Console.WriteLine("╔══════ RetroLottis Magic The Gathering Proxy Generator ═══════╗");
                Console.WriteLine("╠═════╦════════════╦═══════════════════════════════════════════╣");
                Console.WriteLine("╠═════╬════════════╬═══════════════════════════════════════════╣");
                foreach (var item in ReleaseTimelineSets) { Console.WriteLine($"║ {item.Value} ║ {DateTime.Parse(item.Key[..10]):dd.MM.yyyy} ║ {Sets[item.Value].Data.Name,-41} ║"); }
                Console.WriteLine("╚═════╩════════════╩═══════════════════════════════════════════╝");
                var noValidSetFound = true;
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
                    Settings.Save();
                    
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
                Console.WriteLine("Reading set file...");
                ReadSingleSetWithUpdateCheck(set.Code);
                
                Settings.AddSet(set.Code);
                Settings.Save();

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
                DirectoryInfo draftDirectory = new(@$"{BaseDirectory}\{OutputDirectory}\{DraftDirectory}\{DateTime.Now:yyyy-MM-ddTHH-mm-ss}");
                if(!draftDirectory.Exists) { draftDirectory.Create(); }

                for (var i = 1; i <= boosterCount; i++)
                {
                    Console.WriteLine($"Generating booster {i}/{boosterCount}...");

                    // get a booster
                    var booster = GenerateBooster(set.Code);

                    // new booster guid 
                    var boosterGuid = Guid.NewGuid();

                    // create directory
                    DirectoryInfo boosterDirectory = new(@$"{BaseDirectory}\{TemporaryDirectory}\{BoosterDirectory}\{boosterGuid}\");
                    if (!boosterDirectory.Exists) { boosterDirectory.Create(); }

                    Console.WriteLine("Downloading images...");
                    Console.WriteLine("".PadRight(Console.WindowWidth, '═'));

                    // load images
                    foreach (var card in booster) { await GetImage(card.Identifiers, boosterDirectory.FullName); }

                    if (IsWindows)
                    {
                        var proc = CreatePdf(boosterGuid,  @$"{BaseDirectory}\\\{OutputDirectory}\\{BoosterDirectory}", Settings.AutomaticPrinting);

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

                    // update booster count just for fun
                    Settings.UpdateBoosterCount(1);

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

        private static Process CreatePdf(string folder, bool print)
        {
            Console.WriteLine("Creating PDF-file via nanDECK...");
            
            // prepare pdf with nandeck
            Process proc = new();
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.Arguments = $"/c {NanDeckPath} \"{BaseDirectory}\\{ScriptDirectory}\\{DefaultScriptNameNoGuid}.nde\" /[cardfolder]={folder} /createpdf";
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;

            // pdf gets printed right away when desired
            if (print) { proc.StartInfo.Arguments += " /print"; }

            proc.EnableRaisingEvents = true;
            proc.Start();
            proc.WaitForExit();

            return proc;
        }

        /// <summary>
        /// Create PDF using nanDeck
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="folder"></param>
        /// <param name="print"></param>
        /// <returns></returns>
        private static Process CreatePdf(Guid guid, string folder, bool print)
        {
            Console.WriteLine("Creating PDF-file via nanDECK...");

            // prepare pdf with nandeck
            Process proc = new();
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.Arguments = $"/c {NanDeckPath} \"{BaseDirectory}\\{ScriptDirectory}\\{DefaultScriptName}.nde\" /[guid]={guid} /[boosterfolder]={folder} /createpdf";

            // pdf gets printed right away when desired
            if (print) { proc.StartInfo.Arguments += " /print"; }

            proc.EnableRaisingEvents = true;
            proc.Start();
            proc.WaitForExit();

            return proc;
        }

        private static async Task<bool> GetImage(string absoluteDownloadUri, string imageName, string imageExtension, string cacheDirectory, string targetBoosterDirectory)
        {
            // get unique file name guid
            var fileName = $"{Guid.NewGuid()}.png";

            // check for image
            FileInfo file = new(@$"{cacheDirectory}\{imageName[..1]}\{imageName.Substring(1, 1)}\{imageName}.{imageExtension}");
            if (file.Exists) { _ = file.CopyTo($"{targetBoosterDirectory}{fileName}"); return true; }

            // check target directory
            DirectoryInfo directoryInfo = new(@$"{cacheDirectory}\{imageName[..1]}\{imageName.Substring(1, 1)}\");
            if(!directoryInfo.Exists) { directoryInfo.Create(); }

            // download if not present
            await Client.DownloadFileTaskAsync(absoluteDownloadUri, @$"{cacheDirectory}\{imageName[..1]}\{imageName.Substring(1, 1)}\{imageName}.{imageExtension}");

            // copy to booster directory
            FileInfo newFile = new(@$"{cacheDirectory}\{imageName[..1]}\{imageName.Substring(1, 1)}\{imageName}.{imageExtension}");
            if (newFile.Exists) { _ = newFile.CopyTo($"{targetBoosterDirectory}{fileName}"); return true; }

            return false;
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

            if (directory.Exists) 
            { 
                foreach (var subDirectory in directory.GetDirectories()) { DeleteDirectories(subDirectory, filePattern); }
            }
        }

        private static void DeleteFilesInDirectory(DirectoryInfo directory, string filePattern)
        {
            if(directory.Exists)
            {
                foreach (var file in directory.GetFiles(filePattern)) { file.Delete(); }
            }
        }
    }
}
