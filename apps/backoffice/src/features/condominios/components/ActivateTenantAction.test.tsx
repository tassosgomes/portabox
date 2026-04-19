import { describe, it, expect, vi, beforeAll, afterAll, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { ActivateTenantAction } from './ActivateTenantAction'

const BASE = 'http://localhost/api'

const server = setupServer()

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
afterAll(() => server.close())
afterEach(() => { server.resetHandlers(); vi.clearAllMocks() })

function renderAction(onActivated = vi.fn()) {
  return render(<ActivateTenantAction condominioId="condo-1" onActivated={onActivated} />)
}

describe('ActivateTenantAction', () => {
  it('abre modal ao clicar em "Ativar operação"', async () => {
    const user = userEvent.setup()
    renderAction()

    await user.click(screen.getByRole('button', { name: /ativar operação/i }))

    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /confirmar ativação/i })).toBeInTheDocument()
  })

  it('exige segunda confirmação: dois cliques para ativar', async () => {
    server.use(
      http.post(`${BASE}/v1/admin/condominios/condo-1:activate`, () =>
        HttpResponse.json({ id: 'condo-1', status: 2 }),
      ),
    )
    const onActivated = vi.fn()
    const user = userEvent.setup()
    renderAction(onActivated)

    // first click opens modal
    await user.click(screen.getByRole('button', { name: /ativar operação/i }))
    expect(screen.getByRole('dialog')).toBeInTheDocument()

    // second click (inside modal) confirms
    await user.click(screen.getByRole('button', { name: /confirmar ativação/i }))

    await waitFor(() => {
      expect(onActivated).toHaveBeenCalledTimes(1)
    })
  })

  it('fecha modal ao cancelar', async () => {
    const user = userEvent.setup()
    renderAction()

    await user.click(screen.getByRole('button', { name: /ativar operação/i }))
    expect(screen.getByRole('dialog')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /cancelar/i }))

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('mostra erro quando API retorna falha', async () => {
    server.use(
      http.post(`${BASE}/v1/admin/condominios/condo-1:activate`, () =>
        HttpResponse.json({ title: 'Error' }, { status: 500 }),
      ),
    )
    const user = userEvent.setup()
    renderAction()

    await user.click(screen.getByRole('button', { name: /ativar operação/i }))
    await user.click(screen.getByRole('button', { name: /confirmar ativação/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument()
    })
  })
})
