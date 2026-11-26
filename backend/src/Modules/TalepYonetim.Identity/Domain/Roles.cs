namespace TalepYonetim.Identity.Domain;

/// <summary>The role codes of docs/domain.md, Roller.</summary>
public static class Roles
{
    public const string Requester = "Requester";
    public const string DepartmentManager = "DepartmentManager";
    public const string SoftwareStaff = "SoftwareStaff";
    public const string SystemStaff = "SystemStaff";
    public const string ItUnitManager = "ItUnitManager";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All =
        [Requester, DepartmentManager, SoftwareStaff, SystemStaff, ItUnitManager, Admin];
}
