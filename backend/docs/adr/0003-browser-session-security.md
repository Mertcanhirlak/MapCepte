# ADR 0003: Tarayıcı oturum güvenliği

## Durum

Kabul edildi — 28 Temmuz 2026.

## Karar

- İlk parti React web istemcisi için ASP.NET Core cookie authentication kullanılacaktır.
- Authentication cookie `HttpOnly`, uygun `SameSite` ve üretimde `Secure` olacaktır.
- Oturum ömrü 15 dakika ve sliding expiration açık olacaktır.
- Login ve logout CSRF token ile korunacaktır.
- Frontend bütün oturumlu isteklerde `credentials: "include"` kullanacaktır.
- Tarayıcıya özel JWT veya kalıcı access token verilmeyecektir.

## Gerekçe

Microsoft'un .NET 10 güvenlik rehberi, güvenli web uygulamalarında erişim bilgisini JavaScript/localStorage'a açmak yerine `HttpOnly` cookie kullanılmasını önerir. Bu uygulama tek bir React istemcisi ve ona ait backend'den oluştuğu için cookie tabanlı oturum ürün gereksinimini daha küçük saldırı yüzeyiyle karşılar.

Kaynaklar:

- https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0
- https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
- https://learn.microsoft.com/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0

## Sonuçlar

- Ayrı refresh-token tablosu ve `/api/auth/refresh` endpoint'i yoktur.
- Sliding expiration, aktif oturumun yenilenmesini framework seviyesinde sağlar.
- Cookie otomatik gönderildiği için tüm durum değiştiren tarayıcı endpoint'lerinde CSRF koruması zorunludur.
- Çoklu instance production dağıtımında ASP.NET Core Data Protection anahtar deposu ortak ve kalıcı yapılandırılmalıdır.
- Harici istemci veya üçüncü parti API erişimi gerekirse OIDC/OAuth tabanlı ayrı bir yetkilendirme sunucusu değerlendirilecektir.
