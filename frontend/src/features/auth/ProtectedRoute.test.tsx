import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider } from './AuthContext'
import { resetAuthApiForTests } from './authApi'
import { PermissionRoute, ProtectedRoute } from './ProtectedRoute'

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

  it('redirects an authenticated user from a forbidden return path', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse({
          id: 'a27d33fb-f334-421b-ab0e-748fc78dacd6',
          email: 'user@example.com',
          displayName: 'Example User',
          roles: ['User'],
          permissions: ['stops.read'],
        }),
      ),
    )

    render(
      <MemoryRouter initialEntries={['/admin/users']}>
        <AuthProvider>
          <Routes>
            <Route path="/" element={<p>Ana sayfa</p>} />
            <Route
              path="/admin/users"
              element={
                <PermissionRoute permission="users.read" redirectTo="/">
                  <p>Kullanıcı yönetimi</p>
                </PermissionRoute>
              }
            />
          </Routes>
        </AuthProvider>
      </MemoryRouter>,
    )

    expect(await screen.findByText('Ana sayfa')).toBeInTheDocument()
    expect(screen.queryByText('Kullanıcı yönetimi')).not.toBeInTheDocument()
  })
})
