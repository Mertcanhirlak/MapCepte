export type RoutePathDirection = 'Outbound' | 'Inbound' | 'Alternative'

export type RoutePathStatus = 'Generating' | 'Ready' | 'Failed' | 'OutOfDate' | 'Archived'

export type RoutePathCatalogItem = {
  id: string
  transitLineId: string
  name: string
  direction: RoutePathDirection
  version: number
  status: RoutePathStatus
  colorOverride: string | null
  distanceMeters: number
  durationSeconds: number
  routingEngine: string
  generatedAtUtc: string | null
  failureCode: string | null
  failureMessage: string | null
  stopCount: number
  coordinates: [number, number][] | null
}

export type GenerateRoutePathRequest = {
  name: string
  direction: RoutePathDirection
  colorOverride?: string | null
}
