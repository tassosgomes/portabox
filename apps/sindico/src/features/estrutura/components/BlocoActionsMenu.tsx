import { useEffect, useRef, useState } from 'react'
import { Button } from '@portabox/ui'
import styles from './BlocoActionsMenu.module.css'

interface BlocoActionsMenuProps {
  blocoNome: string
  ativo: boolean
  onRename: () => void
  onInativar: () => void
  onReativar: () => void
}

export function BlocoActionsMenu({
  blocoNome,
  ativo,
  onRename,
  onInativar,
  onReativar,
}: BlocoActionsMenuProps) {
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) {
      return
    }

    function handlePointerDown(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false)
      }
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setOpen(false)
      }
    }

    document.addEventListener('mousedown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)

    return () => {
      document.removeEventListener('mousedown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [open])

  function handleAction(callback: () => void) {
    setOpen(false)
    callback()
  }

  return (
    <div className={styles.root} ref={rootRef}>
      <Button
        type="button"
        size="sm"
        variant="ghost"
        aria-haspopup="menu"
        aria-expanded={open}
        aria-label={`Ações do bloco ${blocoNome}`}
        onClick={() => setOpen((current) => !current)}
      >
        Ações
      </Button>

      {open ? (
        <div className={styles.menu} role="menu" aria-label={`Menu de ações do bloco ${blocoNome}`}>
          {ativo ? (
            <>
              <button type="button" role="menuitem" className={styles.item} onClick={() => handleAction(onRename)}>
                Renomear
              </button>
              <button type="button" role="menuitem" className={styles.item} onClick={() => handleAction(onInativar)}>
                Inativar
              </button>
            </>
          ) : (
            <button type="button" role="menuitem" className={styles.item} onClick={() => handleAction(onReativar)}>
              Reativar
            </button>
          )}
        </div>
      ) : null}
    </div>
  )
}
