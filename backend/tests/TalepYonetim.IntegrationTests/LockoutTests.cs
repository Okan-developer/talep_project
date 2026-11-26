using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TalepYonetim.Identity.Infrastructure;

namespace TalepYonetim.IntegrationTests;

/// <summary>
/// Spec 0002, AC-4 (R-4): 5 consecutive failures lock the account for 15 minutes.
/// ASP.NET Core Identity reads the system clock for lockout (it takes no TimeProvider), so elapsed
/// time is simulated by moving the stored lockout end — the behaviour under test is the same
/// (plan 0002, Risks: "Kilit süresinin testi").
/// </summary>
[Trait("Category", "Integration")]
public sealed class LockoutTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private const string Email = "kilit@test.local";
    private const string Wrong = "Yanlis.Parola1!";

    [Fact]
    public async Task FiveFailures_LockTheAccountForFifteenMinutes_EvenForTheRightPassword()
    {
        await using var host = await StartLockedAsync();
        using var client = host.CreateHttpsClient();

        using var sixth = await AuthClient.LoginAsync(client, Email, AuthTestHost.Password);

        sixth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var (code, detail) = await CodeAndDetail(sixth);
        code.ShouldBe("account_locked");
        detail.ShouldBe("Hesap geçici olarak kilitlendi. 15 dakika sonra tekrar deneyin.");
        var end = await LockoutEndAsync(host);
        end.ShouldNotBeNull();
        end.Value.ShouldBeInRange(DateTimeOffset.UtcNow.AddMinutes(14), DateTimeOffset.UtcNow.AddMinutes(15));
    }

    [Fact]
    public async Task FourFailures_DoNotLock()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        await host.SeedUserAsync(Email, "Requester");
        using var client = host.CreateHttpsClient();
        for (var i = 0; i < 4; i++)
        {
            (await AuthClient.LoginAsync(client, Email, Wrong)).Dispose();
        }

        using var response = await AuthClient.LoginAsync(client, Email, AuthTestHost.Password);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LockedAccount_ReportsTheRemainingMinutesRoundedUp()
    {
        await using var host = await StartLockedAsync();
        await SetLockoutEndAsync(host, DateTimeOffset.UtcNow.AddMinutes(4).AddSeconds(30)); // 10.5 of 15 minutes passed
        using var client = host.CreateHttpsClient();

        using var response = await AuthClient.LoginAsync(client, Email, AuthTestHost.Password);

        (await CodeAndDetail(response)).Detail.ShouldBe("Hesap geçici olarak kilitlendi. 5 dakika sonra tekrar deneyin.");
    }

    [Fact]
    public async Task OnceTheLockoutHasPassed_TheRightPasswordIsAccepted()
    {
        await using var host = await StartLockedAsync();
        await SetLockoutEndAsync(host, DateTimeOffset.UtcNow.AddSeconds(-1)); // the 15 minutes are over
        using var client = host.CreateHttpsClient();

        using var response = await AuthClient.LoginAsync(client, Email, AuthTestHost.Password);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SuccessfulLogin_ResetsTheFailureCounter()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        await host.SeedUserAsync(Email, "Requester");
        using var client = host.CreateHttpsClient();
        for (var i = 0; i < 4; i++)
        {
            (await AuthClient.LoginAsync(client, Email, Wrong)).Dispose();
        }

        (await AuthClient.LoginAsync(client, Email, AuthTestHost.Password)).StatusCode.ShouldBe(HttpStatusCode.OK);
        for (var i = 0; i < 4; i++)
        {
            using var failure = await AuthClient.LoginAsync(client, Email, Wrong);
            (await CodeAndDetail(failure)).Code.ShouldBe("invalid_credentials");
        }

        using var response = await AuthClient.LoginAsync(client, Email, AuthTestHost.Password);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task<AuthTestHost> StartLockedAsync()
    {
        var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        await host.SeedUserAsync(Email, "Requester");
        using var client = host.CreateHttpsClient();
        for (var i = 0; i < 5; i++)
        {
            (await AuthClient.LoginAsync(client, Email, Wrong)).Dispose();
        }

        return host;
    }

    private static async Task<DateTimeOffset?> LockoutEndAsync(AuthTestHost host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        return await users.GetLockoutEndDateAsync((await users.FindByEmailAsync(Email))!);
    }

    private static async Task SetLockoutEndAsync(AuthTestHost host, DateTimeOffset end)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        (await users.SetLockoutEndDateAsync((await users.FindByEmailAsync(Email))!, end)).Succeeded.ShouldBeTrue();
    }

    private static async Task<(string? Code, string? Detail)> CodeAndDetail(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return (body.RootElement.GetProperty("code").GetString(), body.RootElement.GetProperty("detail").GetString());
    }
}
