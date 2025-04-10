using OpenTelemetry.Logs;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddOpenTelemetry(logging => logging.AddConsoleExporter());

var app = builder.Build();

Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetrySdk(app.Services)
    .CreateLogger();

Log.Information("{A} + {B} = {C}", 1, 2, 3);

app.MapGet("/", () =>
{
    Log.Information("Hello from {Name}!", "Serilog");
    return "Hello World!";
});

app.Run();