using Newtonsoft.Json;
using ProxyDraftor.models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ProxyDraftor.lib
{
    public static class Helpers
    {
        public static bool Write(string text, int posX, int posY)
        {
            (int left, int top) = Console.GetCursorPosition();
            Console.SetCursorPosition(posX, posY);
            Console.Write(text);
            Console.SetCursorPosition(left, top);

            return true;
        }

        public static void CheckDirectory(string path)
        {
            DirectoryInfo dir = new(path);
            if (!dir.Exists) { dir.Create(); }
        }

        public static models.Deck ReadSingleDeck(string file)
        {
            return JsonConvert.DeserializeObject<models.Deck>(File.ReadAllText(file));
        }

        public static void CheckNanDeck(string fullPath)
        {
            FileInfo nandeck = new(fullPath);
            if (!nandeck.Exists)
            {
                Console.WriteLine("NanDECK wurde nicht gefunden. Bitte geben die den Ort manuell an!");
                Console.Write("> ");
                fullPath = Console.ReadLine();
                ConfigurationManager.AppSettings["NanDeckFullPath"] = fullPath;
            }
        }
        
        public static bool CheckLastUpdate(string setCode, string path)
        {
            LastUpdates updates = LoadLastUpdates(path);
            if (updates == null)
            {
                updates = new LastUpdates();
                updates.LastUpdatesList = new Dictionary<string, DateTime>();
            }

            if (!updates.LastUpdatesList.ContainsKey(setCode))
            {
                updates.LastUpdatesList.Add(setCode, DateTime.Now.AddDays(-2));
            }

            if (updates.LastUpdatesList[setCode].AddDays(1) < DateTime.Now)
            {
                DownloadSetFile(setCode);
                updates.LastUpdatesList[setCode] = DateTime.Now;
                SaveLastUpdates(path, updates);
            }

            return true;
        }

        private static void DownloadSetFile(string setCode)
        {
            WebClient webClient = new();

            // download content file
            webClient.DownloadFile(new Uri($"https://mtgjson.com/api/v5/{setCode}.json"), @$"C:\Users\Affenbande\Source\Repos\RetroLotti\MagicTheGatheringProxyDrafter\src\json\{setCode.ToUpper()}.json");

            // download checksum file
            webClient.DownloadFile(new Uri($"https://mtgjson.com/api/v5/{setCode}.json.sha256"), @$"C:\Users\Affenbande\Source\Repos\RetroLotti\MagicTheGatheringProxyDrafter\src\json\{setCode.ToUpper()}.json.sha256");

            // validate checksum
            bool isValid = ValidateFiles(@$"C:\Users\Affenbande\Source\Repos\RetroLotti\MagicTheGatheringProxyDrafter\src\json\{setCode.ToUpper()}.json", @$"C:\Users\Affenbande\Source\Repos\RetroLotti\MagicTheGatheringProxyDrafter\src\json\{setCode.ToUpper()}.json.sha256");

            if(isValid)
            {
                var downloadedFileText = File.ReadAllText(@$"C:\Users\Affenbande\Source\Repos\RetroLotti\MagicTheGatheringProxyDrafter\src\json\{setCode.ToUpper()}.json");
                var currentFileText = File.ReadAllText(@$"C:\Users\Affenbande\Source\Repos\RetroLotti\MagicTheGatheringProxyDrafter\src\json\sets\{setCode.ToUpper()}.json");
                var downloadSet = JsonConvert.DeserializeObject<Set>(downloadedFileText);
                var currentSet = JsonConvert.DeserializeObject<Set>(currentFileText);

                if(downloadSet.Meta.Date > currentSet.Meta.Date && !downloadSet.Meta.Version.Equals(currentSet.Meta.Version))
                {
                    // replace
                    File.Replace(@$"C:\Users\Affenbande\Source\Repos\RetroLotti\MagicTheGatheringProxyDrafter\src\json\{setCode.ToUpper()}.json", @$"C:\Users\Affenbande\Source\Repos\RetroLotti\MagicTheGatheringProxyDrafter\src\json\sets\{setCode.ToUpper()}.json", @$"C:\Users\Affenbande\Source\Repos\RetroLotti\MagicTheGatheringProxyDrafter\src\json\sets\{setCode.ToUpper()}.json.bak");
                    File.Delete(@$"C:\Users\Affenbande\Source\Repos\RetroLotti\MagicTheGatheringProxyDrafter\src\json\sets\{setCode.ToUpper()}.json.bak");
                    File.Delete(@$"C:\Users\Affenbande\Source\Repos\RetroLotti\MagicTheGatheringProxyDrafter\src\json\{setCode.ToUpper()}.json.sha256");
                }
            }
        }

        private static bool ValidateFiles(string contentFile, string checksumFile)
        {
            var downloadedFileChecksum = CalculateChecksum(contentFile);
            var serverChecksum = ReadChecksum(checksumFile);

            return (downloadedFileChecksum.Equals(serverChecksum));
        }

        private static string ReadChecksum(string file)
        {
            return File.ReadAllText(file);
        }

        private static string CalculateChecksum(string file)
        {
            string checksum = string.Empty;
            FileInfo fileInfo = new(file);
            
            using SHA256 sHA256 = SHA256.Create();
            using FileStream fileStream = fileInfo.Open(FileMode.Open);
            byte[] hashValue = sHA256.ComputeHash(fileStream);

            for (int i = 0; i < hashValue.Length; i++)
            {
                checksum += $"{hashValue[i]:X2}";
            }

            return checksum.ToLower();
        }

        private static LastUpdates LoadLastUpdates(string lastUpdateFilePath)
        {
            LastUpdates setUpdates = null;
            if (File.Exists(lastUpdateFilePath))
            {
                string json = File.ReadAllText(lastUpdateFilePath);
                setUpdates = System.Text.Json.JsonSerializer.Deserialize<LastUpdates>(json);
            }

            return setUpdates;
        }

        public static void SaveLastUpdates(string lastUpdateFilePath, LastUpdates lastUpdates)
        {
            string json = System.Text.Json.JsonSerializer.Serialize(lastUpdates);
            File.WriteAllText(lastUpdateFilePath, json);
        }

        //static bool Test()
        //{
        //    //DirectoryInfo setDirectory = new(@$"{BaseDirectory}\{JsonDirectory}\{JsonSetDirectory}\");
        //    //if (setDirectory.Exists)
        //    //{
        //    //    var files = setDirectory.GetFiles("*.json");
        //    //    foreach (var file in files)
        //    //    {
        //    //        var txt = File.ReadAllText(file.FullName);
        //    //        dynamic set = JObject.Parse(txt);

        //    //        foreach (var item in set.data.booster)
        //    //        {
        //    //            foreach (var boosters in item.Value)
        //    //            {
        //    //                foreach (var content in boosters.Value)
        //    //                {
        //    //                    //((JProperty)content.contents.First).Value
        //    //                    foreach (var sheet in ((JObject)content.contents))
        //    //                    {
        //    //                        var s = Type.GetType("ProxyDraftor.models.Sheets").GetProperties().Where(x => x.Name.ToLower().Equals(sheet.Key.ToLower())).FirstOrDefault();
        //    //                        if (s == null)
        //    //                        {
        //    //                            Console.WriteLine($"Sheet {sheet} nicht gefunden!");
        //    //                        }
        //    //                    }
        //    //                }
        //    //            }
        //    //        }

        //    //    }
        //    //}
        //    return true;
        //}
    }
}
