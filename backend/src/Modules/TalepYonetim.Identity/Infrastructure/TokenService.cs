using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TalepYonetim.Identity.Infrastructure;

/// <summary>Creates access tokens and refresh-token values (docs/security.md, AuthN).</summary>
public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    public const string FullNameClaim = "name";
    public const string EmailClaim = "email";
    public const string RoleClaim = "role";

    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(AppUser user, IEnumerable<string> roles)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7(now).ToString()),
            new(FullNameClaim, user.FullName),
            new(EmailClaim, user.Email ?? string.Empty),
        };
        claims.AddRange(roles.Select(role => new Claim(RoleClaim, role)));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(_options.CreateSecurityKey(), SecurityAlgorithms.HmacSha256),
        };
        return (new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }

    /// <summary>A new random refresh-token value (256 bits, URL-safe).</summary>
    public static string NewRefreshTokenValue() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    /// <summary>Only this hash is stored (docs/security.md: refresh tokens are kept hashed).</summary>
    public static string Hash(string refreshTokenValue) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshTokenValue)));
}
