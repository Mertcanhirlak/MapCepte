import { useEffect, useState } from 'react'
import { ApiError, apiRequest } from '../auth/authApi'

type AuditCatalogItem = {
  id: string
  eventType: string
  outcome: string
  occurredAtUtc: string
  actorUserId: string | null
  subjectUserId: string | null
  ipAddress: string | null
}

const eventLabels: Record<string, string> = {
  'auth.login': 'Giriş denemesi',
  'admin.user.created': 'Kullanıcı oluşturuldu',
  'admin.user.roles_updated': 'Kullanıcı rolleri güncellendi',
}

const outcomeLabels: Record<string, string> = {
  succeeded: 'Başarılı',
  failed: 'Başarısız',
  locked_out: 'Kilitlendi',
}

function shortId(id: string | null) {
  return id ? `${id.slice(0, 8)}…` : '—'
}

export function AdminAuditPage() {
  const [entries, setEntries] = useState<AuditCatalogItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const controller = new AbortController()

    apiRequest<AuditCatalogItem[]>('/api/admin/audit', {
      signal: controller.signal,
    })
      .then(setEntries)
      .catch((requestError: unknown) => {
        if (
          requestError instanceof DOMException &&
          requestError.name === 'AbortError'
        ) {
          return
        }

        setError(
          requestError instanceof ApiError
            ? requestError.message
            : 'Audit kayıtları alınamadı.',
        )
      })
      .finally(() => setIsLoading(false))

    return () => controller.abort()
  }, [])

  return (
    <main className="admin-page">
      <div className="page-heading">
        <div>
          <p className="eyebrow">Güvenlik</p>
          <h2>Audit kayıtları</h2>
        </div>
        <span className="phase-badge dark-badge">Son 100 olay</span>
      </div>

      {isLoading && <p role="status">Audit kayıtları yükleniyor…</p>}
      {error && <p className="form-error" role="alert">{error}</p>}

      {!isLoading && !error && (
        <div className="audit-table-wrap">
          <table className="audit-table">
            <thead>
              <tr>
                <th>Zaman</th>
                <th>Olay</th>
                <th>Sonuç</th>
                <th>Aktör</th>
                <th>Hedef</th>
                <th>IP</th>
              </tr>
            </thead>
            <tbody>
              {entries.map((entry) => (
                <tr key={entry.id}>
                  <td>
                    {new Intl.DateTimeFormat('tr-TR', {
                      dateStyle: 'short',
                      timeStyle: 'medium',
                    }).format(new Date(entry.occurredAtUtc))}
                  </td>
                  <td>{eventLabels[entry.eventType] || entry.eventType}</td>
                  <td>
                    <span className={`audit-outcome audit-${entry.outcome}`}>
                      {outcomeLabels[entry.outcome] || entry.outcome}
                    </span>
                  </td>
                  <td><code>{shortId(entry.actorUserId)}</code></td>
                  <td><code>{shortId(entry.subjectUserId)}</code></td>
                  <td><code>{entry.ipAddress || '—'}</code></td>
                </tr>
              ))}
              {entries.length === 0 && (
                <tr>
                  <td className="empty-table" colSpan={6}>
                    Henüz audit kaydı bulunmuyor.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </main>
  )
}
