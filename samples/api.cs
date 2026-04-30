#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.AspNetCore.OpenApi@9.*
#:package Serilog@4.*
#:package Serilog.Sinks.Console@2.*
#:package Serilog.Sinks.File@7.*
#:package CodeMechanic.Logging@1.0.3
#:package CodeMechanic.Shargs@1.0.6
#:package CodeMechanic.Diagnostics@1.0.6

using CodeMechanic.Shargs;
using Serilog;

var arguments = new ArgsMap(args);

var logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        ".sample_api/logs/sample_api.log",
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true
    )
    .CreateLogger();

// logger.Information(AnsiColors.From("#1af") + "Hello, api!");
logger.Information("Hello, api!");

var Users = new List<User> { };

var builder = WebApplication.CreateBuilder();


builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapGet("/", () => "Hello from file-based API!");
app.MapGet("/users/{id}", (int id) => new
{
    Id = id,
    Name = "foo"
    // User = Users[id]
});

app.Run();

public record User
{
    public string name { get; set; } = string.Empty;
    public int Id { get; set; } = -1;
}
