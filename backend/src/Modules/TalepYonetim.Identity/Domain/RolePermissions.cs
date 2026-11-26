using TalepYonetim.Identity.Contracts;

namespace TalepYonetim.Identity.Domain;

/// <summary>
/// Role → permission map (spec 0002, R-10 and the R-11 menu table). A user's permissions are the union
/// of their roles' permissions; an unknown role grants nothing.
/// </summary>
public static class RolePermissions
{
    private static readonly Dictionary<string, string[]> Map = new(StringComparer.Ordinal)
    {
        [Roles.Requester] = [],
        [Roles.DepartmentManager] = [Permissions.ApprovalsView],
        [Roles.SoftwareStaff] = [Permissions.AssignmentsViewOwn],
        [Roles.SystemStaff] = [Permissions.AssignmentsViewOwn],
        [Roles.ItUnitManager] =
        [
            Permissions.ApprovalsView, Permissions.RequestsViewUnit, Permissions.AssignmentsViewOwn,
            Permissions.RequestsViewAll, Permissions.ReportsView,
        ],
        [Roles.Admin] =
        [
            Permissions.ApprovalsView, Permissions.RequestsViewUnit, Permissions.RequestsViewAll,
            Permissions.ReportsView, Permissions.AdminUsers, Permissions.AdminCategories,
            Permissions.AdminWorkflows, Permissions.AdminSettings,
        ],
    };

    /// <summary>The union of the permissions of <paramref name="roles"/>, in <see cref="Permissions.All"/> order.</summary>
    public static IReadOnlyList<string> For(IEnumerable<string> roles)
    {
        var granted = roles
            .SelectMany(role => Map.TryGetValue(role, out var permissions) ? permissions : [])
            .ToHashSet(StringComparer.Ordinal);
        return Permissions.All.Where(granted.Contains).ToArray();
    }
}
