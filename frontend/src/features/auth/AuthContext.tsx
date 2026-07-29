import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type PropsWithChildren,
} from 'react'
import { ApiError, authApi, type AuthenticatedUser } from './authApi'
import {
  AuthContext,
  type AuthContextValue,
  type AuthStatus,
} from './authState'

function loginErrorMessage(error: unknown) {
  if (error instanceof ApiError) {
    if (error.status === 401) {
      return 'E-posta veya parola hatalı.'
    }

    if (error.status === 429) {
      return 'Çok fazla giriş denemesi yapıldı. Lütfen biraz bekleyin.'
    }

    if (error.status === 400) {
      return 'Güvenlik doğrulaması başarısız oldu. Sayfayı yenileyip tekrar deneyin.'
    }
  }

  return 'Giriş yapılamadı. API bağlantısını kontrol edin.'
}

export function AuthProvider({ children }: PropsWithChildren) {
  const [status, setStatus] = useState<AuthStatus>('loading')
  const [user, setUser] = useState<AuthenticatedUser | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    let active = true

    authApi
      .getCurrentUser()
      .then((currentUser) => {
        if (!active) return
        setUser(currentUser)
        setStatus('authenticated')
      })
      .catch((requestError: unknown) => {
        if (!active) return
        setUser(null)
        setStatus('anonymous')

        if (!(requestError instanceof ApiError && requestError.status === 401)) {
          setError('Oturum bilgisi alınamadı. API bağlantısını kontrol edin.')
        }
      })

    return () => {
      active = false
    }
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    setIsSubmitting(true)
    setError(null)

    try {
      const authenticatedUser = await authApi.login(email, password)
      setUser(authenticatedUser)
      setStatus('authenticated')
      return true
    } catch (requestError) {
      setError(loginErrorMessage(requestError))
      return false
    } finally {
      setIsSubmitting(false)
    }
  }, [])

  const logout = useCallback(async () => {
    setIsSubmitting(true)
    setError(null)

    try {
      await authApi.logout()
      setUser(null)
      setStatus('anonymous')
    } catch {
      setError('Oturum kapatılamadı. Lütfen yeniden deneyin.')
    } finally {
      setIsSubmitting(false)
    }
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      user,
      error,
      isSubmitting,
      login,
      logout,
      hasPermission: (permission) =>
        user?.permissions.includes(permission) ?? false,
    }),
    [error, isSubmitting, login, logout, status, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
