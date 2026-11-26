using Microsoft.EntityFrameworkCore;
using TalepYonetim.Identity.Domain;

namespace TalepYonetim.Identity.Infrastructure;

/// <summary>Outcome of presenting a refresh token.</summary>
public sealed record RotationOutcome(RefreshTokenUse Use, Guid? UserId, string? NewValue, DateTimeOffset? NewExpiresAt);

/// <summary>Persists refresh-token chains: issue, rotate (with reuse detection) and revoke (spec 0002, R-6 / R-7).</summary>
public sealed class RefreshTokenStore(IdentityDbContext db, TimeProvider timeProvider)
{
    public async Task<(string Value, DateTimeOffset ExpiresAt)> StartSessionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var value = TokenService.NewRefreshTokenValue();
        var token = RefreshToken.StartFamily(userId, TokenService.Hash(value), timeProvider.GetUtcNow());
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);
        return (value, token.ExpiresAt);
    }

    public async Task<RotationOutcome> RotateAsync(string value, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var token = await FindForUpdateAsync(value, cancellationToken);
        if (token is null)
        {
            return new RotationOutcome(RefreshTokenUse.Revoked, null, null, null);
        }

        var now = timeProvider.GetUtcNow();
        var use = token.Classify(now);
        if (use == RefreshTokenUse.Reused)
        {
            await RevokeFamilyAsync(token.FamilyId, now, cancellationToken);
        }

        if (use != RefreshTokenUse.Valid)
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new RotationOutcome(use, token.UserId, null, null);
        }

        var successorValue = TokenService.NewRefreshTokenValue();
        var successor = token.Rotate(TokenService.Hash(successorValue), now);
        db.RefreshTokens.Add(successor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new RotationOutcome(RefreshTokenUse.Valid, token.UserId, successorValue, successor.ExpiresAt);
    }

    /// <summary>Ends the session (the whole family) the token belongs to. An unknown token is ignored.</summary>
    public async Task RevokeSessionAsync(string value, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var token = await FindForUpdateAsync(value, cancellationToken);
        if (token is not null)
        {
            await RevokeFamilyAsync(token.FamilyId, timeProvider.GetUtcNow(), cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    // Row lock: two concurrent rotations of one token are serialised, so at most one succeeds.
    // Parameterised raw SQL (docs/security.md, Input & output): EF Core has no FOR UPDATE operator.
    private Task<RefreshToken?> FindForUpdateAsync(string value, CancellationToken cancellationToken)
    {
        var hash = TokenService.Hash(value);
        return db.RefreshTokens
            .FromSql($"SELECT * FROM identity.refresh_token WHERE token_hash = {hash} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var active = await db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var member in active)
        {
            member.Revoke(now);
        }
    }
}
