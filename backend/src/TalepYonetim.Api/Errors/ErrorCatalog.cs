namespace TalepYonetim.Api.Errors;

/// <summary>A Turkish title and detail for one HTTP error status.</summary>
public sealed record ErrorText(string Title, string Detail);

/// <summary>
/// The single catalogue of Turkish texts for generic HTTP errors (docs/conventions.md, Error handling;
/// spec 0002, R-12). Business errors carry their own Turkish message (<c>SharedKernel.Error</c>).
/// </summary>
public static class ErrorCatalog
{
    private static readonly Dictionary<int, ErrorText> ByStatus = new()
    {
        [400] = new("Geçersiz istek", "İstekteki bilgiler doğrulanamadı."),
        [401] = new("Kimlik doğrulama gerekli", "Bu işlem için giriş yapmanız gerekiyor."),
        [403] = new("Yetki yok", "Bu işlem için yetkiniz yok."),
        [404] = new("Bulunamadı", "İstenen kaynak bulunamadı."),
        [409] = new("Çakışma", "İşlem, kaydın mevcut durumuyla çakışıyor."),
        [429] = new("Çok fazla istek", "Çok fazla deneme yapıldı. Lütfen biraz sonra tekrar deneyin."),
        [500] = new("Beklenmeyen hata", "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin."),
    };

    private static readonly ErrorText OtherClientError = new("İstek işlenemedi", "İstek işlenemedi.");

    /// <summary>The text for <paramref name="status"/>; unknown 4xx and 5xx fall back to a generic text.</summary>
    public static ErrorText For(int status) =>
        ByStatus.TryGetValue(status, out var text) ? text
        : status >= 500 ? ByStatus[500]
        : OtherClientError;

    /// <summary>The text of a field validation message: a required field is missing.</summary>
    public const string RequiredField = "Bu alan zorunludur.";
}
