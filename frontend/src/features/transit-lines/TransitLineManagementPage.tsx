import { useEffect, useState, type FormEvent } from 'react'
import { useAuth } from '../auth/authState'
import { ApiError, apiRequest, csrfRequest } from '../auth/authApi'
import type { StopCatalogItem, StopPageResponse } from '../stops/stopModels'
import type {
  TransitLineCatalogItem,
  TransitLinePageResponse,
  TransitLineStopItem,
} from './transitLineModels'
import type {
  RoutePathCatalogItem,
  RoutePathDirection,
} from '../route-paths/routePathModels'
import { TimetableMatrixView } from '../trips/TimetableMatrixView'

function requestErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Güzergâh servisine bağlanılamadı.'
}

function formatDistance(meters: number): string {
  return meters >= 1000
    ? `${(meters / 1000).toFixed(2)} km`
    : `${Math.round(meters)} m`
}

function formatDuration(seconds: number): string {
  const mins = Math.round(seconds / 60)
  return mins >= 60
    ? `${Math.floor(mins / 60)} sa ${mins % 60} dk`
    : `${mins} dk`
}

function TransitLineRoutesManager({
  line,
  canGenerateRoute,
}: {
  line: TransitLineCatalogItem
  canGenerateRoute: boolean
}) {
  const [routes, setRoutes] = useState<RoutePathCatalogItem[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [isGenerating, setIsGenerating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Form states
  const [routeName, setRouteName] = useState('Gidiş Rotası')
  const [direction, setDirection] = useState<RoutePathDirection>('Outbound')
  const [colorOverride, setColorOverride] = useState('')

  useEffect(() => {
    let isCurrent = true
    setIsLoading(true)
    setError(null)

    apiRequest<RoutePathCatalogItem[]>(`/api/transit-lines/${line.id}/route-paths`)
      .then((data) => {
        if (isCurrent) {
          setRoutes(data)
        }
      })
      .catch((err) => {
        if (isCurrent) {
          setError(requestErrorMessage(err))
        }
      })
      .finally(() => {
        if (isCurrent) {
          setIsLoading(false)
        }
      })

    return () => {
      isCurrent = false
    }
  }, [line.id])

  async function handleGenerateRoute(event: FormEvent) {
    event.preventDefault()
    setIsGenerating(true)
    setError(null)

    try {
      const createdRoute = await csrfRequest<RoutePathCatalogItem>(
        `/api/transit-lines/${line.id}/route-paths/generate`,
        {
          method: 'POST',
          body: JSON.stringify({
            name: routeName.trim(),
            direction,
            colorOverride: colorOverride.trim() || null,
          }),
        },
      )
      setRoutes((prev) => [createdRoute, ...prev])
      setRouteName('Dönüş Rotası')
    } catch (err) {
      setError(requestErrorMessage(err))
    } finally {
      setIsGenerating(false)
    }
  }

  return (
    <div className="line-stops-manager">
      <h4>Güzergâh Rotaları ({routes.length})</h4>

      {error && <p className="error-message" role="alert">{error}</p>}

      {canGenerateRoute && (
        <form className="add-stop-form" onSubmit={handleGenerateRoute}>
          <input
            type="text"
            placeholder="Rota adı (örn. Gidiş Rotası)"
            value={routeName}
            onChange={(e) => setRouteName(e.target.value)}
            required
            disabled={isGenerating}
            maxLength={100}
          />
          <select
            value={direction}
            onChange={(e) => setDirection(e.target.value as RoutePathDirection)}
            disabled={isGenerating}
          >
            <option value="Outbound">Gidiş</option>
            <option value="Inbound">Dönüş</option>
            <option value="Alternative">Alternatif</option>
          </select>
          <input
            type="color"
            value={colorOverride || line.color}
            onChange={(e) => setColorOverride(e.target.value)}
            disabled={isGenerating}
            title="Rota özel rengi (isteğe bağlı)"
          />
          <button
            type="submit"
            className="primary-button"
            disabled={isGenerating || line.stopCount < 2}
            title={line.stopCount < 2 ? 'En az 2 durak eklemelisiniz' : 'Rota Üret'}
          >
            {isGenerating ? 'Rota Hesaplanıyor…' : 'Rota Üret'}
          </button>
        </form>
      )}

      {isLoading ? (
        <p className="loading-text">Rotalar yükleniyor...</p>
      ) : routes.length === 0 ? (
        <p className="empty-text">Bu güzergâha henüz rota üretilmedi.</p>
      ) : (
        <ul className="line-stops-list">
          {routes.map((route) => (
            <li key={route.id} className="line-stop-item">
              <span className="stop-sequence">v{route.version}</span>
              <div className="stop-info">
                <strong>{route.name}</strong>
                <span>
                  {formatDistance(route.distanceMeters)} · {formatDuration(route.durationSeconds)} ({route.routingEngine})
                </span>
              </div>
              <div className="stop-actions">
                <span className={`status-pill status-${route.status.toLowerCase()}`}>
                  {route.status}
                </span>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

function TransitLineStopsManager({
  line,
  canUpdate,
  canReorder,
  onLineChanged,
}: {
  line: TransitLineCatalogItem
  canUpdate: boolean
  canReorder: boolean
  onLineChanged: (updatedLine: TransitLineCatalogItem) => void
}) {
  const [lineStops, setLineStops] = useState<TransitLineStopItem[]>([])
  const [availableStops, setAvailableStops] = useState<StopCatalogItem[]>([])
  const [selectedStopId, setSelectedStopId] = useState<string>('')
  const [isLoading, setIsLoading] = useState(false)
  const [isActionBusy, setIsActionBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let isCurrent = true
    setIsLoading(true)
    setError(null)

    Promise.all([
      apiRequest<TransitLineStopItem[]>(`/api/transit-lines/${line.id}/stops`),
      apiRequest<StopPageResponse>('/api/stops?pageSize=100'),
    ])
      .then(([stopsData, availableData]) => {
        if (isCurrent) {
          setLineStops(stopsData)
          setAvailableStops(availableData.items)
        }
      })
      .catch((err) => {
        if (isCurrent) {
          setError(requestErrorMessage(err))
        }
      })
      .finally(() => {
        if (isCurrent) {
          setIsLoading(false)
        }
      })

    return () => {
      isCurrent = false
    }
  }, [line.id])

  async function handleAddStop(event: FormEvent) {
    event.preventDefault()
    if (!selectedStopId) return

    setIsActionBusy(true)
    setError(null)

    try {
      const updatedStops = await csrfRequest<TransitLineStopItem[]>(
        `/api/transit-lines/${line.id}/stops`,
        {
          method: 'POST',
          body: JSON.stringify({
            stopId: selectedStopId,
            expectedVersion: line.version,
          }),
        },
      )
      setLineStops(updatedStops)
      setSelectedStopId('')
      onLineChanged({
        ...line,
        version: line.version + 1,
        stopCount: updatedStops.length,
      })
    } catch (err) {
      setError(requestErrorMessage(err))
    } finally {
      setIsActionBusy(false)
    }
  }

  async function handleRemoveStop(stopId: string) {
    setIsActionBusy(true)
    setError(null)

    try {
      const updatedStops = await csrfRequest<TransitLineStopItem[]>(
        `/api/transit-lines/${line.id}/stops/${stopId}?version=${line.version}`,
        {
          method: 'DELETE',
        },
      )
      setLineStops(updatedStops)
      onLineChanged({
        ...line,
        version: line.version + 1,
        stopCount: updatedStops.length,
      })
    } catch (err) {
      setError(requestErrorMessage(err))
    } finally {
      setIsActionBusy(false)
    }
  }

  async function handleMove(index: number, direction: 'up' | 'down') {
    const targetIndex = direction === 'up' ? index - 1 : index + 1
    if (targetIndex < 0 || targetIndex >= lineStops.length) return

    const newStops = [...lineStops]
    const temp = newStops[index]
    newStops[index] = newStops[targetIndex]
    newStops[targetIndex] = temp

    const orderedStopIds = newStops.map((s) => s.stopId)

    setIsActionBusy(true)
    setError(null)

    try {
      const updatedStops = await csrfRequest<TransitLineStopItem[]>(
        `/api/transit-lines/${line.id}/stops/order`,
        {
          method: 'PUT',
          body: JSON.stringify({
            orderedStopIds,
            expectedVersion: line.version,
          }),
        },
      )
      setLineStops(updatedStops)
      onLineChanged({
        ...line,
        version: line.version + 1,
      })
    } catch (err) {
      setError(requestErrorMessage(err))
    } finally {
      setIsActionBusy(false)
    }
  }

  const existingStopIds = new Set(lineStops.map((s) => s.stopId))
  const addableStops = availableStops.filter((s) => !existingStopIds.has(s.id))

  return (
    <div className="line-stops-manager">
      <h4>Güzergâh Durakları ({lineStops.length})</h4>

      {error && <p className="error-message" role="alert">{error}</p>}

      {canUpdate && addableStops.length > 0 && (
        <form className="add-stop-form" onSubmit={handleAddStop}>
          <select
            value={selectedStopId}
            onChange={(e) => setSelectedStopId(e.target.value)}
            disabled={isActionBusy}
            aria-label="Eklenecek durak seçin"
          >
            <option value="">Durak seçin...</option>
            {addableStops.map((stop) => (
              <option key={stop.id} value={stop.id}>
                {stop.name} {stop.code ? `(${stop.code})` : ''}
              </option>
            ))}
          </select>
          <button
            type="submit"
            className="secondary-button"
            disabled={!selectedStopId || isActionBusy}
          >
            Durak ekle
          </button>
        </form>
      )}

      {isLoading ? (
        <p className="loading-text">Duraklar yükleniyor...</p>
      ) : lineStops.length === 0 ? (
        <p className="empty-text">Bu güzergâha henüz durak eklenmedi.</p>
      ) : (
        <ul className="line-stops-list">
          {lineStops.map((lineStop, index) => (
            <li key={lineStop.lineStopId} className="line-stop-item">
              <span className="stop-sequence">{lineStop.sequence}</span>
              <div className="stop-info">
                <strong>{lineStop.stopName}</strong>
                {lineStop.stopCode && <span>({lineStop.stopCode})</span>}
              </div>
              <div className="stop-actions">
                {canReorder && (
                  <>
                    <button
                      type="button"
                      className="icon-button"
                      disabled={index === 0 || isActionBusy}
                      onClick={() => handleMove(index, 'up')}
                      title="Yukarı taşı"
                      aria-label={`${lineStop.stopName} durağını yukarı taşı`}
                    >
                      ↑
                    </button>
                    <button
                      type="button"
                      className="icon-button"
                      disabled={index === lineStops.length - 1 || isActionBusy}
                      onClick={() => handleMove(index, 'down')}
                      title="Aşağı taşı"
                      aria-label={`${lineStop.stopName} durağını aşağı taşı`}
                    >
                      ↓
                    </button>
                  </>
                )}
                {canUpdate && (
                  <button
                    type="button"
                    className="danger-button-sm"
                    disabled={isActionBusy}
                    onClick={() => handleRemoveStop(lineStop.stopId)}
                    title="Hattan çıkar"
                  >
                    Çıkar
                  </button>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

function TransitLineCard({
  line,
  canUpdate,
  canArchive,
  canReorder,
  canGenerateRoute,
  onChanged,
}: {
  line: TransitLineCatalogItem
  canUpdate: boolean
  canArchive: boolean
  canReorder: boolean
  canGenerateRoute: boolean
  onChanged: (updatedLine: TransitLineCatalogItem) => void
}) {
  const [isEditing, setIsEditing] = useState(false)
  const [isStopsOpen, setIsStopsOpen] = useState(false)
  const [isRoutesOpen, setIsRoutesOpen] = useState(false)
  const [isTimetableOpen, setIsTimetableOpen] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [name, setName] = useState(line.name)
  const [code, setCode] = useState(line.code)
  const [description, setDescription] = useState(line.description || '')
  const [color, setColor] = useState(line.color)

  async function updateLine(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setError(null)

    try {
      const updated = await csrfRequest<TransitLineCatalogItem>(
        `/api/transit-lines/${line.id}`,
        {
          method: 'PUT',
          body: JSON.stringify({
            name: name.trim(),
            code: code.trim(),
            description: description.trim() || null,
            color,
            version: line.version,
          }),
        },
      )
      onChanged(updated)
      setIsEditing(false)
    } catch (requestError) {
      setError(requestErrorMessage(requestError))
    } finally {
      setIsSaving(false)
    }
  }

  async function archiveLine() {
    if (!window.confirm(`${line.name} güzergâhı arşivlensin mi?`)) {
      return
    }

    setIsSaving(true)
    setError(null)

    try {
      const archived = await csrfRequest<TransitLineCatalogItem>(
        `/api/transit-lines/${line.id}/archive`,
        {
          method: 'POST',
          body: JSON.stringify({ version: line.version }),
        },
      )
      onChanged(archived)
      setIsEditing(false)
    } catch (requestError) {
      setError(requestErrorMessage(requestError))
    } finally {
      setIsSaving(false)
    }
  }

  if (isEditing) {
    return (
      <form className="stop-card stop-edit-form" onSubmit={updateLine}>
        <label>
          <span>Güzergâh adı</span>
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            maxLength={100}
          />
        </label>
        <label>
          <span>Güzergâh kodu</span>
          <input
            value={code}
            onChange={(e) => setCode(e.target.value)}
            required
            maxLength={50}
          />
        </label>
        <label>
          <span>Açıklama</span>
          <input
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            maxLength={500}
          />
        </label>
        <label>
          <span>Renk</span>
          <input
            type="color"
            value={color}
            onChange={(e) => setColor(e.target.value)}
          />
        </label>

        {error && <p className="error-message" role="alert">{error}</p>}

        <div className="card-actions">
          <button type="submit" className="primary-button" disabled={isSaving}>
            Kaydet
          </button>
          <button
            type="button"
            className="secondary-button"
            onClick={() => setIsEditing(false)}
            disabled={isSaving}
          >
            İptal
          </button>
        </div>
      </form>
    )
  }

  return (
    <article className="stop-card">
      <header className="stop-card-header">
        <div>
          <h3>{line.name}</h3>
          <span className="stop-code-badge">{line.code}</span>
        </div>
        <span
          className="color-preview-pill"
          style={{ backgroundColor: line.color }}
          title={`Renk: ${line.color}`}
        />
      </header>

      {line.description && <p className="stop-description">{line.description}</p>}

      <div className="stop-meta">
        <span>Duraklar: {line.stopCount}</span>
        <span className={`status-pill status-${line.status.toLowerCase()}`}>
          {line.status}
        </span>
      </div>

      {error && <p className="error-message" role="alert">{error}</p>}

      <div className="card-actions">
        <button
          type="button"
          className="secondary-button"
          onClick={() => setIsStopsOpen(!isStopsOpen)}
        >
          {isStopsOpen ? 'Durakları Gizle' : 'Durakları Yönet'}
        </button>
        <button
          type="button"
          className="secondary-button"
          onClick={() => setIsRoutesOpen(!isRoutesOpen)}
        >
          {isRoutesOpen ? 'Rotaları Gizle' : 'Rotaları Yönet'}
        </button>
        <button
          type="button"
          className="secondary-button"
          onClick={() => setIsTimetableOpen(!isTimetableOpen)}
        >
          {isTimetableOpen ? 'Çizelgeyi Gizle' : 'Zaman Çizelgesi'}
        </button>
        {canUpdate && line.status !== 'Archived' && (
          <button
            type="button"
            className="secondary-button"
            onClick={() => setIsEditing(true)}
          >
            Düzenle
          </button>
        )}
        {canArchive && line.status !== 'Archived' && (
          <button
            type="button"
            className="danger-button"
            disabled={isSaving}
            onClick={() => void archiveLine()}
          >
            Arşivle
          </button>
        )}
      </div>

      {isStopsOpen && (
        <TransitLineStopsManager
          line={line}
          canUpdate={canUpdate}
          canReorder={canReorder}
          onLineChanged={onChanged}
        />
      )}

      {isRoutesOpen && (
        <TransitLineRoutesManager
          line={line}
          canGenerateRoute={canGenerateRoute}
        />
      )}

      {isTimetableOpen && (
        <TimetableMatrixView
          line={line}
          canUpdate={canUpdate}
        />
      )}
    </article>
  )
}

export function TransitLineManagementPage() {
  const { hasPermission } = useAuth()
  const canCreate = hasPermission('transit_lines.create')
  const canUpdate = hasPermission('transit_lines.update')
  const canArchive = hasPermission('transit_lines.delete')
  const canReorder = hasPermission('transit_lines.reorder_stops')
  const canGenerateRoute = hasPermission('route_paths.generate')

  const [lines, setLines] = useState<TransitLineCatalogItem[]>([])
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [isLoading, setIsLoading] = useState(false)
  const [isCreating, setIsCreating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Form states
  const [newName, setNewName] = useState('')
  const [newCode, setNewCode] = useState('')
  const [newDescription, setNewDescription] = useState('')
  const [newColor, setNewColor] = useState('#1e40af')

  useEffect(() => {
    document.title = 'Güzergâh Yönetimi · MapCepte'
  }, [])

  useEffect(() => {
    let isCurrent = true
    setIsLoading(true)
    setError(null)

    const params = new URLSearchParams()
    if (search.trim()) params.set('search', search.trim())
    params.set('page', String(page))
    params.set('pageSize', '12')

    apiRequest<TransitLinePageResponse>(`/api/transit-lines?${params.toString()}`)
      .then((data) => {
        if (isCurrent) {
          setLines(data.items)
          setTotalPages(data.totalPages)
        }
      })
      .catch((err) => {
        if (isCurrent) {
          setError(requestErrorMessage(err))
        }
      })
      .finally(() => {
        if (isCurrent) {
          setIsLoading(false)
        }
      })

    return () => {
      isCurrent = false
    }
  }, [search, page])

  async function createLine(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsCreating(true)
    setError(null)

    try {
      const created = await csrfRequest<TransitLineCatalogItem>('/api/transit-lines', {
        method: 'POST',
        body: JSON.stringify({
          name: newName.trim(),
          code: newCode.trim(),
          description: newDescription.trim() || null,
          color: newColor,
        }),
      })

      setLines((prev) => [created, ...prev])
      setNewName('')
      setNewCode('')
      setNewDescription('')
      setNewColor('#1e40af')
    } catch (requestError) {
      setError(requestErrorMessage(requestError))
    } finally {
      setIsCreating(false)
    }
  }

  function handleLineChanged(updatedLine: TransitLineCatalogItem) {
    setLines((prev) =>
      prev.map((line) => (line.id === updatedLine.id ? updatedLine : line)),
    )
  }

  return (
    <main className="admin-page">
      <header className="page-header">
        <div>
          <p className="eyebrow">Ulaşım Yönetimi</p>
          <h2>Güzergâhlar ve Duraklar</h2>
        </div>
      </header>

      {canCreate && (
        <section className="admin-card">
          <h3>Yeni Güzergâh Ekle</h3>
          <form className="stop-form" onSubmit={createLine}>
            <div className="form-grid">
              <label>
                <span>Güzergâh adı</span>
                <input
                  value={newName}
                  onChange={(e) => setNewName(e.target.value)}
                  placeholder="Örn. 100 Merkez - Kampüs"
                  required
                  maxLength={100}
                />
              </label>
              <label>
                <span>Hat kodu</span>
                <input
                  value={newCode}
                  onChange={(e) => setNewCode(e.target.value)}
                  placeholder="Örn. M-100"
                  required
                  maxLength={50}
                />
              </label>
              <label>
                <span>Açıklama</span>
                <input
                  value={newDescription}
                  onChange={(e) => setNewDescription(e.target.value)}
                  placeholder="İsteğe bağlı açıklama"
                  maxLength={500}
                />
              </label>
              <label>
                <span>Renk</span>
                <input
                  type="color"
                  value={newColor}
                  onChange={(e) => setNewColor(e.target.value)}
                />
              </label>
            </div>
            <button
              type="submit"
              className="primary-button"
              disabled={isCreating}
            >
              {isCreating ? 'Kaydediliyor…' : 'Güzergâhı Kaydet'}
            </button>
          </form>
        </section>
      )}

      <section className="admin-card">
        <div className="card-header-bar">
          <h3>Güzergâh Listesi</h3>
          <input
            type="search"
            placeholder="Ad veya kod ile ara…"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value)
              setPage(1)
            }}
            className="search-input"
          />
        </div>

        {error && <p className="error-message" role="alert">{error}</p>}

        {isLoading ? (
          <p className="loading-text">Güzergâhlar yükleniyor…</p>
        ) : lines.length === 0 ? (
          <p className="empty-text">Kayıtlı güzergâh bulunamadı.</p>
        ) : (
          <div className="stops-grid">
            {lines.map((line) => (
              <TransitLineCard
                key={line.id}
                line={line}
                canUpdate={canUpdate}
                canArchive={canArchive}
                canReorder={canReorder}
                canGenerateRoute={canGenerateRoute}
                onChanged={handleLineChanged}
              />
            ))}
          </div>
        )}

        {totalPages > 1 && (
          <div className="pagination">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
            >
              Önceki
            </button>
            <span>
              {page} / {totalPages}
            </span>
            <button
              type="button"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Sonraki
            </button>
          </div>
        )}
      </section>
    </main>
  )
}
