export const OPERATIONAL_LAYER_IDS = [
  'routes',
  'stops',
  'selection',
  'vehicles',
] as const

export type OperationalLayerId = (typeof OPERATIONAL_LAYER_IDS)[number]

export type LayerVisibility = Record<'base' | OperationalLayerId, boolean>

export interface MapLayerDescriptor {
  id: 'base' | OperationalLayerId
  label: string
  description: string
  color: string
  order: number
  toggleable: boolean
  visibleInPanel: boolean
  phase: string
}

export const MAP_LAYER_CATALOG = [
  {
    id: 'base',
    label: 'Temel harita',
    description: 'Yollar ve coğrafi referans (OpenStreetMap)',
    color: '#8a9aa4',
    order: 0,
    toggleable: true,
    visibleInPanel: true,
    phase: 'Hazır',
  },
  {
    id: 'routes',
    label: 'Rotalar',
    description: 'PostGIS üzerinden gelen gerçek yol çizgileri',
    color: '#13b8a6',
    order: 100,
    toggleable: true,
    visibleInPanel: true,
    phase: 'Faz 4',
  },
  {
    id: 'stops',
    label: 'Duraklar',
    description: 'Harita üzerinde gösterilen durak noktaları',
    color: '#f6b84a',
    order: 200,
    toggleable: true,
    visibleInPanel: true,
    phase: 'Hazır',
  },
  {
    id: 'selection',
    label: 'Seçim',
    description: 'Harita etkileşimi için geçici vurgu',
    color: '#ffffff',
    order: 300,
    toggleable: false,
    visibleInPanel: false,
    phase: 'Hazır',
  },
  {
    id: 'vehicles',
    label: 'Canlı araçlar',
    description: 'Gerçek zamanlı otobüs ve araç konumları',
    color: '#ef6f6c',
    order: 400,
    toggleable: true,
    visibleInPanel: true,
    phase: 'Faz 7',
  },
] as const satisfies readonly MapLayerDescriptor[]

export const DEFAULT_LAYER_VISIBILITY: LayerVisibility = {
  base: true,
  routes: true,
  stops: true,
  selection: true,
  vehicles: true,
}

export const MAP_SOURCE_IDS: Record<OperationalLayerId, string> = {
  routes: 'route-paths-source',
  stops: 'stops-source',
  selection: 'selection-source',
  vehicles: 'vehicles-source',
}

export const MAP_RENDER_LAYER_IDS: Record<OperationalLayerId, string> = {
  routes: 'route-paths-layer',
  stops: 'stops-layer',
  selection: 'selection-layer',
  vehicles: 'vehicles-layer',
}
