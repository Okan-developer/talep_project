using System.Text;
using Microsoft.EntityFrameworkCore;
using TalepYonetim.Identity.Domain;

namespace TalepYonetim.Identity.Infrastructure;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<
        AppUser, AppRole, Guid, AppUserClaim, AppUserRole, AppUserLogin, AppRoleClaim, AppUserToken, AppUserPasskey>(options)
{
    public const string Schema = "identity";

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        var modelBuilder = builder;
        base.OnModelCreating(modelBuilder);
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

        modelBuilder.Entity<AppUser>(user =>
        {
            user.ToTable("user");
            user.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            user.HasOne<Department>().WithMany().HasForeignKey(u => u.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<AppRole>(role =>
        {
            role.ToTable("role");
            role.HasData(SeedRoles.All);
        });
        modelBuilder.Entity<AppUserRole>().ToTable("user_role");
        modelBuilder.Entity<AppUserClaim>().ToTable("user_claim");
        modelBuilder.Entity<AppUserLogin>().ToTable("user_login");
        modelBuilder.Entity<AppRoleClaim>().ToTable("role_claim");
        modelBuilder.Entity<AppUserToken>().ToTable("user_token");
        if (modelBuilder.Model.FindEntityType(typeof(AppUserPasskey)) is not null)
        {
            modelBuilder.Entity<AppUserPasskey>().ToTable("user_passkey");
        }

        modelBuilder.Entity<RefreshToken>(token =>
        {
            token.ToTable("refresh_token");
            token.HasKey(t => t.Id);
            token.Property(t => t.Id).ValueGeneratedNever();
            token.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasIndex(t => t.FamilyId);
            token.Ignore(t => t.IsActive);
            token.HasOne<AppUser>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Tables and columns are snake_case (docs/conventions.md, Naming).
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    internal static string ToSnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]))))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
