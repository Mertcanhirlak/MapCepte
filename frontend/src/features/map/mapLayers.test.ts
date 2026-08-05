import { describe, expect, it } from 'vitest'
import {
  DEFAULT_LAYER_VISIBILITY,
  MAP_LAYER_CATALOG,
  OPERATIONAL_LAYER_IDS,
} from './mapLayers'

describe('map layer catalog', () => {
  it('keeps every layer id unique and in drawing order', () => {
    const ids = MAP_LAYER_CATALOG.map((layer) => layer.id)
    const orders = MAP_LAYER_CATALOG.map((layer) => layer.order)

    expect(new Set(ids).size).toBe(ids.length)
    expect(orders).toEqual([...orders].sort((left, right) => left - right))
    expect(ids).toEqual([
      'base',
      'routes',
      'stops',
      'selection',
      'vehicles',
    ])
  })

  it('defines visibility for every operational layer', () => {
    expect(Object.keys(DEFAULT_LAYER_VISIBILITY).sort()).toEqual(
      [...OPERATIONAL_LAYER_IDS].sort(),
    )
    expect(DEFAULT_LAYER_VISIBILITY.vehicles).toBe(true)
  })
})
