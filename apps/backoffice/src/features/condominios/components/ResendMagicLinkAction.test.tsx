import { describe, it, expect, vi, beforeAll, afterAll, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { ResendMagicLinkAction } from './ResendMagicLinkAction'

const BASE = 'http://localhost/api'
const server = setupServer()

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
afterAll(() => server.close())
afterEach(() => { server.resetHandlers(); vi.clearAllMocks() })

function renderAction() {
  return render(
    <ResendMagicLinkAction condominioId="condo-1" sindicoUserId="user-1" />,
  )
}

describe('ResendMagicLinkAction', () => {
  it('mostra toast de sucesso em 200', async () => {
    server.use(
      http.post(
        `${BASE}/v1/admin/condominios/condo-1/sindicos/user-1:resend-magic-link`,
        () => HttpResponse.json(null, { status: 200 }),
      ),
    )
    const user = userEvent.setup()
    renderAction()

    await user.click(screen.getByRole('button', { name: /reenviar magic link/i }))

    await waitFor(() => {
      expect(screen.getByRole('status')).toHaveTextContent(/reenviado com sucesso/i)
    })
  })

  it('mostra mensagem pt-BR clara em 429 (rate limit)', async () => {
    server.use(
      http.post(
        `${BASE}/v1/admin/condominios/condo-1/sindicos/user-1:resend-magic-link`,
        () => new HttpResponse(null, { status: 429 }),
      ),
    )
    const user = userEvent.setup()
    renderAction()

    await user.click(screen.getByRole('button', { name: /reenviar magic link/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/aguarde alguns minutos/i)
    })
  })

  it('mostra erro genérico em 500', async () => {
    server.use(
      http.post(
        `${BASE}/v1/admin/condominios/condo-1/sindicos/user-1:resend-magic-link`,
        () => new HttpResponse(null, { status: 500 }),
      ),
    )
    const user = userEvent.setup()
    renderAction()

    await user.click(screen.getByRole('button', { name: /reenviar magic link/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument()
    })
  })
})
