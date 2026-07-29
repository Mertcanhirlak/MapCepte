# MapCepte Öğrenme Notları

Bu dosya geliştirme boyunca kısa teknik notlarla güncellenecektir. Amaç yalnızca “ne yapıldı?” sorusunu değil, “hangi parça neden diğerine bağlı?” sorusunu da cevaplamaktır.

## Büyük resim

```text
React arayüzü
    ↓ HTTP/JSON
.NET 10 API
    ├─ Application → kullanım senaryoları
    ├─ Domain      → temel iş kuralları
    └─ Infrastructure
          ├─ PostgreSQL + PostGIS
          └─ açık kaynak rota motoru
```

- React doğrudan veritabanına bağlanmaz; yalnızca .NET API ile konuşur.
- API, iş kurallarını `Domain` ve `Application` katmanlarından çalıştırır.
- `Infrastructure`, veritabanı ve rota motoru gibi dış sistemleri uygulamaya bağlar.
- PostGIS, PostgreSQL'e `Point` ve `LineString` gibi coğrafi veri tipleri kazandırır.

## Faz 0 — Proje temeli

### Ayrı backend ve frontend

- `backend/Transport.sln`, bütün .NET projelerini bir arada derlemek ve test etmek için gereklidir.
- `frontend/package.json`, React bağımlılıklarını ve `dev`, `build`, `test` komutlarını tanımlar.
- İki uygulama ayrı çalışır. Frontend'in ekranda canlı veri gösterebilmesi için API'nin çalışması gerekir; ancak ikisi ayrı ayrı derlenip test edilebilir.

### Backend katmanları

- `Transport.Domain`: Başka uygulama katmanına bağımlı olmaması gereken en iç katmandır.
- `Transport.Application`: Domain'i kullanır; ileride durak/güzergâh kullanım senaryoları burada bulunacaktır.
- `Transport.Infrastructure`: Application ve Domain'i kullanır; EF Core, PostGIS ve rota motoru bağlantıları burada bulunur.
- `Transport.Api`: HTTP giriş noktasıdır; Application ile Infrastructure parçalarını bir araya getirir.

Bağımlılık yönü önemlidir: iç katmanlar dış katmanları bilmez. Böylece veritabanı veya web arayüzü değişse bile iş kuralları korunur.

### PostGIS bağlantısı

- `TransportDbContext`, EF Core'un veritabanı oturumudur.
- `UseNpgsql`, EF Core'un PostgreSQL ile konuşmasını sağlar.
- `UseNetTopologySuite`, `Point` ve `LineString` gibi PostGIS tiplerini C# nesnelerine dönüştürür.
- Migration içindeki `postgis` extension kaydı olmadan mekânsal kolonlar çalışmaz.
- `/health/ready`, hem PostgreSQL bağlantısını hem de PostGIS extension'ını kontrol eder.

### Harita katmanları

Katman çizim sırası aşağıdan yukarıya şöyledir:

1. Temel harita
2. Rota çizgileri
3. Duraklar
4. Seçim/vurgu
5. Canlı araçlar

Durakların rota çizgilerinin üstünde olması gerekir; aksi hâlde rota çizgisi durakları kapatabilir. Katman görünürlüğü React tarafında tutulur, coğrafi veri ise API'den gelir.

### Yerel çalışma bağımlılıkları

- Backend'i derlemek için .NET 10 SDK gerekir.
- Frontend'i çalıştırmak için Node.js ve npm gerekir.
- PostgreSQL ve PostGIS bu ilk aşamada Windows üzerinde doğrudan servis olarak çalışır.
- `backend/scripts/setup-local-database.sql`, uygulamaya özel `mapcepte` rolünü ve veritabanını oluşturur; ardından PostGIS extension'ını etkinleştirir.
- `/health/live` API process'inin ayakta olduğunu, `/health/ready` ise veritabanı ile PostGIS'in de kullanılabilir olduğunu gösterir. Böylece uygulamanın çalışması ile dış bağımlılıkların hazır olması ayrı ayrı ölçülür.

## Hangi parça hangisine ihtiyaç duyuyor?

- `Program.cs` içindeki `AddInfrastructure`, `DependencyInjection.cs` çalışmadan `TransportDbContext` oluşturamaz.
- `TransportDbContext`, Npgsql bağlantısı olmadan PostgreSQL'e; NetTopologySuite olmadan PostGIS geometrilerine erişemez.
- `EnablePostgis` migration'ı veritabanına uygulanmadan gelecekteki `Point` ve `LineString` kolonları kullanılamaz.
- `App.tsx` katmanların açık/kapalı durumunu tutar; `LayerPanel` bu durumu değiştirir, `TransportMap` değişikliği MapLibre katmanına uygular.
- Frontend'in API durumunu gösterebilmesi için `.env` içindeki `VITE_API_BASE_URL` ile .NET API adresinin eşleşmesi gerekir.
- Rota katmanının gerçek çizgi gösterebilmesi için önce durakların kaydedilmesi, ardından routing motorunun geometri üretmesi ve API'nin bu geometriyi frontend'e vermesi gerekir.

