# Security

Her plan ve review'da uyulması zorunlu temel kurallar.

## Secrets
- Secrets never enter the repo, specs, prompts, or chat. `.env` is gitignored; provide `.env.example`.
- Agents never print secret values, even when debugging.
- Backend sırları (DB bağlantı dizesi, JWT imzalama anahtarı) geliştirmede `dotnet user-secrets` ya
  da ortam değişkeniyle verilir; `appsettings*.json` içinde gerçek sır bulunmaz.
- Demo kullanıcı parolaları yalnızca seed'de, yalnızca `Development` ortamında kullanılır.

## Input & output
- Tüm API girdileri sunucuda doğrulanır (FluentValidation ya da DataAnnotations; frontend'deki Zod
  doğrulaması yalnızca kullanıcı deneyimi içindir).
- HTML render edilmez: yorumlar, açıklamalar ve notlar düz metindir; React varsayılan kaçışıyla
  gösterilir, `dangerouslySetInnerHTML` yasaktır.
- Tüm sorgular EF Core üzerinden parametrelidir; ham SQL yalnızca parametreli ve review'da
  gerekçeli olabilir.
- Hata yanıtları ProblemDetails'tir; iç detay sızmaz (`docs/conventions.md`, Error handling).
- **Dosya yükleme:** izinli uzantılar `pdf, docx, xlsx, png, jpg, txt, zip`; uzantı ile içerik
  imzası (magic byte) birlikte doğrulanır; dosya başına en fazla 10 MB, talep başına en fazla 10
  dosya (Ayarlar'dan değiştirilebilir). Dosyalar web kökü dışında, rastgele adla saklanır.
  Orijinal ad yalnızca meta veri olarak tutulur. İndirme yalnızca yetki kontrolü yapan endpoint'ten,
  `Content-Disposition: attachment` ve `X-Content-Type-Options: nosniff` ile yapılır.

## AuthN / AuthZ
- **Kimlik doğrulama:** ASP.NET Core Identity ile yerel kullanıcılar; **JWT Bearer**.
  - Erişim token'ı kısa ömürlüdür (15 dk) ve frontend'de **yalnızca bellekte** tutulur;
    localStorage ve sessionStorage'a yazılmaz.
  - Refresh token `httpOnly` + `Secure` + `SameSite=Strict` cookie'de taşınır ve her kullanımda
    döndürülür (rotation). Yeniden kullanılan eski bir refresh token, o token ailesinin tamamını
    iptal eder. Refresh token'lar DB'de hash'li saklanır; çıkış yapınca iptal edilir.
  - Giriş endpoint'inde hız sınırı ve hesap kilitleme uygulanır.
  - Geliştirmede Vite proxy ile aynı origin kullanılır; üretimde CORS yalnızca tanımlı origin'e
    açıktır.
  - "Demo kullanıcı seç" yalnızca `Development` ortamında vardır.
  - SSO/AD v1 kapsamı dışındadır.
- **Yetkilendirme (hassas alan):**
  - Varsayılan ret: fallback policy her endpoint için kimlik doğrulama ister; anonim endpoint'ler
    (giriş, token yenileme) açıkça işaretlenir.
  - İzin bazlı policy'ler kullanılır (ör. `requests.approve`, `requests.assign`, `admin.users`).
    Rol → izin eşlemesi Kimlik modülündedir; endpoint'ler rol adı değil izin ister.
  - Kayıt bazlı erişim (BR-17), tüm sorgulara sunucuda tek bir görünürlük filtresiyle uygulanır.
    Talep Sahibi kendi taleplerini; Onaycı onayına gelen ve onayladığı talepleri; BT personeli
    biriminin kendisine atanmış taleplerini; BT Birim Yöneticisi biriminin tüm taleplerini; Admin
    her şeyi görür. Liste, detay, dashboard, rapor ve Excel çıktısı aynı filtreden geçer.
  - Dahili notlar yalnızca BT rollerine (SoftwareStaff, SystemStaff, ItUnitManager, Admin) döner.
  - Bir kaydın varlığı, görme yetkisi olmayana 404 ile gizlenir; görebilen ama işlem yetkisi
    olmayana 403 döner.
  - Frontend'de menü ve butonların gizlenmesi yalnızca kullanıcı deneyimi içindir, güvenlik
    sağlamaz.
- **Denetim:** onay, atama, durum, rol/izin, akış ve ayar değişiklikleri `AuditLog`'a aynı
  transaction'da yazılır (BR-14).

## Dependencies
- Her yeni paket, planın dosya listesinde adıyla ve gerekçesiyle yer alır ve plan onayıyla kabul
  edilir.
- İzinli lisanslar: MIT, Apache-2.0, BSD-2/3-Clause, ISC, 0BSD, BlueOak-1.0.0, PostgreSQL License;
  yalnızca dev/derleme zamanı için ayrıca MPL-2.0, Python-2.0, CC-BY-4.0, CC0-1.0 (ADR 0002).
  Diğer lisanslar ADR gerektirir.
- Bakımı yapılmayan paketler (son 12 ayda sürüm yok) gerekçe ister.
- full profilde `dotnet list package --vulnerable --include-transitive` ve
  `npm audit --audit-level=high` çalışır; bulgu varsa check kırmızıdır.

## Review lens
Security is a mandatory dimension of every independent review (see `prompts/review.md`), not a
separate afterthought phase.
