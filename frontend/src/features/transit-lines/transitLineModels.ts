export type TransitLineCatalogItem = {
  id: string
  name: string
  code: string
  description: string | null
  color: string
  status: 'Draft' | 'Published' | 'Archived'
  ownerUserId: string
  createdByUserId: string
  updatedByUserId: string
  createdAtUtc: string
  updatedAtUtc: string
  version: number
  stopCount: number
}

export type TransitLinePageResponse = {
  items: TransitLineCatalogItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type TransitLineStopItem = {
  lineStopId: string
  stopId: string
  stopName: string
  stopCode: string | null
  stopColor: string
  longitude: number
  latitude: number
  sequence: number
}
