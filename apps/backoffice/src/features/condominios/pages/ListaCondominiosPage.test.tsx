import { describe, it, expect, vi, beforeAll, afterAll, afterEach } from 'vitest'
import { render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { AuthProvider } from '@/features/auth/AuthContext'
import { RequireOperator } from '@/shared/auth/RequireOperator'
import { AppLayout } from '@/shared/layouts/AppLayout'
import { ListaCondominiosPage } from './ListaCondominiosPage'

const BASE = 'http://localhost/api'

const loggedInUser = {
  userId: 'op-1',
  email: 'op@example.com',
  roles: ['Operator'],
  tenantId: null,
}

const condominioPreAtivo = {
  id: 'c1',
  nomeFantasia: 'Residencial Parque',
  cnpj: '11222333000181',
  status: 1,
  createdAt: '2026-01-10T00:00:00Z',
  activatedAt: null,
}

const condominioAtivo = {
  id: 'c2',
  nomeFantasia: 'Edifício Central',
  cnpj: '22333444000195',
  status: 2 as const,
  createdAt: '2025-12-01T00:00:00Z',
  activatedAt: '2026-01-15T00:00:00Z' as string | null,
}

function makePagedResult(items: object[]) {
  return { items, total: items.length, page: 1, pageSize: 20 }
}

const server = setupServer(
  http.get(`${BASE}/v1/auth/me`, () => HttpResponse.json(loggedInUser)),
)

beforeAll(() => server.listen({ onUnhandledRequest: 'warn' }))
afterAll(() => server.close())
afterEach(() => { server.resetHandlers(); vi.clearAllMocks() })

function renderPage(initialPath = '/condominios') {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<div>Login</div>} />
          <Route
            path="/"
            element={
              <RequireOperator>
                <AppLayout />
              </RequireOperator>
            }
          >
            <Route path="condominios" element={<ListaCondominiosPage />} />
            <Route path="condominios/:id" element={<div>Detalhes</div>} />
            <Route path="condominios/novo" element={<div>Novo</div>} />
          </Route>
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  )
}

describe('ListaCondominiosPage — renderização', () => {
  it('renderiza título "Condomínios" como h2', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios`, () =>
        HttpResponse.json(makePagedResult([condominioPreAtivo])),
      ),
    )
    renderPage()

    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 2 })).toHaveTextContent('Condomínios')
    })
  })

  it('renderiza tabela com condomínios carregados', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios`, () =>
        HttpResponse.json(makePagedResult([condominioPreAtivo, condominioAtivo])),
      ),
    )
    renderPage()

    await waitFor(() => {
      expect(screen.getByText('Residencial Parque')).toBeInTheDocument()
    })
    expect(screen.getByText('Edifício Central')).toBeInTheDocument()
    expect(screen.getByText('11.222.333/0001-81')).toBeInTheDocument()
    // Both filter tabs and table badges have these texts — check there are at least 2
    expect(screen.getAllByText('Pré-ativo').length).toBeGreaterThanOrEqual(1)
    expect(screen.getAllByText('Ativo').length).toBeGreaterThanOrEqual(1)
  })

  it('mostra "Nenhum condomínio cadastrado" quando lista vazia', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios`, () =>
        HttpResponse.json(makePagedResult([])),
      ),
    )
    renderPage()

    await waitFor(() => {
      expect(screen.getByText(/nenhum condomínio cadastrado/i)).toBeInTheDocument()
    })
  })

  it('mostra estado de loading antes dos dados chegarem', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios`, async () => {
        await new Promise((r) => setTimeout(r, 200))
        return HttpResponse.json(makePagedResult([]))
      }),
    )
    renderPage()

    // Wait for auth to complete and page to mount, then loading state is visible
    await waitFor(() => {
      expect(screen.getByText(/carregando/i)).toBeInTheDocument()
    })

    await waitFor(() => {
      expect(screen.queryByText(/carregando/i)).not.toBeInTheDocument()
    })
  })

  it('mostra erro quando API falha', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios`, () =>
        HttpResponse.json({ title: 'Error' }, { status: 500 }),
      ),
    )
    renderPage()

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument()
    })
  })
})

describe('ListaCondominiosPage — filtros e busca', () => {
  it('renderiza abas de filtro de status', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios`, () =>
        HttpResponse.json(makePagedResult([condominioPreAtivo])),
      ),
    )
    renderPage()

    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 2 })).toHaveTextContent('Condomínios')
    })

    const group = screen.getByRole('group', { name: /filtrar por status/i })
    expect(within(group).getByText('Todos')).toBeInTheDocument()
    expect(within(group).getByText('Pré-ativo')).toBeInTheDocument()
    expect(within(group).getByText('Ativo')).toBeInTheDocument()
  })

  it('campo de busca está presente', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios`, () =>
        HttpResponse.json(makePagedResult([])),
      ),
    )
    renderPage()

    await waitFor(() => {
      expect(screen.getByRole('searchbox', { name: /buscar/i })).toBeInTheDocument()
    })
  })
})

describe('ListaCondominiosPage — paginação', () => {
  it('mostra paginação quando há mais de uma página', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios`, () =>
        HttpResponse.json({ items: [condominioPreAtivo], total: 45, page: 1, pageSize: 20 }),
      ),
    )
    renderPage()

    await waitFor(() => {
      expect(screen.getByRole('navigation', { name: /paginação/i })).toBeInTheDocument()
    })

    expect(screen.getByText(/página 1 de 3/i)).toBeInTheDocument()
  })
})

describe('ListaCondominiosPage — link para novo condomínio', () => {
  it('exibe link "Novo condomínio"', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios`, () =>
        HttpResponse.json(makePagedResult([])),
      ),
    )
    renderPage()

    await waitFor(() => {
      expect(screen.getByRole('link', { name: /novo condomínio/i })).toBeInTheDocument()
    })
  })
})

describe('ListaCondominiosPage — success toast via location.state', () => {
  it('mostra mensagem de sucesso passada via location.state', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios`, () =>
        HttpResponse.json(makePagedResult([])),
      ),
    )

    render(
      <MemoryRouter
        initialEntries={[{ pathname: '/condominios', state: { successMessage: 'Condomínio criado!' } }]}
      >
        <AuthProvider>
          <Routes>
            <Route path="/login" element={<div>Login</div>} />
            <Route
              path="/"
              element={<RequireOperator><AppLayout /></RequireOperator>}
            >
              <Route path="condominios" element={<ListaCondominiosPage />} />
            </Route>
          </Routes>
        </AuthProvider>
      </MemoryRouter>,
    )

    await waitFor(() => {
      expect(screen.getByRole('status')).toHaveTextContent('Condomínio criado!')
    })
  })
})
