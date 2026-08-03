import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../auth/authApi'
import type { StopCatalogItem } from '../stops/stopModels'
import { createStopFeatureCollection, loadStopsInBounds } from './stopMapData'

vi.mock('../auth/authApi', () => ({
  apiRequest: vi.fn(),
}))

const stop = (id: string, color: string): StopCatalogItem => ({
  id,
  name: `Durak ${id}`,
  code: null,
  description: null,
  color,
  longitude: 32.85,
  latitude: 39.93,
  status: 'Published',
  createdByUserId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
  createdAtUtc: '2026-08-01T10:00:00Z',
  updatedAtUtc: '2026-08-01T10:00:00Z',
  version: 1,
})

beforeEach(() => {
  vi.mocked(apiRequest).mockReset()
})

describe('stop map data', () => {
  it('loads every page within the visible map bounds', async () => {
    vi.mocked(apiRequest)
      .mockResolvedValueOnce({
        items: [stop('one', '#13B8A6')],
        page: 1,
        pageSize: 100,
        totalCount: 2,
        totalPages: 2,
      })
      .mockResolvedValueOnce({
        items: [stop('two', '#EF6F6C')],
        page: 2,
        pageSize: 100,
        totalCount: 2,
        totalPages: 2,
      })

    const result = await loadStopsInBounds({
      minLongitude: 32,
      minLatitude: 39,
      maxLongitude: 33,
      maxLatitude: 40,
    })

    expect(result.map((item) => item.id)).toEqual(['one', 'two'])
    expect(apiRequest).toHaveBeenNthCalledWith(
      1,
      expect.stringContaining('page=1'),
      { signal: undefined },
    )
    expect(apiRequest).toHaveBeenNthCalledWith(
      2,
      expect.stringContaining('page=2'),
      { signal: undefined },
    )
    expect(apiRequest).toHaveBeenCalledWith(
      expect.stringContaining('minLongitude=32'),
      { signal: undefined },
    )
  })

  it('preserves coordinates, labels and colors in GeoJSON features', () => {
    const data = createStopFeatureCollection([stop('one', '#13B8A6')])

    expect(data.features[0]).toMatchObject({
      id: 'one',
      properties: {
        name: 'Durak one',
        color: '#13B8A6',
        status: 'Published',
      },
      geometry: {
        type: 'Point',
        coordinates: [32.85, 39.93],
      },
    })
  })
})
