import { useState } from 'react'
import { Button } from '@portabox/ui'
import { Mail } from '@portabox/ui'
import { resendMagicLink, ApiHttpError } from '../api'

interface Props {
  condominioId: string
  sindicoUserId: string
}

export function ResendMagicLinkAction({ condominioId, sindicoUserId }: Props) {
  const [sending, setSending] = useState(false)
  const [message, setMessage] = useState<{ text: string; type: 'success' | 'error' } | null>(null)

  async function handleResend() {
    setSending(true)
    setMessage(null)
    try {
      await resendMagicLink(condominioId, sindicoUserId)
      setMessage({ text: 'Magic link reenviado com sucesso.', type: 'success' })
    } catch (err) {
      if (err instanceof ApiHttpError && err.status === 429) {
        setMessage({
          text: 'Aguarde alguns minutos antes de reenviar o magic link.',
          type: 'error',
        })
      } else {
        setMessage({ text: 'Erro ao reenviar magic link. Tente novamente.', type: 'error' })
      }
    } finally {
      setSending(false)
    }
  }

  return (
    <div>
      <Button variant="secondary" onClick={handleResend} loading={sending}>
        <Mail size={16} aria-hidden="true" />
        Reenviar magic link
      </Button>
      {message && (
        <p
          role={message.type === 'error' ? 'alert' : 'status'}
          style={{
            marginTop: 'var(--sp-2)',
            fontSize: 'var(--fs-sm)',
            color: message.type === 'success' ? 'var(--pb-green-700, #15803d)' : 'var(--pb-red-600, #dc2626)',
          }}
        >
          {message.text}
        </p>
      )}
    </div>
  )
}
