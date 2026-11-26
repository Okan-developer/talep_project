using Shouldly;
using TalepYonetim.Identity.Domain;

namespace TalepYonetim.UnitTests;

/// <summary>Spec 0002, R-6 / AC-8: rotation, reuse and lifetime of a refresh token.</summary>
public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 26, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.CreateVersion7(Start);

    [Fact]
    public void NewToken_IsValidAndExpiresAfterSevenDays()
    {
        var token = RefreshToken.StartFamily(UserId, "h1", Start);

        token.Classify(Start).ShouldBe(RefreshTokenUse.Valid);
        token.ExpiresAt.ShouldBe(Start.AddDays(7));
        token.Classify(Start.AddDays(7).AddSeconds(-1)).ShouldBe(RefreshTokenUse.Valid);
    }

    [Fact]
    public void Token_AtOrPastSevenDays_IsExpired()
    {
        var token = RefreshToken.StartFamily(UserId, "h1", Start);

        token.Classify(Start.AddDays(7)).ShouldBe(RefreshTokenUse.Expired);
        token.Classify(Start.AddDays(8)).ShouldBe(RefreshTokenUse.Expired);
    }

    [Fact]
    public void Rotate_GivesASuccessorInTheSameFamilyAndMakesTheOldTokenReused()
    {
        var token = RefreshToken.StartFamily(UserId, "h1", Start);
        var later = Start.AddMinutes(20);

        var successor = token.Rotate("h2", later);

        successor.FamilyId.ShouldBe(token.FamilyId);
        successor.UserId.ShouldBe(UserId);
        successor.Classify(later).ShouldBe(RefreshTokenUse.Valid);
        successor.ExpiresAt.ShouldBe(later.AddDays(7));
        token.ReplacedById.ShouldBe(successor.Id);
        token.Classify(later).ShouldBe(RefreshTokenUse.Reused);
    }

    [Fact]
    public void Rotate_OfAnAlreadyRotatedToken_IsRefused()
    {
        var token = RefreshToken.StartFamily(UserId, "h1", Start);
        token.Rotate("h2", Start);

        Should.Throw<InvalidOperationException>(() => token.Rotate("h3", Start));
    }

    [Fact]
    public void Rotate_OfAnExpiredToken_IsRefused()
    {
        var token = RefreshToken.StartFamily(UserId, "h1", Start);

        Should.Throw<InvalidOperationException>(() => token.Rotate("h2", Start.AddDays(7)));
    }

    [Fact]
    public void Revoke_WithoutSuccessor_IsRevokedNotReused()
    {
        var token = RefreshToken.StartFamily(UserId, "h1", Start);

        token.Revoke(Start.AddMinutes(1));
        token.Revoke(Start.AddMinutes(5));

        token.Classify(Start.AddMinutes(2)).ShouldBe(RefreshTokenUse.Revoked);
        token.RevokedAt.ShouldBe(Start.AddMinutes(1));
        token.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Revoke_OfARotatedToken_StillReportsReuse()
    {
        var token = RefreshToken.StartFamily(UserId, "h1", Start);
        token.Rotate("h2", Start);

        token.Revoke(Start.AddMinutes(1));

        token.Classify(Start.AddMinutes(1)).ShouldBe(RefreshTokenUse.Reused);
    }

    [Fact]
    public void TwoFamilies_HaveDifferentIds()
    {
        RefreshToken.StartFamily(UserId, "a", Start).FamilyId
            .ShouldNotBe(RefreshToken.StartFamily(UserId, "b", Start).FamilyId);
    }
}
