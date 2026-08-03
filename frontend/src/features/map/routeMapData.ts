import type { RoutePathCatalogItem } from '../route-paths/routePathModels'

export function createRouteFeatureCollection(routes: RoutePathCatalogItem[]) {
  return {
    type: 'FeatureCollection' as const,
    features: routes
      .filter((route) => route.coordinates && route.coordinates.length >= 2)
      .map((route) => ({
        type: 'Feature' as const,
        id: route.id,
        geometry: {
          type: 'LineString' as const,
          coordinates: route.coordinates!,
        },
        properties: {
          id: route.id,
          name: route.name,
          color: route.colorOverride || '#13b8a6',
          direction: route.direction,
          distanceMeters: route.distanceMeters,
          durationSeconds: route.durationSeconds,
        },
      })),
  }
}
