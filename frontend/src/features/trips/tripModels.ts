export type OperatingCalendarCatalogItem = {
  id: string
  name: string
  daysOfWeek: string
  isActive: boolean
  createdAtUtc: string
}

export type TripStopTimeDto = {
  id: string
  stopId: string
  sequence: number
  arrivalTime: string
  departureTime: string
}

export type TripCatalogItem = {
  id: string
  transitLineId: string
  routePathId: string
  operatingCalendarId: string
  tripCode: string
  departureTime: string
  direction: string
  isPublished: boolean
  stopTimes: TripStopTimeDto[]
}

export type TimetableStopHeaderDto = {
  stopId: string
  sequence: number
}

export type TimetableMatrixDto = {
  transitLineId: string
  stops: TimetableStopHeaderDto[]
  trips: TripCatalogItem[]
}
