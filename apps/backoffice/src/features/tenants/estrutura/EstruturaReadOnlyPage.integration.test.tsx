import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import { MemoryRouter } from 'react-router-dom'
import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { configure, queryKeys } from '@portabox/api-client'
import { AuthProvider } from '@/features/auth/AuthContext'
import { AppRoutes } from '@/app/routes'

const BASE = 'http://localhost/api'

const loggedInUser = {
  userId: 'op-1',
  email: 'op@example.com',
  roles: ['Operator'],
  tenantId: null,
}

const tenantOptions = {
  items: [
    {
      id: 'cond-1',
      nomeFantasia: 'Residencial Sol',
      cnpjMasked: '11.222.333/0001-81',
      status: 2,
      createdAt: '2026-01-01T00:00:00Z',
      activatedAt: '2026-01-02T00:00:00Z',
    },
    {
      id: 'cond-2',
      nomeFantasia: 'Residencial Lua',
      cnpjMasked: '22.333.444/0001-81',
      status: 1,
      createdAt: '2026-01-03T00:00:00Z',
      activatedAt: null,
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 100,
}

const estruturaSol = {
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
}

const estruturaLua = {
  condominioId: 'cond-2',
  nomeFantasia: 'Residencial Lua',
  geradoEm: '2026-04-20T10:00:00Z',
  blocos: [
    {
      id: 'bloco-2',
      nome: 'Torre Única',
      ativo: true,
      andares: [
        {
          andar: 2,
          unidades: [{ id: 'un-2', numero: '201', ativo: true }],
        },
      ],
    },
  ],
}

const requestUrls: string[] = []

const server = setupServer(
  http.get(`${BASE}/v1/auth/me`, () => HttpResponse.json(loggedInUser)),
  http.get(`${BASE}/v1/admin/condominios`, ({ request }) => {
    requestUrls.push(request.url)
    return HttpResponse.json(tenantOptions)
  }),
  http.get(`${BASE}/v1/admin/condominios/cond-1/estrutura`, ({ request }) => {
    requestUrls.push(request.url)
    return HttpResponse.json(estruturaSol)
  }),
  http.get(`${BASE}/v1/admin/condominios/cond-2/estrutura`, ({ request }) => {
    requestUrls.push(request.url)
    return HttpResponse.json(estruturaLua)
  }),
)

beforeAll(() => {
  configure({ baseUrl: 'http://localhost/api/v1' })
  server.listen({ onUnhandledRequest: 'warn' })
})
afterAll(() => server.close())
afterEach(() => {
  server.resetHandlers()
  requestUrls.length = 0
  vi.clearAllMocks()
})

function renderApp(initialPath = '/tenants/cond-1/estrutura') {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })

  const view = render(
    <MemoryRouter initialEntries={[initialPath]}>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <AppRoutes />
        </AuthProvider>
      </QueryClientProvider>
    </MemoryRouter>,
  )

  return { ...view, queryClient }
}

describe('EstruturaReadOnlyPage integration', () => {
  it('renders the tree with tenant selector using MSW responses', async () => {
    renderApp()

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Estrutura do condomínio' })).toBeInTheDocument()
      expect(screen.getByRole('combobox', { name: 'Condomínio' })).toBeInTheDocument()
      expect(screen.getByRole('heading', { name: 'Residencial Sol', level: 2 })).toBeInTheDocument()
      expect(screen.getByText('Bloco A · 1 unidade ativa')).toBeInTheDocument()
    })

    expect(screen.queryByRole('button', { name: /novo bloco/i })).not.toBeInTheDocument()
  })

  it('redirects to access denied page when admin estrutura returns 403', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios/cond-1/estrutura`, () =>
        HttpResponse.json({ title: 'Forbidden' }, { status: 403 }),
      ),
    )

    renderApp()

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Acesso negado' })).toBeInTheDocument()
    })
  })

  it('loads a new tenant after selector change and stores the response under a new query key', async () => {
    const { queryClient } = renderApp()
    const user = userEvent.setup()

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Residencial Sol', level: 2 })).toBeInTheDocument()
    })

    await user.selectOptions(screen.getByRole('combobox', { name: 'Condomínio' }), 'cond-2')

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Residencial Lua', level: 2 })).toBeInTheDocument()
    })

    expect(queryClient.getQueryData(queryKeys.estruturaAdmin('cond-1'))).toBeTruthy()
    expect(queryClient.getQueryData(queryKeys.estruturaAdmin('cond-2'))).toBeTruthy()
    expect(requestUrls.some((url) => url.includes('/v1/admin/condominios/cond-1/estrutura'))).toBe(true)
    expect(requestUrls.some((url) => url.includes('/v1/admin/condominios/cond-2/estrutura'))).toBe(true)
  })
})
