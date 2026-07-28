import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { LayerPanel } from './LayerPanel'
import { DEFAULT_LAYER_VISIBILITY } from './mapLayers'

describe('LayerPanel', () => {
  it('reports a route layer visibility change', () => {
    const onToggle = vi.fn()

    render(
      <LayerPanel
        visibility={DEFAULT_LAYER_VISIBILITY}
        onToggle={onToggle}
      />,
    )

    fireEvent.click(screen.getByLabelText('Rotalar katmanını göster'))

    expect(onToggle).toHaveBeenCalledWith('routes')
  })

  it('keeps the base map enabled', () => {
    render(
      <LayerPanel
        visibility={DEFAULT_LAYER_VISIBILITY}
        onToggle={vi.fn()}
      />,
    )

    expect(screen.getByLabelText('Temel harita katmanını göster')).toBeDisabled()
  })
})
