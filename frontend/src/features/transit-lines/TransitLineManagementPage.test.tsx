import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider } from '../auth/AuthContext'
import { resetAuthApiForTests } from '../auth/authApi'
import { TransitLineManagementPage } from './TransitLineManagementPage'

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

describe('TransitLineManagementPage', () => {
  it('renders transit lines and create form for operator', async () => {
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
                'transit_lines.read',
                'transit_lines.create',
                'transit_lines.update',
                'transit_lines.delete',
                'transit_lines.reorder_stops',
              ],
            }),
          )
        }

        return Promise.resolve(
          jsonResponse({
            items: [
              {
                id: 'line-1111-1111-1111',
                name: 'Merkez Hat',
                code: 'M-100',
                description: 'Ana güzergâh',
                color: '#FF0000',
                status: 'Draft',
                ownerUserId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
                createdByUserId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
                updatedByUserId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
                createdAtUtc: '2026-08-01T12:00:00Z',
                updatedAtUtc: '2026-08-01T12:00:00Z',
                version: 1,
                stopCount: 0,
              },
            ],
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
          <TransitLineManagementPage />
        </AuthProvider>
      </MemoryRouter>,
    )

    expect(await screen.findByText('Merkez Hat')).toBeInTheDocument()
    expect(screen.getByText('M-100')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Güzergâhı Kaydet' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Durakları Yönet' }),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Düzenle' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Arşivle' })).toBeInTheDocument()
  })
})
