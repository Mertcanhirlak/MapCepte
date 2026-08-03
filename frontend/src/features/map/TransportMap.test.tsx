import { render, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { TransportMap } from './TransportMap'
import { DEFAULT_LAYER_VISIBILITY, MAP_RENDER_LAYER_IDS } from './mapLayers'
import type { StopCatalogItem } from '../stops/stopModels'

const mapMocks = vi.hoisted(() => ({
  addLayer: vi.fn(),
  addSource: vi.fn(),
  setData: vi.fn(),
  setLayoutProperty: vi.fn(),
}))

vi.mock('maplibre-gl', () => ({
  AttributionControl: class {},
  NavigationControl: class {},
  Map: class {
    addControl() {}
    addLayer = mapMocks.addLayer
    addSource = mapMocks.addSource
    getBounds() {
      return {
        getWest: () => 32,
        getSouth: () => 39,
        getEast: () => 33,
        getNorth: () => 40,
      }
    }
    getLayer() {
      return {}
    }
    getSource() {
      return { setData: mapMocks.setData }
    }
    on(event: string, callback: () => void) {
      if (event === 'load') {
        queueMicrotask(callback)
      }
    }
    remove() {}
    setLayoutProperty = mapMocks.setLayoutProperty
  },
}))

const visibleStop: StopCatalogItem = {
  id: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
  name: 'Merkez Meydan',
  code: 'MRK-001',
  description: null,
  color: '#13B8A6',
  longitude: 32.8597,
  latitude: 39.9334,
  status: 'Published',
  createdByUserId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
  createdAtUtc: '2026-08-01T10:00:00Z',
  updatedAtUtc: '2026-08-01T10:00:00Z',
  version: 1,
}

describe('TransportMap', () => {
  it('renders colored stop data and applies stop layer visibility', async () => {
    const onBoundsChange = vi.fn()
    const { rerender } = render(
      <TransportMap
        onBoundsChange={onBoundsChange}
        stops={[visibleStop]}
        visibility={DEFAULT_LAYER_VISIBILITY}
      />,
    )

    await waitFor(() => {
      expect(onBoundsChange).toHaveBeenCalledWith({
        minLongitude: 32,
        minLatitude: 39,
        maxLongitude: 33,
        maxLatitude: 40,
      })
    })
    expect(mapMocks.addSource).toHaveBeenCalledWith(
      'stops-source',
      expect.objectContaining({
        data: expect.objectContaining({
          features: [
            expect.objectContaining({
              properties: expect.objectContaining({ color: '#13B8A6' }),
            }),
          ],
        }),
      }),
    )

    rerender(
      <TransportMap
        onBoundsChange={onBoundsChange}
        stops={[visibleStop]}
        visibility={{ ...DEFAULT_LAYER_VISIBILITY, stops: false }}
      />,
    )

    await waitFor(() => {
      expect(mapMocks.setLayoutProperty).toHaveBeenCalledWith(
        MAP_RENDER_LAYER_IDS.stops,
        'visibility',
        'none',
      )
    })
  })
})
