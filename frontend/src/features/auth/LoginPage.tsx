import { useEffect, useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from './authState'

type LoginLocationState = {
  from?: string
}

export function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const { status, error, isSubmitting, login } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const destination =
    (location.state as LoginLocationState | null)?.from || '/'

  useEffect(() => {
    document.title = 'Giriş · MapCepte'
  }, [])

  if (status === 'authenticated') {
    return <Navigate to={destination} replace />
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (await login(email.trim(), password)) {
      navigate(destination, { replace: true })
    }
  }

  return (
    <main className="login-page">
      <section className="login-visual" aria-label="MapCepte tanıtımı">
        <div className="brand-lockup">
          <div className="brand-mark" aria-hidden="true">MC</div>
          <div>
            <p className="eyebrow">Ulaşım yönetim platformu</p>
            <h1>MapCepte</h1>
          </div>
        </div>
        <div className="login-message">
          <p className="eyebrow">Operasyon merkezi</p>
          <h2>Durakları, güzergâhları ve rotaları tek haritada yönetin.</h2>
          <p>
            Yetkinize göre açılan araçlarla ulaşım ağını güvenli biçimde
            oluşturun ve takip edin.
          </p>
        </div>
      </section>

      <section className="login-panel">
        <form className="login-card" onSubmit={handleSubmit}>
          <div>
            <p className="eyebrow">Güvenli oturum</p>
            <h2>Hesabınıza giriş yapın</h2>
            <p className="login-help">
              Sistem yöneticinizin oluşturduğu hesap bilgilerini kullanın.
            </p>
          </div>

          <label>
            <span>E-posta</span>
            <input
              autoComplete="username"
              autoFocus
              disabled={isSubmitting}
              inputMode="email"
              name="email"
              onChange={(event) => setEmail(event.target.value)}
              required
              type="email"
              value={email}
            />
          </label>

          <label>
            <span>Parola</span>
            <input
              autoComplete="current-password"
              disabled={isSubmitting}
              name="password"
              onChange={(event) => setPassword(event.target.value)}
              required
              type="password"
              value={password}
            />
          </label>

          {error && (
            <p className="form-error" role="alert">
              {error}
            </p>
          )}

          <button className="primary-button" disabled={isSubmitting} type="submit">
            {isSubmitting ? 'Giriş yapılıyor…' : 'Giriş yap'}
          </button>

          <p className="session-note">
            Oturum bilgisi tarayıcıdan okunamayan güvenli cookie içinde tutulur.
          </p>
        </form>
      </section>
    </main>
  )
}
