namespace TalepYonetim.Reporting.Application;

public enum ApiStatus
{
    Up,
}

public enum DatabaseStatus
{
    Available,
    Unavailable,
}

/// <summary>What the system status card shows (spec 0001, R-3). Never carries internal detail.</summary>
public sealed record SystemStatus(ApiStatus Api, DatabaseStatus Database, int? DepartmentCount);

public sealed class SystemStatusOptions
{
    /// <summary>How long the database may take before it counts as unavailable.</summary>
    public TimeSpan DatabaseTimeout { get; set; } = TimeSpan.FromSeconds(3);
}
