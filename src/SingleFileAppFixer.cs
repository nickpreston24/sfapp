using CodeMechanic.Async;
using CodeMechanic.FileSystem;
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

    public SingleFileAppFixer(ArgsMap argsmap, Logger logger)
    {
        this.argsmap = argsmap;
        this.logger = logger;
        this.debug = argsmap.HasFlag("--debug");


        this.single_file_app_grepper = new Grepper()
        {
            RootPath = Directory.GetCurrentDirectory(),
            Recursive = true,
            FileSearchMask = "*.cs"
        };

        if (argsmap.HasCommand("promote"))
            steps.Add(PromoteSFAToFullCsproj);
    }

    private async Task PromoteSFAToFullCsproj()
    {
        string[] project_types = new[] { "library", "api", "razorapp" };

        var matching_files = single_file_app_grepper.GetMatchingFiles().Select(file => file.FilePath).ToArray();

        string path_of_file_to_promote =
            Prompt.Select("which single file app should be promoted to a new csproj?", matching_files);

        logger.Information($"Promoting file '{Path.GetFileName(path_of_file_to_promote)}'");

        // todo: actually promote it
    }
}
