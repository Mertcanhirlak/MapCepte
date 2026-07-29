import { createContext, useContext } from 'react'
import type { AuthenticatedUser } from './authApi'

export type AuthStatus = 'loading' | 'anonymous' | 'authenticated'

export type AuthContextValue = {
  status: AuthStatus
  user: AuthenticatedUser | null
  error: string | null
  isSubmitting: boolean
  login: (email: string, password: string) => Promise<boolean>
  logout: () => Promise<void>
  hasPermission: (permission: string) => boolean
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth() {
  const context = useContext(AuthContext)

  if (context === null) {
    throw new Error('useAuth, AuthProvider içinde kullanılmalıdır.')
  }

  return context
}
