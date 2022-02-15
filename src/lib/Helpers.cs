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
                Console.WriteLine("NanDECK not found. Please enter path to nandeck.exe manually!");
                Console.Write("> ");
                fullPath = Console.ReadLine();
                ConfigurationManager.AppSettings["NanDeckFullPath"] = fullPath;
            }
        }
        
        public static bool CheckLastUpdate(string setCode, string fullJsonPath, Settings settings, string setFolder)
        {
            if (settings == null)
            {
                settings = new Settings();
                settings.Statistics = new();
                settings.LastUpdatesList = new();
            }
            else if (settings.LastUpdatesList == null)
            {
                settings.LastUpdatesList = new();
            }

            if (!settings.LastUpdatesList.ContainsKey(setCode))
            {
                settings.LastUpdatesList.Add(setCode, DateTime.Now.AddDays(-2));
            }

            if (settings.LastUpdatesList[setCode].AddDays(1) < DateTime.Now)
            {
                DownloadSetFile(setCode, fullJsonPath, setFolder);
                settings.LastUpdatesList[setCode] = DateTime.Now;
                SaveSettings(settings, @$"{fullJsonPath}\settings.json");
            }

            return true;
        }

        private static void DownloadSetFile(string setCode, string fullJsonPath, string setFolder)
        {
            WebClient webClient = new();
            string currentFileText = string.Empty;
            Set currentSet = null;

            // download content file
            webClient.DownloadFile(new Uri($"https://mtgjson.com/api/v5/{setCode}.json"), @$"{fullJsonPath}\{setCode.ToUpper()}.json");

            // download checksum file
            webClient.DownloadFile(new Uri($"https://mtgjson.com/api/v5/{setCode}.json.sha256"), @$"{fullJsonPath}\{setCode.ToUpper()}.json.sha256");

            // validate checksum
            bool isValid = ValidateFiles(@$"{fullJsonPath}\{setCode.ToUpper()}.json", @$"{fullJsonPath}\{setCode.ToUpper()}.json.sha256");

            if(isValid)
            {
                var downloadedFileText = File.ReadAllText(@$"{fullJsonPath}\{setCode.ToUpper()}.json");
                if (File.Exists(@$"{fullJsonPath}\{setFolder}\{setCode.ToUpper()}.json"))
                {
                    currentFileText = File.ReadAllText(@$"{fullJsonPath}\{setFolder}\{setCode.ToUpper()}.json");
                }
                
                var downloadSet = JsonConvert.DeserializeObject<Set>(downloadedFileText);
                currentSet = JsonConvert.DeserializeObject<Set>(currentFileText);

                if(currentSet == null)
                {
                    // move
                    File.Move(@$"{fullJsonPath}\{setCode.ToUpper()}.json", @$"{fullJsonPath}\{setFolder}\{setCode.ToUpper()}.json");
                }
                else if (downloadSet.Meta.Date > currentSet.Meta.Date && !downloadSet.Meta.Version.Equals(currentSet.Meta.Version))
                {
                    // replace
                    File.Replace(@$"{fullJsonPath}\{setCode.ToUpper()}.json", @$"{fullJsonPath}\{setFolder}\{setCode.ToUpper()}.json", @$"{fullJsonPath}\{setFolder}\{setCode.ToUpper()}.json.bak");
                }

                File.Delete(@$"{fullJsonPath}\{setFolder}\{setCode.ToUpper()}.json.bak");
                File.Delete(@$"{fullJsonPath}\{setCode.ToUpper()}.json.sha256");

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

        public static Settings LoadSettings(string settingsFileFullPath)
        {
            Settings setUpdates = null;
            if (File.Exists(settingsFileFullPath))
            {
                string json = File.ReadAllText(settingsFileFullPath);
                setUpdates = System.Text.Json.JsonSerializer.Deserialize<Settings>(json);
            }

            return setUpdates;
        }

        public static void SaveSettings(Settings settings, string settingsFileFullPath)
        {
            string json = System.Text.Json.JsonSerializer.Serialize(settings);
            File.WriteAllText(settingsFileFullPath, json);
        }

        public static void UpdateStatistics(Settings settings, string stat, string value)
        {
            if (settings.Statistics.ContainsKey(stat))
            {
                settings.Statistics[stat] = value;
            }
            else
            {
                settings.Statistics.Add(stat, value);
            }
        }
        public static void UpdateBoosterCount(Settings settings, string settingsFileFullPath, int value)
        {
            if(settings == null)
            {
                settings = new();
                settings.Statistics = new();
                settings.LastUpdatesList = new();
            }
            else if(settings.Statistics == null)
            {
                settings.Statistics = new();
            }

            if(settings.Statistics.TryGetValue("booster", out string currentValue))
            {
                int newValue = int.Parse(currentValue) + value;
                UpdateStatistics(settings, "booster", newValue.ToString());
                SaveSettings(settings, settingsFileFullPath);
            }
            else
            {
                UpdateStatistics(settings, "booster", value.ToString());
                SaveSettings(settings, settingsFileFullPath);
            }
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
