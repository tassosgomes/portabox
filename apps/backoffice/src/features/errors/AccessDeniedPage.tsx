import { Link } from 'react-router-dom'
import { Card } from '@portabox/ui'
import styles from './AccessDeniedPage.module.css'

export function AccessDeniedPage() {
  return (
    <section className={styles.page}>
      <Card className={styles.card} padding="lg">
        <h1 className={styles.title}>Acesso negado</h1>
        <p className={styles.text}>
          Seu usuário não tem permissão para visualizar a estrutura deste condomínio no backoffice.
        </p>
        <Link to="/condominios" className={styles.link}>
          Voltar para a lista de condomínios
        </Link>
      </Card>
    </section>
  )
}
