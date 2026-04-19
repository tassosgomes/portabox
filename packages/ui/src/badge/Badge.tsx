import { type HTMLAttributes } from 'react'
import styles from './Badge.module.css'

export type BadgeStatus =
  | 'pre-ativo'
  | 'ativo'
  | 'inativo'
  | 'pendente'
  | 'processando'
  | 'erro'
  | 'info'

const STATUS_LABELS: Record<BadgeStatus, string> = {
  'pre-ativo': 'Pré-ativo',
  ativo: 'Ativo',
  inativo: 'Inativo',
  pendente: 'Pendente',
  processando: 'Processando',
  erro: 'Erro',
  info: 'Info',
}

export interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  status: BadgeStatus
  label?: string
}

export function Badge({ status, label, className = '', ...rest }: BadgeProps) {
  return (
    <span
      className={[styles.badge, styles[status], className].filter(Boolean).join(' ')}
      {...rest}
    >
      <span className={styles.dot} aria-hidden="true" />
      {label ?? STATUS_LABELS[status]}
    </span>
  )
}
