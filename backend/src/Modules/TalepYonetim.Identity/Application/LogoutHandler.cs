using TalepYonetim.Identity.Infrastructure;

namespace TalepYonetim.Identity.Application;

/// <summary>Spec 0002, R-7: logout ends this device's session on the server too.</summary>
public sealed class LogoutHandler(RefreshTokenStore refreshTokens)
{
    public async Task HandleAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await refreshTokens.RevokeSessionAsync(refreshToken, cancellationToken);
        }
    }
}
