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
    }
}
