import { act, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { StopLocationPicker } from './StopLocationPicker'

const mapState = vi.hoisted(() => ({
  handlers: new globalThis.Map<string, (event: unknown) => void>(),
  setData: vi.fn(),
  setPaintProperty: vi.fn(),
  easeTo: vi.fn(),
}))

vi.mock('maplibre-gl', () => ({
  AttributionControl: class AttributionControl {},
  NavigationControl: class NavigationControl {},
  Map: class MapLibreMock {
    addControl() {}

    addSource() {}

    addLayer() {}

    getSource() {
      return { setData: mapState.setData }
    }

    setPaintProperty(...args: unknown[]) {
      mapState.setPaintProperty(...args)
    }

    easeTo(...args: unknown[]) {
      mapState.easeTo(...args)
    }

    remove() {}

    on(event: string, handler: (value: unknown) => void) {
      mapState.handlers.set(event, handler)
      if (event === 'load') {
        handler(undefined)
      }
    }
  },
}))

beforeEach(() => {
  mapState.handlers.clear()
  mapState.setData.mockClear()
  mapState.setPaintProperty.mockClear()
  mapState.easeTo.mockClear()
})

describe('StopLocationPicker', () => {
  it('converts a map click to rounded longitude and latitude', () => {
    const onChange = vi.fn()
    render(
      <StopLocationPicker
        color="#13B8A6"
        latitude=""
        longitude=""
        onChange={onChange}
      />,
    )

    const clickHandler = mapState.handlers.get('click')
    expect(clickHandler).toBeDefined()

    act(() => {
      clickHandler?.({
        lngLat: { lng: 32.1234567, lat: 39.7654321 },
      })
    })

    expect(onChange).toHaveBeenCalledWith({
      longitude: 32.123457,
      latitude: 39.765432,
    })
    expect(mapState.easeTo).toHaveBeenCalledWith({
      center: [32.123457, 39.765432],
    })
  })

  it('updates marker data when coordinate fields change', () => {
    const { rerender } = render(
      <StopLocationPicker
        color="#13B8A6"
        latitude=""
        longitude=""
        onChange={() => undefined}
      />,
    )

    rerender(
      <StopLocationPicker
        color="#F6B84A"
        latitude="39.9334"
        longitude="32.8597"
        onChange={() => undefined}
      />,
    )

    expect(mapState.setData).toHaveBeenLastCalledWith(
      expect.objectContaining({
        features: [
          expect.objectContaining({
            geometry: {
              type: 'Point',
              coordinates: [32.8597, 39.9334],
            },
          }),
        ],
      }),
    )
    expect(mapState.setPaintProperty).toHaveBeenCalledWith(
      'stop-location-picker-marker',
      'circle-color',
      '#F6B84A',
    )
    expect(
      screen.getByText('32.859700, 39.933400'),
    ).toBeInTheDocument()
  })
})
