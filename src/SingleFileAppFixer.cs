using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CodeMechanic.Async;
using CodeMechanic.Bash;
using CodeMechanic.Diagnostics;
using CodeMechanic.Embeds;
using CodeMechanic.FileSystem;
using CodeMechanic.RegularExpressions;
using CodeMechanic.Types;
using Sharprompt;

namespace sfapp;

using CodeMechanic.Shargs;
using Serilog.Core;

public class SingleFileAppFixer : QueuedService
{
    private readonly ArgsMap argsmap;
    private readonly Logger logger;
    private bool debug;
    private readonly Grepper single_file_app_grepper;
    private string root;

    string[] project_types = new[] { "library", "api", "razorapp" };


    public SingleFileAppFixer(ArgsMap argsmap, Logger logger)
    {
        this.argsmap = argsmap;
        this.logger = logger;
        this.debug = argsmap.HasFlag("--debug");
        this.root = argsmap.WithFlags("--dir", "-d").Value.AsUnixPath();


        if (this.root.IsEmpty())
            root = Environment.GetEnvironmentVariable("SFAPP_ROOT") ?? Directory.GetCurrentDirectory();

        this.single_file_app_grepper = new Grepper()
        {
            RootPath = root,
            Recursive = true,
            FileSearchMask = "*.cs",
            // FileSearchLinePattern = SingleFileAppPatterns.Package().ToString()
        };

        if (argsmap.HasCommand("promote"))
            steps.Add(PromoteSFAToFullCsproj);


        // testing gen:


    }

    private async Task PromoteSFAToFullCsproj()
    {
        logger.Information($"Looking for files in dir '{root}'");

        var packages_detected = single_file_app_grepper
            .GetMatchingFiles(SingleFileAppPatterns.Package());

        var matching_files = packages_detected
            .DistinctBy(f => f.FilePath)
            .Select(file => file.FilePath)
            .ToArray();

        // var dirfiles = single_file_app_grepper.GetFileNames().Dump("filenames");

        var packages_to_convert = packages_detected
            .SelectMany(x => x.Line.Extract<SingleFileAppPackage>(SingleFileAppPatterns.Package()));

        // e.g. `dotnet run promote --debug`  or `sfapp promote --debug`
        if (debug)
            packages_to_convert.Dump(nameof(packages_to_convert));

        if (matching_files.Length == 0)
        {
            logger.Warning("No matching files to promote were found!  Exiting.");
            return;
        }

        string path_of_file_to_promote =
            Prompt.Select("which single file app should be promoted to a new csproj?", matching_files);

        logger.Information($"Promoting file '{Path.GetFileName(path_of_file_to_promote)}'");

        // todo: actually promote it


        string project_namespace = Path.GetFileName(path_of_file_to_promote);
        // string output_from_scaffolding = await $"dotnet new razor".Bash(verbose: false);

        // update the project root
        this.root = Path.Combine(root, project_namespace);
        logger.Information($"New app created at '{this.root}'");

        string template = ReadTemplateV0();

        // var embeds = Assembly.GetExecutingAssembly().GetEmbeddedProvider();

        // string razor = await embeds.ReadAsync("razor.app.template");
        // string pkg   = await embeds.ReadAsync("package_reference.template");

        // embeds.Dump();                    // beautiful output


        // string output_from_build = await $"cd {this.root} && dotnet build".Bash(verbose: true);
        // logger.Information($"{nameof(output_from_build)} :>> {output_from_build}");
    }


    private string ReadTemplateV0()
    {
        var myass = Assembly.GetExecutingAssembly();

        if (debug)
            myass.Dump(nameof(myass));

        string template = myass.ReadSourceFile("razor.app.template", debug: debug);

        Console.WriteLine($"{nameof(template)} :>> {template}");
        return template;
    }
}

public static partial class SingleFileAppPatterns
{
    [GeneratedRegex(
        @"\#\:
(?<kind>sdk|package)  # type/kind
\s+
(?<package_name>[\w_\.\d]+)  # name of package

(?<version>
 @
 (?<major>[\d\*]+)
 (\.(?<minor>[\d\*]+))
 (\.(?<patch>[\d\*]+))?
)?  # version",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace |
        RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 500)]
    public static partial Regex Package(); //https://regex101.com/r/XkMteU/1
}

public class SingleFileAppPackage
{
    public string kind { get; set; } = string.Empty;
    public string version { get; set; } = string.Empty;
    public string major { get; set; } = string.Empty;
    public string minor { get; set; } = string.Empty;
    public string patch { get; set; } = string.Empty;
}
