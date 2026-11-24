# Domain

Bu dosya iş birimi, insanlar ve ajanlar arasındaki ortak dili tanımlar. Kodda İngilizce karşılıklar
parantez içinde verilmiştir (`docs/conventions.md`, Language).

## Ubiquitous language

| Term | Meaning | Notes / not to be confused with |
|---|---|---|
| Talep (`Request`) | BT Yazılım ya da BT Sistem birimine iletilen iş isteği | Ticket değildir; onay ve atama süreçlerini de kapsar |
| Talep No (`RequestNumber`) | İnsanların okuyabildiği benzersiz anahtar: `TLP-YYYY-NNNNNN` (ör. TLP-2026-000001) | Teknik kimlik (UUID v7) değildir. Sıra her yıl sıfırlanır |
| Talep Türü / BT Birimi (`ItUnit`) | `BT Yazılım` ya da `BT Sistem` — talebin yönlendirileceği birim | Departman değildir |
| Departman (`Department`) | Kurumun organizasyon birimi (İnsan Kaynakları, Muhasebe, Finans, Operasyon, BT Yazılım, BT Sistem) | BT birimleri aynı zamanda birer departmandır |
| Kategori (`RequestCategory`) | Bir BT birimine ait talep alt türü (ör. Hata Bildirimi, Sunucu). Sistem Yöneticisi yönetir | Her kategori bir onay akışına bağlıdır |
| Onay Akışı (`ApprovalWorkflow`) | Bir kategori için tanımlanmış, sıralı onay adımları şablonu | 0 adımlı akış: talep doğrudan birime iletilir |
| Onay Adımı (`ApprovalStep`) | Akıştaki tek bir adım ve onaycısını çözen kural | Talep üzerindeki onay kaydı değildir |
| Onaycı Kuralı (`ApproverRule`) | Adımın onaycısını çözer: `RequesterDepartmentManager` · `ItUnitManager` · `Role(<rol>)` · `User(<kullanıcı>)` | Onaycı, talep oluşturulduğunda çözülüp kaydedilir |
| Talep Onayı (`RequestApproval`) | Bir talep için bir onay adımının yürütme kaydı: onaycı, tarih, karar, açıklama | Onay Durumu bu kayıttadır |
| Talep Durumu (`RequestStatus`) | Talebin iş akışındaki yeri: Onay Bekliyor · Birime İletildi · Atandı · İşlemde · Ek Bilgi Bekleniyor · Tamamlandı · Kapatıldı · Reddedildi · İptal Edildi | **Onay Durumundan ayrıdır** |
| Onay Durumu (`ApprovalStatus`) | Tek bir onay adımının sonucu: Bekliyor · Onaylandı · Reddedildi · Atlandı | Talep Durumu değildir |
| Atama (`RequestAssignment`) | Talebin bir BT personeline verilmesi; yeniden atamalar geçmişte tutulur | |
| Öncelik (`Priority`) | Düşük · Orta · Yüksek · Kritik | SLA süresini belirler |
| SLA / Hedef Süre | Önceliğe göre talebin kapanması gereken süre (takvim günü). Varsayılan: Kritik 1 · Yüksek 3 · Orta 5 · Düşük 10 gün. Ayarlar'dan değiştirilir | |
| Geciken Talep | Oluşturulmasının üzerinden SLA süresi geçmiş, Tamamlandı/Kapatıldı/Reddedildi/İptal Edildi olmayan talep | |
| Dahili Not (`InternalNote`) | Yalnızca BT rollerinin görebildiği yorum | Normal yorumu talep sahibi de görür |
| Geçmiş (`RequestHistory`) | Talep üzerindeki her durum, onay, atama, yorum ve ek olayının kronolojik kaydı | Denetim kaydından (`AuditLog`) farklıdır: Geçmiş kullanıcıya gösterilir |
| Denetim Kaydı (`AuditLog`) | Kritik işlemlerin (yetki, rol, akış, ayar değişiklikleri ve tüm onay/atama/durum işlemleri) değiştirilemez kaydı | Yalnızca Sistem Yöneticisi görür |

### Roller
| Rol | Kod |
|---|---|
| Standart Kullanıcı (Talep Sahibi) | `Requester` |
| Birim Yöneticisi / Onaycı | `DepartmentManager` |
| BT Yazılım Personeli | `SoftwareStaff` |
| BT Sistem Personeli | `SystemStaff` |
| BT Birim Yöneticisi | `ItUnitManager` |
| Sistem Yöneticisi | `Admin` |

Bir kullanıcının birden fazla rolü olabilir. Her kullanıcı aynı zamanda talep oluşturabilir.

## Business rules
- **BR-1** Talep Durumu ile Onay Durumu ayrı tutulur. Bir onay kararı, talep durumunu yalnızca BR-5
  ve BR-6'da tanımlı şekilde değiştirir.
- **BR-2** Talep durum geçişleri yalnızca izin tablosundaki (aşağıda) geçişlerden biri olabilir;
  diğer her geçiş reddedilir.
- **BR-3** Talep oluşturulduğunda, kategorinin onay akışındaki adımlar sırayla `RequestApproval`
  kayıtlarına kopyalanır ve onaycılar çözülür. Akış daha sonra değişse bile mevcut talep etkilenmez.
- **BR-4** Adımlar sırayla işlenir: bir adım, önceki tüm adımlar Onaylandı ya da Atlandı
  olmadan işlenemez.
