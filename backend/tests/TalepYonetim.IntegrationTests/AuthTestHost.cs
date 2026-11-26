using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using TalepYonetim.Identity.Contracts;
using TalepYonetim.Identity.Infrastructure;

namespace TalepYonetim.IntegrationTests;

/// <summary>
/// The API host for tests that need endpoints the product does not have (a throwing endpoint, a
/// body-bound endpoint, a permission-guarded endpoint). They are mapped in a second routing branch
/// that runs after the product pipeline found no endpoint, so the product's exception handler,
/// status-code pages and fallback authorization still apply to them. An optional fake clock drives
/// token lifetimes and lockout.
/// </summary>
public sealed class AuthTestHost(string connectionString, string environment = "Testing", FakeTimeProvider? time = null)
    : WebApplicationFactory<Program>
{
    public const string ThrowPath = "/test/throw";
    public const string BodyPath = "/test/body";
    public const string AdminUsersPath = "/test/admin-users";
    public const string LeakedSecret = "gizli-ic-detay-42";
    public const string Password = "Test.Parola1!";

    public sealed record BodyInput(string Name);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-0123456789abcdef");
        builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, TestEndpointsFilter>());
        if (time is not null)
        {
            builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(time));
        }
    }

    /// <summary>A client on https (the refresh cookie is Secure) that leaves cookies to the test.</summary>
    public HttpClient CreateHttpsClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = false });

    /// <summary>Creates a user with <see cref="Password"/> and the given roles in the Muhasebe department.</summary>
    public async Task SeedUserAsync(string email, params string[] roles)
    {
        await using var scope = Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            FullName = "Test " + email,
            DepartmentId = Guid.Parse("019aa000-0000-7000-8000-000000000002"),
        };
        (await users.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        foreach (var role in roles)
        {
            (await users.AddToRoleAsync(user, role)).Succeeded.ShouldBeTrue();
        }
    }

    private sealed class TestEndpointsFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(MapTestEndpoints);
        };

        private static void MapTestEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet(ThrowPath, string () => throw new InvalidOperationException(LeakedSecret)).AllowAnonymous();
            endpoints.MapPost(BodyPath, (BodyInput input) => input.Name).AllowAnonymous();
            endpoints.MapGet(AdminUsersPath, () => Results.Ok("ok")).RequirePermission(Permissions.AdminUsers);
        }
    }
}

/// <summary>Login and session helpers over raw HTTP; the refresh cookie is handled explicitly.</summary>
public static class AuthClient
{
    public const string CookieName = "talep_refresh";

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string? email, string? password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password }, TestContext.Current.CancellationToken);

    /// <summary>Logs in and returns the access token and refresh-cookie value.</summary>
    public static async Task<(string AccessToken, string Refresh)> SignInAsync(HttpClient client, string email, string password = AuthTestHost.Password)
    {
        using var response = await LoginAsync(client, email, password);
        response.EnsureSuccessStatusCode();
        return (await AccessTokenOf(response), RefreshCookieOf(response)!);
    }

    public static async Task<HttpResponseMessage> PostWithCookieAsync(HttpClient client, string path, string? refresh)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (refresh is not null)
        {
            request.Headers.Add("Cookie", $"{CookieName}={refresh}");
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static async Task<HttpResponseMessage> GetWithTokenAsync(HttpClient client, string path, string? accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static async Task<string> AccessTokenOf(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return body.RootElement.GetProperty("accessToken").GetString()!;
    }

    /// <summary>The refresh-cookie value set by the response; empty when the response deletes it; null when absent.</summary>
    public static string? RefreshCookieOf(HttpResponseMessage response) =>
        SetCookieHeader(response)?.Split(';')[0][(CookieName.Length + 1)..];

    public static string? SetCookieHeader(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith(CookieName + "=", StringComparison.Ordinal))
            : null;
}
