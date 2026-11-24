# BT Talep Yönetimi

Kurumsal BT Talep ve Onay Yönetim Sistemi: BT Yazılım ve BT Sistem birimlerine gelen taleplerin
oluşturma → onay → atama → takip → kapatma süreci.

- `backend/` — .NET 10 · ASP.NET Core Web API · modüler monolit · EF Core + PostgreSQL 17
- `frontend/` — React 19 · TypeScript · Vite · Tailwind CSS · shadcn/ui tarzı bileşenler

Proje kuralları ve süreç: [`AGENTS.md`](AGENTS.md) · mimari: [`docs/architecture.md`](docs/architecture.md)
· komutlar: [`docs/commands.md`](docs/commands.md).

## Gereksinimler

| Araç | Sürüm | Kontrol |
|---|---|---|
| .NET SDK | 10.0.x | `dotnet --list-sdks` |
| Node.js | 24 LTS (npm ile) | `node --version` |
| Docker | Docker Desktop / Engine, çalışır durumda | `docker info` |

## Yerelde çalıştırma

Komutlar repo kökünden çalıştırılır. Her biri ayrı bir terminalde açık kalır.

1. **Veritabanı (PostgreSQL 17, port 5433):**

   ```sh
   docker compose up -d --wait postgres
   ```

   `--wait`, veritabanı bağlantı kabul edene kadar bekler; backend'i bundan sonra başlatın.

2. **Backend (http://localhost:5080):**

   ```sh
   cd backend
   dotnet run --project src/TalepYonetim.Api
   ```

   Backend başlarken veritabanı yapısını kurar ve 6 demo departmanını ekler. Veritabanına
   ulaşamazsa yine de açılır; sistem durumu "Veritabanı: Erişilemiyor" gösterir.
   Boş veritabanıyla ilk başlatmada konsolda `__ef_migrations_history` tablosu için bir
   `fail: Microsoft.EntityFrameworkCore.Database.Command` satırı görünür. Bu beklenen bir durumdur:
   EF Core tabloyu henüz bulamayınca onu oluşturur; hata değildir.

3. **Frontend (http://localhost:5173):**

   ```sh
   cd frontend
   npm ci
   npm run dev
   ```

4. Tarayıcıda http://localhost:5173 adresini açın. Genel Bakış ekranındaki **Sistem Durumu**
   kartında "API: Çalışıyor", "Veritabanı: Erişilebilir" ve "Departman sayısı: 6" görünmelidir.

Durdurmak için: frontend ve backend terminallerinde `Ctrl+C`, ardından `docker compose down`
(verileri de silmek için `docker compose down -v`).

Geliştirme bağlantı dizesi `backend/src/TalepYonetim.Api/appsettings.Development.json` içindedir ve
yalnızca bu yerel kapsayıcıya aittir. Diğer ortamlarda `ConnectionStrings__Default` ortam
değişkeni kullanılır (`docs/security.md`).

## Doğrulama

```sh
./scripts/check          # tam profil: derleme, biçim, tüm testler (Docker gerekir), bağımlılık denetimi
./scripts/check --fast   # hızlı profil: entegrasyon testleri ve bağımlılık denetimi hariç her adım
./scripts/doctor         # çalışma alanı sağlık kontrolü
```

## Sık kullanılan geliştirici komutları

| Ne | Komut |
|---|---|
| Yeni migration | `cd backend && dotnet tool restore && dotnet ef migrations add <Ad> --project src/Modules/TalepYonetim.Identity --startup-project src/TalepYonetim.Api --context IdentityDbContext --output-dir Infrastructure/Migrations` |
| API tiplerini yeniden üret (backend derlemesinden sonra) | `npm --prefix frontend run api:generate` |
| Frontend biçimlendirme | `npm --prefix frontend run format` |