## Kodu öğrenmek için okuma sırası

1. `backend/src/Transport.Api/Program.cs`: Uygulamanın hangi servisleri ve endpoint'leri açtığını gör.
2. `backend/src/Transport.Infrastructure/DependencyInjection.cs`: PostGIS bağlantısının API'ye nasıl eklendiğini incele.
3. `backend/src/Transport.Infrastructure/Persistence/TransportDbContext.cs`: EF Core modelinin giriş noktasını gör.
4. `backend/src/Transport.Infrastructure/Persistence/Migrations/`: PostGIS extension'ının veritabanına nasıl yazıldığını incele.
5. `frontend/src/features/map/mapLayers.ts`: Katman kimliklerini ve çizim sırasını gör.
6. `frontend/src/features/map/LayerPanel.tsx`: React state'ini değiştiren kontrolleri incele.
7. `frontend/src/features/map/TransportMap.tsx`: React görünürlük bilgisinin MapLibre'a nasıl uygulandığını gör.

## Faz 0 doğrulama sonucu

- Kullanılan ortam: .NET SDK `10.0.301`, Node.js `24.16.0`, npm `11.17.0`.
- Backend: 8 proje uyarısız derlendi; 3 test projesinde toplam 6 test geçti.
- Frontend: TypeScript/Vite build ve lint geçti; 4 katman testi geçti.
- Backend ve frontend ayrı process olarak başlatıldı; `5268` ve `5173` portlarından HTTP `200` alındı.
- EF migration aracı proje içinde `10.0.10` sürümüne sabitlendi.
- Restore sırasında bildirilen OpenAPI.NET açığı için güvenli `2.7.5` sürümü sabitlendi.
- Bu makinede PostgreSQL `18.4` servisinin çalıştığı, `5432` portunun bağlantı kabul ettiği ve PostGIS `3.6` extension dosyalarının kurulu olduğu doğrulandı.
- İlk veritabanı kurulum betiği hazırlandı ve yönetici parolası yalnızca çalıştırma sırasında kullanılarak tamamlandı.
- Docker kullanılmıyor; daha önce herhangi bir Docker kurulumu veya container çalıştırması yapılmadı ve compose dosyası projeden kaldırıldı.
- Docker'sız geçişten sonra backend yeniden derlendi; 8 proje uyarısız derlendi ve 3 test projesindeki 6 test yeniden geçti.
- `setup-local-database.sql` çalıştırıldı; `mapcepte` uygulama rolü ve aynı adlı veritabanı oluşturuldu.
- `EnablePostgis` migration'ı gerçek veritabanına uygulandı. Migration geçmişi tablosu ve PostGIS `3.6.2` sürümü SQL sorgusuyla doğrulandı.
- API gerçek veritabanına bağlıyken `/health/live` ve `/health/ready` endpoint'lerinin ikisi de HTTP `200` döndürdü.
- Yönetici parolası hiçbir proje dosyasına yazılmadı. Uygulama günlük işlerinde sınırlı yetkili `mapcepte` hesabını kullanır; `postgres` hesabı yalnızca yönetim işlemleri içindir.

## Faz 1 — Kullanıcı, rol ve permission temeli

### Veritabanı ilişkileri

```text
users ──< user_roles >── roles ──< role_permissions >── permissions
```

- `users`, giriş yapabilen hesapları tutar. Parolanın kendisi değil yalnızca `password_hash` alanı bulunur.
- `roles`, `Admin`, `Operator` ve `User` gibi görev gruplarıdır.
- `permissions`, `stops.create` veya `route_paths.generate` gibi tek bir yeteneği tanımlar.
- `user_roles`, bir kullanıcının birden fazla role sahip olabilmesini sağlar.
- `role_permissions`, bir rolün hangi işlemlere izin verdiğini belirler.
- Ara tabloların iki kolonlu birleşik primary key kullanması aynı atamanın iki kez yazılmasını engeller.

### Varsayılan yetki matrisi

- `Admin`: 18 permission'ın tamamı.
- `Operator`: durak CRUD, güzergâh CRUD/sıralama ve rota okuma/üretme/silme için 12 permission.
- `User`: yalnızca durak, güzergâh ve rota okuma için 3 permission.
- Henüz kullanıcı hesabı seed edilmedi. Böylece migration veya kaynak kod içinde varsayılan bir parola/hash bulunmuyor.
- Gerçek kullanıcı oluşturma sırasında güvenli parola hash servisi çalışacak ve rol ataması `user_roles` tablosuna yazılacak.

