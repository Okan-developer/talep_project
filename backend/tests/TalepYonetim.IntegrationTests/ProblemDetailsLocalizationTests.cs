using System.Net;
using System.Text;
using System.Text.Json;
using Shouldly;

namespace TalepYonetim.IntegrationTests;

/// <summary>Spec 0002, AC-16 / R-12: every error response is a Turkish ProblemDetails without internal detail.</summary>
[Trait("Category", "Integration")]
public sealed class ProblemDetailsLocalizationTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task UnknownPath_ReturnsTurkishNotFoundProblem()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateHttpsClient();

        using var response = await AuthClient.GetWithTokenAsync(client, "/api/does-not-exist", host.CreateAccessToken("Requester"));

        await AssertTurkishProblemAsync(response, HttpStatusCode.NotFound, "Bulunamadı", "İstenen kaynak bulunamadı.");
    }

    [Fact]
    public async Task MalformedBody_ReturnsTurkishValidationProblemWithoutParserDetail()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateHttpsClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, AuthTestHost.BodyPath)
        {
            Content = new StringContent("{ \"name\": ", Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", host.CreateAccessToken("Requester"));

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        var raw = await AssertTurkishProblemAsync(response, HttpStatusCode.BadRequest, "Geçersiz istek", "İstekteki bilgiler doğrulanamadı.");
        raw.ShouldNotContain("BodyInput");
        raw.ShouldNotContain("JSON", Case.Sensitive);
    }

    [Fact]
    public async Task UnhandledException_ReturnsTurkishServerErrorWithoutExceptionDetail()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateHttpsClient();

        using var response = await AuthClient.GetWithTokenAsync(client, AuthTestHost.ThrowPath, host.CreateAccessToken("Requester"));

        var raw = await AssertTurkishProblemAsync(
            response, HttpStatusCode.InternalServerError, "Beklenmeyen hata", "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
        raw.ShouldNotContain(AuthTestHost.LeakedSecret);
        raw.ShouldNotContain("InvalidOperationException");
        raw.ShouldNotContain("   at ");
    }

    [Fact]
    public async Task MissingSignIn_ReturnsTurkishUnauthorizedProblem()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateHttpsClient();

        using var response = await AuthClient.GetWithTokenAsync(client, "/api/auth/me", null);

        await AssertTurkishProblemAsync(response, HttpStatusCode.Unauthorized, "Kimlik doğrulama gerekli", "Bu işlem için giriş yapmanız gerekiyor.");
    }

    [Fact]
    public async Task MissingPermission_ReturnsTurkishForbiddenProblem()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateHttpsClient();

        using var response = await AuthClient.GetWithTokenAsync(client, AuthTestHost.AdminUsersPath, host.CreateAccessToken("Requester"));

        await AssertTurkishProblemAsync(response, HttpStatusCode.Forbidden, "Yetki yok", "Bu işlem için yetkiniz yok.");
    }

    internal static async Task<string> AssertTurkishProblemAsync(HttpResponseMessage response, HttpStatusCode status, string title, string detail)
    {
        response.StatusCode.ShouldBe(status);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(raw);
        body.RootElement.GetProperty("status").GetInt32().ShouldBe((int)status);
        body.RootElement.GetProperty("title").GetString().ShouldBe(title);
        body.RootElement.GetProperty("detail").GetString().ShouldBe(detail);
        body.RootElement.TryGetProperty("traceId", out _).ShouldBeTrue();
        body.RootElement.TryGetProperty("exception", out _).ShouldBeFalse();
        return raw;
    }
}
