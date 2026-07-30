# MapCepte Frontend

React, TypeScript, Vite ve MapLibre tabanlı web uygulamasıdır. Backend'den bağımsız process olarak çalışır.

## Gereksinimler

- Node.js 24 veya uyumlu güncel LTS sürümü
- npm

## İlk çalıştırma

```powershell
cd frontend
Copy-Item .env.example .env
npm install
npm run dev
```

Uygulama `http://localhost:5173` adresinde açılır.

Development ortamında `VITE_API_BASE_URL` boş bırakılır; Vite `/api` ve `/health` isteklerini `http://localhost:5268` adresine aynı-origin proxy üzerinden taşır. Bu düzen, `localhost` ile `127.0.0.1` farkının güvenli cookie/CSRF akışını bozmasını engeller. Ayrı origin kullanan bir deployment'ta API adresi `VITE_API_BASE_URL` ile açıkça verilebilir.

## Giriş ve oturum

- `/login`, e-posta ve parola ile güvenli oturum açar.
- Frontend önce `/api/auth/csrf` üzerinden token alır; login/logout isteklerinde `X-CSRF-TOKEN` gönderir.
- Bütün API çağrıları `credentials: "include"` kullanır. Şifreli `HttpOnly` authentication cookie'si JavaScript tarafından okunmaz.
- Uygulama açılırken `/api/auth/me` çağrılır. Oturumu olmayan kullanıcı korumalı sayfalardan `/login` sayfasına yönlendirilir.
- `roles.read` permission'ına sahip kullanıcı `/admin/roles` menüsünü ve salt okunur rol kataloğunu görebilir.
- `users.read` ve `roles.read` permission'larına sahip kullanıcı `/admin/users` sayfasını görebilir; `users.manage` ile `roles.manage` birlikte varsa kullanıcı oluşturabilir ve başka kullanıcıların rollerini değiştirebilir.
- `audit.read` permission'ına sahip kullanıcı `/admin/audit` sayfasında son 100 güvenlik olayını görebilir.
- Yönetici kendi rollerini arayüzden değiştiremez; backend aynı kuralı ayrıca zorunlu tutar.
- Bir kullanıcı önceki oturumdan kalan yetkisiz bir Admin adresine girerse ana sayfaya yönlendirilir; böylece standart `User` rolü boş bir yetki ekranında kalmaz.
- Arayüzde menü gizleme güvenlik sınırı değildir; backend endpoint'i aynı permission'ı ayrıca zorunlu tutar.

## Kontroller

```powershell
npm run lint
npm run test
npm run build
```

## Harita katman sırası

1. Temel harita
2. Rota çizgileri
3. Duraklar
4. Seçim/vurgu
5. Canlı araçlar

Katman kataloğu `src/features/map/mapLayers.ts`, MapLibre kurulumu ise `src/features/map/TransportMap.tsx` içindedir.

Auth akışı `src/features/auth`, rol ve kullanıcı yönetimi görünümleri ise `src/features/admin` altında bulunur.
