import { Building2 } from 'lucide-react'
import { Button, Card } from '@portabox/ui'
import styles from './EmptyState.module.css'

interface EmptyStateProps {
  onCreateFirstBlock: () => void
}

export function EmptyState({ onCreateFirstBlock }: EmptyStateProps) {
  return (
    <Card className={styles.card} padding="lg">
      <div className={styles.iconWrap} aria-hidden="true">
        <Building2 size={28} strokeWidth={2} />
      </div>
      <div className={styles.copy}>
        <h2 className={styles.title}>Sua estrutura ainda está vazia</h2>
        <p className={styles.description}>
          Cadastre o primeiro bloco para começar a organizar andares e unidades do condomínio.
        </p>
      </div>
      <Button type="button" onClick={onCreateFirstBlock}>
        Cadastrar primeiro bloco
      </Button>
    </Card>
  )
}
