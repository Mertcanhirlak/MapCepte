import { useEffect, useState, type FormEvent } from 'react'
import { useAuth } from '../auth/authState'
import { ApiError, apiRequest, csrfRequest } from '../auth/authApi'

type RoleCatalogItem = {
  id: string
  name: string
  description: string
}

type UserCatalogItem = {
  id: string
  email: string
  displayName: string
  isActive: boolean
  createdAtUtc: string
  roles: string[]
}

const minimumPasswordLength = import.meta.env.DEV ? 6 : 12

function requestErrorMessage(error: unknown) {
  if (error instanceof ApiError) {
    return error.message
  }

  return 'API bağlantısı kurulamadı.'
}

function RoleChoices({
  roles,
  selected,
  onChange,
  disabled = false,
}: {
  roles: RoleCatalogItem[]
  selected: string[]
  onChange: (roles: string[]) => void
  disabled?: boolean
}) {
  function toggle(roleName: string) {
    onChange(
      selected.includes(roleName)
        ? selected.filter((role) => role !== roleName)
        : [...selected, roleName],
    )
  }

  return (
    <div className="role-choices">
      {roles.map((role) => (
        <label key={role.id}>
          <input
            checked={selected.includes(role.name)}
            disabled={disabled}
            onChange={() => toggle(role.name)}
            type="checkbox"
          />
          <span>
            <strong>{role.name}</strong>
            <small>{role.description}</small>
          </span>
        </label>
      ))}
    </div>
  )
}

