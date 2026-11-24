# Spec 0001 — Walking skeleton

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0001-plan.md`
- Self-critique: done

## Intent
Geliştirme ekibi, ilk iş özelliğinden önce iki projenin (frontend ve backend) veritabanıyla birlikte
uçtan uca çalıştığını görmek ve tek doğrulama komutunun (`scripts/check`) gerçekten bir şey
kanıtladığından emin olmak istiyor. Başarı şöyle görünür: uygulama açıldığında kurumsal kabuk
(sidebar, üst bar, açık/koyu tema) görünür; Genel Bakış ekranındaki "Sistem Durumu" kartı
backend'in ve veritabanının durumunu ve tanımlı departman sayısını gösterir; `scripts/check` her iki
projeyi derler, test eder ve yeşil yanar. Bilinçli olarak yapılmayanlar: giriş/kimlik doğrulama,
kullanıcı ve roller, rol bazlı menü filtreleme (spec 0002) ve tüm talep, onay ve rapor
özellikleri. Netleştirme kararları (Q1–Q7, 2026-11-20) bu spec'e işlenmiştir.

## Requirements
- R-1 Uygulama, `docs/architecture.md`'de tanımlanan iki ayrı projeden oluşur: backend
  (modüler monolit) ve frontend (tek sayfalı uygulama). Frontend backend'e yalnızca API üzerinden
  erişir.
- R-2 Kimlik modülü bu dilimde yalnızca Departman kavramını taşır. Boş bir veritabanı ilk
  kurulduğunda 6 demo departmanı oluşur: İnsan Kaynakları, Muhasebe, Finans, Operasyon,
  BT Yazılım, BT Sistem. "Kurulum", backend başlarken bekleyen veritabanı değişikliklerinin ve
  demo verisinin uygulanmasıdır; backend tekrar başlatıldığında departmanlar çoğalmaz.
- R-3 Backend, kimlik doğrulama gerektirmeyen bir sistem durumu bilgisi sunar: API'nin çalıştığı,
  veritabanının erişilebilir olup olmadığı ve tanımlı departman sayısı. Bu bilgi iç detay
  (bağlantı dizesi, hata metni, stack trace, sunucu adı) içermez. Veritabanına bağlanılabildiği
  ama beklenen yapının bulunmadığı durum da "erişilemiyor" sayılır.
- R-4 Frontend kabuğu: solda 12 menü öğeli, daraltılabilir sidebar; üstte tema seçicili üst bar;
  içerik alanı. Menü öğeleri sırasıyla: Genel Bakış, Taleplerim, Yeni Talep Oluştur, Onay
  Bekleyenler, Birim Talepleri, Bana Atanan İşler, Tüm Talepler, Raporlar, Kullanıcı Yönetimi,
  Kategori Yönetimi, Onay Akışı Yönetimi, Ayarlar. Her öğenin bir ikonu vardır.
- R-5 Genel Bakış ekranı "Sistem Durumu" kartını gösterir. Diğer menü öğeleri, kendi başlıklarıyla
  "Bu ekran hazırlanıyor" boş durum sayfasına gider.
- R-6 Tema: Açık / Koyu / Sistem. Seçim tarayıcıda hatırlanır; ilk açılışta varsayılan Sistem'dir.
- R-7 Sidebar masaüstünde yalnızca ikon gösterecek şekilde daraltılabilir ve bu tercih hatırlanır;
  768 px altındaki ekranlarda sidebar bir çekmecedir (drawer).
- R-8 Kullanıcıya görünen tüm metinler Türkçedir ve frontend metin sözlüğünden gelir.
- R-9 Mimari sınırlar (`docs/architecture.md`, Forbidden dependencies 1, 2, 3 ve 6) otomatik olarak
  doğrulanır; ihlal `scripts/check`'i kırmızı yapar.
- R-10 `scripts/check.conf` içinde planlanan adımlar etkinleştirilir. Fast ve full profiller
  çalışır ve check arkada hiçbir süreç ya da veri bırakmaz (`docs/testing.md`, Resource hygiene).
  Full profil bağımlılık güvenlik denetimini de içerir; giderilemeyen yüksek seviye bir bulgu
  BUILD'i durdurur ve insana sorulur.
- R-11 Bir geliştirici, Türkçe README'deki adımlarla PostgreSQL'i (Docker), backend'i ve
  frontend'i yerelde ayağa kaldırabilir.

## User experience
- **Flow:** Kullanıcı uygulama adresini açar → kabuk ve Genel Bakış görünür → Sistem Durumu kartı
  yüklenir → kullanıcı menüden başka bir öğe seçer → o öğenin "hazırlanıyor" sayfası açılır →
  tema seçiciden Koyu'yu seçer → tüm arayüz koyu temaya geçer → sidebar'ı daraltır.
- **States (Sistem Durumu kartı):**
  - Yükleniyor: yanıt gelene kadar yükleniyor göstergesi (iskelet).
  - Başarılı: "API: Çalışıyor", "Veritabanı: Erişilebilir", "Departman sayısı: 6"; renk kodlu
    durum göstergeleri (yeşil).
  - Kısmi hata: API çalışıyor, veritabanına erişilemiyor → "Veritabanı: Erişilemiyor" (kırmızı),
    departman sayısı "—".
  - Hata: API'ye ulaşılamıyor ya da 10 saniye içinde yanıt yok → Türkçe hata mesajı ve
    "Tekrar dene" butonu; kabuk ve menü çalışmaya devam eder.
  - Boş: veritabanı erişilebilir ama departman yok → "Tanımlı departman yok".
- **States (diğer ekranlar):** menü öğesi sayfaları → "Bu ekran hazırlanıyor" boş durumu; bilinmeyen
  adres → "Sayfa bulunamadı" ve Genel Bakış'a dönüş bağlantısı. Yetki yok durumu bu dilimde
  yoktur (kimlik doğrulama spec 0002).
- **Design reference:** none — the flow above is the design. Görsel yön: nötr gri tonlar, mavi
  vurgu rengi, shadcn/ui bileşenleri, Lucide ikonları; Jira/Linear tarzı sade kurumsal görünüm
  (kopya değil).
- **Accessibility:** tüm menü öğeleri ve kontroller yalnızca klavyeyle kullanılabilir (Tab sırası
  sidebar → üst bar → içerik); odak görünür; ikon butonlarının ve daraltılmış menü ikonlarının
  Türkçe erişilebilir adları vardır; çekmece Esc ile kapanır; metin kontrastı her iki temada en
  az WCAG AA (4.5:1).

## Constraints & out of scope
- Hassas alana dokunmaz: kimlik doğrulama yoktur; sistem durumu bilgisi bu dilimde anonimdir ve
  yalnızca R-3'teki alanları içerir. Spec 0002 varsayılan-ret yetkilendirmeyi getirdiğinde bu
  bilginin anonim kalıp kalmayacağına karar verir.
- Kapsam dışı: giriş ekranı, JWT, kullanıcı, rol, izin (spec 0002); rol bazlı menü filtreleme;
  üst bardaki arama, bildirim ve profil alanları; talep, onay, rapor ve yönetim ekranları; sistem
  durumunun otomatik yenilenmesi (yalnızca sayfa açılışı ve "Tekrar dene"); kartta son kontrol
  zamanı; CI yapılandırması
  (backlog B-1).
- Teknoloji ve sürümler: `docs/conventions.md`. Yeni paketler planın dosya listesinde gerekçeyle yer
  alır (`docs/security.md`, Dependencies).
- Split: proposed (backend skeleton / frontend shell); kept as one — önerilen, çünkü bu dilimin tek
  amacı uçtan uca bağlantıyı kanıtlamak; bölünürse hiçbir parça tek başına bunu kanıtlayamaz.

## Acceptance criteria
- [ ] AC-1 — Uygulama açıldığında sidebar 12 menü öğesini R-4'teki sırayla, Türkçe adları ve
  ikonlarıyla gösterir; varsayılan sayfa Genel Bakış'tır ve aktif menü öğesi vurgulanır.
- [ ] AC-2 — Genel Bakış dışındaki bir menü öğesi seçildiğinde, o öğenin adını başlık olarak
  taşıyan "Bu ekran hazırlanıyor" sayfası açılır ve öğe aktif olarak vurgulanır; bilinmeyen bir
  adres "Sayfa bulunamadı" sayfasını ve Genel Bakış'a dönüş bağlantısını gösterir.
- [ ] AC-3 — Sistem Durumu, yanıt beklenirken yükleniyor durumunu; başarılı yanıtta "API:
  Çalışıyor", "Veritabanı: Erişilebilir" ve gerçek departman sayısını gösterir (demo veriyle 6).
- [ ] AC-4 — API'ye ulaşılamadığında ya da 10 saniye içinde yanıt gelmediğinde kart Türkçe hata
  mesajı ve "Tekrar dene" butonu gösterir; buton yeniden dener ve başarıda başarılı durum görünür;
  bu sırada menü ve tema çalışmaya devam eder.
- [ ] AC-5 — Veritabanına erişilemediğinde (ya da bağlanılıp beklenen yapı bulunamadığında) backend'in durum yanıtı API'nin çalıştığını ve
  veritabanının erişilemediğini bildirir ve iç detay (bağlantı dizesi, hata metni, stack trace,
  sunucu adı) içermez; kart "Veritabanı: Erişilemiyor" ve departman sayısı "—" gösterir.
- [ ] AC-6 — Veritabanı erişilebilir ve departman yoksa kart "Tanımlı departman yok" gösterir.
- [ ] AC-7 — Boş bir veritabanına karşı başlatılan backend 6 demo departmanını R-2'deki adlarla
  oluşturur; backend aynı veritabanına karşı ikinci kez başlatıldığında departman sayısı 6 kalır.
- [ ] AC-8 — Tema seçici Açık / Koyu / Sistem sunar; ilk açılış Sistem'dir ve işletim sistemi
  tercihini izler; seçilen tema sayfa yenilendikten sonra korunur.
- [ ] AC-9 — 768 px ve üzerinde sidebar daraltıldığında yalnızca ikonlar görünür ve her ikonun
  erişilebilir Türkçe adı vardır; tercih sayfa yenilendikten sonra korunur. 768 px altında
  sidebar menü butonuyla açılan bir çekmecedir; bir öğe seçildiğinde ya da Esc'e basıldığında
  kapanır.
- [ ] AC-10 — Tüm menü öğeleri, tema seçici ve "Tekrar dene" butonu yalnızca klavyeyle kullanılabilir
  ve odak görünürdür; sayfa dili `tr` olarak işaretlenir; tema renklerindeki her metin/arka plan
  çifti iki temada da en az 4.5:1 kontrast oranına sahiptir.
- [ ] AC-11 — `docs/architecture.md` yasak bağımlılık kuralları 1, 2, 3 ve 6'dan herhangi birinin
  kasıtlı ihlali `scripts/check`'i kırmızı yapar; ihlal yokken yeşildir.
- [ ] AC-12 — `scripts/check` (full) backend derleme, biçim, birim, mimari ve entegrasyon
  testlerini, frontend lint, tip kontrolü, testler ve derlemeyi ve bağımlılık güvenlik denetimini
  çalıştırır ve yeşil biter;
  `scripts/check --fast` entegrasyon testleri ve bağımlılık güvenlik denetimi dışındaki adımları
  çalıştırır ve yeşil biter. (R-08, review round 1 M-1 — Okan Özkan, 2026-11-23: bağımlılık
  denetimi ağ istediği için yalnızca full profildedir, plan 0001 Risks.)
- [ ] AC-13 — README'deki Türkçe adımlar, repo dışındaki geçici ve temiz bir kopyada (derleme
  çıktısı, `node_modules` ve mevcut veritabanı olmadan) izlendiğinde uygulama açılır ve AC-3'teki
  başarılı durum görünür.

## Definition of Done
- [ ] Every acceptance criterion mapped to proof (test or reproducible observation)
- [ ] `scripts/check` green
- [ ] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [ ] Docs / ADRs updated if behavior or architecture changed
- [ ] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | |
| Fix rounds | |
| Review findings: real / noise / deferred | |
| Regressions introduced | |
| Bugs escaped to production | |
