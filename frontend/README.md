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

`.env` içindeki `VITE_API_BASE_URL`, ayrı çalışan .NET API adresini belirtir. Backend çalışmıyorsa arayüz açılmaya devam eder ancak API durumu “bekleniyor” görünür.

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