function UserRoleEditor({
  user,
  roles,
  canEdit,
  onUpdated,
}: {
  user: UserCatalogItem
  roles: RoleCatalogItem[]
  canEdit: boolean
  onUpdated: (user: UserCatalogItem) => void
}) {
  const [selection, setSelection] = useState(user.roles)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function saveRoles() {
    setIsSaving(true)
    setError(null)

    try {
      const updated = await csrfRequest<UserCatalogItem>(
        `/api/admin/users/${user.id}/roles`,
        {
          method: 'PUT',
          body: JSON.stringify({ roles: selection }),
        },
      )
      onUpdated(updated)
      setSelection(updated.roles)
    } catch (requestError) {
      setError(requestErrorMessage(requestError))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <div className="user-role-editor">
      <RoleChoices
        disabled={!canEdit || isSaving}
        onChange={setSelection}
        roles={roles}
        selected={selection}
      />
      {error && <p className="inline-error" role="alert">{error}</p>}
      {canEdit ? (
        <button
          className="secondary-button"
          disabled={
            isSaving ||
            selection.length === 0 ||
            selection.toSorted().join() === user.roles.toSorted().join()
          }
          onClick={() => void saveRoles()}
          type="button"
        >
          {isSaving ? 'Kaydediliyor…' : 'Rolleri kaydet'}
        </button>
      ) : (
        <small className="self-role-note">
          Kendi rollerinizi değiştiremezsiniz.
        </small>
      )}
    </div>
  )
}

export function AdminUsersPage() {
  const { user: currentUser, hasPermission } = useAuth()
  const [users, setUsers] = useState<UserCatalogItem[]>([])
  const [roles, setRoles] = useState<RoleCatalogItem[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [selectedRoles, setSelectedRoles] = useState<string[]>(['User'])
  const canManage =
    hasPermission('users.manage') && hasPermission('roles.manage')

  useEffect(() => {
    const controller = new AbortController()

    Promise.all([
      apiRequest<UserCatalogItem[]>('/api/admin/users', {
        signal: controller.signal,
      }),
      apiRequest<RoleCatalogItem[]>('/api/admin/roles', {
        signal: controller.signal,
      }),
    ])
      .then(([loadedUsers, loadedRoles]) => {
        setUsers(loadedUsers)
        setRoles(loadedRoles)
      })
      .catch((requestError: unknown) => {
        if (
          requestError instanceof DOMException &&
          requestError.name === 'AbortError'
        ) {
          return
        }

        setError(requestErrorMessage(requestError))
      })
      .finally(() => setIsLoading(false))

    return () => controller.abort()
  }, [])

  async function createUser(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsCreating(true)
    setError(null)
    setMessage(null)

    try {
      const created = await csrfRequest<UserCatalogItem>(
        '/api/admin/users',
        {
          method: 'POST',
          body: JSON.stringify({
            email: email.trim(),
            displayName: displayName.trim(),
            password,
            roles: selectedRoles,
          }),
        },
      )

      setUsers((current) =>
        [...current, created].toSorted((left, right) =>
          left.displayName.localeCompare(right.displayName, 'tr'),
        ),
      )
      setEmail('')
      setDisplayName('')
      setPassword('')
      setSelectedRoles(['User'])
      setMessage(`${created.displayName} kullanıcısı oluşturuldu.`)
    } catch (requestError) {
      setError(requestErrorMessage(requestError))
    } finally {
      setIsCreating(false)
    }
  }

  function updateUser(updated: UserCatalogItem) {
    setUsers((current) =>
      current.map((user) => (user.id === updated.id ? updated : user)),
    )
  }

  return (
    <main className="admin-page">
      <div className="page-heading">
        <div>
          <p className="eyebrow">Yönetim</p>
          <h2>Kullanıcı ve rol atamaları</h2>
        </div>
        <span className="phase-badge dark-badge">
          {users.length} kullanıcı
        </span>
      </div>

      {canManage && (
        <form className="user-create-card" onSubmit={createUser}>
          <div className="user-create-heading">
            <div>
              <p className="eyebrow">Yeni hesap</p>
              <h3>Kullanıcı oluştur</h3>
            </div>
            <span>Parola en az {minimumPasswordLength} karakter</span>
          </div>

          <div className="user-form-grid">
            <label>
              <span>Görünen ad</span>
              <input
                disabled={isCreating}
                maxLength={120}
                onChange={(event) => setDisplayName(event.target.value)}
                required
                value={displayName}
              />
            </label>
            <label>
              <span>E-posta</span>
              <input
                disabled={isCreating}
                inputMode="email"
                maxLength={320}
                onChange={(event) => setEmail(event.target.value)}
                required
                type="email"
                value={email}
              />
            </label>
            <label>
              <span>Başlangıç parolası</span>
              <input
                autoComplete="new-password"
                disabled={isCreating}
                minLength={minimumPasswordLength}
                onChange={(event) => setPassword(event.target.value)}
                required
                type="password"
                value={password}
              />
            </label>
          </div>

          <RoleChoices
            disabled={isCreating}
            onChange={setSelectedRoles}
            roles={roles}
            selected={selectedRoles}
          />

          <button
            className="primary-button compact-button"
            disabled={isCreating || selectedRoles.length === 0}
            type="submit"
          >
            {isCreating ? 'Oluşturuluyor…' : 'Kullanıcı oluştur'}
          </button>
        </form>
      )}

      {message && <p className="success-message" role="status">{message}</p>}
      {error && <p className="form-error" role="alert">{error}</p>}
      {isLoading && <p role="status">Kullanıcılar yükleniyor…</p>}

      {!isLoading && (
        <div className="user-list">
          {users.map((user) => (
            <article className="user-card" key={user.id}>
              <div className="user-card-heading">
                <div>
                  <h3>{user.displayName}</h3>
                  <a href={`mailto:${user.email}`}>{user.email}</a>
                </div>
                <span className={user.isActive ? 'active-user' : 'inactive-user'}>
                  {user.isActive ? 'Aktif' : 'Pasif'}
                </span>
              </div>
              <UserRoleEditor
                canEdit={canManage && currentUser?.id !== user.id}
                onUpdated={updateUser}
                roles={roles}
                user={user}
              />
            </article>
          ))}
        </div>
      )}
    </main>
  )
}
