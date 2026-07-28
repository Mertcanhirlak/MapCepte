import {
  MAP_LAYER_CATALOG,
  type LayerVisibility,
  type OperationalLayerId,
} from './mapLayers'

interface LayerPanelProps {
  visibility: LayerVisibility
  onToggle: (layerId: OperationalLayerId) => void
}

export function LayerPanel({ visibility, onToggle }: LayerPanelProps) {
  return (
    <section className="layer-card" aria-labelledby="layer-title">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Harita görünümü</p>
          <h2 id="layer-title">Katmanlar</h2>
        </div>
        <span className="layer-count">
          {MAP_LAYER_CATALOG.filter((layer) => layer.visibleInPanel).length}
        </span>
      </div>

      <div className="layer-list">
        {MAP_LAYER_CATALOG.filter((layer) => layer.visibleInPanel).map(
          (layer) => {
            const isBaseLayer = layer.id === 'base'
            const isChecked = isBaseLayer
              ? true
              : visibility[layer.id as OperationalLayerId]

            return (
              <label className="layer-row" key={layer.id}>
                <span
                  className="layer-swatch"
                  style={{ '--layer-color': layer.color } as React.CSSProperties}
                  aria-hidden="true"
                />
                <span className="layer-copy">
                  <span className="layer-name">
                    {layer.label}
                    <small>{layer.phase}</small>
                  </span>
                  <span className="layer-description">{layer.description}</span>
                </span>
                <input
                  aria-label={`${layer.label} katmanını göster`}
                  type="checkbox"
                  checked={isChecked}
                  disabled={!layer.toggleable}
                  onChange={() => {
                    if (!isBaseLayer) {
                      onToggle(layer.id as OperationalLayerId)
                    }
                  }}
                />
              </label>
            )
          },
        )}
      </div>
    </section>
  )
}
