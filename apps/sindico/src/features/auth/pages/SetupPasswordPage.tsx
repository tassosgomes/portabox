import { type FormEvent, useState, useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { Button, Input } from '@portabox/ui'
import { apiClient, ApiHttpError } from '@/shared/api/client'
import type { PasswordSetupRequest } from '@/shared/api/types'
import styles from './SetupPasswordPage.module.css'

const MIN_LENGTH = 10
const POLICY_REGEX = /^(?=.*[A-Za-z])(?=.*\d).{10,}$/

function meetsPolicy(password: string): boolean {
  return POLICY_REGEX.test(password)
}

export function SetupPasswordPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''

  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)
  const [loading, setLoading] = useState(false)

  const passwordValid = meetsPolicy(password)
  const confirmMatch = password === confirm
  const canSubmit = passwordValid && confirmMatch && !loading

  useEffect(() => {
    if (success) {
      const timer = setTimeout(() => {
        navigate('/login', { replace: true })
      }, 2000)
      return () => clearTimeout(timer)
    }
  }, [success, navigate])

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      const payload: PasswordSetupRequest = { token, password }
      await apiClient.post('/v1/auth/password-setup', payload)
      setSuccess(true)
    } catch (err) {
      if (err instanceof ApiHttpError && err.status === 400) {
        setError(
          'Link inválido ou expirado. Entre em contato com a equipe do condomínio para receber um novo link.',
        )
      } else {
        setError('Ocorreu um erro inesperado. Tente novamente.')
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className={styles.card} role="main">
      <img src="/logo-portabox.png" alt="PortaBox" className={styles.logo} />
      <h1 className={styles.title}>Defina sua senha</h1>
      {success ? (
        <p className={styles.successMsg} role="status">
          Senha definida com sucesso. Redirecionando para o login…
        </p>
      ) : (
        <form className={styles.form} onSubmit={handleSubmit} noValidate>
          <Input
            label="Nova senha"
            id="password"
            type="password"
            autoComplete="new-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
          <Input
            label="Confirmar senha"
            id="confirm"
            type="password"
            autoComplete="new-password"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            required
          />
          <ul className={styles.policy} aria-label="Requisitos da senha">
            <li className={password.length >= MIN_LENGTH ? styles.met : undefined}>
              Mínimo {MIN_LENGTH} caracteres
            </li>
            <li className={/[A-Za-z]/.test(password) ? styles.met : undefined}>
              Pelo menos uma letra
            </li>
            <li className={/\d/.test(password) ? styles.met : undefined}>
              Pelo menos um número
            </li>
          </ul>
          {error ? (
            <p className={styles.error} role="alert">
              {error}
            </p>
          ) : null}
          <Button type="submit" variant="primary" loading={loading} disabled={!canSubmit}>
            Definir senha
          </Button>
        </form>
      )}
      {error ? (
        <a href="/login" className={styles.backLink}>
          Voltar para login
        </a>
      ) : null}
    </div>
  )
}
