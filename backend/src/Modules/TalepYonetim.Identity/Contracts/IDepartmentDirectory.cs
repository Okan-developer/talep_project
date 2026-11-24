namespace TalepYonetim.Identity.Contracts;

/// <summary>The Identity module's public read surface for departments.</summary>
public interface IDepartmentDirectory
{
    /// <summary>Number of departments defined. Throws when the database cannot be reached.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken);
}
