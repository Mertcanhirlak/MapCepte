import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider } from '../auth/AuthContext'
import { resetAuthApiForTests } from '../auth/authApi'
import { StopManagementPage } from './StopManagementPage'

vi.mock('./StopLocationPicker', () => ({
  StopLocationPicker: () => <div>Harita konum seçici</div>,
}))

function jsonResponse(body: unknown) {
  return new Response(JSON.stringify(body), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  })
}

afterEach(() => {
  resetAuthApiForTests()
  vi.unstubAllGlobals()
})

describe('StopManagementPage', () => {
  it('shows the create form to an operator and renders stops', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL) => {
        const url = input.toString()
        if (url.endsWith('/api/auth/me')) {
          return Promise.resolve(
            jsonResponse({
              id: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
              email: 'operator@example.com',
              displayName: 'Example Operator',
              roles: ['Operator'],
              permissions: [
                'stops.read',
                'stops.create',
                'stops.update',
                'stops.delete',
              ],
            }),
          )
        }

        return Promise.resolve(
          jsonResponse({
            items: [{
              id: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
              name: 'Merkez Meydan',
              code: 'MRK-001',
              description: 'Ana meydan durağı',
              color: '#13B8A6',
              longitude: 32.8597,
              latitude: 39.9334,
              status: 'Draft',
              createdByUserId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
              createdAtUtc: '2026-07-30T10:00:00Z',
              updatedAtUtc: '2026-07-30T10:00:00Z',
              version: 1,
            }],
            page: 1,
            pageSize: 12,
            totalCount: 1,
            totalPages: 1,
          }),
        )
      }),
    )

    render(
      <MemoryRouter>
        <AuthProvider>
          <StopManagementPage />
        </AuthProvider>
      </MemoryRouter>,
    )

    expect(await screen.findByText('Merkez Meydan')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Durak oluştur' }),
    ).toBeInTheDocument()
    expect(screen.getByText('MRK-001')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Düzenle' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Arşivle' })).toBeInTheDocument()
    expect(screen.getByText('Harita konum seçici')).toBeInTheDocument()
    expect(screen.getByText('1 durak')).toBeInTheDocument()
  })

  it('sends search and map bounds as list query parameters', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = input.toString()
      if (url.endsWith('/api/auth/me')) {
        return Promise.resolve(
          jsonResponse({
            id: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
            email: 'operator@example.com',
            displayName: 'Example Operator',
            roles: ['Operator'],
            permissions: ['stops.read'],
          }),
        )
      }

      return Promise.resolve(
        jsonResponse({
          items: [],
          page: 1,
          pageSize: 12,
          totalCount: 0,
          totalPages: 0,
        }),
      )
    })
    vi.stubGlobal('fetch', fetchMock)

    render(
      <MemoryRouter>
        <AuthProvider>
          <StopManagementPage />
        </AuthProvider>
      </MemoryRouter>,
    )

    await screen.findByText(/Filtrelere uyan/)
    fireEvent.change(screen.getByLabelText('Ad veya kod'), {
      target: { value: 'Merkez' },
    })
    fireEvent.change(screen.getByLabelText('Min. boylam'), {
      target: { value: '32' },
    })
    fireEvent.change(screen.getByLabelText('Min. enlem'), {
      target: { value: '39' },
    })
    fireEvent.change(screen.getByLabelText('Maks. boylam'), {
      target: { value: '33' },
    })
    fireEvent.change(screen.getByLabelText('Maks. enlem'), {
      target: { value: '40' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Filtreleri uygula' }))

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        expect.stringContaining('search=Merkez'),
        expect.any(Object),
      )
      expect(fetchMock).toHaveBeenCalledWith(
        expect.stringContaining('minLongitude=32'),
        expect.any(Object),
      )
      expect(fetchMock).toHaveBeenCalledWith(
        expect.stringContaining('maxLatitude=40'),
        expect.any(Object),
      )
    })
  })
})
