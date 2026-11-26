using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalepYonetim.Identity.Contracts;
using TalepYonetim.Identity.Infrastructure;

namespace TalepYonetim.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(
            configuration.GetConnectionString("Default"),
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.Schema)));
        services.AddScoped<IDepartmentDirectory, DepartmentDirectory>();

        services.TryAddTimeProvider();
        services.AddAuthentication();
        services
            .AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                // spec 0002, R-4: 5 consecutive failures lock the account for 15 minutes
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddSignInManager();
        services.AddScoped<DemoUserSeeder>();
        return services;
    }

    /// <summary>Applies pending migrations, including the department and role seed (spec 0001, R-2; spec 0002, R-2).</summary>
    public static async Task MigrateIdentityModuleAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>Creates the demo users of spec 0002, R-2. The host calls it only in Development.</summary>
    public static async Task SeedIdentityDemoUsersAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DemoUserSeeder>().SeedAsync(cancellationToken);
    }

    private static void TryAddTimeProvider(this IServiceCollection services)
    {
        if (services.All(d => d.ServiceType != typeof(TimeProvider)))
        {
            services.AddSingleton(TimeProvider.System);
        }
    }
}
