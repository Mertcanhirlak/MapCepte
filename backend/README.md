# MapCepte Backend

.NET 10 tabanlı API; uygulama katmanlarını, yerel PostgreSQL/PostGIS bağlantısını ve rota motoru adaptörünü barındırır.

## Gereksinimler

- .NET 10 SDK
- Yerel PostgreSQL servisi
- PostgreSQL sürümüyle uyumlu PostGIS extension dosyaları

Bu çalışma ortamında PostgreSQL `18.4`, PostGIS `3.6` ve `5432` portu doğrulanmıştır.

## İlk veritabanı hazırlığı

Bu işlem yalnızca ilk kurulumda gerekir ve PostgreSQL `postgres` yöneticisinin parolasını güvenli biçimde terminalde sorar:

```powershell
cd backend
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" `
  -h 127.0.0.1 `
  -p 5432 `
  -U postgres `
  -d postgres `
  -f scripts/setup-local-database.sql
```

Script:

- `mapcepte` uygulama kullanıcısını oluşturur veya yerel geliştirme parolasını yeniler.
- `mapcepte` veritabanını oluşturur.
- Veritabanında PostGIS extension'ını etkinleştirir.

Bu dosyadaki `mapcepte_dev` yalnızca yerel geliştirme parolasıdır; üretimde kullanılmaz.

## API'yi çalıştırma

```powershell
cd backend
dotnet restore
dotnet tool restore
dotnet ef database update `
  --project src/Transport.Infrastructure `
  --startup-project src/Transport.Api
dotnet run --project src/Transport.Api
```

Adresler:

- API: `http://localhost:5268`
- OpenAPI: `http://localhost:5268/openapi/v1.json`
- Liveness: `http://localhost:5268/health/live`
- Readiness/PostGIS: `http://localhost:5268/health/ready`

`/health/live` API process'inin çalıştığını, `/health/ready` ise yerel PostgreSQL ve PostGIS'in hazır olduğunu gösterir.

## İlk Admin hesabını oluşturma

Bu işlem yalnızca sistemde henüz bir Admin bulunmadığında çalışır. Parola dosyaya yazılmaz; açık terminal oturumunda güvenli biçimde istenir:

```powershell
cd backend

$env:BootstrapAdmin__Enabled = "true"
$env:BootstrapAdmin__Email = "admin@example.com"
$env:BootstrapAdmin__DisplayName = "Sistem Yöneticisi"
$securePassword = Read-Host "Admin parolası" -AsSecureString
$env:BootstrapAdmin__Password = [System.Net.NetworkCredential]::new(
  "",
  $securePassword
).Password

dotnet run --project src/Transport.Api
```

Logda Admin hesabının oluşturulduğu görüldükten sonra API `Ctrl+C` ile durdurulur ve geçici değişkenler temizlenir:

```powershell
Remove-Item Env:BootstrapAdmin__Enabled
Remove-Item Env:BootstrapAdmin__Email
Remove-Item Env:BootstrapAdmin__DisplayName
Remove-Item Env:BootstrapAdmin__Password
Remove-Variable securePassword
```

Production parolası 12-128 karakter olmalı; büyük harf, küçük harf, rakam ve sembol içermelidir. `appsettings.Development.json` içindeki `IdentitySecurity:AllowWeakPasswordsInDevelopment` yerel geliştirmede en az 6 karakterlik basit parolaya açıkça izin verir; bu override production ortamında çalışmaz. İkinci çalıştırma yeni bir Admin oluşturmaz. Normal API başlangıcında `BootstrapAdmin__Enabled` kapalı kalmalıdır.

## Oturum API'si

Tarayıcı oturumu şifreli ve `HttpOnly` cookie ile korunur. JavaScript cookie değerini okuyamaz; frontend bütün API isteklerinde `credentials: "include"` kullanır.

1. `GET /api/auth/csrf` ile CSRF token alınır.
2. Token `X-CSRF-TOKEN` header'ında gönderilerek `POST /api/auth/login` çağrılır.
3. Login sonrasında kimlik değiştiği için `/api/auth/csrf` tekrar çağrılır.
4. `GET /api/auth/me` mevcut kullanıcı, roller ve permission'ları döndürür.
5. Güncel CSRF token ile `POST /api/auth/logout` oturumu kapatır.

Oturum 15 dakika geçerlidir ve aktif kullanımda güvenli biçimde yenilenir. Login denemeleri IP başına dakikada beş istekle sınırlandırılmıştır. Bir hesap art arda beş hatalı denemeden sonra 15 dakika kilitlenir; başarılı giriş sayacı sıfırlar. Üretimde cookie yalnızca HTTPS üzerinden gönderilir.

## Permission korumalı API

Endpoint'ler yalnızca role adına göre değil, oturum cookie'sindeki permission claim'lerine göre korunur. Örneğin:

- `GET /api/admin/roles`, `roles.read` permission'ı ister.
- Oturumu olmayan çağrı `401 Unauthorized` döndürür.
- Oturumu bulunan fakat permission'ı olmayan kullanıcı `403 Forbidden` alır.
- Permission'a sahip Admin rol ve permission kataloğunu okuyabilir.

Admin kullanıcı yönetimi:

- `GET /api/admin/users`, `users.read` ister.
- `POST /api/admin/users`, `users.manage` ve `roles.manage` ister.
- `PUT /api/admin/users/{userId}/roles`, `users.manage` ve `roles.manage` ister.
- Kullanıcı oluşturma ve rol değiştirme istekleri CSRF token gerektirir.
- Yönetici kendi rollerini bu endpoint üzerinden değiştiremez.
- `GET /api/admin/audit`, `audit.read` ister ve son 100 güvenlik olayını döndürür.

Başarılı/başarısız/kilitli girişler, kullanıcı oluşturma ve rol değişiklikleri audit tablosuna yazılır. Parola, cookie ve CSRF token gibi hassas değerler audit kaydına alınmaz.

Yeni bir endpoint'e koruma eklemek için endpoint tanımında `RequirePermission(PermissionNames.<Permission>)` kullanılır.

## Durak API'si

- `GET /api/stops`, `stops.read` permission'ı ister.
- Admin tüm durakları, Operator kendi oluşturduğu durakları, standart User yalnız yayımlanmış durakları görür.
- `POST /api/stops`, `stops.create` permission'ı ve CSRF token ister.
- Yeni duraklar `Draft` durumunda, isteği yapan kullanıcıya ait olarak oluşturulur.
- Ad, benzersiz kod, `#RRGGBB` renk ve WGS84 enlem/boylam değerleri doğrulanır.
- Konum PostgreSQL'de `geography(Point,4326)` olarak saklanır ve GIST spatial indeks kullanır.

## Build ve test

```powershell
cd backend
dotnet restore
dotnet build Transport.sln --no-restore
dotnet test Transport.sln --no-build --no-restore
```

Bağlantı adresi `ConnectionStrings__TransportDb` environment değişkeniyle override edilebilir.

## Rota motoru

OSRM bu ilk aşamada kurulmamıştır. Rota motoru, PostGIS ve auth temelinden sonra bağımsız bir native servis veya kontrollü geliştirme endpoint'i olarak ele alınacaktır.
