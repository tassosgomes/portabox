import { type FormEvent, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { AlertCircle, Eye, EyeOff, LogIn } from 'lucide-react'
import { Button } from '@portabox/ui'
import { useAuth } from '../hooks/useAuth'
import styles from './LoginPage.module.css'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPwd, setShowPwd] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      await login({ email, password })
      const redirectTo = searchParams.get('redirectTo') ?? '/condominios'
      navigate(redirectTo, { replace: true })
    } catch {
      setError('E-mail ou senha inválidos. Tente novamente.')
    } finally {
      setLoading(false)
    }
  }

  const invalid = error !== null

  return (
    <div className={styles.screen}>
      <section className={styles.brandPane} aria-hidden="true">
        <div className={styles.lockup}>
          <img src="/logo-portabox-mark.svg" alt="" className={styles.lockupLogo} />
          <span className={styles.lockupWm}>PortaBox</span>
        </div>
        <div className={styles.hero}>
          <div className={styles.eyebrowLine}>Backoffice · Operação</div>
          <h1 className={styles.heroTitle}>
            Logística interna dos condomínios atendidos pela plataforma.
          </h1>
          <p className={styles.heroLead}>
            Provisionamento de tenants, onboarding de síndicos e auditoria das transições — tudo
            em um só lugar.
          </p>
          <div className={styles.footline}>
            <span>v0.1.0 · homolog</span>
            <span className={styles.dot}>•</span>
            <span>Acesso restrito à equipe</span>
          </div>
        </div>
        <div className={styles.deco} />
        <div className={`${styles.deco} ${styles.decoB}`} />
        <div className={styles.botLegal}>© 2026 PortaBox · pt-BR</div>
      </section>

      <section className={styles.formPane}>
        <div className={styles.formCard} role="main">
          <div className={styles.welcome}>Bem-vindo ao PortaBox</div>
          <h2 className={styles.formTitle}>Entrar no backoffice</h2>
          <p className={styles.lede}>
            Use o e-mail corporativo fornecido pela equipe da plataforma.
          </p>

          {error ? (
            <div className={styles.errMsg} role="alert">
              <AlertCircle size={18} aria-hidden="true" className={styles.errIcon} />
              <div>
                <strong>Não conseguimos entrar com esses dados</strong>
                {error}
              </div>
            </div>
          ) : null}

          <form onSubmit={handleSubmit} noValidate autoComplete="off">
            <div className={styles.ff}>
              <label className={styles.label} htmlFor="email">
                E-mail
              </label>
              <input
                id="email"
                name="email"
                type="email"
                autoComplete="email"
                placeholder="voce@portabox.app"
                className={styles.input}
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                data-state={invalid ? 'error' : undefined}
                required
              />
            </div>

            <div className={styles.ff}>
              <label className={styles.label} htmlFor="password">
                Senha
              </label>
              <div className={styles.inputWrap}>
                <input
                  id="password"
                  name="password"
                  type={showPwd ? 'text' : 'password'}
                  autoComplete="current-password"
                  placeholder="••••••••••"
                  className={styles.input}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  data-state={invalid ? 'error' : undefined}
                  required
                />
                <button
                  type="button"
                  className={styles.pwdToggle}
                  onClick={() => setShowPwd((v) => !v)}
                  aria-label={showPwd ? 'Ocultar senha' : 'Mostrar senha'}
                  aria-pressed={showPwd}
                >
                  {showPwd ? (
                    <EyeOff size={16} aria-hidden="true" />
                  ) : (
                    <Eye size={16} aria-hidden="true" />
                  )}
                </button>
              </div>
            </div>

            <Button type="submit" variant="primary" loading={loading} className={styles.submit}>
              {loading ? (
                'Entrando…'
              ) : (
                <>
                  <LogIn size={16} aria-hidden="true" />
                  Entrar
                </>
              )}
            </Button>
          </form>

          <div className={styles.formMeta}>
            <span>COOKIE-BASED · XSRF</span>
            <span>POST /api/v1/auth/login</span>
          </div>
        </div>
      </section>
    </div>
  )
}
