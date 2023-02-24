using Newtonsoft.Json;
using MgcPrxyDrftr.models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace MgcPrxyDrftr.lib
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

        public static DeckRoot ReadSingleDeck(string file)
        {
            return JsonConvert.DeserializeObject<models.DeckRoot>(File.ReadAllText(file));
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

        /// <summary>
        /// Generic method to download the given file and validate sha256 hash
        /// </summary>
        /// <param name="downloadFileUri"></param>
        /// <param name="validationFileUri"></param>
        /// <param name="targetDirectory"></param>
        /// <returns></returns>
        public static bool DownloadAndValidateFile(string downloadFileUri, string validationFileUri, string targetDirectory)
        {
            WebClient webClient = new();
            Uri downloadUri = new(downloadFileUri);
            Uri checksumUri = new(validationFileUri);

            webClient.DownloadFile(downloadUri, @$"{targetDirectory}\{downloadUri.Segments[^1]}");
            webClient.DownloadFile(checksumUri, @$"{targetDirectory}\{checksumUri.Segments[^1]}");

            return ValidateFiles(@$"{targetDirectory}\{downloadUri.Segments[^1]}", @$"{targetDirectory}\{checksumUri.Segments[^1]}");
        }

        public static async void DownloadSetFile(string setCode, string fullJsonPath, string setFolder)
        {
            WebClient webClient = new();
            string currentFileText = string.Empty;
            SetRoot currentSet;

            // download content file
            webClient.DownloadFile(new Uri($"https://mtgjson.com/api/v5/{setCode}.json"), @$"{fullJsonPath}\{setCode.ToUpper()}.json");

            // download checksum file
            webClient.DownloadFile(new Uri($"https://mtgjson.com/api/v5/{setCode}.json.sha256"), @$"{fullJsonPath}\{setCode.ToUpper()}.json.sha256");

            // validate checksum
            bool isValid = ValidateFiles(@$"{fullJsonPath}\{setCode.ToUpper()}.json", @$"{fullJsonPath}\{setCode.ToUpper()}.json.sha256");

            if(isValid)
            {
                var downloadedFileText = await File.ReadAllTextAsync(@$"{fullJsonPath}\{setCode.ToUpper()}.json");
                if (File.Exists(@$"{fullJsonPath}\{setFolder}\{setCode.ToUpper()}.json"))
                {
                    currentFileText = await File.ReadAllTextAsync(@$"{fullJsonPath}\{setFolder}\{setCode.ToUpper()}.json");
                }
                
                var downloadSet = JsonConvert.DeserializeObject<SetRoot>(downloadedFileText);
                currentSet = JsonConvert.DeserializeObject<SetRoot>(currentFileText);

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

                File.Delete(@$"{fullJsonPath}\{setCode.ToUpper()}.json");
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
            using FileStream fileStream = fileInfo.Open(FileMode.Open); // 
            byte[] hashValue = sHA256.ComputeHash(fileStream);

            for (int i = 0; i < hashValue.Length; i++)
            {
                checksum += $"{hashValue[i]:X2}";
            }

            return checksum.ToLower();
        }



    }
}
