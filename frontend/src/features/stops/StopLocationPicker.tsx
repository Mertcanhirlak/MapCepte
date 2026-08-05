import { useEffect, useRef, useState } from 'react'
import {
  AttributionControl,
  Map as MapLibreMap,
  NavigationControl,
  type GeoJSONSource,
} from 'maplibre-gl'
import { configuredMapStyle, TURKEY_MAP_CENTER } from '../map/mapStyle'

const SOURCE_ID = 'stop-location-picker-source'
const HALO_LAYER_ID = 'stop-location-picker-halo'
const MARKER_LAYER_ID = 'stop-location-picker-marker'

type Coordinates = {
  longitude: number
  latitude: number
}

type StopLocationPickerProps = {
  longitude: string
  latitude: string
  color: string
  disabled?: boolean
  compact?: boolean
  onChange: (coordinates: Coordinates) => void
}

function validCoordinates(
  longitudeValue: string,
  latitudeValue: string,
): Coordinates | null {
  if (longitudeValue.trim() === '' || latitudeValue.trim() === '') {
    return null
  }

  const longitude = Number(longitudeValue)
  const latitude = Number(latitudeValue)
  if (
    !Number.isFinite(longitude) ||
    longitude < -180 ||
    longitude > 180 ||
    !Number.isFinite(latitude) ||
    latitude < -90 ||
    latitude > 90
  ) {
    return null
  }

  return { longitude, latitude }
}

function markerData(coordinates: Coordinates | null) {
  return {
    type: 'FeatureCollection' as const,
    features: coordinates
      ? [
          {
            type: 'Feature' as const,
            properties: {},
            geometry: {
              type: 'Point' as const,
              coordinates: [coordinates.longitude, coordinates.latitude],
            },
          },
        ]
      : [],
  }
}

function roundCoordinate(value: number) {
  return Number(value.toFixed(6))
}

export function StopLocationPicker({
  longitude,
  latitude,
  color,
  disabled = false,
  compact = false,
  onChange,
}: StopLocationPickerProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const mapRef = useRef<MapLibreMap | null>(null)
  const onChangeRef = useRef(onChange)
  const disabledRef = useRef(disabled)
  const initialColorRef = useRef(color)
  const compactRef = useRef(compact)
  const initialCoordinatesRef = useRef(validCoordinates(longitude, latitude))
  const [isReady, setIsReady] = useState(false)
  const [hasError, setHasError] = useState(false)

  onChangeRef.current = onChange
  disabledRef.current = disabled

  useEffect(() => {
    if (!containerRef.current) {
      return
    }

    const initialCoordinates = initialCoordinatesRef.current
    const map = new MapLibreMap({
      container: containerRef.current,
      style: configuredMapStyle(),
      center: initialCoordinates
        ? [initialCoordinates.longitude, initialCoordinates.latitude]
        : TURKEY_MAP_CENTER, // Ankara center [32.8597, 39.9208]
      zoom: initialCoordinates ? 15 : 12,
      minZoom: 5,
      attributionControl: false,
    })

    mapRef.current = map
    map.addControl(new NavigationControl(), 'top-right')
    map.addControl(new AttributionControl({ compact: true }))

    map.on('load', () => {
      map.addSource(SOURCE_ID, {
        type: 'geojson',
        data: markerData(initialCoordinatesRef.current),
      })
      map.addLayer({
        id: HALO_LAYER_ID,
        type: 'circle',
        source: SOURCE_ID,
        paint: {
          'circle-radius': compactRef.current ? 12 : 15,
          'circle-color': 'rgba(255, 255, 255, 0.7)',
          'circle-stroke-color': '#132c36',
          'circle-stroke-width': 2,
        },
      })
      map.addLayer({
        id: MARKER_LAYER_ID,
        type: 'circle',
        source: SOURCE_ID,
        paint: {
          'circle-radius': compactRef.current ? 6 : 8,
          'circle-color': initialColorRef.current,
          'circle-stroke-color': '#ffffff',
          'circle-stroke-width': 2,
        },
      })
      setIsReady(true)
    })

    map.on('click', (event) => {
      if (disabledRef.current) {
        return
      }

      const coordinates = {
        longitude: roundCoordinate(event.lngLat.lng),
        latitude: roundCoordinate(event.lngLat.lat),
      }
      onChangeRef.current(coordinates)
      map.easeTo({ center: [coordinates.longitude, coordinates.latitude] })
    })

    map.on('error', () => setHasError(true))

    return () => {
      map.remove()
      mapRef.current = null
    }
  }, [])

  useEffect(() => {
    const map = mapRef.current
    if (!map || !isReady) {
      return
    }

    const coordinates = validCoordinates(longitude, latitude)
    const source = map.getSource(SOURCE_ID) as GeoJSONSource | undefined
    source?.setData(markerData(coordinates))
    map.setPaintProperty(MARKER_LAYER_ID, 'circle-color', color)
  }, [color, isReady, latitude, longitude])

  const coordinates = validCoordinates(longitude, latitude)

  return (
    <section
      className={`stop-location-picker${compact ? ' compact-picker' : ''}`}
      aria-label="Durak konumu seçimi"
    >
      <div className="picker-heading">
        <div>
          <strong>Haritadan konum seç</strong>
          <span>Haritada durağın bulunduğu noktaya tıklayın.</span>
        </div>
        <code>
          {coordinates
            ? `${coordinates.longitude.toFixed(6)}, ${coordinates.latitude.toFixed(6)}`
            : 'Henüz konum seçilmedi'}
        </code>
      </div>
      <div className="stop-picker-map-wrap">
        <div
          ref={containerRef}
          className="stop-picker-map"
          aria-label="Tıklayarak durak konumu seçilen harita"
        />
        {!isReady && !hasError && (
          <span className="picker-map-status" role="status">
            Harita hazırlanıyor…
          </span>
        )}
        {hasError && !isReady && (
          <span className="picker-map-status picker-map-error" role="alert">
            Harita yüklenemedi; koordinatları alanlardan girebilirsiniz.
          </span>
        )}
      </div>
      <div className="picker-help" style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap', alignItems: 'center', marginTop: '0.5rem' }}>
        <span style={{ fontWeight: 600, fontSize: '0.8rem' }}>Ankara Hazır Konumlar:</span>
        {[
          { name: 'Kızılay', lng: 32.8543, lat: 39.9208 },
          { name: 'Ulus', lng: 32.8547, lat: 39.9431 },
          { name: 'AŞTİ', lng: 32.8128, lat: 39.9186 },
          { name: 'Tunalı', lng: 32.8601, lat: 39.9056 },
          { name: 'Batıkent', lng: 32.7486, lat: 39.9678 },
        ].map((preset) => (
          <button
            key={preset.name}
            type="button"
            className="secondary-button"
            style={{ padding: '0.2rem 0.5rem', fontSize: '0.75rem' }}
            disabled={disabled}
            onClick={() => {
              const coords = { longitude: preset.lng, latitude: preset.lat }
              onChangeRef.current(coords)
              mapRef.current?.easeTo({ center: [preset.lng, preset.lat], zoom: 14 })
            }}
          >
            📍 {preset.name}
          </button>
        ))}
      </div>
    </section>
  )
}
