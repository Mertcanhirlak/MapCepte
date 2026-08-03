import { apiRequest } from '../auth/authApi'
import type { StopCatalogItem, StopPageResponse } from '../stops/stopModels'

export type MapBounds = {
  minLongitude: number
  minLatitude: number
  maxLongitude: number
  maxLatitude: number
}

function appendBounds(parameters: URLSearchParams, bounds: MapBounds) {
  parameters.set('minLongitude', String(bounds.minLongitude))
  parameters.set('minLatitude', String(bounds.minLatitude))
  parameters.set('maxLongitude', String(bounds.maxLongitude))
  parameters.set('maxLatitude', String(bounds.maxLatitude))
}

export async function loadStopsInBounds(
  bounds: MapBounds,
  signal?: AbortSignal,
) {
  const stops = new Map<string, StopCatalogItem>()
  let page = 1
  let totalPages = 1

  do {
    const parameters = new URLSearchParams({
      page: String(page),
      pageSize: '100',
    })
    appendBounds(parameters, bounds)

    const response = await apiRequest<StopPageResponse>(
      `/api/stops?${parameters}`,
      { signal },
    )

    for (const stop of response.items) {
      stops.set(stop.id, stop)
    }

    totalPages = response.totalPages
    page += 1
  } while (page <= totalPages)

  return [...stops.values()]
}

export function createStopFeatureCollection(stops: StopCatalogItem[]) {
  return {
    type: 'FeatureCollection' as const,
    features: stops.map((stop) => ({
      type: 'Feature' as const,
      id: stop.id,
      properties: {
        id: stop.id,
        name: stop.name,
        code: stop.code,
        color: stop.color,
        status: stop.status,
      },
      geometry: {
        type: 'Point' as const,
        coordinates: [stop.longitude, stop.latitude],
      },
    })),
  }
}
