import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { ApiError } from '@portabox/api-client'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { EstruturaPage } from '../EstruturaPage'

const mockUseEstrutura = vi.fn()
const mockUseAuth = vi.fn()

vi.mock('../hooks/useEstrutura', () => ({
  useEstrutura: (...args: unknown[]) => mockUseEstrutura(...args),
}))

vi.mock('@/features/auth/hooks/useAuth', () => ({
  useAuth: () => mockUseAuth(),
}))

function renderPage(initialPath = '/estrutura') {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route path="/login" element={<p>Tela de login</p>} />
          <Route path="/" element={<p>Início</p>} />
          <Route path="/estrutura" element={<EstruturaPage />} />
          <Route path="/estrutura/:condominioId" element={<EstruturaPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('EstruturaPage', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({
      user: { id: 'u1', email: 'sindico@example.com', name: 'Maria', role: 'Sindico', tenantId: 'cond-1' },
      isAuthenticated: true,
      isLoading: false,
      login: vi.fn(),
      logout: vi.fn(),
    })
    mockUseEstrutura.mockReset()
  })

  it('renders loading state while query is pending', () => {
    mockUseEstrutura.mockReturnValue({
      data: undefined,
      error: null,
      isPending: true,
      isFetching: true,
      isError: false,
      refetch: vi.fn(),
    })

    renderPage()

    expect(screen.getByRole('status')).toHaveTextContent('Carregando a estrutura do condomínio')
  })

  it('renders EmptyState when the condomínio has no blocos', () => {
    mockUseEstrutura.mockReturnValue({
      data: {
        condominioId: 'cond-1',
        nomeFantasia: 'Residencial Sol',
        blocos: [],
        geradoEm: '2026-04-20T10:00:00Z',
      },
      error: null,
      isPending: false,
      isFetching: false,
      isError: false,
      refetch: vi.fn(),
    })

    renderPage()

    expect(screen.getByRole('heading', { level: 2 })).toHaveTextContent('Sua estrutura ainda está vazia')
    expect(screen.getByRole('button', { name: 'Cadastrar primeiro bloco' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Novo bloco' })).toBeInTheDocument()
  })

  it('renders Tree with mapped items on success', () => {
    mockUseEstrutura.mockReturnValue({
      data: {
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
                unidades: [{ id: 'un-1', numero: '101', ativo: true }],
              },
            ],
          },
        ],
      },
      error: null,
      isPending: false,
      isFetching: false,
      isError: false,
      refetch: vi.fn(),
    })

    renderPage()

    expect(screen.getByRole('tree')).toBeInTheDocument()
    expect(screen.getByRole('treeitem', { name: /Residencial Sol/i })).toBeInTheDocument()
    expect(screen.getByRole('treeitem', { name: /Bloco A/i })).toBeInTheDocument()
    expect(screen.getByText('Bloco selecionado: Bloco A')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Adicionar unidade' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Adicionar próxima unidade' })).toBeInTheDocument()
  })

  it('toggles includeInactive and re-runs the hook with the new value', async () => {
    const user = userEvent.setup()

    mockUseEstrutura.mockReturnValue({
      data: {
        condominioId: 'cond-1',
        nomeFantasia: 'Residencial Sol',
        geradoEm: '2026-04-20T10:00:00Z',
        blocos: [],
      },
      error: null,
      isPending: false,
      isFetching: false,
      isError: false,
      refetch: vi.fn(),
    })

    renderPage()

    expect(mockUseEstrutura).toHaveBeenLastCalledWith('cond-1', false)

    await user.click(screen.getByLabelText('Mostrar inativos'))

    expect(mockUseEstrutura).toHaveBeenLastCalledWith('cond-1', true)
  })

  it('redirects to login on 401 errors', async () => {
    mockUseEstrutura.mockReturnValue({
      data: undefined,
      error: new ApiError({ type: 'about:blank', title: 'Unauthorized', status: 401 }),
      isPending: false,
      isFetching: false,
      isError: true,
      refetch: vi.fn(),
    })

    renderPage('/estrutura/cond-1')

    await waitFor(() => {
      expect(screen.getByText('Tela de login')).toBeInTheDocument()
    })
  })

  it('renders dedicated 404 state with friendly copy', () => {
    mockUseEstrutura.mockReturnValue({
      data: undefined,
      error: new ApiError({ type: 'about:blank', title: 'Not Found', status: 404 }),
      isPending: false,
      isFetching: false,
      isError: true,
      refetch: vi.fn(),
    })

    renderPage('/estrutura/cond-1')

    expect(screen.getByRole('heading', { level: 2 })).toHaveTextContent('Estrutura não encontrada')
    expect(screen.getByText(/não localizamos a estrutura deste condomínio/i)).toBeInTheDocument()
  })
})
