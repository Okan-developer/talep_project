namespace TalepYonetim.Identity.Domain;

/// <summary>What presenting a refresh token means (spec 0002, R-6 / AC-8).</summary>
public enum RefreshTokenUse
{
    /// <summary>Active and not expired: may be rotated.</summary>
    Valid,

    /// <summary>Already replaced by a newer token: presenting it again is reuse — the family ends.</summary>
    Reused,

    /// <summary>Revoked (logout or family revocation) without a successor.</summary>
    Revoked,

    /// <summary>Past its lifetime.</summary>
    Expired,
}

/// <summary>
/// One link of a refresh-token chain (docs/security.md, AuthN). Only the hash of the token value is
/// kept. Every use rotates the token; reuse of a rotated token revokes the whole family. The family
/// lives at most <see cref="Lifetime"/> from sign-in: a successor inherits the family's end, so
/// rotation never extends a session (spec 0002, R-6: "7 güne kadar").
/// </summary>
public sealed class RefreshToken
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private RefreshToken(Guid id, Guid userId, Guid familyId, string tokenHash, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid FamilyId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedById { get; private set; }

    public bool IsActive => RevokedAt is null;

    /// <summary>The first token of a new session (a new family).</summary>
    public static RefreshToken StartFamily(Guid userId, string tokenHash, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), userId, Guid.CreateVersion7(now), tokenHash, now, now + Lifetime);

    /// <summary>Classifies presenting this token at <paramref name="now"/>.</summary>
    public RefreshTokenUse Classify(DateTimeOffset now) =>
        ReplacedById is not null ? RefreshTokenUse.Reused
        : RevokedAt is not null ? RefreshTokenUse.Revoked
        : now >= ExpiresAt ? RefreshTokenUse.Expired
        : RefreshTokenUse.Valid;

    /// <summary>Replaces this valid token by its successor in the same family, ending when the family ends.</summary>
    public RefreshToken Rotate(string successorHash, DateTimeOffset now)
    {
        if (Classify(now) != RefreshTokenUse.Valid)
        {
            throw new InvalidOperationException("Only a valid refresh token can be rotated.");
        }

        var successor = new RefreshToken(Guid.CreateVersion7(now), UserId, FamilyId, successorHash, now, ExpiresAt);
        RevokedAt = now;
        ReplacedById = successor.Id;
        return successor;
    }

    /// <summary>Ends this token (logout, family revocation). Revoking twice keeps the first time.</summary>
    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}
