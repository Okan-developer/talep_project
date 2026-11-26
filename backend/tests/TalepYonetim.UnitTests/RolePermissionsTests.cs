using Shouldly;
using TalepYonetim.Identity.Contracts;
using TalepYonetim.Identity.Domain;

namespace TalepYonetim.UnitTests;

/// <summary>Spec 0002, AC-13 (role → permission map) and the R-11 menu table it serves.</summary>
public sealed class RolePermissionsTests
{
    public static TheoryData<string, string[]> Table() => new()
    {
        { Roles.Requester, [] },
        { Roles.DepartmentManager, [Permissions.ApprovalsView] },
        { Roles.SoftwareStaff, [Permissions.AssignmentsViewOwn] },
        { Roles.SystemStaff, [Permissions.AssignmentsViewOwn] },
        {
            Roles.ItUnitManager,
            [Permissions.ApprovalsView, Permissions.RequestsViewUnit, Permissions.AssignmentsViewOwn, Permissions.RequestsViewAll, Permissions.ReportsView]
        },
        {
            Roles.Admin,
            [
                Permissions.ApprovalsView, Permissions.RequestsViewUnit, Permissions.RequestsViewAll, Permissions.ReportsView,
                Permissions.AdminUsers, Permissions.AdminCategories, Permissions.AdminWorkflows, Permissions.AdminSettings,
            ]
        },
    };

    [Theory]
    [MemberData(nameof(Table))]
    public void For_SingleRole_GrantsExactlyTheR11Permissions(string role, string[] expected)
    {
        RolePermissions.For([role]).ShouldBe(expected);
    }

    [Fact]
    public void For_EveryRoleOfTheDomain_HasAnEntry()
    {
        Table().Select(row => (string)row.Data.Item1).ShouldBe(Roles.All, ignoreOrder: true);
    }

    [Fact]
    public void For_TwoRoles_GrantsTheUnionInCanonicalOrder()
    {
        var permissions = RolePermissions.For([Roles.SoftwareStaff, Roles.DepartmentManager]);

        permissions.ShouldBe([Permissions.ApprovalsView, Permissions.AssignmentsViewOwn]);
    }

    [Fact]
    public void For_DuplicateRoles_GrantsEachPermissionOnce()
    {
        RolePermissions.For([Roles.DepartmentManager, Roles.DepartmentManager]).ShouldBe([Permissions.ApprovalsView]);
    }

    [Fact]
    public void For_UnknownOrDifferentlyCasedRole_GrantsNothing()
    {
        RolePermissions.For(["admin", "Root"]).ShouldBeEmpty();
    }

    [Fact]
    public void Admin_IsTheOnlyRoleWithAdministrationPermissions()
    {
        foreach (var role in Roles.All.Where(r => r != Roles.Admin))
        {
            RolePermissions.For([role]).ShouldNotContain(p => p.StartsWith("admin.", StringComparison.Ordinal), role);
        }
    }
}
