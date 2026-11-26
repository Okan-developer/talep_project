namespace TalepYonetim.Identity.Contracts;

/// <summary>
/// Permission names (docs/security.md, AuthZ: endpoints ask for a permission, never a role). The
/// role → permission map lives in the Identity module; later specs add their permissions here.
/// </summary>
public static class Permissions
{
    public const string ApprovalsView = "approvals.view";
    public const string RequestsViewUnit = "requests.view_unit";
    public const string AssignmentsViewOwn = "assignments.view_own";
    public const string RequestsViewAll = "requests.view_all";
    public const string ReportsView = "reports.view";
    public const string AdminUsers = "admin.users";
    public const string AdminCategories = "admin.categories";
    public const string AdminWorkflows = "admin.workflows";
    public const string AdminSettings = "admin.settings";

    public static readonly IReadOnlyList<string> All =
    [
        ApprovalsView, RequestsViewUnit, AssignmentsViewOwn, RequestsViewAll, ReportsView,
        AdminUsers, AdminCategories, AdminWorkflows, AdminSettings,
    ];
}
