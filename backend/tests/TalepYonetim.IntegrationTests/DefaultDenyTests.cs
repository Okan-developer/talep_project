using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace TalepYonetim.IntegrationTests;

/// <summary>Spec 0002, AC-11 (R-9): every backend operation needs a signed-in user unless it is one of the four anonymous ones.</summary>
[Trait("Category", "Integration")]
public sealed partial class DefaultDenyTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private static readonly string[] ExpectedAnonymous =
    [
        "POST /api/auth/login",
        "POST /api/auth/refresh",
        "POST /api/auth/logout",
        "GET /api/auth/demo-users",
    ];

    [Fact]
    public async Task AnonymousEndpoints_AreExactlyLoginRefreshLogoutAndDemoUsers()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        host.CreateClient().Dispose();

        var anonymous = ProductEndpoints(host)
            .Where(e => e.Endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(e => e.Key);

        anonymous.ShouldBe(ExpectedAnonymous, ignoreOrder: true);
    }

    [Fact]
    public async Task EveryOtherEndpoint_RejectsAnAnonymousRequestWithTheTurkishProblem()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateHttpsClient();
        var guarded = ProductEndpoints(host).Where(e => e.Endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null).ToList();
        guarded.Select(e => e.Key).ShouldContain("GET /api/system/status");
        guarded.Select(e => e.Key).ShouldContain("GET /api/auth/me");

        foreach (var (key, method, path, _) in guarded)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, key);
            await ProblemDetailsLocalizationTests.AssertTurkishProblemAsync(
                response, HttpStatusCode.Unauthorized, "Kimlik doğrulama gerekli", "Bu işlem için giriş yapmanız gerekiyor.");
        }
    }

    [Fact]
    public async Task AnEndpointAddedWithoutAnyAuthMarking_RequiresASignedInUserByDefault()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateHttpsClient();

        using var anonymous = await AuthClient.GetWithTokenAsync(client, AuthTestHost.UnmarkedPath, null);
        using var signedIn = await AuthClient.GetWithTokenAsync(client, AuthTestHost.UnmarkedPath, host.CreateAccessToken("Requester"));

        anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        signedIn.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ATokenSignedWithAnotherKey_IsRejected()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateHttpsClient();
        await using var other = new OtherKeyHost(await postgres.CreateDatabaseAsync());
        var forged = other.CreateAccessToken("Admin");

        using var response = await AuthClient.GetWithTokenAsync(client, "/api/auth/me", forged);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static IEnumerable<(string Key, string Method, string Path, RouteEndpoint Endpoint)> ProductEndpoints(AuthTestHost host) =>
        host.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText is { } raw && !raw.StartsWith("/test/", StringComparison.Ordinal))
            .SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["GET"])
                .Select(method => ($"{method} {e.RoutePattern.RawText}", method, Concrete(e.RoutePattern.RawText!), e)))
            .DistinctBy(e => e.Item1);

    // Route parameters get a placeholder value; today no product route has one.
    private static string Concrete(string pattern) => RouteParameter().Replace(pattern, "00000000-0000-0000-0000-000000000000");

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex RouteParameter();

    private sealed class OtherKeyHost(string connectionString) : Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Default", connectionString);
            builder.UseSetting("Jwt:SigningKey", "another-signing-key-that-the-api-does-not-know-0123");
        }

        public string CreateAccessToken(params string[] roles)
        {
            using var scope = Services.CreateScope();
            var tokens = scope.ServiceProvider.GetRequiredService<TalepYonetim.Identity.Infrastructure.TokenService>();
            var user = new TalepYonetim.Identity.Infrastructure.AppUser { Id = Guid.CreateVersion7(), FullName = "Sahte", Email = "sahte@test.local" };
            return tokens.CreateAccessToken(user, roles).Token;
        }
    }
}
