using Shouldly;
using TalepYonetim.Api.Errors;

namespace TalepYonetim.UnitTests;

/// <summary>Spec 0002, AC-16 / R-12: the Turkish text of each error status.</summary>
public sealed class ErrorCatalogTests
{
    [Theory]
    [InlineData(400, "Geçersiz istek")]
    [InlineData(401, "Kimlik doğrulama gerekli")]
    [InlineData(403, "Yetki yok")]
    [InlineData(404, "Bulunamadı")]
    [InlineData(409, "Çakışma")]
    [InlineData(429, "Çok fazla istek")]
    [InlineData(500, "Beklenmeyen hata")]
    public void For_KnownStatus_ReturnsItsTurkishTitle(int status, string title)
    {
        var text = ErrorCatalog.For(status);

        text.Title.ShouldBe(title);
        text.Detail.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void For_TooManyRequests_UsesTheLoginThrottleMessageOfTheSpec()
    {
        ErrorCatalog.For(429).Detail.ShouldBe("Çok fazla deneme yapıldı. Lütfen biraz sonra tekrar deneyin.");
    }

    [Theory]
    [InlineData(502)]
    [InlineData(503)]
    public void For_OtherServerError_FallsBackToTheUnexpectedErrorText(int status)
    {
        ErrorCatalog.For(status).ShouldBe(ErrorCatalog.For(500));
    }

    [Theory]
    [InlineData(405)]
    [InlineData(415)]
    public void For_OtherClientError_ReturnsAGenericTurkishText(int status)
    {
        ErrorCatalog.For(status).Title.ShouldBe("İstek işlenemedi");
    }

    [Fact]
    public void EveryText_IsFreeOfTheFrameworkEnglishDefaults()
    {
        foreach (var status in new[] { 400, 401, 403, 404, 409, 429, 500, 405 })
        {
            var text = ErrorCatalog.For(status);
            foreach (var english in new[] { "Bad Request", "Unauthorized", "Forbidden", "Not Found", "error occurred" })
            {
                text.Title.ShouldNotContain(english, Case.Insensitive);
                text.Detail.ShouldNotContain(english, Case.Insensitive);
            }
        }
    }
}
