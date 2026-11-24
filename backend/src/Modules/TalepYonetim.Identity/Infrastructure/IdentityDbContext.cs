using Microsoft.EntityFrameworkCore;
using TalepYonetim.Identity.Domain;

namespace TalepYonetim.Identity.Infrastructure;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public const string Schema = "identity";

    public DbSet<Department> Departments => Set<Department>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Department>(department =>
        {
            department.ToTable("department");
            department.HasKey(d => d.Id);
            department.Property(d => d.Id).HasColumnName("id").ValueGeneratedNever();
            department.Property(d => d.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            department.HasIndex(d => d.Name).IsUnique();
            department.HasData(DemoDepartments.All);
        });
    }
}
