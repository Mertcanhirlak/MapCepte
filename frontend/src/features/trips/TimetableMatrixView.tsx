import { useEffect, useState, type FormEvent } from 'react'
import { ApiError, apiRequest, csrfRequest } from '../auth/authApi'
import type { RoutePathCatalogItem } from '../route-paths/routePathModels'
import type { TransitLineCatalogItem, TransitLineStopItem } from '../transit-lines/transitLineModels'
import type {
  OperatingCalendarCatalogItem,
  TimetableMatrixDto,
  TripCatalogItem,
} from './tripModels'

function requestErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Sefer servisine bağlanılamadı.'
}

export function TimetableMatrixView({
  line,
  canUpdate,
}: {
  line: TransitLineCatalogItem
  canUpdate: boolean
}) {
  const [timetable, setTimetable] = useState<TimetableMatrixDto | null>(null)
  const [calendars, setCalendars] = useState<OperatingCalendarCatalogItem[]>([])
  const [routePaths, setRoutePaths] = useState<RoutePathCatalogItem[]>([])
  const [lineStops, setLineStops] = useState<TransitLineStopItem[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [isActionBusy, setIsActionBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Form states
  const [selectedRoutePathId, setSelectedRoutePathId] = useState('')
  const [selectedCalendarId, setSelectedCalendarId] = useState('')
  const [tripCode, setTripCode] = useState('TRIP-01')
  const [departureTime, setDepartureTime] = useState('08:00')
  const [direction, setDirection] = useState<'Outbound' | 'Inbound'>('Outbound')

  useEffect(() => {
    let isCurrent = true
    setIsLoading(true)
    setError(null)

    Promise.all([
      apiRequest<TimetableMatrixDto>(`/api/transit-lines/${line.id}/timetable`),
      apiRequest<OperatingCalendarCatalogItem[]>('/api/operating-calendars'),
      apiRequest<RoutePathCatalogItem[]>(`/api/transit-lines/${line.id}/route-paths`),
      apiRequest<TransitLineStopItem[]>(`/api/transit-lines/${line.id}/stops`),
    ])
      .then(([matrixData, calendarData, routeData, stopsData]) => {
        if (isCurrent) {
          setTimetable(matrixData)
          setCalendars(calendarData)
          setRoutePaths(routeData.filter((r) => r.status === 'Ready'))
          setLineStops(stopsData)
          if (routeData.length > 0) setSelectedRoutePathId(routeData[0].id)
          if (calendarData.length > 0) setSelectedCalendarId(calendarData[0].id)
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

  async function handleCreateTrip(event: FormEvent) {
    event.preventDefault()
    if (!selectedRoutePathId || !selectedCalendarId) return

    setIsActionBusy(true)
    setError(null)

    try {
      const createdTrip = await csrfRequest<TripCatalogItem>(
        `/api/transit-lines/${line.id}/trips`,
        {
          method: 'POST',
          body: JSON.stringify({
            routePathId: selectedRoutePathId,
            operatingCalendarId: selectedCalendarId,
            tripCode: tripCode.trim(),
            departureTime,
            direction,
          }),
        },
      )

      setTimetable((prev) =>
        prev
          ? {
              ...prev,
              trips: [...prev.trips, createdTrip].sort((a, b) =>
                a.departureTime.localeCompare(b.departureTime),
              ),
            }
          : null,
      )

      // Increment trip code default suggestion
      setTripCode((code) => {
        const match = code.match(/\d+$/)
        if (match) {
          const nextNum = parseInt(match[0], 10) + 1
          return code.replace(/\d+$/, nextNum.toString().padStart(match[0].length, '0'))
        }
        return `${code}-2`
      })
    } catch (err) {
      setError(requestErrorMessage(err))
    } finally {
      setIsActionBusy(false)
    }
  }

  async function handleShiftTrip(tripId: string, minutesOffset: number) {
    setIsActionBusy(true)
    setError(null)

    try {
      const updatedTrip = await csrfRequest<TripCatalogItem>(
        `/api/trips/${tripId}/shift`,
        {
          method: 'POST',
          body: JSON.stringify({ minutesOffset }),
        },
      )

      setTimetable((prev) =>
        prev
          ? {
              ...prev,
              trips: prev.trips
                .map((t) => (t.id === tripId ? updatedTrip : t))
                .sort((a, b) => a.departureTime.localeCompare(b.departureTime)),
            }
          : null,
      )
    } catch (err) {
      setError(requestErrorMessage(err))
    } finally {
      setIsActionBusy(false)
    }
  }

  const stopsMap = new Map(lineStops.map((s) => [s.stopId, s.stopName]))

  return (
    <div className="line-stops-manager">
      <h4>Seferler ve Zaman Çizelgesi Matrisi ({timetable?.trips.length || 0})</h4>

      {error && <p className="error-message" role="alert">{error}</p>}

      {canUpdate && routePaths.length > 0 && (
        <form className="add-stop-form" onSubmit={handleCreateTrip}>
          <input
            type="text"
            placeholder="Sefer kodu (örn. TRIP-01)"
            value={tripCode}
            onChange={(e) => setTripCode(e.target.value)}
            required
            disabled={isActionBusy}
            maxLength={50}
          />
          <input
            type="time"
            value={departureTime}
            onChange={(e) => setDepartureTime(e.target.value)}
            required
            disabled={isActionBusy}
            title="Kalkış saati"
          />
          <select
            value={selectedRoutePathId}
            onChange={(e) => setSelectedRoutePathId(e.target.value)}
            disabled={isActionBusy}
            aria-label="Kullanılacak rota seçin"
          >
            {routePaths.map((path) => (
              <option key={path.id} value={path.id}>
                {path.name} (v{path.version})
              </option>
            ))}
          </select>
          <select
            value={selectedCalendarId}
            onChange={(e) => setSelectedCalendarId(e.target.value)}
            disabled={isActionBusy}
            aria-label="Çalışma günü takvimi seçin"
          >
            {calendars.map((cal) => (
              <option key={cal.id} value={cal.id}>
                {cal.name} ({cal.daysOfWeek})
              </option>
            ))}
          </select>
          <select
            value={direction}
            onChange={(e) => setDirection(e.target.value as 'Outbound' | 'Inbound')}
            disabled={isActionBusy}
          >
            <option value="Outbound">Gidiş</option>
            <option value="Inbound">Dönüş</option>
          </select>
          <button
            type="submit"
            className="primary-button"
            disabled={isActionBusy || !selectedRoutePathId || !selectedCalendarId}
          >
            Sefer Ekle
          </button>
        </form>
      )}

      {routePaths.length === 0 && canUpdate && (
        <p className="empty-text">
          Sefer eklemek için öncelikle &quot;Rotaları Yönet&quot; bölümünden en az bir rota üretmelisiniz.
        </p>
      )}

      {isLoading ? (
        <p className="loading-text">Zaman çizelgesi yükleniyor...</p>
      ) : !timetable || timetable.trips.length === 0 ? (
        <p className="empty-text">Bu güzergâha henüz sefer eklenmedi.</p>
      ) : (
        <div style={{ overflowX: 'auto', marginTop: '1rem' }}>
          <table className="user-table" style={{ fontSize: '0.85rem' }}>
            <thead>
              <tr>
                <th>Sefer Kodu</th>
                <th>Kalkış</th>
                <th>Yön</th>
                {timetable.stops.map((header) => (
                  <th key={header.stopId}>
                    {stopsMap.get(header.stopId) || `Durak #${header.sequence}`}
                  </th>
                ))}
                {canUpdate && <th>Saat Kaydır</th>}
              </tr>
            </thead>
            <tbody>
              {timetable.trips.map((trip) => {
                const stopTimesByStopId = new Map(
                  trip.stopTimes.map((st) => [st.stopId, st.arrivalTime]),
                )

                return (
                  <tr key={trip.id}>
                    <td>
                      <strong>{trip.tripCode}</strong>
                    </td>
                    <td>{trip.departureTime.substring(0, 5)}</td>
                    <td>
                      <span className="stop-code-badge">{trip.direction}</span>
                    </td>
                    {timetable.stops.map((header) => (
                      <td key={header.stopId}>
                        {stopTimesByStopId.get(header.stopId)?.substring(0, 5) || '-'}
                      </td>
                    ))}
                    {canUpdate && (
                      <td>
                        <div style={{ display: 'flex', gap: '0.25rem' }}>
                          <button
                            type="button"
                            className="secondary-button"
                            style={{ padding: '0.2rem 0.4rem', fontSize: '0.75rem' }}
                            disabled={isActionBusy}
                            onClick={() => handleShiftTrip(trip.id, 15)}
                            title="+15 Dakika Kaydır"
                          >
                            +15 dk
                          </button>
                          <button
                            type="button"
                            className="secondary-button"
                            style={{ padding: '0.2rem 0.4rem', fontSize: '0.75rem' }}
                            disabled={isActionBusy}
                            onClick={() => handleShiftTrip(trip.id, -10)}
                            title="-10 Dakika Kaydır"
                          >
                            -10 dk
                          </button>
                        </div>
                      </td>
                    )}
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
