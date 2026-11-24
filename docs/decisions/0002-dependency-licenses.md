# ADR 0002 — Bağımlılık lisansları: izinli listenin genişletilmesi ve NetArchTest.Rules

- Status: Accepted
- Date: 2026-11-23
- Revised: 2026-11-23 — NetArchTest.Rules 1.3.2 nuspec'inde lisans meta verisi yok; lisans
  upstream depodaki `LICENSE` dosyasından doğrulandı: MIT (github.com/BenMorris/NetArchTest).
  Karar değişmedi (review 0001 round 2 I-1).

## Context
`docs/security.md` (Dependencies) yalnızca MIT, Apache-2.0 ve BSD-2/3-Clause lisanslarına izin
veriyor; diğer her lisans bir ADR gerektiriyor. Ayrıca son 12 ayda sürüm çıkarmamış paketler
gerekçe istiyor. Spec 0001'in review'ı (round 1, M-2) bu kurala uymayan şu bağımlılıkları buldu:

- **Uygulamaya giren (runtime):**
  - `lucide-react` (ISC): ikon seti.
  - `tslib` (0BSD): geçişli bağımlılık.
  - `Npgsql.EntityFrameworkCore.PostgreSQL` (PostgreSQL License): EF Core sağlayıcısı.
- **Yalnızca geliştirme ve derleme zamanında kullanılan:**
  - ISC: semver, minimatch, `eslint-import-resolver-typescript` ve diğerleri.
  - BlueOak-1.0.0: graceful-fs, lru-cache ve diğerleri.
  - Python-2.0: argparse.
  - CC-BY-4.0: caniuse-lite (veri).
  - MPL-2.0: lightningcss (Tailwind'in CSS derleyicisi).
  - `(MIT OR CC0-1.0)`: type-fest.
- **Bakım:** `NetArchTest.Rules` 1.3.2 en son 2021'de yayımlandı.

Sorulması gereken şuydu: bu lisanslar bizim için MIT, Apache ve BSD'den daha fazla yükümlülük
getiriyor mu?

## Decision
1. **ISC, 0BSD, BlueOak-1.0.0 ve PostgreSQL License** izinli lisanslar listesine eklenir.
   Dördü de MIT/BSD ailesinden serbest lisanslardır: telif ve izin metninin korunması dışında
   yükümlülük getirmezler. Runtime ve dev bağımlılıklarında kullanılabilirler.
2. **Yalnızca derleme ve geliştirme zamanında** (dev bağımlılığı olarak) şu lisanslar kabul
   edilir: **MPL-2.0** (dosya düzeyinde copyleft; biz bu dosyaları değiştirmiyor ve dağıtmıyoruz,
   yalnızca derleme aracı olarak çalıştırıyoruz), **Python-2.0**, **CC-BY-4.0** (veri) ve
   **CC0-1.0**. Bu lisanslardan biri uygulama paketine (runtime) girerse yeni bir ADR gerekir.
3. **NetArchTest.Rules 1.3.2** bakım gerekçesiyle kabul edilir:
   - Yalnızca test projesinde kullanılır; üretim koduna girmez.
   - API'si küçük ve kararlıdır (Mono.Cecil üzerinden tip bağımlılığı okur).
   - Mimari testlerimiz .NET 10 derlemelerinde çalışıyor ve yeşil.
   - Bilinen bir güvenlik açığı yok (deps-audit).
4. Bu kararlar `docs/security.md` → Dependencies bölümünden bu ADR'ye bağlantıyla duyurulur.

## Consequences
- Kazanç: Yaygın ve serbest lisanslı paketler (lucide-react, Npgsql) her seferinde ayrı ADR
  istemeden kullanılabilir. Review'ın lisans denetimi için net bir liste oluşur.
- Bedel: İzinli liste uzar. "Yalnızca derleme zamanı" ayrımını review'da kontrol etmek gerekir;
  bir lisans denetim aracı bunu henüz otomatik yapmıyor.
- Bedel: NetArchTest.Rules bakımsız kalırsa (ör. yeni bir .NET sürümünde IL okuyamazsa) mimari
  testler kırılabilir. Bu durumda alternatife geçilir.

## Alternatives considered
- **lucide-react yerine MIT lisanslı bir ikon seti:** Brifte Lucide açıkça istenmiş; ISC lisansı
  MIT'e eşdeğer olduğu için değiştirmenin faydası yok.
- **Npgsql yerine başka bir sağlayıcı:** PostgreSQL için fiili standart sağlayıcı Npgsql'dir;
  PostgreSQL License da MIT benzeridir.
- **NetArchTest.eNhancedEdition** (bakımı süren bir çatal) ya da **ArchUnitNET**: İkisi de
  geçerli. Mevcut testler NetArchTest.Rules ile yeşil ve geçişin şu an somut bir faydası yok.
  Aşağıdaki tetikleyicilerden biri gerçekleşirse ilk tercih eNhancedEdition olur, çünkü API'si aynı.
- **Lisans istisnası yerine paket başına ADR:** Her ISC lisanslı geçişli bağımlılık için ADR
  yazmak orantısız bir yük getirir.

## Revisit triggers
- MPL, GPL, AGPL, SSPL gibi copyleft ya da ticari bir lisans runtime bağımlılığına girerse.
- NetArchTest.Rules yeni bir .NET sürümünde çalışmazsa ya da bir güvenlik açığı bildirilirse.
- Kurumun hukuk veya tedarik birimi lisans politikası yayımlarsa.
