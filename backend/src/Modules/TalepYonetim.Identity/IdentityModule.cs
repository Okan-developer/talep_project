using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TalepYonetim.Identity.Application;
using TalepYonetim.Identity.Contracts;
using TalepYonetim.Identity.Endpoints;
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

        services.AddJwtAuthentication(configuration);
        services.AddAuthorization();
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddLoginRateLimit();

        services.AddScoped<DemoUserSeeder>();
        services.AddScoped<TokenService>();
        services.AddScoped<RefreshTokenStore>();
        services.AddScoped<SessionIssuer>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshHandler>();
        services.AddScoped<LogoutHandler>();
        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app, bool isDevelopment) =>
        app.MapAuthEndpoints(isDevelopment);

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

    private static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Section))
            .PostConfigure<IHostEnvironment>((options, environment) =>
            {
                // Zero-setup development: a per-process key (restarts drop only 15-minute access tokens).
                if (!options.HasUsableKey && environment.IsDevelopment())
                {
                    options.SigningKey = JwtOptions.NewEphemeralKey();
                }
            })
            .Validate(o => o.HasUsableKey, $"Jwt:SigningKey must be configured (at least {JwtOptions.MinimumKeyBytes} bytes) outside Development.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, TimeProvider>((bearer, jwt, timeProvider) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TimeProvider = timeProvider;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = jwt.Value.CreateSecurityKey(),
                    NameClaimType = TokenService.FullNameClaim,
                    RoleClaimType = TokenService.RoleClaim,
                    ClockSkew = TimeSpan.Zero,
                    // "now" comes from TimeProvider (docs/conventions.md, Data rules), not the token library's clock.
                    LifetimeValidator = (notBefore, expires, _, _) =>
                    {
                        var now = timeProvider.GetUtcNow().UtcDateTime;
                        return (notBefore is null || notBefore <= now) && expires is not null && now < expires;
                    },
                };
            });
    }

    // spec 0002, R-4: at most 10 login requests per client address per minute.
    private static void AddLoginRateLimit(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthEndpoints.LoginRateLimitPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.OnRejected = async (context, cancellationToken) =>
            {
                var problems = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context.HttpContext });
            };
        });

    private static void TryAddTimeProvider(this IServiceCollection services)
    {
        if (services.All(d => d.ServiceType != typeof(TimeProvider)))
        {
            services.AddSingleton(TimeProvider.System);
        }
    }
}
