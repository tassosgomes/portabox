import { useAuth } from '@/features/auth/hooks/useAuth'
import styles from './HomePage.module.css'

export function HomePage() {
  const { user } = useAuth()

  return (
    <div className={styles.page}>
      <h1 className={styles.heading}>Bem-vindo, {user?.name}</h1>
      <p className={styles.subtitle}>
        Este é o seu painel do síndico. Funcionalidades adicionais estarão disponíveis em breve.
      </p>
    </div>
  )
}
