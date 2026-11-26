using Microsoft.AspNetCore.Identity;
using TalepYonetim.Identity.Domain;
using TalepYonetim.Identity.Infrastructure;
using TalepYonetim.SharedKernel;

namespace TalepYonetim.Identity.Application;

/// <summary>The signed-in user as the frontend sees it (spec 0002, R-10).</summary>
public sealed record CurrentUser(Guid Id, string FullName, string Email, IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);

/// <summary>What login and refresh return in the body; the refresh token travels only in a cookie.</summary>
public sealed record AuthSession(string AccessToken, DateTimeOffset ExpiresAt, CurrentUser User);

/// <summary>An issued session: the body plus the refresh-token value for the cookie.</summary>
public sealed record IssuedSession(AuthSession Session, string RefreshToken, DateTimeOffset RefreshExpiresAt);

public sealed record LoginInput(string? Email, string? Password);

/// <summary>Turkish business errors of authentication (docs/conventions.md, Error handling).</summary>
public static class AuthErrors
{
    public static readonly Error InvalidCredentials = new("invalid_credentials", "E-posta ya da parola hatalı.");

    public static readonly Error SessionExpired = new("session_expired", "Oturumunuz sona erdi. Lütfen tekrar giriş yapın.");

    public static Error Locked(int minutes) =>
        new("account_locked", $"Hesap geçici olarak kilitlendi. {minutes} dakika sonra tekrar deneyin.");
}

/// <summary>Spec 0002, R-3 / R-4: e-mail + password login with lockout.</summary>
public sealed class LoginHandler(
    UserManager<AppUser> users,
    SignInManager<AppUser> signIn,
    SessionIssuer issuer,
    TimeProvider timeProvider)
{
    private static readonly Lazy<string> DummyHash = new(() => new PasswordHasher<AppUser>().HashPassword(new AppUser(), "dummy-password"));

    public async Task<Result<IssuedSession>> HandleAsync(LoginInput input, CancellationToken cancellationToken)
    {
        var email = input.Email!.Trim();
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            // Same work as a real check, so an unknown e-mail is not told apart by timing either.
            users.PasswordHasher.VerifyHashedPassword(new AppUser(), DummyHash.Value, input.Password!);
            return Result.Failure<IssuedSession>(AuthErrors.InvalidCredentials);
        }

        var outcome = await signIn.CheckPasswordSignInAsync(user, input.Password!, lockoutOnFailure: true);
        if (outcome.IsLockedOut)
        {
            return Result.Failure<IssuedSession>(AuthErrors.Locked(await RemainingLockoutMinutesAsync(user)));
        }

        if (!outcome.Succeeded)
        {
            return Result.Failure<IssuedSession>(AuthErrors.InvalidCredentials);
        }

        return Result.Success(await issuer.StartAsync(user, cancellationToken));
    }

    private async Task<int> RemainingLockoutMinutesAsync(AppUser user)
    {
        var end = await users.GetLockoutEndDateAsync(user) ?? timeProvider.GetUtcNow();
        var remaining = end - timeProvider.GetUtcNow();
        return Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
    }
}

/// <summary>Builds the access token and the user view for a user, and starts or continues a session.</summary>
public sealed class SessionIssuer(UserManager<AppUser> users, TokenService tokens, RefreshTokenStore refreshTokens)
{
    public async Task<IssuedSession> StartAsync(AppUser user, CancellationToken cancellationToken)
    {
        var (refresh, refreshExpiresAt) = await refreshTokens.StartSessionAsync(user.Id, cancellationToken);
        return new IssuedSession(await CreateSessionAsync(user), refresh, refreshExpiresAt);
    }

    public async Task<AuthSession> CreateSessionAsync(AppUser user)
    {
        var roles = (await users.GetRolesAsync(user)).Order(StringComparer.Ordinal).ToArray();
        var (accessToken, expiresAt) = tokens.CreateAccessToken(user, roles);
        var view = new CurrentUser(user.Id, user.FullName, user.Email ?? string.Empty, roles, RolePermissions.For(roles));
        return new AuthSession(accessToken, expiresAt, view);
    }
}
