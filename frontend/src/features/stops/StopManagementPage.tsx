import { useEffect, useState, type FormEvent } from 'react'
import { useAuth } from '../auth/authState'
import { ApiError, apiRequest, csrfRequest } from '../auth/authApi'
import { StopLocationPicker } from './StopLocationPicker'
import type { StopCatalogItem, StopPageResponse } from './stopModels'

type StopBounds = {
  minLongitude: number
  minLatitude: number
  maxLongitude: number
  maxLatitude: number
}

function requestErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Durak servisine bağlanılamadı.'
}

function StopCard({
  stop,
  canUpdate,
  canArchive,
  onChanged,
}: {
  stop: StopCatalogItem
  canUpdate: boolean
  canArchive: boolean
  onChanged: (stop: StopCatalogItem) => void
}) {
  const [isEditing, setIsEditing] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [name, setName] = useState(stop.name)
  const [code, setCode] = useState(stop.code || '')
  const [description, setDescription] = useState(stop.description || '')
  const [color, setColor] = useState(stop.color)
  const [longitude, setLongitude] = useState(String(stop.longitude))
  const [latitude, setLatitude] = useState(String(stop.latitude))

  async function updateStop(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setError(null)

    try {
      const updated = await csrfRequest<StopCatalogItem>(
        `/api/stops/${stop.id}`,
        {
          method: 'PUT',
          body: JSON.stringify({
            name: name.trim(),
            code: code.trim() || null,
            description: description.trim() || null,
            color,
            longitude: Number(longitude),
            latitude: Number(latitude),
            version: stop.version,
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

  async function archiveStop() {
    if (!window.confirm(`${stop.name} durağı arşivlensin mi?`)) {
      return
    }

    setIsSaving(true)
    setError(null)
    try {
      const archived = await csrfRequest<StopCatalogItem>(
        `/api/stops/${stop.id}/archive`,
        {
          method: 'POST',
          body: JSON.stringify({ version: stop.version }),
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
      <form className="stop-card stop-edit-form" onSubmit={updateStop}>
        <label>
          <span>Durak adı</span>
          <input
            disabled={isSaving}
            maxLength={160}
            onChange={(event) => setName(event.target.value)}
            required
            value={name}
          />
        </label>
        <label>
          <span>Kod</span>
          <input
            disabled={isSaving}
            maxLength={40}
            onChange={(event) => setCode(event.target.value)}
            value={code}
          />
        </label>
        <label>
          <span>Açıklama</span>
          <textarea
            disabled={isSaving}
            maxLength={1000}
            onChange={(event) => setDescription(event.target.value)}
            rows={3}
            value={description}
          />
        </label>
        <div className="stop-edit-row">
          <label>
            <span>Renk</span>
            <input
              aria-label="Düzenlenen durak rengi"
              disabled={isSaving}
              onChange={(event) => setColor(event.target.value)}
              type="color"
              value={color}
            />
          </label>
          <label>
            <span>Boylam</span>
            <input
              disabled={isSaving}
              max={180}
              min={-180}
              onChange={(event) => setLongitude(event.target.value)}
              required
              step="any"
              type="number"
              value={longitude}
            />
          </label>
          <label>
            <span>Enlem</span>
            <input
              disabled={isSaving}
              max={90}
              min={-90}
              onChange={(event) => setLatitude(event.target.value)}
              required
              step="any"
              type="number"
              value={latitude}
            />
          </label>
        </div>
        <StopLocationPicker
          color={color}
          compact
          disabled={isSaving}
          latitude={latitude}
          longitude={longitude}
          onChange={(coordinates) => {
            setLongitude(String(coordinates.longitude))
            setLatitude(String(coordinates.latitude))
          }}
        />
        {error && <p className="inline-error" role="alert">{error}</p>}
        <div className="stop-card-actions">
          <button
            className="primary-button compact-button"
            disabled={isSaving}
            type="submit"
          >
            {isSaving ? 'Kaydediliyor…' : 'Değişiklikleri kaydet'}
          </button>
          <button
            className="secondary-button"
            disabled={isSaving}
            onClick={() => setIsEditing(false)}
            type="button"
          >
            Vazgeç
          </button>
        </div>
      </form>
    )
  }

  return (
    <article className="stop-card">
      <div className="stop-card-heading">
        <span
          aria-label={`Durak rengi ${stop.color}`}
          className="stop-color"
          style={{ backgroundColor: stop.color }}
        />
        <div>
          <h3>{stop.name}</h3>
          <code>{stop.code || 'Kod yok'}</code>
        </div>
        <span className={`stop-status stop-status-${stop.status.toLowerCase()}`}>
          {stop.status}
        </span>
      </div>
      {stop.description && <p>{stop.description}</p>}
      <dl>
        <div>
          <dt>Boylam</dt>
          <dd>{stop.longitude.toFixed(6)}</dd>
        </div>
        <div>
          <dt>Enlem</dt>
          <dd>{stop.latitude.toFixed(6)}</dd>
        </div>
      </dl>
      {error && <p className="inline-error" role="alert">{error}</p>}
      {stop.status !== 'Archived' && (canUpdate || canArchive) && (
        <div className="stop-card-actions">
          {canUpdate && (
            <button
              className="secondary-button"
              disabled={isSaving}
              onClick={() => setIsEditing(true)}
              type="button"
            >
              Düzenle
            </button>
          )}
          {canArchive && (
            <button
              className="archive-button"
              disabled={isSaving}
              onClick={() => void archiveStop()}
              type="button"
            >
              Arşivle
            </button>
          )}
        </div>
      )}
    </article>
  )
}

export function StopManagementPage() {
  const { hasPermission } = useAuth()
  const [stops, setStops] = useState<StopCatalogItem[]>([])
  const [page, setPage] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [refreshKey, setRefreshKey] = useState(0)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [bounds, setBounds] = useState<StopBounds | null>(null)
  const [minLongitude, setMinLongitude] = useState('')
  const [minLatitude, setMinLatitude] = useState('')
  const [maxLongitude, setMaxLongitude] = useState('')
  const [maxLatitude, setMaxLatitude] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [isCreating, setIsCreating] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [name, setName] = useState('')
  const [code, setCode] = useState('')
  const [description, setDescription] = useState('')
  const [color, setColor] = useState('#13B8A6')
  const [longitude, setLongitude] = useState('')
  const [latitude, setLatitude] = useState('')
  const canCreate = hasPermission('stops.create')
  const canUpdate = hasPermission('stops.update')
  const canArchive = hasPermission('stops.delete')

  useEffect(() => {
    document.title = 'Duraklar · MapCepte'
    const controller = new AbortController()
    let isCurrentRequest = true
    const parameters = new URLSearchParams({
      page: String(page),
      pageSize: '12',
    })

    if (search) {
      parameters.set('search', search)
    }

    if (bounds) {
      parameters.set('minLongitude', String(bounds.minLongitude))
      parameters.set('minLatitude', String(bounds.minLatitude))
      parameters.set('maxLongitude', String(bounds.maxLongitude))
      parameters.set('maxLatitude', String(bounds.maxLatitude))
    }

    setIsLoading(true)
    setError(null)
    apiRequest<StopPageResponse>(`/api/stops?${parameters}`, {
      signal: controller.signal,
    })
      .then((response) => {
        if (!isCurrentRequest) {
          return
        }

        setStops(response.items)
        setTotalCount(response.totalCount)
        setTotalPages(response.totalPages)
      })
      .catch((requestError: unknown) => {
        if (
          !isCurrentRequest ||
          requestError instanceof DOMException &&
          requestError.name === 'AbortError'
        ) {
          return
        }

        setError(requestErrorMessage(requestError))
      })
      .finally(() => {
        if (isCurrentRequest) {
          setIsLoading(false)
        }
      })

    return () => {
      isCurrentRequest = false
      controller.abort()
    }
  }, [bounds, page, refreshKey, search])

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const coordinateValues = [
      minLongitude,
      minLatitude,
      maxLongitude,
      maxLatitude,
    ]
    const filledCoordinateCount = coordinateValues.filter(Boolean).length

    if (filledCoordinateCount !== 0 && filledCoordinateCount !== 4) {
      setError('Harita alanı için dört sınır değerini de girin.')
      return
    }

    const nextBounds = filledCoordinateCount === 4
      ? {
          minLongitude: Number(minLongitude),
          minLatitude: Number(minLatitude),
          maxLongitude: Number(maxLongitude),
          maxLatitude: Number(maxLatitude),
        }
      : null

    if (
      nextBounds &&
      (nextBounds.minLongitude >= nextBounds.maxLongitude ||
        nextBounds.minLatitude >= nextBounds.maxLatitude)
    ) {
      setError('Minimum sınırlar maksimum sınırlardan küçük olmalıdır.')
      return
    }

    setError(null)
    setPage(1)
    setSearch(searchInput.trim())
    setBounds(nextBounds)
  }

  function clearFilters() {
    setSearchInput('')
    setMinLongitude('')
    setMinLatitude('')
    setMaxLongitude('')
    setMaxLatitude('')
    setSearch('')
    setBounds(null)
    setPage(1)
    setError(null)
  }

  async function createStop(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsCreating(true)
    setError(null)
    setMessage(null)

    try {
      const created = await csrfRequest<StopCatalogItem>('/api/stops', {
        method: 'POST',
        body: JSON.stringify({
          name: name.trim(),
          code: code.trim() || null,
          description: description.trim() || null,
          color,
          longitude: Number(longitude),
          latitude: Number(latitude),
        }),
      })

      setPage(1)
      setRefreshKey((current) => current + 1)
      setName('')
      setCode('')
      setDescription('')
      setLongitude('')
      setLatitude('')
      setMessage(`${created.name} durağı taslak olarak oluşturuldu.`)
    } catch (requestError) {
      setError(requestErrorMessage(requestError))
    } finally {
      setIsCreating(false)
    }
  }

  function changeStop(changed: StopCatalogItem) {
    setRefreshKey((current) => current + 1)
    setMessage(`${changed.name} durağı güncellendi.`)
  }

  return (
    <main className="admin-page">
      <div className="page-heading">
        <div>
          <p className="eyebrow">Ulaşım kataloğu</p>
          <h2>Durak yönetimi</h2>
        </div>
        <span className="phase-badge dark-badge">{totalCount} durak</span>
      </div>

      {canCreate && (
        <form className="stop-create-card" onSubmit={createStop}>
          <div className="user-create-heading">
            <div>
              <p className="eyebrow">Yeni kayıt</p>
              <h3>Durak oluştur</h3>
            </div>
            <span>İlk kayıt durumu: Taslak</span>
          </div>

          <div className="stop-form-grid">
            <label>
              <span>Durak adı</span>
              <input
                disabled={isCreating}
                maxLength={160}
                onChange={(event) => setName(event.target.value)}
                required
                value={name}
              />
            </label>
            <label>
              <span>Kod</span>
              <input
                disabled={isCreating}
                maxLength={40}
                onChange={(event) => setCode(event.target.value)}
                placeholder="Örn. MRK-001"
                value={code}
              />
            </label>
            <label className="color-field">
              <span>Renk</span>
              <input
                aria-label="Durak rengi"
                disabled={isCreating}
                onChange={(event) => setColor(event.target.value)}
                type="color"
                value={color}
              />
              <code>{color.toUpperCase()}</code>
            </label>
            <label>
              <span>Boylam</span>
              <input
                disabled={isCreating}
                max={180}
                min={-180}
                onChange={(event) => setLongitude(event.target.value)}
                placeholder="32.8597"
                required
                step="any"
                type="number"
                value={longitude}
              />
            </label>
            <label>
              <span>Enlem</span>
              <input
                disabled={isCreating}
                max={90}
                min={-90}
                onChange={(event) => setLatitude(event.target.value)}
                placeholder="39.9334"
                required
                step="any"
                type="number"
                value={latitude}
              />
            </label>
            <label className="description-field">
              <span>Açıklama</span>
              <textarea
                disabled={isCreating}
                maxLength={1000}
                onChange={(event) => setDescription(event.target.value)}
                rows={3}
                value={description}
              />
            </label>
          </div>

          <StopLocationPicker
            color={color}
            disabled={isCreating}
            latitude={latitude}
            longitude={longitude}
            onChange={(coordinates) => {
              setLongitude(String(coordinates.longitude))
              setLatitude(String(coordinates.latitude))
            }}
          />

          <button
            className="primary-button compact-button"
            disabled={isCreating}
            type="submit"
          >
            {isCreating ? 'Oluşturuluyor…' : 'Durak oluştur'}
          </button>
        </form>
      )}

      <form className="stop-filter-card" onSubmit={applyFilters}>
        <div className="stop-filter-heading">
          <div>
            <p className="eyebrow">Liste filtreleri</p>
            <h3>Durak ara ve harita alanını sınırla</h3>
          </div>
          <button className="secondary-button" onClick={clearFilters} type="button">
            Filtreleri temizle
          </button>
        </div>
        <label>
          <span>Ad veya kod</span>
          <input
            maxLength={160}
            onChange={(event) => setSearchInput(event.target.value)}
            placeholder="Örn. Merkez veya MRK-001"
            value={searchInput}
          />
        </label>
        <div className="stop-bounds-grid">
          <label>
            <span>Min. boylam</span>
            <input max={180} min={-180} onChange={(event) => setMinLongitude(event.target.value)} step="any" type="number" value={minLongitude} />
          </label>
          <label>
            <span>Min. enlem</span>
            <input max={90} min={-90} onChange={(event) => setMinLatitude(event.target.value)} step="any" type="number" value={minLatitude} />
          </label>
          <label>
            <span>Maks. boylam</span>
            <input max={180} min={-180} onChange={(event) => setMaxLongitude(event.target.value)} step="any" type="number" value={maxLongitude} />
          </label>
          <label>
            <span>Maks. enlem</span>
            <input max={90} min={-90} onChange={(event) => setMaxLatitude(event.target.value)} step="any" type="number" value={maxLatitude} />
          </label>
        </div>
        <button className="primary-button compact-button" type="submit">
          Filtreleri uygula
        </button>
      </form>

      {message && <p className="success-message" role="status">{message}</p>}
      {error && <p className="form-error" role="alert">{error}</p>}
      {isLoading && <p role="status">Duraklar yükleniyor…</p>}

      {!isLoading && !error && (
        <>
          <div className="stop-grid">
            {stops.map((stop) => (
              <StopCard
                canArchive={canArchive}
                canUpdate={canUpdate}
                key={stop.id}
                onChanged={changeStop}
                stop={stop}
              />
            ))}
            {stops.length === 0 && (
              <div className="empty-stop-list">
                Filtrelere uyan, görüntüleyebileceğiniz bir durak bulunmuyor.
              </div>
            )}
          </div>
          {totalPages > 0 && (
            <nav aria-label="Durak sayfaları" className="stop-pagination">
              <button className="secondary-button" disabled={page <= 1} onClick={() => setPage((current) => current - 1)} type="button">
                Önceki
              </button>
              <span>Sayfa {page} / {totalPages}</span>
              <button className="secondary-button" disabled={page >= totalPages} onClick={() => setPage((current) => current + 1)} type="button">
                Sonraki
              </button>
            </nav>
          )}
        </>
      )}
    </main>
  )
}
