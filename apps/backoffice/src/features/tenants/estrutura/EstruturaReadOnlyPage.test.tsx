import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { Estrutura } from '@portabox/api-client'
import { EstruturaReadOnlyPage } from './EstruturaReadOnlyPage'
import { useEstruturaAdmin } from './hooks/useEstruturaAdmin'

vi.mock('./hooks/useEstruturaAdmin', () => ({
  useEstruturaAdmin: vi.fn(),
}))

vi.mock('../components/TenantSelector', () => ({
  TenantSelector: ({ condominioId }: { condominioId: string }) => (
    <div data-testid="tenant-selector">tenant:{condominioId}</div>
  ),
}))

const estruturaFixture: Estrutura = {
  condominioId: 'cond-1',
  nomeFantasia: 'Residencial Sol',
  geradoEm: '2026-04-20T10:00:00Z',
  blocos: [
    {
      id: 'bloco-1',
      nome: 'Bloco A',
      ativo: true,
      andares: [
        {
          andar: 1,
          unidades: [
            { id: 'un-1', numero: '101', ativo: true },
            { id: 'un-2', numero: '102', ativo: false },
          ],
        },
      ],
    },
    {
      id: 'bloco-2',
      nome: 'Bloco B',
      ativo: false,
      andares: [
        {
          andar: 0,
          unidades: [{ id: 'un-3', numero: '001', ativo: true }],
        },
      ],
    },
  ],
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/tenants/cond-1/estrutura']}>
      <Routes>
        <Route path="/tenants/:condominioId/estrutura" element={<EstruturaReadOnlyPage />} />
        <Route path="/erro/acesso-negado" element={<div>Acesso negado</div>} />
        <Route path="/login" element={<div>Login</div>} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('EstruturaReadOnlyPage', () => {
  beforeEach(() => {
    vi.mocked(useEstruturaAdmin).mockReset()
    vi.mocked(useEstruturaAdmin).mockImplementation((_condominioId, includeInactive) => ({
      data: estruturaFixture,
      error: null,
      isPending: false,
      isFetching: includeInactive,
    }) as ReturnType<typeof useEstruturaAdmin>)
  })

  it('renders in read-only mode without write CTAs and shows active counters', async () => {
    renderPage()

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Estrutura do condomínio' })).toBeInTheDocument()
    })

    expect(screen.getByText('Blocos ativos')).toBeInTheDocument()
    expect(screen.getByText('Unidades ativas')).toBeInTheDocument()
    expect(screen.getByText('1')).toBeInTheDocument()
    expect(screen.getByText('2')).toBeInTheDocument()
    expect(screen.getByText('Esta visão é somente leitura. Criação, edição e inativação continuam restritas ao app do síndico.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /novo bloco/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /adicionar unidade/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /inativar/i })).not.toBeInTheDocument()
  })

  it('toggles includeInactive and refetches the page hook with the updated flag', async () => {
    const user = userEvent.setup()
    renderPage()

    await waitFor(() => {
      expect(vi.mocked(useEstruturaAdmin)).toHaveBeenCalledWith('cond-1', false)
    })

    await user.click(screen.getByLabelText('Mostrar inativos'))

    await waitFor(() => {
      expect(vi.mocked(useEstruturaAdmin)).toHaveBeenLastCalledWith('cond-1', true)
    })
  })
})
