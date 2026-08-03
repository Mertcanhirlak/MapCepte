export type StopCatalogItem = {
  id: string
  name: string
  code: string | null
  description: string | null
  color: string
  longitude: number
  latitude: number
  status: string
  createdByUserId: string
  createdAtUtc: string
  updatedAtUtc: string
  version: number
}

export type StopPageResponse = {
  items: StopCatalogItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
