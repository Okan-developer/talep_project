using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using TalepYonetim.Identity;
using TalepYonetim.Reporting;
using TalepYonetim.Reporting.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict; // numbers are numbers in the contract
});

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddReportingModule();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapSystemStatusEndpoints();

// The build-time OpenAPI generator starts this host too; it must not touch the database.
var isOpenApiGeneration = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
if (!isOpenApiGeneration)
{
    try
    {
        await app.Services.MigrateIdentityModuleAsync(CancellationToken.None);
    }
#pragma warning disable CA1031 // the API must start without a database (spec 0001, AC-5)
    catch (Exception exception)
#pragma warning restore CA1031
    {
        StartupLog.MigrationFailed(app.Logger, exception);
    }
}

await app.RunAsync();

public partial class Program;

internal static partial class StartupLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Database migration failed at startup; the API starts without it")]
    public static partial void MigrationFailed(ILogger logger, Exception exception);
}
