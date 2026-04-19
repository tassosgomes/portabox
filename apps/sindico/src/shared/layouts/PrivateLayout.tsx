import { Outlet } from 'react-router-dom'
import { useNavigate } from 'react-router-dom'
import { LogOut } from 'lucide-react'
import { useAuth } from '@/features/auth/hooks/useAuth'
import styles from './PrivateLayout.module.css'

export function PrivateLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  async function handleLogout() {
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className={styles.layout}>
      <header className={styles.topbar} role="banner">
        <img src="/logo-portabox.png" alt="PortaBox" className={styles.logo} />
        <div className={styles.userArea}>
          <span className={styles.userName}>{user?.name}</span>
          <button
            type="button"
            className={styles.logoutBtn}
            onClick={() => void handleLogout()}
          >
            <LogOut size={16} aria-hidden="true" />
            Sair
          </button>
        </div>
      </header>
      <main className={styles.main}>
        <Outlet />
      </main>
    </div>
  )
}
