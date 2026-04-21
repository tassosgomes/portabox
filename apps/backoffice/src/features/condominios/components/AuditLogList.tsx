import type { AuditEntry } from '../types'
import styles from './AuditLogList.module.css'

const EVENT_KIND_LABELS: Record<number, string> = {
  1: 'Criado',
  2: 'Ativado',
  3: 'Magic link reenviado',
  4: 'Outro',
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}

interface AuditLogListProps {
  entries: AuditEntry[]
}

export function AuditLogList({ entries }: AuditLogListProps) {
  if (entries.length === 0) {
    return <p className={styles.empty}>Nenhuma entrada de auditoria.</p>
  }
  return (
    <ol className={styles.list} aria-label="Histórico de auditoria">
      {entries.map((entry) => (
        <li key={entry.id} className={styles.item}>
          <span className={styles.kind}>{EVENT_KIND_LABELS[entry.eventKind] ?? 'Outro'}</span>
          <span className={styles.meta}>
            {entry.performedByUserId} &middot; {formatDate(entry.occurredAt)}
          </span>
          {entry.note && <span className={styles.note}>{entry.note}</span>}
        </li>
      ))}
    </ol>
  )
}
