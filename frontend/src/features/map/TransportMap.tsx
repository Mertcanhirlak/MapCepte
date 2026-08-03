import { useEffect, useRef, useState } from 'react'
import {
  AttributionControl,
  Map as MapLibreMap,
  NavigationControl,
  type GeoJSONSource,
} from 'maplibre-gl'
import 'maplibre-gl/dist/maplibre-gl.css'
import {
  MAP_RENDER_LAYER_IDS,
  MAP_SOURCE_IDS,
  OPERATIONAL_LAYER_IDS,
  type LayerVisibility,
} from './mapLayers'
import { configuredMapStyle, TURKEY_MAP_CENTER } from './mapStyle'
import {
  createStopFeatureCollection,
  type MapBounds,
} from './stopMapData'
import { createRouteFeatureCollection } from './routeMapData'
import type { RoutePathCatalogItem } from '../route-paths/routePathModels'
import type { StopCatalogItem } from '../stops/stopModels'

interface TransportMapProps {
  visibility: LayerVisibility
  stops: StopCatalogItem[]
  routes?: RoutePathCatalogItem[]
  onBoundsChange: (bounds: MapBounds) => void
}

const emptyFeatureCollection = () => ({
  type: 'FeatureCollection' as const,
  features: [],
})

function mapVisibility(isVisible: boolean): 'visible' | 'none' {
  return isVisible ? 'visible' : 'none'
}

function addOperationalLayers(
  map: MapLibreMap,
  visibility: LayerVisibility,
  stops: StopCatalogItem[],
  routes: RoutePathCatalogItem[] = [],
) {
  for (const layerId of OPERATIONAL_LAYER_IDS) {
    map.addSource(MAP_SOURCE_IDS[layerId], {
      type: 'geojson',
      data:
        layerId === 'stops'
          ? createStopFeatureCollection(stops)
          : layerId === 'routes'
            ? createRouteFeatureCollection(routes)
            : emptyFeatureCollection(),
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
      'line-color': ['coalesce', ['get', 'color'], '#13b8a6'],
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
      'circle-color': ['coalesce', ['get', 'color'], '#f6b84a'],
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

function currentMapBounds(map: MapLibreMap): MapBounds {
  const bounds = map.getBounds()
  return {
    minLongitude: Math.max(-180, Number(bounds.getWest().toFixed(6))),
    minLatitude: Math.max(-90, Number(bounds.getSouth().toFixed(6))),
    maxLongitude: Math.min(180, Number(bounds.getEast().toFixed(6))),
    maxLatitude: Math.min(90, Number(bounds.getNorth().toFixed(6))),
  }
}

export function TransportMap({
  visibility,
  stops,
  routes,
  onBoundsChange,
}: TransportMapProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const mapRef = useRef<MapLibreMap | null>(null)
  const initialVisibilityRef = useRef(visibility)
  const initialStopsRef = useRef(stops)
  const initialRoutesRef = useRef(routes)
  const onBoundsChangeRef = useRef(onBoundsChange)
  const [isReady, setIsReady] = useState(false)
  const [hasError, setHasError] = useState(false)

  onBoundsChangeRef.current = onBoundsChange

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
      addOperationalLayers(
        map,
        initialVisibilityRef.current,
        initialStopsRef.current,
        initialRoutesRef.current,
      )
      setIsReady(true)
      onBoundsChangeRef.current(currentMapBounds(map))
    })

    map.on('moveend', () => {
      onBoundsChangeRef.current(currentMapBounds(map))
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

  useEffect(() => {
    const map = mapRef.current
    if (!map || !isReady) {
      return
    }

    const source = map.getSource(
      MAP_SOURCE_IDS.stops,
    ) as GeoJSONSource | undefined
    source?.setData(createStopFeatureCollection(stops))
  }, [isReady, stops])

  useEffect(() => {
    const map = mapRef.current
    if (!map || !isReady) {
      return
    }

    const source = map.getSource(
      MAP_SOURCE_IDS.routes,
    ) as GeoJSONSource | undefined
    source?.setData(createRouteFeatureCollection(routes || []))
  }, [isReady, routes])

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

      <div className="map-empty-state" role="status">
        <span>Görünür harita alanı</span>
        <strong>
          {stops.length > 0
            ? `${stops.length} durak haritada gösteriliyor.`
            : 'Bu alanda görüntülenebilir durak bulunmuyor.'}
        </strong>
      </div>
    </div>
  )
}
