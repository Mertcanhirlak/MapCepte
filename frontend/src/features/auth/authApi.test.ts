import { afterEach, describe, expect, it, vi } from 'vitest'
import { authApi, resetAuthApiForTests } from './authApi'

const user = {
  id: 'a27d33fb-f334-421b-ab0e-748fc78dacd6',
  email: 'admin@example.com',
  displayName: 'Sistem Yöneticisi',
  roles: ['Admin'],
  permissions: ['roles.read'],
}

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

afterEach(() => {
  resetAuthApiForTests()
  vi.unstubAllGlobals()
})

describe('authApi', () => {
  it('uses credentials and renews the CSRF token after login', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(jsonResponse({ token: 'before-login' }))
      .mockResolvedValueOnce(jsonResponse(user))
      .mockResolvedValueOnce(jsonResponse({ token: 'after-login' }))

    vi.stubGlobal('fetch', fetchMock)

    await expect(
      authApi.login('admin@example.com', 'Secret123!'),
    ).resolves.toEqual(user)

    expect(fetchMock).toHaveBeenCalledTimes(3)
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/auth/csrf')
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/auth/login')
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({
      method: 'POST',
      credentials: 'include',
      body: JSON.stringify({
        email: 'admin@example.com',
        password: 'Secret123!',
      }),
    })

    const loginHeaders = fetchMock.mock.calls[1]?.[1]?.headers as Headers
    expect(loginHeaders.get('X-CSRF-TOKEN')).toBe('before-login')
  })

  it('sends the renewed token when logging out', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(jsonResponse({ token: 'before-login' }))
      .mockResolvedValueOnce(jsonResponse(user))
      .mockResolvedValueOnce(jsonResponse({ token: 'after-login' }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))

    vi.stubGlobal('fetch', fetchMock)

    await authApi.login('admin@example.com', 'Secret123!')
    await authApi.logout()

    const logoutHeaders = fetchMock.mock.calls[3]?.[1]?.headers as Headers
    expect(logoutHeaders.get('X-CSRF-TOKEN')).toBe('after-login')
    expect(fetchMock.mock.calls[3]?.[1]).toMatchObject({
      method: 'POST',
      credentials: 'include',
    })
  })
})
