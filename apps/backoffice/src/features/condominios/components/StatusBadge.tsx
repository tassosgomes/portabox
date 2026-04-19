import { Badge } from '@portabox/ui'
import type { CondominioStatus } from '../types'

const STATUS_BADGE_MAP: Record<CondominioStatus, { status: 'pre-ativo' | 'ativo'; label: string }> = {
  1: { status: 'pre-ativo', label: 'Pré-ativo' },
  2: { status: 'ativo', label: 'Ativo' },
}

interface StatusBadgeProps {
  status: CondominioStatus
}

export function StatusBadge({ status }: StatusBadgeProps) {
  const { status: badgeStatus, label } = STATUS_BADGE_MAP[status] ?? { status: 'info', label: 'Desconhecido' }
  return <Badge status={badgeStatus} label={label} />
}
