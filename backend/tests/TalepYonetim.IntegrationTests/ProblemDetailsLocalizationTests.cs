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
        using var client = host.CreateClient();

        using var response = await client.GetAsync("/api/does-not-exist", TestContext.Current.CancellationToken);

        await AssertTurkishProblemAsync(response, HttpStatusCode.NotFound, "Bulunamadı", "İstenen kaynak bulunamadı.");
    }

    [Fact]
    public async Task MalformedBody_ReturnsTurkishValidationProblemWithoutParserDetail()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateClient();
        using var content = new StringContent("{ \"name\": ", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(AuthTestHost.BodyPath, content, TestContext.Current.CancellationToken);

        var raw = await AssertTurkishProblemAsync(response, HttpStatusCode.BadRequest, "Geçersiz istek", "İstekteki bilgiler doğrulanamadı.");
        raw.ShouldNotContain("BodyInput");
        raw.ShouldNotContain("JSON", Case.Sensitive);
    }

    [Fact]
    public async Task UnhandledException_ReturnsTurkishServerErrorWithoutExceptionDetail()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateClient();

        using var response = await client.GetAsync(AuthTestHost.ThrowPath, TestContext.Current.CancellationToken);

        var raw = await AssertTurkishProblemAsync(
            response, HttpStatusCode.InternalServerError, "Beklenmeyen hata", "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
        raw.ShouldNotContain(AuthTestHost.LeakedSecret);
        raw.ShouldNotContain("InvalidOperationException");
        raw.ShouldNotContain("   at ");
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
