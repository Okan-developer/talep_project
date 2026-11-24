using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TalepYonetim.Identity.Contracts;

namespace TalepYonetim.Reporting.Application;

public sealed partial class SystemStatusQuery(
    IDepartmentDirectory departments,
    IOptions<SystemStatusOptions> options,
    ILogger<SystemStatusQuery> logger)
{
    public async Task<SystemStatus> ExecuteAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Value.DatabaseTimeout);
        try
        {
            var count = await departments.CountAsync(timeout.Token);
            return new SystemStatus(ApiStatus.Up, DatabaseStatus.Available, count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // the caller went away: not a database verdict
        }
#pragma warning disable CA1031 // any database failure means "unavailable"; details stay in the log
        catch (Exception exception)
#pragma warning restore CA1031
        {
            LogDatabaseUnavailable(logger, exception);
            return new SystemStatus(ApiStatus.Up, DatabaseStatus.Unavailable, null);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "System status: database unavailable")]
    private static partial void LogDatabaseUnavailable(ILogger logger, Exception exception);
}