- **BR-5** Son adım onaylandığında (ya da akışta adım yoksa) talep durumu `Birime İletildi` olur.
- **BR-6** Herhangi bir adım reddedildiğinde talep `Reddedildi` olur ve kalan adımlar `Atlandı`
  olarak işaretlenir. Ret nihaidir.
- **BR-7** Ret için gerekçe zorunludur (boş ya da yalnızca boşluktan oluşan gerekçe kabul edilmez).
- **BR-8** Bir onay adımı yalnızca bir kez işlenir; ikinci ya da eşzamanlı ikinci karar reddedilir.
- **BR-9** Talep sahibi kendi talebinin hiçbir onay adımını onaylayamaz ya da reddedemez. Çözülen
  onaycı talep sahibiyse adım Sistem Yöneticisine yönlendirilir.
- **BR-10** Bir adımı yalnızca o adımın çözülmüş onaycısı (ya da BR-9 kapsamında Sistem
  Yöneticisi) işleyebilir.
- **BR-11** Talebi personele yalnızca talebin BT biriminin BT Birim Yöneticisi atayabilir ya da
  yeniden atayabilir. Atanan kişi o birimin personeli olmalıdır.
- **BR-12** Talebi `İşlemde` ve `Tamamlandı` durumlarına yalnızca atanmış personel taşıyabilir.
- **BR-13** Talep No, oluşturma anında yılın sırasından atomik olarak üretilir ve tekrar etmez.
- **BR-14** Her durum, onay, atama, yorum ve ek değişikliği `RequestHistory`'ye; onay, atama, durum,
  rol, akış ve ayar değişiklikleri ayrıca `AuditLog`'a yazılır. İkisi de işlemle aynı
  transaction'da yazılır.
- **BR-15** Bildirim oluşturan olaylar: talep oluşturulması, yeni onay beklenmesi (sıradaki
  onaycıya), onay, ret, personel ataması, ek bilgi istenmesi, durum değişikliği, tamamlanma.
  Bildirim, okunma durumuyla birlikte alıcı başına tutulur.
- **BR-16** Geciken Talep tanımı yukarıdaki sözlüktedir. SLA süreleri Ayarlar'dan okunur.
- **BR-17** Kullanıcı yalnızca görme yetkisi olduğu talepleri görür (`docs/security.md`, AuthZ).
  Liste, detay, dashboard, rapor ve Excel çıktısı aynı görünürlük kuralına tabidir.

### Talep durum geçişleri (taslak — ilk onay/iş akışı spec'i kesinleştirir)
| Kaynak | Hedef | Tetikleyen |
|---|---|---|
| (yeni) | Onay Bekliyor | Akışta en az bir adım varsa, oluşturma anında |
| (yeni) | Birime İletildi | Akış 0 adımlıysa, oluşturma anında |
| Onay Bekliyor | Birime İletildi | Son adımın onaylanması (BR-5) |
| Onay Bekliyor | Reddedildi | Herhangi bir adımın reddedilmesi (BR-6) |
| Onay Bekliyor · Birime İletildi | İptal Edildi | Talep sahibi |
| Birime İletildi | Atandı | BT Birim Yöneticisi (BR-11) |
| Atandı · İşlemde | Atandı | Yeniden atama (BR-11) |
| Atandı | İşlemde | Atanan personel (BR-12) |
| Birime İletildi · Atandı · İşlemde | Ek Bilgi Bekleniyor | BT Birim Yöneticisi ya da atanan personel |
| Ek Bilgi Bekleniyor | (önceki durum) | Talep sahibinin yanıt vermesi |
| İşlemde | Tamamlandı | Atanan personel (BR-12) |
| Tamamlandı | Kapatıldı | Talep sahibi ya da BT Birim Yöneticisi |

Açık noktalar (ilgili spec'in netleştirme adımında karar verilir): "iade" işleminin `Ek Bilgi
Bekleniyor` ile aynı şey olup olmadığı; `Tamamlandı` durumundan yeniden açma; otomatik kapatma.

## Key domain invariants
- Bir talebin aynı anda tek bir Talep Durumu vardır ve bu durum yalnızca izin tablosundaki bir
  geçişle değişir.
- Bir `RequestApproval` kaydı en fazla bir kez karara bağlanır; karar verilmiş kayıt değiştirilemez.
- Onay sırası atlanamaz: N. adım karara bağlanmadan N+1. adım işlenemez.
- Talep sahibi kendi talebini onaylayamaz.
- Talep No benzersizdir ve değiştirilemez.
- Dahili notlar BT rolleri dışında hiçbir kullanıcıya hiçbir yanıtta (API, rapor, Excel) dönmez.
- `RequestHistory` ve `AuditLog` yalnızca eklenir; kayıtlar güncellenmez ve silinmez.

## Sensitive areas
Bu alanlara dokunan bir spec, Constraints bölümünde `Sensitive area: <name>` yazar.

- **Yetkilendirme** — kimlik doğrulama (JWT, refresh token), rol → izin eşlemesi, kayıt bazlı veri
  görünürlüğü (BR-17), dahili notlar, dosya yükleme ve indirme erişimi.
- **Onay motoru** — onay akışının yürütülmesi, adım sırası (BR-4), adımın tek kez işlenmesi (BR-8),
  öz-onay yasağı (BR-9, BR-10), talep durum geçişleri (BR-2).

Kişisel veri (KVKK) şu an hassas alan değildir, çünkü ilk aşama demo veriyle çalışır. Gerçek
kullanıcı verisine geçilmeden önce bu alan eklenir (`specs/backlog.md`).
