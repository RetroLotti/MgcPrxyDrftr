using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MgcPrxyDrftrLib
{
    public class DraftLibrary
    {
        public string BaseDirectory { get; set; } = ConfigurationManager.AppSettings["BaseDirectory"] ?? Environment.CurrentDirectory;
        public string JsonDirectory { get; set; } = ConfigurationManager.AppSettings["JsonDirectory"] ?? "json";
        public string SetDirectory { get; set; } = ConfigurationManager.AppSettings["SetDirectory"] ?? "sets";
        public string DeckDirectory { get; set; } = ConfigurationManager.AppSettings["DeckDirectory"] ?? "decks";
        public string BoosterDirectory { get; set; } = ConfigurationManager.AppSettings["BoosterDirectory"] ?? "booster";
        public string CacheDirectory { get; set; } = ConfigurationManager.AppSettings["CacheDirectory"] ?? "cache";
        public string ScryfallCacheDirectory { get; set; } = ConfigurationManager.AppSettings["ScryfallCacheDirectory"] ?? "scryfall";
        public string ScriptDirectory { get; set; } = ConfigurationManager.AppSettings["ScriptDirectory"] ?? "scripts";
        public string DraftDirectory { get; set; } = ConfigurationManager.AppSettings["DraftDirectory"] ?? "draft";
        public string OutputDirectory { get; set; } = ConfigurationManager.AppSettings["OutputDirectory"] ?? "output";
        public string FileDirectory { get; set; } = ConfigurationManager.AppSettings["FileDirectory"] ?? "files";
        public string TemporaryDirectory { get; set; } = ConfigurationManager.AppSettings["TemporaryDirectory"] ?? "temporary";
        public string ListDirectory { get; set; } = ConfigurationManager.AppSettings["ListDirectory"] ?? "lists";
        public string DefaultScriptName { get; set; } = ConfigurationManager.AppSettings["DefaultScriptName"];
        public string DefaultScriptNameNoGuid { get; set; } = ConfigurationManager.AppSettings["DefaultScriptNameNoGuid"];
        public string NanDeckPath { get; set; } = ConfigurationManager.AppSettings["NanDeckPath"];
        //public bool UseSetList { get; set; } = bool.Parse(ConfigurationManager.AppSettings[""]);

        public string FullJsonPath { get; set; }
        public string FullTemporaryPath { get; set; }
        public string FullScryfallCachePath { get; set; }
        public string FullDraftOutputPath { get; set; }
        public string FullSetPath { get; set; }

        public DraftLibrary()
        {
            FullJsonPath = @$"{BaseDirectory}\{JsonDirectory}\";
            FullTemporaryPath = @$"{BaseDirectory}\{TemporaryDirectory}\";
            FullScryfallCachePath = @$"{BaseDirectory}\{CacheDirectory}\{ScryfallCacheDirectory}\";
            FullSetPath = @$"{BaseDirectory}\{JsonDirectory}\{SetDirectory}\";

            FullDraftOutputPath = @$"{BaseDirectory}\{OutputDirectory}\{DraftDirectory}\";
        }

        public void CleanFolders()
        {
            DirectoryInfo temporaryDirectory = new(FullTemporaryPath);
            DeleteDirectories(temporaryDirectory, "*.*");

            DirectoryInfo jsonDirectory = new(FullJsonPath);
            DeleteDirectories(jsonDirectory, "*.sha256");
        }

        private void DeleteDirectories(DirectoryInfo directory, string filePattern)
        {
            DeleteFilesInDirectory(directory, filePattern);

            if (!directory.Exists) return;
            foreach (var subDirectory in directory.GetDirectories()) { DeleteDirectories(subDirectory, filePattern); }
        }

        private void DeleteFilesInDirectory(DirectoryInfo directory, string filePattern)
        {
            if (!directory.Exists) return;
            foreach (var file in directory.GetFiles(filePattern)) { file.Delete(); }
        }

        public void CheckAllDirectories()
        {
            CheckDirectory(@$"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\MgcPrxyDrftr\");

            CheckDirectory(FullScryfallCachePath);

            CheckDirectory(@$"{BaseDirectory}\{FileDirectory}\");

            CheckDirectory(@$"{BaseDirectory}\{JsonDirectory}\{DeckDirectory}\");
            CheckDirectory(@$"{BaseDirectory}\{JsonDirectory}\{SetDirectory}\");

            CheckDirectory(@$"{BaseDirectory}\{OutputDirectory}\{DeckDirectory}\");
            CheckDirectory(@$"{BaseDirectory}\{OutputDirectory}\{DraftDirectory}\");
            CheckDirectory(@$"{BaseDirectory}\{OutputDirectory}\{ListDirectory}\");

            CheckDirectory(@$"{BaseDirectory}\{TemporaryDirectory}\{BoosterDirectory}\");
            CheckDirectory(@$"{BaseDirectory}\{TemporaryDirectory}\{DeckDirectory}\");
            CheckDirectory(@$"{BaseDirectory}\{TemporaryDirectory}\{DraftDirectory}\");
            CheckDirectory(@$"{BaseDirectory}\{TemporaryDirectory}\{ListDirectory}\");
        }

        private static void CheckDirectory(string path)
        {
            DirectoryInfo dir = new(path);
            if (!dir.Exists) { dir.Create(); }
        }

        // removes all json files and clears the output
        // also resets the settings file
        public void ResetAndCleanEverything(Settings _settings)
        {
            _settings = new();
            _settings.Save();

            DirectoryInfo outputDirectoryInfo = new(@$"{BaseDirectory}\{OutputDirectory}\");
            outputDirectoryInfo.Delete(true);

            DirectoryInfo cacheDirectoryInfo = new(@$"{BaseDirectory}\{CacheDirectory}\");
            cacheDirectoryInfo.Delete(true);

            DirectoryInfo tempDirectoryInfo = new(@$"{BaseDirectory}\{TemporaryDirectory}\");
            tempDirectoryInfo.Delete(true);

            DirectoryInfo jsonDirectoryInfo = new(@$"{BaseDirectory}\{JsonDirectory}\");
            jsonDirectoryInfo.Delete(true);
        }
    }
}
