import { API_BASE_URL } from '../../shared/useApiStatus'

export type AuthenticatedUser = {
  id: string
  email: string
  displayName: string
  roles: string[]
  permissions: string[]
}

type CsrfTokenResponse = {
  token: string
}

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

let csrfToken: string | null = null

async function readError(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as {
      title?: string
      error?: string
    }
    return (
      problem.error ||
      problem.title ||
      `API isteği başarısız oldu (${response.status}).`
    )
  } catch {
    return `API isteği başarısız oldu (${response.status}).`
  }
}

export async function apiRequest<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')

  if (init.body !== undefined) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers,
    credentials: 'include',
  })

  if (!response.ok) {
    throw new ApiError(response.status, await readError(response))
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

async function refreshCsrfToken(): Promise<string> {
  const response = await apiRequest<CsrfTokenResponse>('/api/auth/csrf')
  csrfToken = response.token
  return response.token
}

async function currentCsrfToken(): Promise<string> {
  return csrfToken ?? refreshCsrfToken()
}

export async function csrfRequest<T>(
  path: string,
  init: RequestInit,
): Promise<T> {
  const token = await currentCsrfToken()
  const headers = new Headers(init.headers)
  headers.set('X-CSRF-TOKEN', token)

  return apiRequest<T>(path, {
    ...init,
    headers,
  })
}

export const authApi = {
  getCurrentUser: () =>
    apiRequest<AuthenticatedUser>('/api/auth/me'),

  async login(email: string, password: string) {
    const token = await refreshCsrfToken()
    const user = await apiRequest<AuthenticatedUser>('/api/auth/login', {
      method: 'POST',
      headers: { 'X-CSRF-TOKEN': token },
      body: JSON.stringify({ email, password }),
    })

    // Login kimliği değiştirdiği için sonraki yazma isteği yeni token kullanır.
    await refreshCsrfToken()
    return user
  },

  async logout() {
    await csrfRequest<void>('/api/auth/logout', {
      method: 'POST',
    })
    csrfToken = null
  },
}

export function resetAuthApiForTests() {
  csrfToken = null
}
