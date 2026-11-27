# Backlog — deferred findings

Findings a human triaged as **defer**: real, but fixed later by decision (`workflows/feature-development.md`,
step 7). Nothing lands here without that decision; noise is rejected in the triage record, not parked here.

- A criterion moved out at the ship gate (R-08, `→ moved to B-<n>`) lands here too, by the
  human's ship decision; the entry names the spec and criterion.
- One entry per finding, newest last. Delete the entry in the commit of the work that closes it.
- An entry that grows into new behavior becomes a spec (`specs/active/`), not a fix.

<!-- Format (labels stay English):
- **B-<n>** — <finding, one line> — Source: spec <NNNN>, review round <n> (<file:line>) — Severity: <…>
  — Recommendation: <the reviewer's action> — Deferred by: <name>, <YYYY-MM-DD>, <why>
  A decision deferred at bootstrap (its REPORT) uses Source: bootstrap <YYYY-MM-DD>.
-->

- **B-2** — Kişisel veri (KVKK) hassas alanı: gerçek kullanıcı verisine geçilmeden önce
  `docs/domain.md` Sensitive areas'a eklenmeli — Source: bootstrap 2026-11-20 — Severity: Medium
  — Recommendation: gerçek veriye geçiş spec'inden önce eklemek — Deferred by: proje sahibi,
  2026-11-20, ilk aşama demo veriyle çalışıyor
- **B-4** — AC-10 görünür odak için otomatik test yok; kanıt yalnızca `theme.css` `:focus-visible`
  kuralı ve ekran görüntüsü — Source: spec 0001, VERIFY G-2 (frontend/src/styles/theme.css) —
  Severity: Low — Recommendation: `theme-contrast.test.ts` tarzında `:focus-visible` kuralının
  varlığını ve halka/arka plan kontrastını (≥ 3:1) ölçen test — Deferred by: Okan Özkan,
  2026-11-23, SHIP'te ekran görüntüsüyle kanıtlanıyor
- **B-5** — Kontrast testi renk çifti listesi elle tutuluyor; yeni bir metin/arka plan çifti
  testten kaçabilir — Source: spec 0001, VERIFY G-3 (frontend/src/styles/theme-contrast.test.ts) —
  Severity: Info — Recommendation: review lens'e "yeni renk çifti → listeye ekle" maddesi —
  Deferred by: Okan Özkan, 2026-11-23, bugün hata yok; ilk yeni renk çiftinde ele alınır
- **B-6** — Bulunamadı sayfasında aynı metinle h1 ve h2 "Sayfa bulunamadı" (yinelenen başlık) —
  Source: spec 0001, VERIFY G-6 (frontend/src/app/pages/NotFoundPage.tsx) — Severity: Info —
  Recommendation: EmptyState başlığı bu sayfada başlık rolü taşımasın ya da farklı metin kullansın
  — Deferred by: Okan Özkan, 2026-11-23, üretim kodu değişikliği; sayfalar işlendiğinde ele alınır
- **B-7** — `refresh_token` tablosu temizlenmiyor; her yenileme yeni satır ekliyor — Source: spec 0002,
  review round 1 L-5 (backend/src/Modules/TalepYonetim.Identity/Infrastructure/RefreshTokenStore.cs) —
  Severity: Low — Recommendation: süresi 7 günden fazla geçmiş satırları silen periyodik temizlik —
  Deferred by: Okan Özkan, 2026-11-27, hiçbir kriteri etkilemiyor; üretime geçiş spec'inde ele alınır
- **B-8** — Login girdisi endpoint'te elle doğrulanıyor; docs/security.md FluentValidation ya da
  DataAnnotations diyor — Source: spec 0002, review round 1 I-3
  (backend/src/Modules/TalepYonetim.Identity/Endpoints/AuthEndpoints.cs) — Severity: Info —
  Recommendation: girdi alan bir sonraki uçla birlikte doğrulama mekanizmasına taşımak — Deferred by:
  Okan Özkan, 2026-11-27, doğrulama sunucuda var ve AC-3 karşılanıyor
