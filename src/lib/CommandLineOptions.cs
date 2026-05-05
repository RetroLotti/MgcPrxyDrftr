using CommandLine;
using OpenBoosters.Api;

namespace MgcPrxyDrftr.lib
{
    public class CommandLineOptions
    {
        [Option('b', "booster", Required = true, HelpText = "Booster/s to be generated. Example \"sos|6|play\". Multiple boosters can be specified separated by a comma.")]
        public string Booster { get; set; }

        [Option('g', "game", Required = false, Default = Enumerators.Game.Magic, HelpText = "Game this/these booster/s is/are from.")]
        public Enumerators.Game Game { get; set; } = Enumerators.Game.Magic;

        [Option('s', "silent", Required = false, Default = false, HelpText = "Set output to silent.")]
        public bool Silent { get; set; } = false;

        [Option('m', "mode", Required = false, Default = RunModes.Pdf, HelpText = "Mode to be used.")]
        public RunModes Mode { get; set; } = RunModes.Pdf;

        [Option('l', "single", Required = false, Default = false, HelpText = "Whether to create a single pdf file or not.")]
        public bool Single { get; set; } = false;
    }

    public enum RunModes
    {
        Pdf,
        Photo
    }
}
