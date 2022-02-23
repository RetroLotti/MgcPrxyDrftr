using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MgcPrxyDrftr.models
{
    public class Settings
    {
        public bool AutomaticPrinting { get; set; }
        public bool DownloadBasicLands { get; set; }
        public string LastGeneratedSet { get; set; }
        public Dictionary<string, DateTime> LastUpdatesList { get; set; }
        public Dictionary<string, string> Statistics { get; set; }
        public List<string> SetsToLoad { get; set; }

        public string OwnPath { get; private set; }

        public Settings()
        {
            LastUpdatesList = new();
            Statistics = new();
            SetsToLoad = new();
        }
        public Settings(string settingsPath)
        {
            LastUpdatesList = new();
            Statistics = new();
            SetsToLoad = new();
            OwnPath = settingsPath;
        }

        public void AddSet(string setCode)
        {
            if(SetsToLoad != null && !SetsToLoad.Contains(setCode))
            {
                SetsToLoad.Add(setCode);
            }
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
            string json = System.Text.Json.JsonSerializer.Serialize(this);
            File.WriteAllText(OwnPath, json);
        }
    }
}
