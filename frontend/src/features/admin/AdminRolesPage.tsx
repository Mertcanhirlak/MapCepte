import { useEffect, useState } from 'react'
import { ApiError, apiRequest } from '../auth/authApi'

type RoleCatalogItem = {
  id: string
  name: string
  permissions: string[]
}

export function AdminRolesPage() {
  const [roles, setRoles] = useState<RoleCatalogItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const controller = new AbortController()

    apiRequest<RoleCatalogItem[]>('/api/admin/roles', {
      signal: controller.signal,
    })
      .then(setRoles)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') {
          return
        }

        setError(
          requestError instanceof ApiError && requestError.status === 403
            ? 'Bu veriyi görüntüleme yetkiniz bulunmuyor.'
            : 'Rol kataloğu alınamadı.',
        )
      })
      .finally(() => setIsLoading(false))

    return () => controller.abort()
  }, [])

  return (
    <main className="admin-page">
      <div className="page-heading">
        <div>
          <p className="eyebrow">Yönetim</p>
          <h2>Rol ve permission kataloğu</h2>
        </div>
        <span className="phase-badge dark-badge">Salt okunur</span>
      </div>

      {isLoading && <p role="status">Roller yükleniyor…</p>}
      {error && <p className="form-error" role="alert">{error}</p>}

      {!isLoading && !error && (
        <div className="role-grid">
          {roles.map((role) => (
            <article className="role-card" key={role.id}>
              <div className="role-card-heading">
                <h3>{role.name}</h3>
                <span>{role.permissions.length} izin</span>
              </div>
              <ul>
                {role.permissions.map((permission) => (
                  <li key={permission}><code>{permission}</code></li>
                ))}
              </ul>
            </article>
          ))}
        </div>
      )}
    </main>
  )
}
