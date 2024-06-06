using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MgcPrxyDrftr.lib;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace MgcPrxyDrftr.models
{
    public class Settings
    {
        public bool RunsForFirstTime { get; set; }
        public bool AutomaticPrinting { get; set; }
        public bool DownloadBasicLands { get; set; }
        public bool PromptForDraftConfirmation { get; set; }
        public bool NewDraftMenu { get; set; }
        public string LastGeneratedSet { get; set; }
        public DateTime LastPriceDownload { get; set; }
        public Dictionary<string, DateTime> LastUpdatesList { get; set; }
        private Dictionary<string, string> Statistics { get; set; }
        public List<string> SetsToLoad { get; set; }
        public List<string> SupportSetsToLoad { get; set; }

        //private bool HasChanges { get; set; }

        public Settings()
        {
            LastUpdatesList = new Dictionary<string, DateTime>();
            Statistics = new Dictionary<string, string>();
            SetsToLoad = new List<string>();
            SupportSetsToLoad = new List<string>();
            LastPriceDownload = DateTime.Today.AddDays(-1);
        }

        private void AddSetGeneric(string setCode, ICollection<string> list)
        {
            if (list != null && !list.Contains(setCode.ToUpper()))
            {
                list.Add(setCode.ToUpper());
            }
        }

        public void AddSupportSet(string setCode)
        {
            AddSetGeneric(setCode, SupportSetsToLoad);
        }

        public void AddSet(string setCode)
        {
            AddSetGeneric(setCode, SetsToLoad);
        }
        public void RemoveSet(string setCode)
        {
            if(SetsToLoad != null && SetsToLoad.Contains(setCode))
            {
                SetsToLoad.Remove(setCode);
            }
        }
        public void Save()
        {
            var json = JsonSerializer.Serialize(this);
            File.WriteAllText(@$"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\MgcPrxyDrftr\settings.json", json);
        }
        public void Load()
        {
            if (!File.Exists(
                    $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\MgcPrxyDrftr\settings.json"))
                return;
            var json = File.ReadAllText($@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\MgcPrxyDrftr\settings.json");
            JsonConvert.PopulateObject(json, this);
        }
        public async Task<bool> CheckLastUpdate(string setCode, string fullJsonPath, string setFolder)
        {
            LastUpdatesList.TryAdd(setCode, DateTime.Now.AddDays(-2));

            if (LastUpdatesList[setCode].AddDays(1) >= DateTime.Now) return true;
            await Helpers.DownloadSetFile(setCode, fullJsonPath, setFolder).ConfigureAwait(false);
            LastUpdatesList[setCode] = DateTime.Now;
            Save();

            return true;
        }
        public async Task<bool> CheckSetFile(string setCode, string fullJsonPath, string setFolder)
        {
            _ = CheckLastUpdate(setCode, fullJsonPath, setFolder);

            // if the file is still not there we download it
            if (File.Exists(@$"{fullJsonPath}\{setFolder}\{setCode.ToUpper()}.json")) return true;

            await Helpers.DownloadSetFile(setCode, fullJsonPath, setFolder).ConfigureAwait(false);
   
            return true;
        }
        public void UpdateStatistics(string stat, string value)
        {
            if (Statistics.ContainsKey(stat))
            {
                Statistics[stat] = value;
            }
            else
            {
                Statistics.Add(stat, value);
            }
        }
        public void UpdateBoosterCount(int value)
        {
            if (Statistics.TryGetValue("booster", out var currentValue))
            {
                var newValue = int.Parse(currentValue) + value;
                UpdateStatistics("booster", newValue.ToString());
                Save();
            }
            else
            {
                UpdateStatistics("booster", value.ToString());
                Save();
            }
        }
    }
}
