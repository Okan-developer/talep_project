using Npgsql;
using Shouldly;

namespace TalepYonetim.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class MigrationSeedTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private static readonly string[] ExpectedDepartments =
        ["BT Sistem", "BT Yazılım", "Finans", "Muhasebe", "Operasyon", "İnsan Kaynakları"];

    [Fact]
    public async Task Startup_OnEmptyDatabase_SeedsSixDemoDepartments_AndSecondStartupDoesNotDuplicate()
    {
        var connectionString = await postgres.CreateDatabaseAsync();

        await StartApiAsync(connectionString);
        (await ReadDepartmentNamesAsync(connectionString)).ShouldBe(ExpectedDepartments, ignoreOrder: true);

        await StartApiAsync(connectionString);
        (await ReadDepartmentNamesAsync(connectionString)).ShouldBe(ExpectedDepartments, ignoreOrder: true);
    }

    private static async Task StartApiAsync(string connectionString)
    {
        await using var factory = new ApiFactory(connectionString);
        using var client = factory.CreateClient(); // host start applies pending migrations and the seed
        using var response = await client.GetAsync("/api/system/status", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<List<string>> ReadDepartmentNamesAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand("SELECT name FROM identity.department", connection);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var names = new List<string>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }
}