### Kod parçalarının bağlantısı

- `Transport.Domain/Identity`, veritabanından bağımsız kullanıcı ve yetki kavramlarını içerir.
- `TransportDbContext`, bu sınıfları EF Core'a açar.
- `Persistence/Configurations`, property uzunluklarını, benzersiz indeksleri, foreign key'leri ve silme davranışlarını belirler.
- `IdentitySeedData`, sabit kimliklerle rollerin ve permission'ların her ortamda aynı şekilde oluşmasını sağlar.
- `AddIdentityAuthorizationSchema` ve `AlignPermissionCatalog` migration'ları şema ile son permission kataloğunu gerçek PostgreSQL veritabanına taşır.

### Doğrulama

- Çözümdeki 8 proje sıfır uyarı ve sıfır hatayla derlendi.
- Üç test projesinde toplam 11 test geçti.
- Gerçek veritabanında 3 rol, 18 permission ve 33 rol-permission ilişkisi doğrulandı.
- `users` tablosunun boş olduğu doğrulandı; gizli bir başlangıç hesabı oluşturulmadı.

### Parola hash ve Admin bootstrap

- `IPasswordHashService`, Application katmanının belirli bir hash kütüphanesine bağımlı olmasını engeller.
- `AspNetPasswordHashService`, bu sözleşmeyi ASP.NET Core'un standart `PasswordHasher` bileşeniyle uygular.
- Veritabanına düz parola değil, salt içeren tek yönlü hash yazılır. Aynı parola yeniden hash'lendiğinde farklı çıktı üretilebilir; doğrulama hash bileşeni üzerinden yapılır.
- `AdminBootstrapService`, önce sistemde Admin olup olmadığını kontrol eder. Admin varsa ikinci hesap oluşturmaz.
- Bootstrap parolası en az 12 karakter; büyük/küçük harf, rakam ve sembol içermek zorundadır.
- `AdminBootstrapHostedService`, yalnızca `BootstrapAdmin__Enabled=true` olduğunda çalışır. E-posta, görünen ad ve parola environment variable üzerinden gelir.
- Bootstrap tamamlandıktan sonra ilgili environment variable'lar temizlenmeli ve özellik kapalı tutulmalıdır.
- Çalışan kullanıcı API süreci durdurulmadan ayrı Release çıktısıyla derleme yapıldı; toplam test sayısı 19'a çıktı ve tamamı geçti.
- Yerel veritabanındaki `admin@mapcepte.local` hesabı bootstrap ile yeniden oluşturuldu ve `Admin` rolü SQL sorgusuyla doğrulandı. Development ortamında `AllowWeakPasswordInDevelopment` açık olduğunda en az 6 karakter kabul edilir; production ortamında 12 karakter, büyük/küçük harf, rakam ve sembol şartı devam eder.
- Yerel giriş parolası environment variable üzerinden yalnızca bootstrap sürecine verildi; kaynak koda yazılmadı. Geçici bootstrap betiği ve logları işlem sonunda silindi.

### Login ve güvenli tarayıcı oturumu

- Özel JWT/refresh token yerine ASP.NET Core'un şifreli authentication cookie'si seçildi. Cookie `HttpOnly` olduğu için React veya zararlı bir JavaScript kodu cookie değerini okuyamaz.
- Cookie 15 dakika geçerlidir. Kullanıcı aktif oldukça sliding expiration mekanizması oturumu yeniler; bu nedenle ayrı `/refresh` endpoint'i yoktur.
- `LoginService`, e-postayı normalize eder, kullanıcıyı rol/permission bilgileriyle getirir ve hash doğrulaması yapar. Olmayan kullanıcılar için sahte hash doğrulaması çalıştırılarak zamanlama farkı azaltılır.
- Başarılı login sonucunda kullanıcı kimliği, roller ve permission'lar şifreli cookie ticket'ına claim olarak yazılır.
- `GET /api/auth/me`, cookie'deki claim'lerden mevcut kullanıcı görünümünü üretir.
- `POST /api/auth/logout`, authentication cookie'sini geçersiz kılar.
- Cookie tarayıcı tarafından otomatik gönderildiği için yazma istekleri CSRF saldırısına açıktır. `/api/auth/csrf` çift token üretir; login ve logout doğru `X-CSRF-TOKEN` header'ı olmadan `400` döndürür.
- Kimlik login sırasında değiştiğinden login sonrasında CSRF token yeniden alınmalıdır.
- Login endpoint'i IP başına dakikada beş denemeyle sınırlandırılmıştır.
- HTTP entegrasyon testi anonim `/me`, CSRF'siz login, başarılı login, cookie ile `/me`, CSRF yenileme ve logout zincirini doğrular.
- Çözümdeki 8 proje uyarısız derlendi; üç test projesinde toplam 25 test geçti.

