import { lazy, Suspense, useCallback, useEffect, useState } from 'react'
import {
  Navigate,
  NavLink,
  Outlet,
  Route,
  Routes,
} from 'react-router-dom'
import './App.css'
import { AdminRolesPage } from './features/admin/AdminRolesPage'
import { AdminAuditPage } from './features/admin/AdminAuditPage'
import { AdminUsersPage } from './features/admin/AdminUsersPage'
import { useAuth } from './features/auth/authState'
import { LoginPage } from './features/auth/LoginPage'
import {
  PermissionRoute,
  ProtectedRoute,
} from './features/auth/ProtectedRoute'
import { LayerPanel } from './features/map/LayerPanel'
import {
  loadStopsInBounds,
  type MapBounds,
} from './features/map/stopMapData'
import {
  DEFAULT_LAYER_VISIBILITY,
  type LayerVisibility,
  type OperationalLayerId,
} from './features/map/mapLayers'
import { API_BASE_URL, useApiStatus } from './shared/useApiStatus'
import type { StopCatalogItem } from './features/stops/stopModels'
import type { RoutePathCatalogItem } from './features/route-paths/routePathModels'
import type { TransitLinePageResponse } from './features/transit-lines/transitLineModels'
import { apiRequest } from './features/auth/authApi'

const TransportMap = lazy(async () => {
  const module = await import('./features/map/TransportMap')

  return { default: module.TransportMap }
})

const StopManagementPage = lazy(async () => {
  const module = await import('./features/stops/StopManagementPage')

  return { default: module.StopManagementPage }
})

const TransitLineManagementPage = lazy(async () => {
  const module = await import('./features/transit-lines/TransitLineManagementPage')

  return { default: module.TransitLineManagementPage }
})

const apiStatusCopy = {
  checking: 'API kontrol ediliyor',
  online: 'API bağlantısı hazır',
  offline: 'API bekleniyor',
} as const

function AuthenticatedLayout() {
  const { user, hasPermission, isSubmitting, logout } = useAuth()
  const apiStatus = useApiStatus()

  return (
    <div className="app-shell">
      <header className="topbar">
        <NavLink className="brand-lockup brand-link" to="/">
          <div className="brand-mark" aria-hidden="true">MC</div>
          <div>
            <p className="eyebrow">Ulaşım yönetim platformu</p>
            <h1>MapCepte</h1>
          </div>
        </NavLink>

        <div className="topbar-status">
          <span className={`api-status api-status-${apiStatus}`}>
            <i aria-hidden="true" />
            {apiStatusCopy[apiStatus]}
          </span>
          <div className="user-summary">
            <strong>{user?.displayName}</strong>
            <span>{user?.roles.join(', ')}</span>
          </div>
          <button
            className="logout-button"
            disabled={isSubmitting}
            onClick={() => void logout()}
            type="button"
          >
            Çıkış
          </button>
        </div>
      </header>

      <nav className="primary-nav" aria-label="Ana menü">
        <NavLink end to="/">Operasyon haritası</NavLink>
        {hasPermission('stops.read') && (
          <NavLink to="/stops">Duraklar</NavLink>
        )}
        {hasPermission('transit_lines.read') && (
          <NavLink to="/transit-lines">Güzergâhlar</NavLink>
        )}
        {hasPermission('roles.read') && (
          <NavLink to="/admin/roles">Rol yönetimi</NavLink>
        )}
        {hasPermission('users.read') && hasPermission('roles.read') && (
          <NavLink to="/admin/users">Kullanıcı yönetimi</NavLink>
        )}
        {hasPermission('audit.read') && (
          <NavLink to="/admin/audit">Audit kayıtları</NavLink>
        )}
      </nav>

      <Outlet />
    </div>
  )
}

