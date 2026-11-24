using Microsoft.EntityFrameworkCore;
using TalepYonetim.Identity.Contracts;

namespace TalepYonetim.Identity.Infrastructure;

internal sealed class DepartmentDirectory(IdentityDbContext db) : IDepartmentDirectory
{
    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        db.Departments.CountAsync(cancellationToken);
}
