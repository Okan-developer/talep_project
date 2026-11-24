# Conventions

Yalnızca gerçek kurallar: her kural ya araçlarla zorlanır (tercih edilen) ya da review'da kontrol
edilir.

## Language
<!-- Set at bootstrap (step 0) and mirrored in AGENTS.md. Changing it later is allowed — edit both
     places; existing documents are not translated retroactively. -->
- Chat language (interviews, sessions): Türkçe (`tr`)
- Document language (docs/, specs, plans, ADRs, review and verify reports): Türkçe (`tr`)
- Always English (protocol, not prose): ANEW core files, template headings and field labels,
  `Status:` values (Draft / Approved / In progress / Shipped), `Approved by / on:`, `Source:`.
- Code identifiers, branch names, commit messages: İngilizce. Veritabanı tablo ve kolon adları da
  İngilizcedir.
- Kullanıcıya görünen tüm metinler Türkçedir. Frontend metinleri `frontend/src/shared/i18n`
  altındaki tek sözlükte tutulur; bileşenlere gömülmez. Backend hata mesajları da Türkçedir ve
  tek bir mesaj kataloğundan gelir.
- Agent messages (handoff summaries, refusals, `Next:` lines): chat language; command names, file
  paths and protocol fields verbatim.

## Language & framework versions
Kesin sürümler walking skeleton'da lockfile'larla (`packages.lock.json`, `package-lock.json`) ve
`global.json` ile sabitlenir.

- Backend: .NET 10 SDK (LTS, `global.json` ile sabit) · ASP.NET Core 10 · EF Core 10 + Npgsql ·
  PostgreSQL 17.
- Frontend: Node 24 LTS · npm · React 19 · TypeScript 5.9 (`strict: true`; TS 7 typescript-eslint ile henüz
  uyumsuz — plan 0001) · Vite · React Router ·
  TanStack Query · TanStack Table · React Hook Form + Zod · Tailwind CSS 4 · shadcn/ui · Lucide ·
  Recharts.
- Excel dışa aktarımı backend'de yapılır (ClosedXML).

## Naming
- C#: tipler, metotlar ve özellikler PascalCase; yerel değişkenler ve parametreler camelCase; arayüzler
  `I` önekiyle başlar. Klasör ve isim alanı yapısı: `TalepYonetim.<Module>.<Layer>`.
- TypeScript: değişken ve fonksiyonlar camelCase; tipler ve bileşenler PascalCase; bileşen
  dosyaları `PascalCase.tsx`; hook'lar `useX.ts`.
- Veritabanı: tablo ve kolonlar snake_case; her modülün kendi şeması vardır (ör. `requests.request`).
  Enum'lar string olarak saklanır.
- Testler: `<Sınıf/Bileşen>Tests` / `*.test.tsx`; test adları davranışı anlatır
  (`Approve_ByRequester_IsRejected`).

## Error handling
- Beklenen iş hataları (`Result` / `Error` tipi) değer olarak döner; beklenmeyen durumlar exception
  olarak fırlatılır ve global handler'da yakalanır.
- API her hatayı **RFC 9457 ProblemDetails** olarak döndürür: Türkçe `title`/`detail`, alan bazlı
  doğrulama hataları (`errors`) ve `traceId`. Eşleme: doğrulama → 400, kimlik doğrulama yok → 401,
  yetki yok → 403, bulunamadı → 404, durum/sıra/eşzamanlılık çakışması → 409.
- Stack trace, SQL, iç tip adları ve sistem yolları asla yanıta yazılmaz; yalnızca loglanır.
- Frontend: alan hataları form alanının altında, diğer hatalar toast ile gösterilir. Her veri
  ekranının boş, yükleniyor ve hata durumu vardır.

## Data rules
- Zaman: DB'de `timestamptz` (UTC); kodda `DateTimeOffset` kullanılır ve "şimdi" yalnızca
  `TimeProvider`'dan alınır. Ekranda `Europe/Istanbul` saat dilimi ve `tr-TR` biçimi kullanılır.
- Kimlikler: UUID v7. Talep No ayrı bir anahtardır; yıl bazlı bir DB sequence'tan atomik olarak
  üretilir (BR-13).
- Eşzamanlılık: `Request` üzerinde iyimser eşzamanlılık (PostgreSQL `xmin`) kullanılır.
  `RequestApproval` için (talep, adım sırası) unique kısıtı vardır. Çakışma 409 döner (BR-8).
- SLA ve gecikme hesabı takvim günüyle yapılır.
- Durum değişiklikleri doğrudan alan atamasıyla yapılmaz; yalnızca domain metotlarıyla yapılır
  (geçiş tablosu bu metotlarda doğrulanır).

## Known traps
<!-- "<trap> — <what to do instead> — from <spec/finding>" -->
- Türkçe büyük/küçük harf (`I/ı/İ/i`): `ToUpper()`/`ToLower()` ve kültüre duyarlı karşılaştırmalar
  kod, enum ve anahtarları bozar — teknik karşılaştırmalarda `ToUpperInvariant` ve
  `StringComparison.Ordinal` kullanın; Türkçe arama/sıralamada `tr-TR` kültürünü bilinçli olarak
  seçin — from bootstrap.
- Npgsql `timestamptz` yalnızca UTC kabul eder — `DateTimeOffset` ya da `DateTime` (Kind=Utc)
  kullanın; `DateTime.Now` kullanmayın, saati `TimeProvider`'dan alın — from bootstrap.
- Vitest varsayılan olarak izleme (watch) modunda açılır — check içinde her zaman `vitest run`
  kullanın — from bootstrap.
- dotnet build sunucuları ve MSBuild node'ları arkada açık kalır — `--disable-build-servers` ve
  `-nodeReuse:false` kullanın (check.conf) — from bootstrap.
- Windows'ta CRLF ve çalıştırma bitleri — `.gitattributes`'a uyun, `scripts/*` dosyalarını
  `git add --chmod=+x` ile ekleyin — from bootstrap.
- `dotnet format` her koşuda örtük ve kilitsiz bir restore yapar; bayat bir `packages.lock.json`'u
  sessizce yeniden yazar — check'te önce kilitli restore'lu build (`-p:RestoreLockedMode=true`),
  ardından `dotnet format --no-restore` koşun — from review 0001 round 2 L-1.

## Enforced by tooling
Walking skeleton bu araçları `scripts/check`'e bağlar:
- C#: `Nullable` etkin, `TreatWarningsAsErrors`, .NET analizörleri, `dotnet format --verify-no-changes`.
- Modül sınırları: NetArchTest mimari testleri (`docs/architecture.md`, Forbidden dependencies).
- TypeScript: `tsc --noEmit` (strict), ESLint (`import/no-restricted-paths` ile feature izolasyonu),
  Prettier kontrolü.
