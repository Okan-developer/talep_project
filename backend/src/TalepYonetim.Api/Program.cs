using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using TalepYonetim.Api.Errors;
using TalepYonetim.Identity;
using TalepYonetim.Reporting;
using TalepYonetim.Reporting.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// The build-time OpenAPI generator starts this host too; it must not touch the database, and it
// never issues a token, so it gets a throwaway signing key instead of failing the key check.
var isOpenApiGeneration = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
if (isOpenApiGeneration)
{
    builder.Configuration["Jwt:SigningKey"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
}

builder.Services.AddTurkishProblemDetails();
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
app.UseRateLimiter();
// Explicit, after the status-code pages, so a bodyless 401/403 becomes a Turkish ProblemDetails (spec 0002, R-12).
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapIdentityEndpoints(app.Environment.IsDevelopment());
app.MapSystemStatusEndpoints();

if (!isOpenApiGeneration)
{
    try
    {
        await app.Services.MigrateIdentityModuleAsync(CancellationToken.None);
        if (app.Environment.IsDevelopment())
        {
            await app.Services.SeedIdentityDemoUsersAsync(CancellationToken.None); // spec 0002, R-2
        }
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
