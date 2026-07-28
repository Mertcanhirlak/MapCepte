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

Parola 12-128 karakter olmalı; büyük harf, küçük harf, rakam ve sembol içermelidir. İkinci çalıştırma yeni bir Admin oluşturmaz. Normal API başlangıcında `BootstrapAdmin__Enabled` kapalı kalmalıdır.

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
