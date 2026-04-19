import { type FormEvent, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { Button, Input } from '@portabox/ui'
import { useAuth } from '../hooks/useAuth'
import styles from './LoginPage.module.css'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    if (!e.currentTarget.checkValidity()) return
    setError(null)
    setLoading(true)
    try {
      await login({ email, password })
      const redirectTo = searchParams.get('redirectTo') ?? '/'
      navigate(redirectTo, { replace: true })
    } catch {
      setError('E-mail ou senha inválidos. Tente novamente.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className={styles.card} role="main">
      <img src="/logo-portabox.png" alt="PortaBox" className={styles.logo} />
      <h1 className={styles.title}>Bem-vindo ao PortaBox</h1>
      <form className={styles.form} onSubmit={handleSubmit} noValidate>
        <Input
          label="E-mail"
          id="email"
          type="email"
          autoComplete="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />
        <Input
          label="Senha"
          id="password"
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />
        {error ? <p className={styles.error} role="alert">{error}</p> : null}
        <Button type="submit" variant="primary" loading={loading}>
          Entrar
        </Button>
      </form>
    </div>
  )
}
