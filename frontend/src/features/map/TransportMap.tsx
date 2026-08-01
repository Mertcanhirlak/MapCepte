import { useEffect, useRef, useState } from 'react'
import {
  AttributionControl,
  Map as MapLibreMap,
  NavigationControl,
} from 'maplibre-gl'
import 'maplibre-gl/dist/maplibre-gl.css'
import {
  MAP_RENDER_LAYER_IDS,
  MAP_SOURCE_IDS,
  OPERATIONAL_LAYER_IDS,
  type LayerVisibility,
} from './mapLayers'
import { configuredMapStyle, TURKEY_MAP_CENTER } from './mapStyle'

interface TransportMapProps {
  visibility: LayerVisibility
}

const emptyFeatureCollection = () => ({
  type: 'FeatureCollection' as const,
  features: [],
})

function mapVisibility(isVisible: boolean): 'visible' | 'none' {
  return isVisible ? 'visible' : 'none'
}

function addOperationalLayers(map: MapLibreMap, visibility: LayerVisibility) {
  for (const layerId of OPERATIONAL_LAYER_IDS) {
    map.addSource(MAP_SOURCE_IDS[layerId], {
      type: 'geojson',
      data: emptyFeatureCollection(),
    })
  }

  map.addLayer({
    id: MAP_RENDER_LAYER_IDS.routes,
    type: 'line',
    source: MAP_SOURCE_IDS.routes,
    layout: {
      visibility: mapVisibility(visibility.routes),
      'line-cap': 'round',
      'line-join': 'round',
    },
    paint: {
      'line-color': '#13b8a6',
      'line-width': 5,
      'line-opacity': 0.88,
    },
  })

  map.addLayer({
    id: MAP_RENDER_LAYER_IDS.stops,
    type: 'circle',
    source: MAP_SOURCE_IDS.stops,
    layout: {
      visibility: mapVisibility(visibility.stops),
    },
    paint: {
      'circle-radius': 7,
      'circle-color': '#f6b84a',
      'circle-stroke-color': '#152630',
      'circle-stroke-width': 2,
    },
  })

  map.addLayer({
    id: MAP_RENDER_LAYER_IDS.selection,
    type: 'circle',
    source: MAP_SOURCE_IDS.selection,
    layout: {
      visibility: mapVisibility(visibility.selection),
    },
    paint: {
      'circle-radius': 12,
      'circle-color': 'rgba(255, 255, 255, 0.18)',
      'circle-stroke-color': '#ffffff',
      'circle-stroke-width': 2,
    },
  })

  map.addLayer({
    id: MAP_RENDER_LAYER_IDS.vehicles,
    type: 'circle',
    source: MAP_SOURCE_IDS.vehicles,
    layout: {
      visibility: mapVisibility(visibility.vehicles),
    },
    paint: {
      'circle-radius': 8,
      'circle-color': '#ef6f6c',
      'circle-stroke-color': '#ffffff',
      'circle-stroke-width': 2,
    },
  })
}

export function TransportMap({ visibility }: TransportMapProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const mapRef = useRef<MapLibreMap | null>(null)
  const initialVisibilityRef = useRef(visibility)
  const [isReady, setIsReady] = useState(false)
  const [hasError, setHasError] = useState(false)

  useEffect(() => {
    if (!containerRef.current) {
      return
    }

    const map = new MapLibreMap({
      container: containerRef.current,
      style: configuredMapStyle(),
      center: TURKEY_MAP_CENTER,
      zoom: 5.15,
      minZoom: 3,
      attributionControl: false,
    })

    mapRef.current = map
    map.addControl(new NavigationControl(), 'top-right')
    map.addControl(new AttributionControl({ compact: true }))

    map.on('load', () => {
      addOperationalLayers(map, initialVisibilityRef.current)
      setIsReady(true)
    })

    map.on('error', () => {
      setHasError(true)
    })

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

    for (const layerId of OPERATIONAL_LAYER_IDS) {
      const renderLayerId = MAP_RENDER_LAYER_IDS[layerId]

      if (map.getLayer(renderLayerId)) {
        map.setLayoutProperty(
          renderLayerId,
          'visibility',
          mapVisibility(visibility[layerId]),
        )
      }
    }
  }, [isReady, visibility])

  return (
    <div className="map-frame">
      <div
        ref={containerRef}
        className="map-canvas"
        aria-label="Türkiye ulaşım haritası"
      />

      {!isReady && !hasError && (
        <div className="map-status" role="status">
          Harita hazırlanıyor…
        </div>
      )}

      {hasError && !isReady && (
        <div className="map-status map-status-error" role="alert">
          Harita kaynağına şu anda erişilemiyor.
        </div>
      )}

      <div className="map-empty-state">
        <span>Katman altyapısı hazır</span>
        <strong>Durak ve rota verileri sonraki fazlarda bağlanacak.</strong>
      </div>
    </div>
  )
}
