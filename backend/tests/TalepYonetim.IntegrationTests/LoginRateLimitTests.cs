using System.Net;
using Shouldly;

namespace TalepYonetim.IntegrationTests;

/// <summary>Spec 0002, AC-5 (R-4): at most 10 login requests per client address per minute.</summary>
[Trait("Category", "Integration")]
public sealed class LoginRateLimitTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task EleventhLoginWithinAMinute_IsRejectedWithTheTurkishThrottleMessage()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateHttpsClient();

        for (var i = 1; i <= 10; i++)
        {
            using var allowed = await AuthClient.LoginAsync(client, $"kimse{i}@test.local", "Yanlis.Parola1!");
            allowed.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, $"request {i} must reach the login check");
        }

        using var eleventh = await AuthClient.LoginAsync(client, "kimse11@test.local", "Yanlis.Parola1!");

        await ProblemDetailsLocalizationTests.AssertTurkishProblemAsync(
            eleventh, HttpStatusCode.TooManyRequests, "Çok fazla istek", "Çok fazla deneme yapıldı. Lütfen biraz sonra tekrar deneyin.");
    }

    [Fact]
    public async Task RateLimit_AppliesToLoginOnly()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateHttpsClient();
        for (var i = 0; i < 11; i++)
        {
            (await AuthClient.LoginAsync(client, "kimse@test.local", "x")).Dispose();
        }

        using var refresh = await AuthClient.PostWithCookieAsync(client, "/api/auth/refresh", null);

        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
