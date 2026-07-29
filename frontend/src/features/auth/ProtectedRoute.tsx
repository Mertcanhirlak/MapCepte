import type { ReactNode } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from './authState'

export function ProtectedRoute() {
  const { status } = useAuth()
  const location = useLocation()

  if (status === 'loading') {
    return (
      <main className="auth-loading" role="status">
        Oturum kontrol ediliyor…
      </main>
    )
  }

  if (status === 'anonymous') {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return <Outlet />
}

export function PermissionRoute({
  permission,
  children,
}: {
  permission: string
  children: ReactNode
}) {
  const { hasPermission } = useAuth()

  if (!hasPermission(permission)) {
    return (
      <main className="access-denied">
        <p className="eyebrow">Yetki gerekli</p>
        <h2>Bu sayfayı görüntüleme izniniz bulunmuyor.</h2>
        <p>Gerekli permission: <code>{permission}</code></p>
      </main>
    )
  }

  return children
}
