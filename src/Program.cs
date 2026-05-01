using CodeMechanic.FileSystem;
using CodeMechanic.Shargs;
using Serilog;
using Serilog.Core;
using Microsoft.Extensions.DependencyInjection;

namespace sfapp;

internal class Program
{
    static async Task Main(string[] args)
    {
        DotEnv.Load();

        // var x = CodeMechanic.Embeds.Embedded.HelloWorldTemplate();

        var arguments = new ArgsMap(args);

        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(
                ".sfapp/logs/sfapp.log",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true
            )
            .CreateLogger();

        bool run_as_cli = !arguments.HasCommand("web");

        if (run_as_cli) await RunAsCli(arguments, logger);
    }

    static async Task RunAsCli(ArgsMap arguments, Logger logger)
    {
        var services = CreateServices(arguments, logger);
        Application app = services.GetRequiredService<Application>();
        await app.Run();
    }

    private static ServiceProvider CreateServices(
        ArgsMap arguments,
        Logger logger)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton(arguments)
            .AddSingleton<Logger>(logger)
            .AddSingleton<Application>()
            .AddSingleton<SingleFileAppFixer>()
            .AddSingleton<WeatherService>()
            .BuildServiceProvider();

        return serviceProvider;
    }
}
