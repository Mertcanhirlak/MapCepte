import {
  MAP_LAYER_CATALOG,
  type LayerVisibility,
  type OperationalLayerId,
} from './mapLayers'

interface LayerPanelProps {
  visibility: LayerVisibility
  onToggle: (layerId: 'base' | OperationalLayerId) => void
}

export function LayerPanel({ visibility, onToggle }: LayerPanelProps) {
  const visibleLayers = MAP_LAYER_CATALOG.filter((layer) => layer.visibleInPanel)
  const activeCount = visibleLayers.filter((layer) => Boolean(visibility[layer.id])).length

  return (
    <section className="layer-card" aria-labelledby="layer-title">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Harita Görünümü</p>
          <h2 id="layer-title">Katmanlar</h2>
        </div>
        <span className="layer-count">
          {activeCount} / {visibleLayers.length} Aktif
        </span>
      </div>

      <div className="layer-list">
        {visibleLayers.map((layer) => {
          const isChecked = Boolean(visibility[layer.id])

          return (
            <label
              className={`layer-row${isChecked ? ' layer-active' : ' layer-disabled'}`}
              key={layer.id}
              style={{ cursor: layer.toggleable ? 'pointer' : 'default' }}
            >
              <span
                className="layer-swatch"
                style={{ '--layer-color': isChecked ? layer.color : '#94a3b8' } as React.CSSProperties}
                aria-hidden="true"
              />
              <span className="layer-copy">
                <span className="layer-name">
                  {layer.label}
                  <small style={{
                    marginLeft: '0.4rem',
                    padding: '0.1rem 0.4rem',
                    borderRadius: '4px',
                    fontSize: '0.7rem',
                    background: isChecked ? 'rgba(19, 184, 166, 0.15)' : '#f1f5f9',
                    color: isChecked ? '#0d9488' : '#64748b'
                  }}>
                    {isChecked ? 'Görünür' : 'Gizli'}
                  </small>
                </span>
                <span className="layer-description">{layer.description}</span>
              </span>
              <input
                aria-label={`${layer.label} katmanını göster`}
                type="checkbox"
                checked={isChecked}
                disabled={!layer.toggleable}
                onChange={() => {
                  if (layer.toggleable) {
                    onToggle(layer.id)
                  }
                }}
              />
            </label>
          )
        })}
      </div>
    </section>
  )
}
