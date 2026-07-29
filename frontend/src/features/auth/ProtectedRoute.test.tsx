import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider } from './AuthContext'
import { resetAuthApiForTests } from './authApi'
import { ProtectedRoute } from './ProtectedRoute'

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function renderProtectedRoute() {
  render(
    <MemoryRouter initialEntries={['/private']}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<p>Giriş sayfası</p>} />
          <Route element={<ProtectedRoute />}>
            <Route path="/private" element={<p>Korumalı içerik</p>} />
          </Route>
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  )
}

afterEach(() => {
  resetAuthApiForTests()
  vi.unstubAllGlobals()
})

describe('ProtectedRoute', () => {
  it('redirects an anonymous visitor to login', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse({ title: 'Unauthorized' }, 401),
      ),
    )

    renderProtectedRoute()

    expect(await screen.findByText('Giriş sayfası')).toBeInTheDocument()
  })

  it('renders content for an authenticated user', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse({
          id: 'a27d33fb-f334-421b-ab0e-748fc78dacd6',
          email: 'admin@example.com',
          displayName: 'Sistem Yöneticisi',
          roles: ['Admin'],
          permissions: ['roles.read'],
        }),
      ),
    )

    renderProtectedRoute()

    expect(await screen.findByText('Korumalı içerik')).toBeInTheDocument()
  })
})
