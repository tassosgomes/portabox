import { useState } from 'react'
import { Button, Modal } from '@portabox/ui'
import { Power } from '@portabox/ui'
import { activateCondominio, ApiHttpError } from '../api'

interface Props {
  condominioId: string
  onActivated: () => void
}

export function ActivateTenantAction({ condominioId, onActivated }: Props) {
  const [open, setOpen] = useState(false)
  const [confirming, setConfirming] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleConfirm() {
    setConfirming(true)
    setError(null)
    try {
      await activateCondominio(condominioId)
      setOpen(false)
      onActivated()
    } catch (err) {
      if (err instanceof ApiHttpError) {
        setError('Não foi possível ativar o condomínio. Tente novamente.')
      } else {
        setError('Erro inesperado. Tente novamente.')
      }
    } finally {
      setConfirming(false)
    }
  }

  return (
    <>
      <Button variant="primary" onClick={() => setOpen(true)}>
        <Power size={16} aria-hidden="true" />
        Ativar operação
      </Button>

      <Modal
        open={open}
        onClose={() => setOpen(false)}
        title="Confirmar ativação"
        size="sm"
      >
        <p>
          Ao ativar, o condomínio passará para estado <strong>Ativo</strong> e o síndico
          poderá acessar o painel. Esta ação não pode ser desfeita.
        </p>
        {error && (
          <p role="alert" style={{ color: 'var(--pb-red-600, #dc2626)', fontSize: 'var(--fs-sm)' }}>
            {error}
          </p>
        )}
        <div style={{ display: 'flex', gap: 'var(--sp-3)', justifyContent: 'flex-end', marginTop: 'var(--sp-4)' }}>
          <Button variant="secondary" onClick={() => setOpen(false)} disabled={confirming}>
            Cancelar
          </Button>
          <Button variant="primary" onClick={handleConfirm} loading={confirming}>
            Confirmar ativação
          </Button>
        </div>
      </Modal>
    </>
  )
}
