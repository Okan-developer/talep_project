# Architecture

## System overview
TalepYönetim, bir kurumun BT Yazılım ve BT Sistem birimlerine gelen taleplerin oluşturma, dinamik
onay, birime yönlendirme, personel atama, iş takibi ve kapatma süreçlerini yöneten bir web
uygulamasıdır. Tek repoda iki ayrı proje vardır:

- `backend/` — .NET 10 (LTS) · ASP.NET Core Web API · **modüler monolit** (tek dağıtım birimi;
  içeride sınırları mimari testlerle korunan modüller) · EF Core 10 + Npgsql · PostgreSQL 17.
- `frontend/` — React 19 + TypeScript (strict) · Vite SPA. Backend'e yalnızca HTTP/JSON API
  üzerinden bağlanır.

API sözleşmesi backend'in OpenAPI çıktısıdır; frontend tipleri ve istemcisi bu çıktıdan üretilir.
Gerekçe: ADR 0001 (modüler monolit + ayrı SPA).

## Modules / components and ownership

### Backend modülleri
Her modülün içinde `Domain` / `Application` / `Infrastructure` / `Endpoints` katmanları ve dışa
açık tek yüzey olarak bir `Contracts` bölümü bulunur. Tek PostgreSQL veritabanı kullanılır;
**her modülün kendi şeması** vardır.

| Module | Single responsibility | Owns |
|---|---|---|
| Kimlik (`Identity`) | Kimlik doğrulama (JWT), kullanıcı, rol, izin, departman yönetimi; rol → izin eşlemesi | User, Role, Permission, Department, RefreshToken |
| Katalog (`Catalog`) | Talep kategorileri ve kategoriye bağlı onay akışı tanımları (şablonlar) | RequestCategory, ApprovalWorkflow, ApprovalStep |
| Talep (`Requests`) | Talep yaşam döngüsü: oluşturma, talep no üretimi, onay yürütme, durum geçişleri, atama, yorum, ek, geçmiş | Request, RequestApproval, RequestAssignment, RequestComment, RequestAttachment, RequestHistory |
| Bildirim (`Notifications`) | Uygulama içi bildirimler ve okunma durumu; kanal soyutlaması (`INotificationChannel`) | Notification |
| Raporlama (`Reporting`) | Dashboard KPI'ları, grafik verileri ve raporlar — **salt okuma** | (veri sahibi değildir) |
| Denetim (`Audit`) | Kritik işlemlerin değiştirilemez (append-only) denetim kaydı | AuditLog |
| Ayarlar (`Settings`, Kimlik içinde) | Sistem parametreleri: SLA süreleri, dosya limitleri | SystemSetting |

Paylaşılan çekirdek (`SharedKernel`): Result tipi, hata modeli, `TimeProvider` kullanımı,
domain event altyapısı. İş kuralı içermez.

### Frontend
| Bölüm | Sorumluluk |
|---|---|
| `src/app` | Uygulama kabuğu: router, sağlayıcılar (TanStack Query, tema), layout (daraltılabilir sidebar, üst bar) |
| `src/features/<özellik>` | Özellik bazlı ekranlar ve bileşenler (talepler, onaylar, dashboard, raporlar, yönetim, bildirimler, oturum) |
| `src/shared/ui` | shadcn/ui tabanlı ortak bileşenler, durum rozetleri, boş/yükleniyor/hata ekranları |
| `src/shared/api` | OpenAPI'den üretilmiş tipler ve istemci; token ekleme ve yenileme mantığı |
| `src/shared/i18n` | Tüm Türkçe kullanıcı metinlerinin tutulduğu sözlük |

## Communication rules
- Bir modül başka bir modülü **yalnızca onun `Contracts` arayüzleri üzerinden**, süreç içi doğrudan
  çağrıyla kullanır (ör. Talep → Katalog: "bu kategorinin onay akışını getir").
- Bildirim ve Denetim, Talep ve Kimlik modüllerinin yayınladığı **süreç içi domain event'lerle**
  beslenir. Event işleyicileri **aynı transaction içinde** çalışır: işlem geri alınırsa bildirim ve
  denetim kaydı da geri alınır.
- Onay verme/reddetme, atama ve durum değişikliği **Talep modülü içinde tek transaction'da** yapılır.
- Raporlama, diğer modüllerin `Contracts` altında yayınladığı salt okuma sorgu arayüzlerini (ya da
  o modülün tanımladığı salt okuma view'larını) kullanır; tablolara doğrudan erişmez.
- Frontend backend'e yalnızca `src/shared/api` istemcisiyle erişir; bileşenlerde doğrudan `fetch`
  yazılmaz.
- E-posta gibi yeni bildirim kanalları, `INotificationChannel` arayüzünün yeni bir uygulaması olarak
  eklenir; mevcut modüller değişmez.

## Forbidden dependencies (make them testable)
Backend tarafındaki kurallar NetArchTest ile, frontend tarafındakiler ESLint ile `scripts/check`
içinde doğrulanır.

1. Bir modül başka bir modülün `Domain`, `Application` ya da `Infrastructure` isim alanlarına
   referans veremez; yalnızca `Contracts` kullanılabilir.
2. Bir modül başka bir modülün DB şemasındaki tablolara erişemez (EF eşlemesi yalnızca kendi şeması).
3. `Domain` katmanları EF Core, ASP.NET Core ya da başka bir altyapı paketine bağımlı olamaz.
4. `Reporting` modülü yazma işlemi yapamaz (`SaveChanges` çağrısı, komut endpoint'i yok).
5. Endpoint'ler iş kuralı içermez; işi `Application` katmanına devreder.
6. Frontend: `src/features/A` içinden `src/features/B` içine import yapılamaz; ortak ihtiyaç
   `src/shared` altına taşınır. `src/shared` hiçbir `features` klasörünü import edemez.

## Deliberately out of scope
v1 kapsamı dışında kalanlar:
- E-posta gönderimi (yalnızca kanal arayüzü hazırlanır).
- SSO / Active Directory entegrasyonu.
- Mikroservis mimarisi, mesaj kuyruğu, outbox.
- Mobil uygulama.
- Çoklu kiracı (multi-tenant) yapı.
