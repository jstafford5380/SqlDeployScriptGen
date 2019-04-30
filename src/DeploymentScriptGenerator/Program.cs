using System;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;

namespace DeploymentScriptGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var testArgs = new[] {"-i", "database/install", "-r", "database/rollback"};
            var p = new Program();
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(p.Run);
        }

        private void Run(Options args)
        {
            CheckManifests(args);
            CreateCombinedScript(args.InstallScripts, "install", args.OutputFolder);
            CreateCombinedScript(args.RollbackScripts, "rollback", args.OutputFolder);
        }

        private void CheckManifests(Options options)
        {
            bool FileExists(string sourceDirectory) => File.Exists(Path.Combine(Directory.GetCurrentDirectory(), sourceDirectory, "manifest.txt"));

            if (!FileExists(options.InstallScripts))
            {
                Console.Out.WriteLine($"Could not find manifest at '{options.InstallScripts}'!!");
                Environment.Exit(1);
            }

            if (!FileExists(options.RollbackScripts))
            {
                Console.Out.WriteLine($"Could not find manifest at '{options.RollbackScripts}'!!");
                Environment.Exit(1);
            }

            if (options.OutputFolder != null && !Directory.Exists(options.OutputFolder))
            {
                Console.Out.WriteLine($"Output directory '{options.OutputFolder}' does not exist.");
                Environment.Exit(1);
            }
        }

        private void CreateCombinedScript(string sourcePath, string name, string outputPath)
        {
            var currentDirectory = Directory.GetCurrentDirectory();

            var scriptDirectory = Path.GetRelativePath(currentDirectory, sourcePath);
            var manifestPath = Path.Combine(scriptDirectory, "manifest.txt");
            var manifest = File.ReadAllText(manifestPath);

            var installScriptPaths = manifest.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            Console.Out.WriteLine($"Found {installScriptPaths.Length} scripts in the manifest for {name}.");

            var sb = new StringBuilder();
            foreach (var path in installScriptPaths.Where(p => !string.IsNullOrEmpty(p)))
            {
                var scriptPath = Path.Combine(scriptDirectory, path);
                var scriptContent = File.ReadAllText(scriptPath);

                sb.Append(scriptContent);
                sb.AppendLine();
                sb.AppendLine();
            }

            var outputDirectory = outputPath ?? scriptDirectory;
            if (outputDirectory == "./" || outputDirectory == "/")
                outputDirectory = currentDirectory;

            var outputFilename = Path.Combine(outputDirectory, $"combined_{name}.sql");
            File.WriteAllText(outputFilename, sb.ToString());

            Console.Out.WriteLine($"Generated {Path.GetRelativePath(currentDirectory, outputFilename)}.");
        }
    }

    public class Options
    {
        [Option('i', "install", Required = true, HelpText = "Install scripts base directory")]
        public string InstallScripts { get; set; }

        [Option('r', "rollback", Required = true, HelpText = "Rollback scripts base directory")]
        public string RollbackScripts { get; set; }

        [Option('o', "output", Required = false, HelpText = "The output destination folder.")]
        public string OutputFolder { get; set; }
    }
}
