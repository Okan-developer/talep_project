using Microsoft.AspNetCore.Identity;
using TalepYonetim.Identity.Domain;
using TalepYonetim.Identity.Infrastructure;
using TalepYonetim.SharedKernel;

namespace TalepYonetim.Identity.Application;

/// <summary>Spec 0002, R-6: rotates the refresh token and issues a new access token.</summary>
public sealed class RefreshHandler(RefreshTokenStore refreshTokens, UserManager<AppUser> users, SessionIssuer issuer)
{
    public async Task<Result<IssuedSession>> HandleAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Result.Failure<IssuedSession>(AuthErrors.SessionExpired);
        }

        var outcome = await refreshTokens.RotateAsync(refreshToken, cancellationToken);
        if (outcome.Use != RefreshTokenUse.Valid)
        {
            return Result.Failure<IssuedSession>(AuthErrors.SessionExpired);
        }

        var user = await users.FindByIdAsync(outcome.UserId!.Value.ToString());
        if (user is null)
        {
            return Result.Failure<IssuedSession>(AuthErrors.SessionExpired);
        }

        var session = await issuer.CreateSessionAsync(user);
        return Result.Success(new IssuedSession(session, outcome.NewValue!, outcome.NewExpiresAt!.Value));
    }
}