function MapPage() {
  const { user } = useAuth()
  const [visibility, setVisibility] = useState<LayerVisibility>(
    DEFAULT_LAYER_VISIBILITY,
  )
  const [bounds, setBounds] = useState<MapBounds | null>(null)
  const [stops, setStops] = useState<StopCatalogItem[]>([])
  const [routes, setRoutes] = useState<RoutePathCatalogItem[]>([])
  const [areStopsLoading, setAreStopsLoading] = useState(false)
  const [stopLoadError, setStopLoadError] = useState<string | null>(null)
  const canReadStops = Boolean(user?.permissions.includes('stops.read'))
  const canReadTransitLines = Boolean(user?.permissions.includes('transit_lines.read'))
  const canReadRoutes = Boolean(user?.permissions.includes('route_paths.read'))

  useEffect(() => {
    document.title = 'Operasyon Haritası · MapCepte'
  }, [])

  const toggleLayer = useCallback((layerId: OperationalLayerId) => {
    setVisibility((current) => ({
      ...current,
      [layerId]: !current[layerId],
    }))
  }, [])

  const updateBounds = useCallback((nextBounds: MapBounds) => {
    setBounds(nextBounds)
  }, [])

  useEffect(() => {
    if (!canReadTransitLines || !canReadRoutes) {
      setRoutes([])
      return
    }

    let isCurrent = true
    apiRequest<TransitLinePageResponse>('/api/transit-lines?pageSize=50')
      .then(async (linesData) => {
        if (!isCurrent || linesData.items.length === 0) return

        const routePromises = linesData.items.map((line) =>
          apiRequest<RoutePathCatalogItem[]>(`/api/transit-lines/${line.id}/route-paths`).catch(() => []),
        )

        const results = await Promise.all(routePromises)
        if (isCurrent) {
          setRoutes(results.flat())
        }
      })
      .catch(() => {
        if (isCurrent) setRoutes([])
      })

    return () => {
      isCurrent = false
    }
  }, [canReadTransitLines, canReadRoutes])

  useEffect(() => {
    if (!bounds || !canReadStops) {
      setStops([])
      return
    }

    const controller = new AbortController()
    let isCurrentRequest = true
    setAreStopsLoading(true)
    setStopLoadError(null)

    loadStopsInBounds(bounds, controller.signal)
      .then((loadedStops) => {
        if (isCurrentRequest) {
          setStops(loadedStops)
        }
      })
      .catch((error: unknown) => {
        if (
          isCurrentRequest &&
          !(error instanceof DOMException && error.name === 'AbortError')
        ) {
          setStopLoadError('Görünür duraklar yüklenemedi.')
        }
      })
      .finally(() => {
        if (isCurrentRequest) {
          setAreStopsLoading(false)
        }
      })

    return () => {
      isCurrentRequest = false
      controller.abort()
    }
  }, [bounds, canReadStops])

  return (
    <main className="workspace">
      <aside className="sidebar">
        <section className="intro-card">
          <p className="eyebrow">Operasyon merkezi</p>
          <h2>Ulaşım verisini katmanlar halinde yönetin.</h2>
          <p>
            Duraklar, güzergâhlar, rotalar ve gelecekte canlı araçlar aynı
            harita üzerinde bağımsız katmanlar olarak çalışır.
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
          <code>{API_BASE_URL || 'Aynı origin · Vite proxy'}</code>
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
            <span><i className="legend-route" aria-hidden="true" />Rota</span>
            <span><i className="legend-stop" aria-hidden="true" />Durak</span>
            {canReadStops && (
              <span>{areStopsLoading ? 'Duraklar yükleniyor…' : `${stops.length} görünür`}</span>
            )}
          </div>
        </div>

        {stopLoadError && (
          <p className="map-data-error" role="alert">{stopLoadError}</p>
        )}

        <Suspense
          fallback={
            <div className="map-frame map-loading" role="status">
              Harita modülü yükleniyor…
            </div>
          }
        >
          <TransportMap
            onBoundsChange={updateBounds}
            stops={stops}
            routes={routes}
            visibility={visibility}
          />
        </Suspense>
      </section>
    </main>
  )
}

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<AuthenticatedLayout />}>
          <Route index element={<MapPage />} />
          <Route
            path="/stops"
            element={
              <PermissionRoute permission="stops.read" redirectTo="/">
                <Suspense
                  fallback={
                    <main className="admin-page" role="status">
                      Durak yönetimi yükleniyor…
                    </main>
                  }
                >
                  <StopManagementPage />
                </Suspense>
              </PermissionRoute>
            }
          />
          <Route
            path="/transit-lines"
            element={
              <PermissionRoute permission="transit_lines.read" redirectTo="/">
                <Suspense
                  fallback={
                    <main className="admin-page" role="status">
                      Güzergâh yönetimi yükleniyor…
                    </main>
                  }
                >
                  <TransitLineManagementPage />
                </Suspense>
              </PermissionRoute>
            }
          />
          <Route
            path="/admin/roles"
            element={
              <PermissionRoute permission="roles.read" redirectTo="/">
                <AdminRolesPage />
              </PermissionRoute>
            }
          />
          <Route
            path="/admin/users"
            element={
              <PermissionRoute permission="users.read" redirectTo="/">
                <PermissionRoute permission="roles.read" redirectTo="/">
                  <AdminUsersPage />
                </PermissionRoute>
              </PermissionRoute>
            }
          />
          <Route
            path="/admin/audit"
            element={
              <PermissionRoute permission="audit.read" redirectTo="/">
                <AdminAuditPage />
              </PermissionRoute>
            }
          />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