### Permission policy ve sunucu tarafı yetkilendirme

Kimlik doğrulama kullanıcının kim olduğunu, yetkilendirme ise hangi işlemleri yapabileceğini belirler:

- Oturum cookie'si yoksa korumalı endpoint `401 Unauthorized` döndürür.
- Kullanıcı giriş yapmış fakat gereken permission claim'ine sahip değilse `403 Forbidden` döndürür.
- Gereken permission varsa endpoint çalışır ve `200 OK` dönebilir.

Kodun çalışma zinciri şöyledir:

```text
Login
  → rol ve permission'lar claim olarak cookie'ye yazılır
  → endpoint RequirePermission(...) ile policy ister
  → PermissionAuthorizationHandler claim'i denetler
  → izin varsa kullanım senaryosu çalışır
```

- `PermissionAuthorizationExtensions`, permission kataloğundaki her değer için merkezi bir policy oluşturur.
- `PermissionAuthorizationHandler`, kullanıcının istenen permission claim'ine sahip olup olmadığını kontrol eder.
- `GET /api/admin/roles`, ilk gerçek korumalı endpoint'tir ve `roles.read` ister.
- `RoleCatalogService`, rol ve permission listesini Application katmanından sunar; API doğrudan EF Core'a bağlanmaz.
- Entegrasyon testleri anonim kullanıcı için `401`, Operator için `403`, yetkili Admin için `200` sonucunu doğrular.
- Bu faz sonunda 8 backend projesi uyarısız derlendi; üç test projesindeki toplam 26 test geçti.

### React auth state ve korumalı rotalar

Frontend oturum zinciri şöyledir:

```text
Uygulama açılır
  → AuthProvider `/api/auth/me` çağırır
  ├─ 200: kullanıcı auth state'e yazılır, korumalı route açılır
  └─ 401: kullanıcı `/login` sayfasına yönlendirilir

Login formu
  → CSRF token alır
  → e-posta/parolayı login endpoint'ine gönderir
  → authentication cookie tarayıcıya yazılır
  → kimlik değiştiği için CSRF token yenilenir
  → kullanıcı hedeflediği korumalı sayfaya döner
```

- `authApi.ts`, bütün isteklerde `credentials: "include"` kullanarak cookie'nin API'ye gönderilmesini sağlar.
- `AuthProvider`, kullanıcı, yüklenme, hata ve işlem durumlarının tek merkezidir.
- `ProtectedRoute`, oturum yokken uygulama içeriğinin ve ağır harita modülünün açılmasını engeller.
- `PermissionRoute`, permission'a göre sayfa görünümünü sınırlar; backend policy kontrolünün yerine geçmez.
- React Router, `/login`, `/` ve `/admin/roles` adreslerini birbirinden ayırır.
- `roles.read` permission'ı menüde rol yönetimini gösterir ve salt okunur rol kataloğunu açar.
- Backend CORS listesi hem `localhost:5173` hem `127.0.0.1:5173` geliştirme adreslerine cookie'li isteğe izin verir.
- Tarayıcı açısından `localhost` ve `127.0.0.1` farklı sitelerdir. Frontend `127.0.0.1` üzerinden açılıp API `localhost` adresine çağrıldığında `SameSite=Lax` CSRF cookie'si login POST'una gönderilmez ve API `400` döndürür.
- Development çözümü olarak `VITE_API_BASE_URL` boş bırakılır. Vite, `/api` ve `/health` yollarını backend'e proxy'ler; tarayıcı bütün auth çağrılarını frontend ile aynı origin üzerinden görür.
- Gerçek tarayıcı doğrulamasında CSRF `200`, login `200` döndü; uygulama operasyon haritasına yönlendi ve `Admin` menüsü görüntülendi.
- Frontend lint ve production build geçti; dört test dosyasındaki toplam 8 test başarılı oldu. Backend'in 27 testi de yeniden geçti.
- React Router kurulurken npm iki yüksek önem seviyeli bağımlılık uyarısı bildirdi. Ayrıntılı audit güvenlik politikası nedeniyle çalıştırılamadığı için bu bulgu sonraki güvenlik kontrolüne açık risk olarak taşındı.

## Sonraki öğrenme konusu

Admin kullanıcı yönetimi, ilk bootstrap hesabından sonra yeni Operator/User hesaplarının güvenli biçimde oluşturulmasını ve rollerinin atanmasını sağlayacak. Backend önce `users.manage` permission'ını ve kullanıcının kendi yetkisini yükseltememesi gibi kuralları zorunlu tutacak; frontend yalnızca bu kullanım senaryolarını yöneten ekran olacak.
