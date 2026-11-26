using System.Net;
using System.Text.Json;
using Npgsql;
using Shouldly;

namespace TalepYonetim.IntegrationTests;

/// <summary>Spec 0001 AC-3/AC-5/AC-6 behaviour, now behind sign-in (spec 0002, R-9 / AC-11 / AC-12).</summary>
[Trait("Category", "Integration")]
public sealed class SystemStatusEndpointTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private const string StatusPath = "/api/system/status";

    [Fact]
    public async Task Status_WithMigratedDatabase_ReportsUpAvailableAndSixDepartments()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateHttpsClient();

        using var response = await AuthClient.GetWithTokenAsync(client, StatusPath, host.CreateAccessToken("Requester"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        body.RootElement.GetProperty("api").GetString().ShouldBe("up");
        body.RootElement.GetProperty("database").GetString().ShouldBe("available");
        body.RootElement.GetProperty("departmentCount").GetInt32().ShouldBe(6);
    }

    [Theory]
    [InlineData("Requester")]
    [InlineData("DepartmentManager")]
    [InlineData("SoftwareStaff")]
    [InlineData("SystemStaff")]
    [InlineData("ItUnitManager")]
    [InlineData("Admin")]
    public async Task Status_IsAvailableToEverySignedInRole(string role)
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateHttpsClient();

        using var response = await AuthClient.GetWithTokenAsync(client, StatusPath, host.CreateAccessToken(role));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Status_WithoutSignIn_IsRejected()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateHttpsClient();

        using var response = await AuthClient.GetWithTokenAsync(client, StatusPath, null);

        await ProblemDetailsLocalizationTests.AssertTurkishProblemAsync(
            response, HttpStatusCode.Unauthorized, "Kimlik doğrulama gerekli", "Bu işlem için giriş yapmanız gerekiyor.");
    }

    [Fact]
    public async Task Status_WhenDatabaseUnreachable_ReportsUnavailableWithoutInternalDetail()
    {
        const string unreachable = "Host=127.0.0.1;Port=1;Database=nowhere;Username=nobody;Password=secret-pw;Timeout=2";
        await using var host = new AuthTestHost(unreachable);
        using var client = host.CreateHttpsClient();

        using var response = await AuthClient.GetWithTokenAsync(client, StatusPath, host.CreateAccessToken("Requester"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        AssertUnavailableWithoutDetail(raw);
        raw.ShouldNotContain("127.0.0.1");
        raw.ShouldNotContain("secret-pw");
    }

    [Fact]
    public async Task Status_WhenSchemaMissing_ReportsUnavailableWithoutInternalDetail()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var host = new AuthTestHost(connectionString);
        using var client = host.CreateHttpsClient(); // starts the host: migrations run
        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var drop = new NpgsqlCommand("DROP SCHEMA identity CASCADE", connection);
            await drop.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        using var response = await AuthClient.GetWithTokenAsync(client, StatusPath, host.CreateAccessToken("Requester"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        AssertUnavailableWithoutDetail(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private static void AssertUnavailableWithoutDetail(string raw)
    {
        using var body = JsonDocument.Parse(raw);
        body.RootElement.GetProperty("api").GetString().ShouldBe("up");
        body.RootElement.GetProperty("database").GetString().ShouldBe("unavailable");
        body.RootElement.GetProperty("departmentCount").ValueKind.ShouldBe(JsonValueKind.Null);
        body.RootElement.EnumerateObject().Select(p => p.Name).ShouldBe(["api", "database", "departmentCount"], ignoreOrder: true);
        foreach (var leak in new[] { "Host=", "Npgsql", "Exception", "   at ", "relation", "identity" })
        {
            raw.ShouldNotContain(leak, Case.Insensitive);
        }
    }
}
