using Serilog.Core;
using Microsoft.Extensions.DependencyInjection;
using CodeMechanic.Async;

namespace sfapp;

public class Application
{
    private readonly Logger logger;

    private readonly SingleFileAppFixer todos;
    private readonly WeatherService weather;

    public Application(Logger logger
        , SingleFileAppFixer todos
        , WeatherService weather
    )
    {
        this.logger = logger;
        this.todos = todos;
        this.weather = weather;
    }

    public async Task Run()
    {
        await todos.Run();
        await weather.Run();
    }
}

public class WeatherService : QueuedService {}

