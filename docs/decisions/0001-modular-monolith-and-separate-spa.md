# ADR 0001 — Modüler monolit backend (.NET 10) + ayrı React SPA, PostgreSQL

- Status: Accepted
- Date: 2026-11-20

## Context
BT Talep ve Onay Yönetim Sistemi, dinamik onay akışları, rol bazlı yetkilendirme ve
transaction tutarlılığı gerektiriyor. Proje sahibi bootstrap görüşmesinde backend ile frontend'in
ayrı projeler olmasını, backend'in monolitik olmasını, frontend'de React + TypeScript
kullanılmasını ve veritabanı olarak PostgreSQL seçilmesini istedi. Ekip küçük; bu makinede .NET 10
SDK ve Docker kurulu.

## Decision
Backend, .NET 10 ASP.NET Core Web API olarak **modüler monolit** biçiminde kurulur: tek dağıtım
birimi, içeride `Contracts` üzerinden konuşan ve sınırları mimari testlerle korunan modüller,
modül başına bir şemayla tek bir PostgreSQL 17 veritabanı. Frontend ise ayrı bir React 19 +
TypeScript Vite SPA'dır. İki proje tek repoda durur ve OpenAPI sözleşmesiyle bağlanır.

## Consequences
- Kazanç: onay + durum + denetim kaydı tek transaction'da yapılır; dağıtık sistem karmaşıklığı
  yoktur; modül sınırları sayesinde ileride bir modül ayrılabilir.
- Kazanç: tek repo, tek `scripts/check` ve tek süreç.
- Bedel: iki toolchain (dotnet + node) gerekir. Entegrasyon testleri Docker ister.
- Bedel: modül sınırlarına disiplin gerekir; bu bedel mimari testlerle ödenir.
- Bedel: briften gelen "mock servis katmanı" aşaması atlanır; demo veriler
  doğrudan PostgreSQL'e seed edilir.

## Alternatives considered
- **Next.js full-stack + bellek içi mock:** proje sahibi ayrı backend ve gerçek DB istediği için
  reddedildi.
- **NestJS (TypeScript) backend:** tek dil avantajı var; ancak .NET 10 kurulu, LTS ve kurumsal
  ortamda yaygın olduğu için proje sahibi .NET'i seçti.
- **Spring Boot:** bu makinede yalnızca Java 8 var; ek kurulum gerektiriyor.
- **Mikroservisler:** ekip ve kapsam için gereksiz karmaşıklık.

## Revisit triggers
- Bir modülün bağımsız ölçeklenmesi ya da ayrı ekip tarafından yönetilmesi gerekirse.
- Mobil istemci ya da üçüncü taraf entegrasyonu ayrı bir API yüzeyi gerektirirse.
