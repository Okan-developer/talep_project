using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace TalepYonetim.IntegrationTests;

/// <summary>
/// The API host for tests that need endpoints the product does not have (a throwing endpoint, a
/// body-bound endpoint, a permission-guarded endpoint). They are mapped in a second routing branch
/// that runs after the product pipeline found no endpoint, so the product's exception handler,
/// status-code pages and fallback authorization still apply to them.
/// </summary>
public sealed class AuthTestHost(string connectionString, string environment = "Testing") : WebApplicationFactory<Program>
{
    public const string ThrowPath = "/test/throw";
    public const string BodyPath = "/test/body";
    public const string LeakedSecret = "gizli-ic-detay-42";

    public sealed record BodyInput(string Name);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, TestEndpointsFilter>());
    }

    private sealed class TestEndpointsFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.UseRouting();
            app.UseEndpoints(MapTestEndpoints);
        };

        private static void MapTestEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet(ThrowPath, string () => throw new InvalidOperationException(LeakedSecret));
            endpoints.MapPost(BodyPath, (BodyInput input) => input.Name);
        }
    }
}
