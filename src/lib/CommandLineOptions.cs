﻿using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MgcPrxyDrftr.lib
{
    public class CommandLineOptions
    {
        [Option('b', "booster", Required = true, HelpText = "Boosters to be generated.")]
        public string Booster { get; set; }

        [Option('s', "silent", Required = false, HelpText = "Set output to silent.")]
        public bool Silent { get; set; }
    }
}
