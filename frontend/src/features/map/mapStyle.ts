import type { StyleSpecification } from 'maplibre-gl'

export const TURKEY_MAP_CENTER: [number, number] = [35.2, 39]

export const DEVELOPMENT_MAP_STYLE: StyleSpecification = {
  version: 8,
  sources: {
    'openstreetmap-raster': {
      type: 'raster',
      tiles: ['https://tile.openstreetmap.org/{z}/{x}/{y}.png'],
      tileSize: 256,
      attribution:
        '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    },
  },
  layers: [
    {
      id: 'base-map',
      type: 'raster',
      source: 'openstreetmap-raster',
    },
  ],
}

export function configuredMapStyle() {
  return import.meta.env.VITE_MAP_STYLE_URL?.trim() || DEVELOPMENT_MAP_STYLE
}
