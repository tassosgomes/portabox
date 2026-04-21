import type { HTMLAttributes } from 'react'
import { Button } from '../button/Button'
import { Modal } from '../modal/Modal'
import styles from './ConfirmModal.module.css'

export interface ConfirmModalProps extends HTMLAttributes<HTMLDivElement> {
  open: boolean
  title: string
  description: string
  confirmLabel: string
  cancelLabel: string
  danger?: boolean
  onConfirm: () => void
  onCancel: () => void
}

export function ConfirmModal({
  open,
  title,
  description,
  confirmLabel,
  cancelLabel,
  danger = false,
  onConfirm,
  onCancel,
  className = '',
  ...rest
}: ConfirmModalProps) {
  return (
    <Modal open={open} onClose={onCancel} title={title} size="sm" className={className} {...rest}>
      <div className={styles.content}>
        <p className={styles.description}>{description}</p>
        <div className={styles.actions}>
          <Button type="button" variant="ghost" onClick={onCancel}>
            {cancelLabel}
          </Button>
          <Button type="button" variant={danger ? 'danger' : 'primary'} onClick={onConfirm}>
            {confirmLabel}
          </Button>
        </div>
      </div>
    </Modal>
  )
}
