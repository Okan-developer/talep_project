# Spec 0002 — Kimlik ve yetki

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0002-plan.md`
- Self-critique: done

## Intent
Kurum çalışanları uygulamaya kendi hesaplarıyla girmeli ve yalnızca rollerinin izin verdiği menüleri
görmeli; BT ekibi de her API çağrısının varsayılan olarak kimlik doğrulama istediğinden emin olmak
istiyor. Başarı şöyle görünür: bir demo kullanıcıyla giriş yapılır, oturum 15 dakikalık erişim
süresinin ötesinde kullanıcı fark etmeden yenilenir, çıkışla oturum gerçekten kapanır, menü
kullanıcının rollerine göre süzülür, girişsiz istekler reddedilir, hata mesajları Türkçedir ve her
push ile PR'da GitHub'da tam doğrulama (full check + doctor --strict) otomatik koşar. Bilinçli olarak
yapılmayanlar: kullanıcı yönetimi ekranları (oluşturma, düzenleme, rol atama), parola sıfırlama ve
değiştirme, SSO/AD ve tüm talep/onay özellikleri. Netleştirme kararları (Q1–Q7, 2026-11-25) bu
spec'e işlenmiştir.

## Requirements
- R-1 **Kullanıcı ve rol.** Bir kullanıcının adı soyadı, e-postası, bir departmanı ve bir ya da
  daha fazla rolü vardır (`docs/domain.md`, Roller). Her kullanıcı aynı zamanda talep oluşturabilir.
- R-2 **Demo kullanıcılar (yalnızca Development).** Development ortamında boş bir veritabanı
  kurulduğunda aşağıdaki demo kullanıcılar oluşur; tekrar başlatmada çoğalmazlar. Development
  dışındaki ortamlarda hiçbir demo kullanıcı oluşmaz.

  | Kullanıcı (ad soyad) | E-posta | Departman | Rol(ler) |
  |---|---|---|---|
  | Talep Sahibi | `talep.sahibi@demo.local` | Muhasebe | Requester |
  | Birim Yöneticisi | `birim.yoneticisi@demo.local` | Muhasebe | DepartmentManager |
  | BT Yazılım Personeli | `bt.yazilim@demo.local` | BT Yazılım | SoftwareStaff |
  | BT Sistem Personeli | `bt.sistem@demo.local` | BT Sistem | SystemStaff |
  | BT Yazılım Birim Yöneticisi | `bt.yazilim.yonetici@demo.local` | BT Yazılım | ItUnitManager |
  | BT Sistem Birim Yöneticisi | `bt.sistem.yonetici@demo.local` | BT Sistem | ItUnitManager |
  | Sistem Yöneticisi | `admin@demo.local` | BT Sistem | Admin |

  Demo kullanıcıların parolası gerçek bir sır değildir ve yalnızca Development'ta geçerlidir.
- R-3 **Giriş.** Kullanıcı e-posta ve parolayla giriş yapar. E-posta büyük/küçük harf duyarsızdır
  ve baştaki/sondaki boşluklar yok sayılır. Yanlış parola ile kayıtlı olmayan e-posta aynı mesajı
  alır: "E-posta ya da parola hatalı."
- R-4 **Hesap kilitleme ve hız sınırı.** Bir hesap üst üste 5 başarısız denemeden sonra 15 dakika
  kilitlenir; kilit süresince doğru parola da kabul edilmez ve kullanıcı "Hesap geçici olarak
  kilitlendi. <N> dakika sonra tekrar deneyin." mesajını görür (N, kalan süre, yukarı yuvarlanmış
  dakika). Başarılı bir giriş başarısız deneme sayacını sıfırlar. Aynı istemci adresinden dakikada
  10'dan fazla giriş isteği "Çok fazla deneme yapıldı. Lütfen biraz sonra tekrar deneyin." ile
  reddedilir.
- R-5 **Demo kullanıcı seç (yalnızca Development).** Development'ta giriş ekranı, R-2'deki demo
  kullanıcıların listesini gösterir; birine tıklamak e-posta ve parola alanlarını doldurur ve normal
  girişle aynı yoldan oturum açar. Development dışında liste görünmez ve backend demo kullanıcı
  bilgisi vermez.
- R-6 **Oturum.** Erişim yetkisi 15 dakika geçerlidir ve tarayıcıda kalıcı depolamaya yazılmaz.
  Oturum, kullanıcı fark etmeden 7 güne kadar yenilenir; sayfa yenilendiğinde ya da yeni sekme
  açıldığında oturum yeniden giriş istemeden geri yüklenir. Her yenileme bir öncekini geçersiz
  kılar; geçersiz kılınmış bir yenileme yeniden kullanılırsa o oturum zincirinin tamamı sona erer.
  Aynı anda açılan birden fazla sekmenin oturumu geri yüklemesi oturumu sonlandırmaz.
- R-7 **Çıkış.** Üst bar, giriş yapan kullanıcının adını ve "Çıkış" butonunu gösterir. Çıkış o
  cihazdaki oturumu sunucuda da sonlandırır; aynı yenileme bir daha kullanılamaz. Diğer açık
  sekmeler bir sonraki sunucu isteğinde ya da yenilemede giriş ekranına döner.
- R-8 **Oturumun düşmesi.** Yenileme başarısız olduğunda (süre doldu, sonlandırıldı) kullanıcı
  "Oturumunuz sona erdi. Lütfen tekrar giriş yapın." mesajıyla giriş ekranına döner; girişten sonra
  kaldığı adrese yönlendirilir. Girişsiz biri herhangi bir uygulama adresini açarsa giriş ekranına
  yönlendirilir ve girişten sonra o adrese döner. Yönlendirme yalnızca uygulama içi adreslere yapılır.
- R-9 **Varsayılan ret.** Backend'in her işlemi kimlik doğrulama ister; anonim olan yalnızca giriş,
  oturum yenileme, çıkış ve (yalnızca Development'ta) demo kullanıcı listesidir. Sistem durumu bilgisi
  (spec 0001, R-3) artık kimlik doğrulama ister, ama özel bir izin istemez; içeriği değişmez.
- R-10 **İzinler.** Her rol bir izin kümesine eşlenir; bir kullanıcının izinleri rollerinin
  birleşimidir. Backend işlemleri rol adına değil izne göre yetkilendirir. Frontend, giriş yapan
  kullanıcının adını, rollerini ve izinlerini backend'den alır.
- R-11 **Rol bazlı menü.** Menü (spec 0001, R-4 sırası korunarak) kullanıcının izinlerine göre
  süzülür:

  | Menü öğesi | Görenler |
  |---|---|
  | Genel Bakış, Taleplerim, Yeni Talep Oluştur | tüm girişli kullanıcılar |
  | Onay Bekleyenler | DepartmentManager, ItUnitManager, Admin |
  | Birim Talepleri | ItUnitManager, Admin |
  | Bana Atanan İşler | SoftwareStaff, SystemStaff, ItUnitManager |
  | Tüm Talepler, Raporlar | ItUnitManager, Admin |
  | Kullanıcı Yönetimi, Kategori Yönetimi, Onay Akışı Yönetimi, Ayarlar | Admin |

  Çok rollü bir kullanıcı rollerinin birleşimini görür. Menüde görmediği bir adresi elle açan
  kullanıcı "Bu sayfayı görüntüleme yetkiniz yok" ekranını görür. Menü gizleme yalnızca kullanıcı
  deneyimidir; güvenlik backend'dedir (`docs/security.md`).
- R-12 **Türkçe hata yanıtları (backlog B-3).** Backend'in ürettiği her hata yanıtının başlığı ve
  açıklaması Türkçedir: doğrulama hatası, kimlik doğrulama gerekli, yetki yok, bulunamadı, çok
  fazla istek ve beklenmeyen hata. Yanıtlar iç detay (stack trace, bağlantı dizesi, istisna metni)
  içermez.
- R-13 **Sürekli entegrasyon (backlog B-1).** Her push ve PR'da GitHub, `./scripts/doctor --strict`
  ve `./scripts/check` (full profil: derleme, biçim, birim, mimari, entegrasyon testleri, frontend
  adımları ve bağımlılık güvenlik denetimi) çalıştırır. Bir adımın kırmızı olması PR'da başarısız
  kontrol olarak görünür. Bu spec'in PR'ındaki yeşil CI koşusu, `docs/testing.md` kural 6'daki full
  green kanıtıdır.

## User experience
- **Flow:** Kullanıcı uygulama adresini açar → girişsizse giriş ekranı görünür → (Development'ta)
  demo kullanıcı listesinden "BT Yazılım Personeli"ni seçer ya da e-posta ve parola girip "Giriş yap"a
  basar → kabuk açılır; üst barda adı ve "Çıkış" butonu, menüde yalnızca rolünün öğeleri görünür →
  sayfayı yeniler, oturum korunur → menüde olmayan `/yonetim/kullanicilar` adresini elle açar, "yetki
  yok" ekranını görür → "Çıkış"a basar, giriş ekranına döner.
- **States (giriş ekranı):**
  - Boş: e-posta ve parola alanları, "Giriş yap" butonu; Development'ta demo kullanıcı listesi.
  - Eksik alan: boş e-posta ya da parola ile gönderimde alanın altında Türkçe zorunlu alan mesajı;
    istek gönderilmez.
  - Gönderiliyor: buton devre dışı ve yükleniyor göstergesi; çift gönderim olmaz.
  - Hatalı bilgi: "E-posta ya da parola hatalı."; girilen e-posta korunur, parola temizlenir.
  - Kilitli hesap: "Hesap geçici olarak kilitlendi. <N> dakika sonra tekrar deneyin."
  - Hız sınırı: "Çok fazla deneme yapıldı. Lütfen biraz sonra tekrar deneyin."
  - Ağ hatası: API'ye ulaşılamıyor ya da 10 saniye içinde yanıt yok → Türkçe hata mesajı; form
    tekrar gönderilebilir.
  - Oturum sona erdi: ekranın üstünde "Oturumunuz sona erdi. Lütfen tekrar giriş yapın." bilgisi.
- **States (uygulama açılışı):** oturum geri yüklenirken tam sayfa yükleniyor göstergesi; giriş
  ekranı ya da korumalı içerik bu sırada yanıp sönmez.
- **States (yetki yok):** "Bu sayfayı görüntüleme yetkiniz yok" başlığı ve Genel Bakış'a dönüş
  bağlantısı; kabuk ve menü çalışmaya devam eder.
- **Design reference:** none — the flow above is the design. Giriş ekranı kabuk dışında, ortalanmış
  tek bir kart; spec 0001'in görsel yönü geçerlidir. Giriş ekranı kayıtlı temayı uygular (kayıt
  yoksa Sistem); giriş ekranında ayrı bir tema seçici yoktur.
- **Accessibility:** giriş formu yalnızca klavyeyle doldurulup Enter ile gönderilebilir; alanların
  görünür Türkçe etiketleri vardır; hata mesajları ilgili alana bağlıdır ve ekran okuyucuya duyurulur;
  demo kullanıcı listesi klavyeyle gezilir; odak görünür; metin kontrastı her iki temada en az 4.5:1.

## Constraints & out of scope
- Sensitive area: Yetkilendirme (`docs/domain.md`) — kimlik doğrulama, oturum yenileme, rol → izin
  eşlemesi. `docs/security.md` AuthN / AuthZ kuralları bu spec'in bağlayıcı kısıtlarıdır.
- Kapsam dışı: kullanıcı yönetimi ekranları (kullanıcı oluşturma, düzenleme, pasifleştirme, rol
  atama); parola sıfırlama ve değiştirme; "beni hatırla"; sekmeler arası anlık oturum senkronizasyonu
  (R-7'deki bir sonraki istek davranışı yeterlidir); tüm cihazlardan çıkış; SSO/AD; Development
  dışındaki ortamlarda ilk yönetici hesabının oluşturulması (üretime geçiş spec'i); kayıt bazlı veri
  görünürlüğü (BR-17 — talepler gelince); altyapı için anonim canlılık ucu; ters proxy arkasında
  gerçek istemci adresinin okunması (bu spec'te hız sınırının istemci adresi, backend'in gördüğü
  bağlantı adresidir — üretime geçiş spec'i); üst bardaki arama ve
  bildirim alanları; menü öğelerinin arkasındaki ekranlar ("hazırlanıyor" kalır); KVKK (B-2).
- Bilinçli kabul edilen risk (Q4, G5 — Okan Özkan, 2026-11-25): kilit mesajı yalnızca kayıtlı bir
  hesapta görünebildiği için hesabın varlığını ele verir; hız sınırı (R-4) denemeyi yavaşlatır.
  Review bunu bulgu olarak açmaz.
- Backlog: bu spec B-1'i (CI) ve B-3'ü (Türkçe ProblemDetails) kapatır; B-1'deki `chore/bootstrap`
  maddesi repo kurulduğunda geçersiz kalmıştır.
- Teknoloji ve sürümler: `docs/conventions.md`. Yeni paketler planın dosya listesinde gerekçeyle yer
  alır (`docs/security.md`, Dependencies).
- VERIFY: yetkilendirme kriterleri kodun kasıtlı kırılmasıyla da kanıtlanır (`prompts/verify.md`);
  bu yüzden lite tek geçişli review uygulanmaz.
- Split: proposed (0002a giriş + oturum + varsayılan ret · 0002b rol bazlı menü · CI ve B-3 ayrı
  chore); kept as one — Okan Özkan, 2026-11-25 (Q1): CI ve Türkçe hata yanıtları küçük, destekleyici
  işler; giriş ve menü filtreleme birbirini kanıtlar, ayrılırsa filtreleme kullanıcı olmadan test
  edilemez.

## Acceptance criteria
- [ ] AC-1 — Development'ta boş veritabanına karşı başlatılan backend R-2'deki 7 demo kullanıcıyı
  departman ve rolleriyle oluşturur; ikinci başlatmada sayı 7 kalır. Development dışında başlatılan
  backend hiçbir demo kullanıcı oluşturmaz ve demo kullanıcı listesi istendiğinde bulunamadı döner.
- [ ] AC-2 — Doğru e-posta ve parolayla giriş yapan kullanıcı kabuğa ulaşır; üst barda adı ve
  "Çıkış" görünür. E-posta farklı büyük/küçük harfle ya da baş/son boşlukla girildiğinde de giriş
  başarılıdır.
- [ ] AC-3 — Yanlış parola ve kayıtlı olmayan e-posta aynı yanıtı ve aynı "E-posta ya da parola
  hatalı." mesajını üretir; girilen e-posta korunur, parola temizlenir. Boş e-posta ya da parola ile
  gönderimde alanın altında Türkçe zorunlu alan mesajı görünür ve backend'e istek gitmez; backend de
  boş alanları Türkçe doğrulama hatasıyla reddeder.
- [ ] AC-4 — Üst üste 5 başarısız denemeden sonra 6. deneme (doğru parolayla da) kilit mesajını ve
  kalan dakikayı döner; 15 dakika sonra doğru parola kabul edilir. 4 başarısız denemeden sonraki
  başarılı giriş sayacı sıfırlar (ardından gelen 4 hatalı deneme kilitlemez).
- [ ] AC-5 — Aynı istemci adresinden bir dakika içinde 11. giriş isteği "Çok fazla deneme
  yapıldı…" mesajıyla reddedilir; 10. istek reddedilmez.
- [ ] AC-6 — Development'ta giriş ekranı 7 demo kullanıcıyı listeler; birine tıklamak o kullanıcıyla
  normal giriş yolundan oturum açar. Development dışında liste görünmez.
- [ ] AC-7 — Girişten sonra erişim yetkisi tarayıcının kalıcı depolamasında (localStorage,
  sessionStorage, okunabilir cookie) bulunmaz. 15 dakikayı aşan kullanımda kullanıcı yeniden giriş
  yapmadan çalışmaya devam eder; sayfa yenilendiğinde ve yeni sekmede oturum giriş istemeden geri
  yüklenir; iki sekme aynı anda açıldığında oturum sona ermez.
- [ ] AC-8 — Kullanılmış (yerine yenisi verilmiş) bir oturum yenilemesi tekrar kullanıldığında
  reddedilir ve o zincirin en yeni yenilemesi de geçersiz olur; 7 günü aşmış bir yenileme
  reddedilir.
- [ ] AC-9 — "Çıkış"tan sonra kullanıcı giriş ekranına döner; çıkıştan önceki yenileme tekrar
  kullanıldığında reddedilir; aynı oturumun açık diğer sekmesi bir sonraki sunucu isteğinde giriş
  ekranına döner.
- [ ] AC-10 — Yenileme başarısız olduğunda kullanıcı "Oturumunuz sona erdi…" mesajıyla giriş
  ekranına döner ve girişten sonra kaldığı adrese gider. Girişsiz açılan bir uygulama adresi giriş
  sonrası o adrese döner; uygulama dışı bir adrese yönlendirme yapılmaz (Genel Bakış'a gidilir).
  Oturum geri yüklenirken tam sayfa yükleniyor göstergesi görünür.
- [ ] AC-11 — Kimlik doğrulaması olmayan bir istek, giriş, oturum yenileme, çıkış ve (Development'ta)
  demo kullanıcı listesi dışındaki her backend işleminde Türkçe "kimlik doğrulama gerekli" yanıtıyla
  reddedilir; bu, sistem durumu bilgisi için de geçerlidir. Backend'e yeni bir işlem anonim
  işaretlenmeden eklendiğinde de varsayılan olarak kimlik doğrulama ister (otomatik testle).
- [ ] AC-12 — Girişli her kullanıcı, hangi rolde olursa olsun, sistem durumu bilgisini alır ve Genel
  Bakış'taki Sistem Durumu kartı spec 0001'deki gibi çalışır.
- [ ] AC-13 — Rol → izin eşlemesi otomatik testle doğrulanır; çok rollü bir kullanıcının izinleri
  rollerinin birleşimidir (otomatik testte oluşturulan iki rollü bir kullanıcıyla, ör.
  DepartmentManager + SoftwareStaff). Gereken izni olmayan girişli bir kullanıcının izin isteyen bir işleme
  isteği Türkçe "yetki yok" yanıtıyla reddedilir; izin kontrolü kasıtlı olarak kaldırıldığında
  bunu yakalayan test kırmızıya döner.
- [ ] AC-14 — R-2'deki 7 demo kullanıcının her biri için menü, R-11 tablosundaki öğeleri ve yalnızca
  onları, spec 0001 sırasıyla gösterir; çok rollü bir kullanıcı birleşimi görür (otomatik testle;
  demo kullanıcılar tek rollüdür).
- [ ] AC-15 — Menüsünde olmayan bir adresi elle açan girişli kullanıcı "Bu sayfayı görüntüleme
  yetkiniz yok" ekranını ve Genel Bakış'a dönüş bağlantısını görür; menü çalışmaya devam eder.
- [ ] AC-16 — Backend'in doğrulama, kimlik doğrulama gerekli, yetki yok, bulunamadı, çok fazla istek
  ve beklenmeyen hata yanıtlarının başlığı ve açıklaması Türkçedir ve iç detay içermez.
- [ ] AC-17 — Giriş ekranı klavyeyle doldurulup Enter ile gönderilir; alanların Türkçe etiketleri
  vardır; hata mesajları alana bağlı ve ekran okuyucuya duyurulur; demo listesi klavyeyle gezilir;
  odak görünür; giriş ekranındaki her metin/arka plan çifti iki temada da en az 4.5:1 kontrastlıdır.
- [ ] AC-18 — GitHub'da bu spec'in PR'ında `./scripts/doctor --strict` ve `./scripts/check` (full)
  koşar ve yeşil biter; `scripts/check`'in kırmızı olduğu bir commit'te CI kontrolü başarısız
  görünür (kanıt: geçici bir branch'e push edilen, kasıtlı kırılmış commit'in kırmızı CI koşusunun
  bağlantısı; branch merge edilmeden silinir).
- [ ] AC-19 — Giriş gönderilirken buton devre dışıdır ve ikinci istek gitmez; API'ye
  ulaşılamadığında ya da 10 saniye içinde yanıt gelmediğinde Türkçe hata mesajı görünür ve form
  tekrar gönderilebilir.

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
