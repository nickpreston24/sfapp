using CodeMechanic.Async;

namespace sfapp;

using CodeMechanic.Shargs;
using Serilog.Core;

public class SingleFileAppFixer : QueuedService
{
    private readonly ArgsMap argsmap;
    private readonly Logger logger;
    private bool debug;

    public SingleFileAppFixer(ArgsMap argsmap, Logger logger)
    {
        this.argsmap = argsmap;
        this.logger = logger;
        this.debug = argsmap.HasFlag("--debug");


    }
}
