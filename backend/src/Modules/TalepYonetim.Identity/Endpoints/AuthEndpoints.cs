using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using TalepYonetim.Identity.Application;
using TalepYonetim.Identity.Infrastructure;
using TalepYonetim.SharedKernel;

namespace TalepYonetim.Identity.Endpoints;

/// <summary>A demo user as the Development login screen lists it (spec 0002, R-5).</summary>
public sealed record DemoUserView(string FullName, string Email, string Role, string Password);

public static class AuthEndpoints
{
    public const string LoginRateLimitPolicy = "login";
    public const string RefreshCookie = "talep_refresh";
    private const string CookiePath = "/api/auth";
    private const string RequiredField = "Bu alan zorunludur.";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app, bool isDevelopment)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        auth.MapPost("/login", LoginAsync)
            .WithName("Login")
            .AllowAnonymous()
            .RequireRateLimiting(LoginRateLimitPolicy);

        auth.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshSession")
            .AllowAnonymous();

        auth.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .AllowAnonymous();

        auth.MapGet("/me", (ClaimsPrincipal principal) => TypedResults.Ok(CurrentUserQuery.From(principal)))
            .WithName("GetCurrentUser")
            .RequireAuthorization();

        // Outside Development the endpoint exists only to answer "not found" (spec 0002, R-5 / AC-1):
        // without it the fallback policy would answer 401 instead.
        auth.MapGet("/demo-users", () => DemoUsersList(isDevelopment))
            .WithName("GetDemoUsers")
            .AllowAnonymous();

        return app;
    }

    private static async Task<Results<Ok<AuthSession>, ValidationProblem, ProblemHttpResult>> LoginAsync(
        LoginInput input, LoginHandler handler, HttpContext http, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(input.Email))
        {
            errors["email"] = [RequiredField];
        }

        if (string.IsNullOrEmpty(input.Password))
        {
            errors["password"] = [RequiredField];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(
                errors,
                title: "Geçersiz istek",
                detail: "İstekteki bilgiler doğrulanamadı.",
                extensions: new Dictionary<string, object?> { [ProblemExtensions.Code] = "validation_failed" });
        }

        var result = await handler.HandleAsync(input, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(StatusCodes.Status401Unauthorized, "Giriş başarısız", result.Error!);
        }

        WriteRefreshCookie(http, result.Value);
        return TypedResults.Ok(result.Value.Session);
    }

    private static async Task<Results<Ok<AuthSession>, ProblemHttpResult>> RefreshAsync(
        RefreshHandler handler, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(http.Request.Cookies[RefreshCookie], cancellationToken);
        if (!result.IsSuccess)
        {
            ClearRefreshCookie(http);
            return Problem(StatusCodes.Status401Unauthorized, "Oturum sona erdi", result.Error!);
        }

        WriteRefreshCookie(http, result.Value);
        return TypedResults.Ok(result.Value.Session);
    }

    private static async Task<NoContent> LogoutAsync(LogoutHandler handler, HttpContext http, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(http.Request.Cookies[RefreshCookie], cancellationToken);
        ClearRefreshCookie(http);
        return TypedResults.NoContent();
    }

    private static Results<Ok<DemoUserView[]>, NotFound> DemoUsersList(bool isDevelopment) =>
        isDevelopment
            ? TypedResults.Ok(DemoUsers.All.Select(u => new DemoUserView(u.FullName, u.Email, u.Role, DemoUsers.Password)).ToArray())
            : TypedResults.NotFound();

    private static ProblemHttpResult Problem(int status, string title, Error error) =>
        TypedResults.Problem(
            statusCode: status,
            title: title,
            detail: error.Message,
            extensions: new Dictionary<string, object?> { [ProblemExtensions.Code] = error.Code });

    private static void WriteRefreshCookie(HttpContext http, IssuedSession issued) =>
        http.Response.Cookies.Append(RefreshCookie, issued.RefreshToken, CookieOptions(issued.RefreshExpiresAt));

    private static void ClearRefreshCookie(HttpContext http) =>
        http.Response.Cookies.Delete(RefreshCookie, CookieOptions(null));

    // docs/security.md, AuthN: httpOnly + Secure + SameSite=Strict, sent only to the auth endpoints.
    private static CookieOptions CookieOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = CookiePath,
        Expires = expires,
        IsEssential = true,
    };
}
