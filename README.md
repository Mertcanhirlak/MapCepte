# MapCepte

MapCepte; durak, güzergâh, gerçek yol geometrisi ve ileride canlı araç takibi yönetimi için geliştirilen ulaşım platformudur.

Proje iki bağımsız uygulamadan oluşur:

- `backend/`: .NET 10 ASP.NET Core Web API, PostgreSQL/PostGIS ve rota motoru entegrasyonu.
- `frontend/`: React, TypeScript, Vite ve MapLibre tabanlı web uygulaması.

## Çalıştırma modeli

Backend ve frontend ayrı terminallerde çalıştırılır. Ayrıntılar kendi README dosyalarındadır:

- [Backend başlangıç rehberi](backend/README.md)
- [Frontend başlangıç rehberi](frontend/README.md)
- [Öğrenme notları](edu.md)

### Terminal 1 — backend

```powershell
cd backend
dotnet restore
dotnet tool restore
dotnet run --project src/Transport.Api
```

API varsayılan olarak `http://localhost:5268` adresinde çalışır.

PostgreSQL ve PostGIS Windows üzerinde doğrudan servis olarak çalışır. İlk veritabanı hazırlığı için [backend rehberini](backend/README.md) kullanın.

### Terminal 2 — frontend

```powershell
cd frontend
npm install
npm run dev
```

Frontend varsayılan olarak `http://localhost:5173` adresinde çalışır.

## Faz durumu

Faz 0 proje temeli ile Faz 1 kimlik doğrulama ve yetkilendirme temeli tamamlanmıştır. Faz 2'de PostGIS tabanlı durak şeması, sahiplik filtreli listeleme, oluşturma, güncelleme, arşivleme ve optimistic concurrency akışları React ekranıyla birlikte hazırdır. Sıradaki dilim haritadan koordinat seçimi ve marker önizlemesidir.
