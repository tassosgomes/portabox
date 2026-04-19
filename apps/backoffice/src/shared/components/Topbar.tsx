import { LogOut } from 'lucide-react'
import { useAuth } from '@/features/auth/hooks/useAuth'
import styles from './Topbar.module.css'

function initialsFrom(name: string | null | undefined): string {
  const tokens = (name ?? '').trim().split(/\s+/).filter(Boolean)
  if (tokens.length === 0) return '?'
  const first = tokens[0]?.[0] ?? ''
  const second = tokens.length > 1 ? (tokens[tokens.length - 1]?.[0] ?? '') : ''
  return (first + second).toUpperCase()
}

export function Topbar() {
  const { user, logout } = useAuth()

  return (
    <header className={styles.topbar} role="banner">
      <div className={styles.brand}>
        <img src="/logo-portabox-mark.svg" alt="PortaBox" className={styles.brandLogo} />
        <span className={styles.brandWm}>PortaBox</span>
        <span className={styles.envTag}>Homolog</span>
      </div>
      <div className={styles.spacer} />
      {user ? (
        <div className={styles.opId}>
          <div className={styles.avatar} aria-hidden="true">
            {initialsFrom(user.name)}
          </div>
          <div className={styles.who}>
            <span className={styles.whoName}>{user.name}</span>
            <span className={styles.whoRole}>{user.role}</span>
          </div>
        </div>
      ) : null}
      <button
        type="button"
        className={styles.logoutBtn}
        onClick={() => void logout()}
        aria-label="Sair"
      >
        <LogOut size={14} strokeWidth={1.75} aria-hidden="true" />
        Sair
      </button>
    </header>
  )
}
