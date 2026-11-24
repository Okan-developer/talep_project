using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TalepYonetim.Identity.Contracts;
using TalepYonetim.Reporting.Application;

namespace TalepYonetim.UnitTests;

public sealed class SystemStatusQueryTests
{
    private static SystemStatusQuery CreateQuery(IDepartmentDirectory directory, TimeSpan? timeout = null) =>
        new(
            directory,
            Options.Create(new SystemStatusOptions { DatabaseTimeout = timeout ?? TimeSpan.FromSeconds(3) }),
            NullLogger<SystemStatusQuery>.Instance);

    [Fact]
    public async Task Execute_WhenDatabaseAnswers_ReportsAvailableWithCount()
    {
        var status = await CreateQuery(new StubDirectory(_ => Task.FromResult(6)))
            .ExecuteAsync(TestContext.Current.CancellationToken);

        status.ShouldBe(new SystemStatus(ApiStatus.Up, DatabaseStatus.Available, 6));
    }

    [Fact]
    public async Task Execute_WhenNoDepartments_ReportsAvailableWithZero()
    {
        var status = await CreateQuery(new StubDirectory(_ => Task.FromResult(0)))
            .ExecuteAsync(TestContext.Current.CancellationToken);

        status.ShouldBe(new SystemStatus(ApiStatus.Up, DatabaseStatus.Available, 0));
    }

    [Fact]
    public async Task Execute_WhenDatabaseThrows_ReportsUnavailableWithoutCount()
    {
        var status = await CreateQuery(new StubDirectory(_ => throw new InvalidOperationException("Host=secret-db")))
            .ExecuteAsync(TestContext.Current.CancellationToken);

        status.ShouldBe(new SystemStatus(ApiStatus.Up, DatabaseStatus.Unavailable, null));
    }

    [Fact]
    public async Task Execute_WhenDatabaseHangsPastTimeout_ReportsUnavailable()
    {
        var hanging = new StubDirectory(async token =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return 6;
        });

        var status = await CreateQuery(hanging, TimeSpan.FromMilliseconds(50))
            .ExecuteAsync(TestContext.Current.CancellationToken);

        status.ShouldBe(new SystemStatus(ApiStatus.Up, DatabaseStatus.Unavailable, null));
    }

    [Fact]
    public async Task Execute_WhenCallerCancels_PropagatesCancellation()
    {
        using var caller = new CancellationTokenSource();
        await caller.CancelAsync();
        var directory = new StubDirectory(token => Task.FromCanceled<int>(token));

        await Should.ThrowAsync<OperationCanceledException>(() => CreateQuery(directory).ExecuteAsync(caller.Token));
    }

    private sealed class StubDirectory(Func<CancellationToken, Task<int>> count) : IDepartmentDirectory
    {
        public Task<int> CountAsync(CancellationToken cancellationToken) => count(cancellationToken);
    }
}
