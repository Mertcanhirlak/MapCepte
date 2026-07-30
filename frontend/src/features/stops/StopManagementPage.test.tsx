import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider } from '../auth/AuthContext'
import { resetAuthApiForTests } from '../auth/authApi'
import { StopManagementPage } from './StopManagementPage'

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
              permissions: ['stops.read', 'stops.create'],
            }),
          )
        }

        return Promise.resolve(
          jsonResponse([
            {
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
            },
          ]),
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
  })
})
