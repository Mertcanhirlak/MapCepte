import { lazy, Suspense, useCallback, useState } from 'react'
import './App.css'
import { LayerPanel } from './features/map/LayerPanel'
import {
  DEFAULT_LAYER_VISIBILITY,
  type LayerVisibility,
  type OperationalLayerId,
} from './features/map/mapLayers'
import { API_BASE_URL, useApiStatus } from './shared/useApiStatus'

const TransportMap = lazy(async () => {
  const module = await import('./features/map/TransportMap')

  return { default: module.TransportMap }
})

const apiStatusCopy = {
  checking: 'API kontrol ediliyor',
  online: 'API bağlantısı hazır',
  offline: 'API bekleniyor',
} as const

function App() {
  const [visibility, setVisibility] = useState<LayerVisibility>(
    DEFAULT_LAYER_VISIBILITY,
  )
  const apiStatus = useApiStatus()

  const toggleLayer = useCallback((layerId: OperationalLayerId) => {
    setVisibility((current) => ({
      ...current,
      [layerId]: !current[layerId],
    }))
  }, [])

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="brand-lockup">
          <div className="brand-mark" aria-hidden="true">
            MC
          </div>
          <div>
            <p className="eyebrow">Ulaşım yönetim platformu</p>
            <h1>MapCepte</h1>
          </div>
        </div>

        <div className="topbar-status">
          <span className="phase-badge">Faz 0 · Temel</span>
          <span className={`api-status api-status-${apiStatus}`}>
            <i aria-hidden="true" />
            {apiStatusCopy[apiStatus]}
          </span>
        </div>
      </header>

      <main className="workspace">
        <aside className="sidebar">
          <section className="intro-card">
            <p className="eyebrow">Teknik temel</p>
            <h2>Bağımsız uygulamalar, ortak ulaşım verisi.</h2>
            <p>
              React arayüzü katman görünümünü yönetir. .NET 10 API, PostGIS ve
              ileride rota motoruyla konuşur.
            </p>

            <dl className="stack-list">
              <div>
                <dt>Backend</dt>
                <dd>.NET 10</dd>
              </div>
              <div>
                <dt>Veri</dt>
                <dd>PostGIS</dd>
              </div>
              <div>
                <dt>Harita</dt>
                <dd>MapLibre</dd>
              </div>
            </dl>
          </section>

          <LayerPanel visibility={visibility} onToggle={toggleLayer} />

          <section className="connection-card" aria-label="API bağlantısı">
            <span>Frontend bağlantı hedefi</span>
            <code>{API_BASE_URL}</code>
            <p>Adres, frontend `.env` dosyasından değiştirilebilir.</p>
          </section>
        </aside>

        <section className="map-section" aria-labelledby="map-heading">
          <div className="map-toolbar">
            <div>
              <p className="eyebrow">Operasyon görünümü</p>
              <h2 id="map-heading">Türkiye ulaşım haritası</h2>
            </div>
            <div className="map-legend" aria-label="Harita katman özeti">
              <span>
                <i className="legend-route" aria-hidden="true" />
                Rota
              </span>
              <span>
                <i className="legend-stop" aria-hidden="true" />
                Durak
              </span>
            </div>
          </div>

          <Suspense
            fallback={
              <div className="map-frame map-loading" role="status">
                Harita modülü yükleniyor…
              </div>
            }
          >
            <TransportMap visibility={visibility} />
          </Suspense>
        </section>
      </main>
    </div>
  )
}

export default App
