using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MgcPrxyDrftrLib;
using MgcPrxyDrftrLib.models;
using Newtonsoft.Json;

namespace MgcPrxyDrftrVanced
{
    public partial class MainForm : Form
    {
        private Settings _settings { get; set; }
        private DraftLibrary _draftLibrary { get; set; }

        public MainForm()
        {
            InitializeComponent();
        }

        private List<SetRoot> ReadInstalledSets()
        {
            var setDirectoryInfo = new DirectoryInfo(_draftLibrary.FullSetPath);
            var sets = new List<SetRoot>();

            foreach (var set in setDirectoryInfo.GetFiles("*.json"))
            {
                var foo = set.OpenText();
                var bar = JsonConvert.DeserializeObject<SetRoot>(foo.ReadToEnd());
                sets.Add(bar);
            }

            return sets;
        }
        
        private void MainForm_Load(object sender, EventArgs e)
        {
            _settings = new Settings();
            _settings.Load();

            _draftLibrary = new DraftLibrary();

            var sets = ReadInstalledSets();
        }
    }
}
