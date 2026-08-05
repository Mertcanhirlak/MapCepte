export type VehiclePositionCatalogItem = {
  id: string
  vehicleCode: string
  transitLineId: string
  routePathId?: string | null
  longitude: number
  latitude: number
  speedKmh?: number | null
  heading?: number | null
  recordedAtUtc: string
}

export function createVehicleFeatureCollection(
  vehicles: VehiclePositionCatalogItem[],
) {
  return {
    type: 'FeatureCollection' as const,
    features: vehicles.map((v) => ({
      type: 'Feature' as const,
      id: v.id,
      properties: {
        id: v.id,
        vehicleCode: v.vehicleCode,
        transitLineId: v.transitLineId,
        speedKmh: v.speedKmh ?? 0,
        heading: v.heading ?? 0,
      },
      geometry: {
        type: 'Point' as const,
        coordinates: [v.longitude, v.latitude],
      },
    })),
  }
}
