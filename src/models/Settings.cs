using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyDraftor.models
{
    public class Settings
    {
        public bool AutomaticPrinting { get; set; }
        public string LastGeneratedSet { get; set; }
        public Dictionary<string, DateTime> LastUpdatesList { get; set; }
        public Dictionary<string, string> Statistics { get; set; }
        public List<string> SetsToLoad { get; set; }

        public void AddSet(string setCode)
        {
            if(SetsToLoad != null && !SetsToLoad.Contains(setCode))
            {
                SetsToLoad.Add(setCode);
            }
        }
        public void Save()
        {

        }
    }
}
