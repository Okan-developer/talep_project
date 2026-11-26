using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TalepYonetim.Identity.Infrastructure;

/// <summary>
/// Access-token settings (section <c>Jwt</c>). The signing key never lives in the repo
/// (docs/security.md, Secrets): it comes from user-secrets or the environment; in Development a
/// missing key is replaced by a random one per process.
/// </summary>
public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "TalepYonetim";

    public string Audience { get; set; } = "TalepYonetim";

    /// <summary>Spec 0002, R-6: 15 minutes.</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    public string? SigningKey { get; set; }

    internal const int MinimumKeyBytes = 32;

    internal bool HasUsableKey => SigningKey is not null && Encoding.UTF8.GetByteCount(SigningKey) >= MinimumKeyBytes;

    internal SymmetricSecurityKey CreateSecurityKey() => new(Encoding.UTF8.GetBytes(SigningKey!));

    internal static string NewEphemeralKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
