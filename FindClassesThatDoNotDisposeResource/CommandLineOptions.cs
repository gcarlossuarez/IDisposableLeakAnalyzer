using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindClassesThatDoNotDisposeResource
{
    public class CommandLineOptions
    {
        [Option('s', "solution", Required = false, HelpText = "Path to the solution file (.sln).")]
        public string SolutionPath { get; set; }

        [Option('l', "language", Required = false, HelpText = "Language of messages(EN or SP).")]
        public string Language { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Display MSBuild errors or warnings.")]
        public bool Verbose { get; set; }

        [Option('c', "csvseparator", Required = false, HelpText = "Separator for the CSV.")]
        public string CsvSeparator { get; set; }

        [Option('o', "output", Required = false, HelpText = "Directory to save results.")]
        public string OutputPath { get; set; }
    }

}
