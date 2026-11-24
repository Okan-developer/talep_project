namespace TalepYonetim.Identity.Domain;

/// <summary>An organisational unit of the company (docs/domain.md, Departman).</summary>
public sealed class Department
{
    public Department(Guid id, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id;
        Name = name;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }
}
