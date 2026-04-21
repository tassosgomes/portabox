import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { configure } from '@portabox/api-client'
import { delay, http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import type { ReactNode } from 'react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '@/app/routes'
import { EstruturaPage } from '../EstruturaPage'

const mockUseAuth = vi.fn()

vi.mock('@/features/auth/hooks/useAuth', () => ({
  useAuth: () => mockUseAuth(),
}))

const server = setupServer()

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
afterAll(() => server.close())
afterEach(() => server.resetHandlers())

beforeEach(() => {
  configure({ baseUrl: 'http://localhost/api/v1', getAuthToken: () => null })
  mockUseAuth.mockReturnValue({
    user: { id: 'u1', email: 'sindico@example.com', name: 'Maria', role: 'Sindico', tenantId: 'cond-1' },
    isAuthenticated: true,
    isLoading: false,
    login: vi.fn(),
    logout: vi.fn(),
  })
})

function renderWithQuery(ui: ReactNode, initialEntries = ['/estrutura']) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })

  return render(
    <MemoryRouter initialEntries={initialEntries}>
      <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>
    </MemoryRouter>,
  )
}

describe('EstruturaPage integration', () => {
  it('renders the tree when GET /estrutura succeeds', async () => {
    server.use(
      http.get('http://localhost/api/v1/condominios/cond-1/estrutura', () =>
        HttpResponse.json({
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
        }),
      ),
    )

    renderWithQuery(
      <Routes>
        <Route path="/estrutura" element={<EstruturaPage />} />
      </Routes>,
    )

    expect(await screen.findByRole('tree')).toBeInTheDocument()
    expect(screen.getByRole('treeitem', { name: /Bloco A/i })).toBeInTheDocument()
  })

  it('shows error and retry button on 500, then clears the error after a successful retry', async () => {
    let shouldFail = true

    server.use(
      http.get('http://localhost/api/v1/condominios/cond-1/estrutura', () => {
        if (shouldFail) {
          return HttpResponse.json(
            {
              type: 'about:blank',
              title: 'Erro interno',
              status: 500,
              detail: 'Tente novamente em instantes.',
            },
            { status: 500, headers: { 'Content-Type': 'application/problem+json' } },
          )
        }

        return HttpResponse.json({
          condominioId: 'cond-1',
          nomeFantasia: 'Residencial Sol',
          geradoEm: '2026-04-20T10:00:00Z',
          blocos: [],
        })
      }),
    )

    const user = userEvent.setup()
    renderWithQuery(
      <Routes>
        <Route path="/estrutura" element={<EstruturaPage />} />
      </Routes>,
    )

    expect(await screen.findByRole('alert')).toHaveTextContent('Tente novamente em instantes.')

    shouldFail = false
    await user.click(screen.getByRole('button', { name: 'Tentar novamente' }))

    await waitFor(() => {
      expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    })

    expect(screen.getByRole('heading', { level: 2 })).toHaveTextContent('Sua estrutura ainda está vazia')
  })

  it('registers the /estrutura route in AppRoutes', async () => {
    server.use(
      http.get('http://localhost/api/v1/condominios/cond-1/estrutura', () =>
        HttpResponse.json({
          condominioId: 'cond-1',
          nomeFantasia: 'Residencial Sol',
          geradoEm: '2026-04-20T10:00:00Z',
          blocos: [],
        }),
      ),
    )

    renderWithQuery(<AppRoutes />, ['/estrutura'])

    expect(await screen.findByRole('heading', { level: 1 })).toHaveTextContent('Estrutura do condomínio')
  })

  it('creates a bloco from the empty state and shows it in the tree', async () => {
    let estrutura = {
      condominioId: 'cond-1',
      nomeFantasia: 'Residencial Sol',
      geradoEm: '2026-04-20T10:00:00Z',
      blocos: [] as Array<{
        id: string
        nome: string
        ativo: boolean
        andares: Array<{ andar: number; unidades: Array<{ id: string; numero: string; ativo: boolean }> }>
      }>,
    }

    server.use(
      http.get('http://localhost/api/v1/condominios/cond-1/estrutura', async ({ request }) => {
        const includeInactive = new URL(request.url).searchParams.get('includeInactive') === 'true'
        return HttpResponse.json({
          ...estrutura,
          blocos: includeInactive ? estrutura.blocos : estrutura.blocos.filter((bloco) => bloco.ativo),
        })
      }),
      http.post('http://localhost/api/v1/condominios/cond-1/blocos', async ({ request }) => {
        await delay(80)
        const body = await request.json() as { nome: string }
        estrutura = {
          ...estrutura,
          blocos: [
            ...estrutura.blocos,
            { id: 'bloco-1', nome: body.nome, ativo: true, andares: [] },
          ],
        }
        return HttpResponse.json(estrutura.blocos[0], { status: 201 })
      }),
    )

    const user = userEvent.setup()
    renderWithQuery(
      <Routes>
        <Route path="/estrutura" element={<EstruturaPage />} />
      </Routes>,
    )

    await screen.findByText('Sua estrutura ainda está vazia')
    await user.click(screen.getByRole('button', { name: 'Cadastrar primeiro bloco' }))
    await user.type(screen.getByLabelText('Nome do bloco'), 'Bloco A')
    await user.click(screen.getByRole('button', { name: 'Criar bloco' }))

    expect(await screen.findByText('Bloco selecionado: Bloco A')).toBeInTheDocument()
  })

  it('keeps the form open and suggests reactivation on create conflict', async () => {
    server.use(
      http.get('http://localhost/api/v1/condominios/cond-1/estrutura', ({ request }) => {
        const includeInactive = new URL(request.url).searchParams.get('includeInactive') === 'true'
        return HttpResponse.json({
          condominioId: 'cond-1',
          nomeFantasia: 'Residencial Sol',
          geradoEm: '2026-04-20T10:00:00Z',
          blocos: includeInactive
            ? [{ id: 'bloco-inativo', nome: 'Bloco A', ativo: false, andares: [] }]
            : [],
        })
      }),
      http.post('http://localhost/api/v1/condominios/cond-1/blocos', () =>
        HttpResponse.json(
          {
            type: 'about:blank',
            title: 'Conflict',
            status: 409,
            detail: 'Ja existe um bloco inativo com esse nome. Reative-o em vez de criar outro.',
          },
          { status: 409, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    )

    const user = userEvent.setup()
    renderWithQuery(
      <Routes>
        <Route path="/estrutura" element={<EstruturaPage />} />
      </Routes>,
    )

    await screen.findByText('Sua estrutura ainda está vazia')
    await user.click(screen.getByRole('button', { name: 'Cadastrar primeiro bloco' }))
    await user.type(screen.getByLabelText('Nome do bloco'), 'Bloco A')
    await user.click(screen.getByRole('button', { name: 'Criar bloco' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Reative-o em vez de criar outro')
    expect(screen.getByRole('dialog', { name: 'Reativar bloco' })).toBeInTheDocument()
    expect(screen.getByLabelText('Mostrar inativos')).toBeChecked()
  })

  it('renames a bloco from the action menu', async () => {
    let blocoNome = 'Bloco A'

    server.use(
      http.get('http://localhost/api/v1/condominios/cond-1/estrutura', () =>
        HttpResponse.json({
          condominioId: 'cond-1',
          nomeFantasia: 'Residencial Sol',
          geradoEm: '2026-04-20T10:00:00Z',
          blocos: [{ id: 'bloco-1', nome: blocoNome, ativo: true, andares: [] }],
        }),
      ),
      http.patch('http://localhost/api/v1/condominios/cond-1/blocos/bloco-1', async ({ request }) => {
        const body = await request.json() as { nome: string }
        blocoNome = body.nome
        return HttpResponse.json({
          id: 'bloco-1',
          condominioId: 'cond-1',
          nome: blocoNome,
          ativo: true,
          inativadoEm: null,
        })
      }),
    )

    const user = userEvent.setup()
    renderWithQuery(
      <Routes>
        <Route path="/estrutura" element={<EstruturaPage />} />
      </Routes>,
    )

    await screen.findByRole('treeitem', { name: /Bloco A/i })
    await user.click(screen.getByRole('button', { name: 'Ações do bloco Bloco A' }))
    await user.click(screen.getByRole('menuitem', { name: 'Renomear' }))
    const input = await screen.findByLabelText('Nome do bloco')
    await user.clear(input)
    await user.type(input, 'Torre Alfa')
    await user.click(screen.getByRole('button', { name: 'Salvar nome' }))

    expect(await screen.findByRole('treeitem', { name: /Torre Alfa/i })).toBeInTheDocument()
  })

  it('inactivates a bloco, hides it, then shows it again when inactive items are enabled', async () => {
    let blocoAtivo = true

    server.use(
      http.get('http://localhost/api/v1/condominios/cond-1/estrutura', ({ request }) => {
        const includeInactive = new URL(request.url).searchParams.get('includeInactive') === 'true'
        return HttpResponse.json({
          condominioId: 'cond-1',
          nomeFantasia: 'Residencial Sol',
          geradoEm: '2026-04-20T10:00:00Z',
          blocos: includeInactive || blocoAtivo
            ? [{ id: 'bloco-1', nome: 'Bloco A', ativo: blocoAtivo, andares: [] }]
            : [],
        })
      }),
      http.post('http://localhost/api/v1/condominios/cond-1/blocos/bloco-1:inativar', () => {
        blocoAtivo = false
        return HttpResponse.json({
          id: 'bloco-1',
          condominioId: 'cond-1',
          nome: 'Bloco A',
          ativo: false,
          inativadoEm: '2026-04-20T10:00:00Z',
        })
      }),
    )

    const user = userEvent.setup()
    renderWithQuery(
      <Routes>
        <Route path="/estrutura" element={<EstruturaPage />} />
      </Routes>,
    )

    await screen.findByRole('treeitem', { name: /Bloco A/i })
    await user.click(screen.getByRole('button', { name: 'Ações do bloco Bloco A' }))
    await user.click(screen.getByRole('menuitem', { name: 'Inativar' }))
    await user.click(screen.getByRole('button', { name: 'Inativar bloco' }))

    await screen.findByText('Sua estrutura ainda está vazia')
    await user.click(screen.getByLabelText('Mostrar inativos'))

    expect(await screen.findByRole('treeitem', { name: /Bloco A/i })).toBeInTheDocument()
    expect(screen.getByTestId('tree-node-state-icon-bloco-bloco-1')).toBeInTheDocument()
  })

  it('creates three unidades in batch mode and keeps the modal open between submits', async () => {
    let estrutura = {
      condominioId: 'cond-1',
      nomeFantasia: 'Residencial Sol',
      geradoEm: '2026-04-20T10:00:00Z',
      blocos: [
        {
          id: 'bloco-1',
          nome: 'Bloco A',
          ativo: true,
          andares: [] as Array<{ andar: number; unidades: Array<{ id: string; numero: string; ativo: boolean }> }>,
        },
      ],
    }

    let createCount = 0

    server.use(
      http.get('http://localhost/api/v1/condominios/cond-1/estrutura', ({ request }) => {
        const includeInactive = new URL(request.url).searchParams.get('includeInactive') === 'true'
        return HttpResponse.json({
          ...estrutura,
          blocos: includeInactive ? estrutura.blocos : estrutura.blocos.filter((bloco) => bloco.ativo),
        })
      }),
      http.post('http://localhost/api/v1/condominios/cond-1/blocos/bloco-1/unidades', async ({ request }) => {
        const body = await request.json() as { andar: number; numero: string }
        createCount += 1
        const unidade = { id: `un-${createCount}`, numero: body.numero, ativo: true }
        const bloco = estrutura.blocos[0]
        const andares = bloco.andares.filter((andar) => andar.andar !== body.andar)
        const current = bloco.andares.find((andar) => andar.andar === body.andar)
        andares.push({ andar: body.andar, unidades: [...(current?.unidades ?? []), unidade] })
        andares.sort((left, right) => left.andar - right.andar)
        estrutura = {
          ...estrutura,
          blocos: [{ ...bloco, andares }],
        }

        return HttpResponse.json({
          id: unidade.id,
          blocoId: 'bloco-1',
          andar: body.andar,
          numero: body.numero,
          ativo: true,
          inativadoEm: null,
        }, { status: 201 })
      }),
    )

    const user = userEvent.setup()
    renderWithQuery(
      <Routes>
        <Route path="/estrutura" element={<EstruturaPage />} />
      </Routes>,
    )

    await screen.findByRole('treeitem', { name: /Bloco A/i })
    await user.click(screen.getByRole('button', { name: 'Adicionar próxima unidade' }))

    const andarInput = await screen.findByLabelText('Andar')
    const numeroInput = screen.getByLabelText('Número')

    await user.clear(andarInput)
    await user.type(andarInput, '1')
    await user.type(numeroInput, '101')
    await user.click(screen.getByRole('button', { name: 'Salvar e continuar' }))

    await waitFor(() => expect(screen.getByLabelText('Andar')).toHaveFocus())

    await user.clear(screen.getByLabelText('Andar'))
    await user.type(screen.getByLabelText('Andar'), '1')
    await user.type(screen.getByLabelText('Número'), '102')
    await user.click(screen.getByRole('button', { name: 'Salvar e continuar' }))

    await waitFor(() => expect(screen.getByLabelText('Andar')).toHaveFocus())

    await user.clear(screen.getByLabelText('Andar'))
    await user.type(screen.getByLabelText('Andar'), '2')
    await user.type(screen.getByLabelText('Número'), '201')
    await user.click(screen.getByRole('button', { name: 'Salvar e continuar' }))

    await user.click(screen.getByRole('treeitem', { name: /Bloco A/i }))
    await user.click(screen.getByRole('treeitem', { name: /Andar 1/i }))
    await user.click(screen.getByRole('treeitem', { name: /Andar 2/i }))

    expect(await screen.findByRole('treeitem', { name: /Unidade 101/i })).toBeInTheDocument()
    expect(screen.getByRole('treeitem', { name: /Unidade 102/i })).toBeInTheDocument()
    expect(screen.getByRole('treeitem', { name: /Unidade 201/i })).toBeInTheDocument()
    expect(screen.getByRole('dialog', { name: 'Adicionar unidade' })).toBeInTheDocument()
  })

  it('shows a clear toast and keeps the unidade form open on create conflict', async () => {
    server.use(
      http.get('http://localhost/api/v1/condominios/cond-1/estrutura', () =>
        HttpResponse.json({
          condominioId: 'cond-1',
          nomeFantasia: 'Residencial Sol',
          geradoEm: '2026-04-20T10:00:00Z',
          blocos: [{ id: 'bloco-1', nome: 'Bloco A', ativo: true, andares: [] }],
        }),
      ),
      http.post('http://localhost/api/v1/condominios/cond-1/blocos/bloco-1/unidades', () =>
        HttpResponse.json(
          {
            type: 'about:blank',
            title: 'Conflict',
            status: 409,
            detail: 'Ja existe outra unidade ativa para Bloco A / Andar 1 / Apto 101.',
          },
          { status: 409, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    )

    const user = userEvent.setup()
    renderWithQuery(
      <Routes>
        <Route path="/estrutura" element={<EstruturaPage />} />
      </Routes>,
    )

    await screen.findByRole('treeitem', { name: /Bloco A/i })
    await user.click(screen.getByRole('button', { name: 'Adicionar unidade' }))
    await user.clear(screen.getByLabelText('Andar'))
    await user.type(screen.getByLabelText('Andar'), '1')
    await user.type(screen.getByLabelText('Número'), '101')
    await user.click(within(screen.getByRole('dialog', { name: 'Adicionar unidade' })).getByRole('button', { name: 'Adicionar unidade' }))

    const alerts = await screen.findAllByRole('alert')
    expect(alerts.some((alert) => alert.textContent?.includes('Ja existe outra unidade ativa'))).toBe(true)
    expect(screen.getByRole('dialog', { name: 'Adicionar unidade' })).toBeInTheDocument()
  })

  it('shows a 422 toast telling the user to reactivate the bloco first', async () => {
    server.use(
      http.get('http://localhost/api/v1/condominios/cond-1/estrutura', () =>
        HttpResponse.json({
          condominioId: 'cond-1',
          nomeFantasia: 'Residencial Sol',
          geradoEm: '2026-04-20T10:00:00Z',
          blocos: [{ id: 'bloco-1', nome: 'Bloco A', ativo: true, andares: [] }],
        }),
      ),
      http.post('http://localhost/api/v1/condominios/cond-1/blocos/bloco-1/unidades', () =>
        HttpResponse.json(
          {
            type: 'about:blank',
            title: 'Unprocessable Entity',
            status: 422,
            detail: 'O bloco selecionado esta inativo. Reative-o antes de cadastrar unidades.',
          },
          { status: 422, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    )

    const user = userEvent.setup()
    renderWithQuery(
      <Routes>
        <Route path="/estrutura" element={<EstruturaPage />} />
      </Routes>,
    )

    await screen.findByRole('treeitem', { name: /Bloco A/i })
    await user.click(screen.getByRole('button', { name: 'Adicionar unidade' }))
    await user.clear(screen.getByLabelText('Andar'))
    await user.type(screen.getByLabelText('Andar'), '1')
    await user.type(screen.getByLabelText('Número'), '101')
    await user.click(within(screen.getByRole('dialog', { name: 'Adicionar unidade' })).getByRole('button', { name: 'Adicionar unidade' }))

    const alerts = await screen.findAllByRole('alert')
    expect(alerts.some((alert) => alert.textContent?.includes('Reative-o antes de cadastrar unidades'))).toBe(true)
  })

  it('inactivates a unidade, hides it, then shows it again when inactive items are enabled', async () => {
    let unidadeAtiva = true

    server.use(
      http.get('http://localhost/api/v1/condominios/cond-1/estrutura', ({ request }) => {
        const includeInactive = new URL(request.url).searchParams.get('includeInactive') === 'true'
        return HttpResponse.json({
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
                  unidades: includeInactive || unidadeAtiva
                    ? [{ id: 'un-1', numero: '101', ativo: unidadeAtiva }]
                    : [],
                },
              ],
            },
          ],
        })
      }),
      http.post('http://localhost/api/v1/condominios/cond-1/blocos/bloco-1/unidades/un-1:inativar', () => {
        unidadeAtiva = false
        return HttpResponse.json({
          id: 'un-1',
          blocoId: 'bloco-1',
          andar: 1,
          numero: '101',
          ativo: false,
          inativadoEm: '2026-04-20T10:00:00Z',
        })
      }),
    )

    const user = userEvent.setup()
    renderWithQuery(
      <Routes>
        <Route path="/estrutura" element={<EstruturaPage />} />
      </Routes>,
    )

    await user.click(await screen.findByRole('treeitem', { name: /Bloco A/i }))
    await screen.findByRole('treeitem', { name: /Andar 1/i })
    await user.click(screen.getByRole('treeitem', { name: /Andar 1/i }))
    await screen.findByRole('treeitem', { name: /Unidade 101/i })
    await user.click(screen.getByRole('button', { name: 'Ações da unidade 101' }))
    await user.click(screen.getByRole('menuitem', { name: 'Inativar' }))
    await user.click(screen.getByRole('button', { name: 'Inativar unidade' }))

    await waitFor(() => {
      expect(screen.queryByRole('treeitem', { name: /Unidade 101/i })).not.toBeInTheDocument()
    })

    await user.click(screen.getByLabelText('Mostrar inativos'))

    const andarNode = await screen.findByRole('treeitem', { name: /Andar 1/i })
    if (andarNode.getAttribute('aria-expanded') !== 'true') {
      await user.click(andarNode)
    }

    expect(await screen.findByRole('treeitem', { name: /Unidade 101/i })).toBeInTheDocument()
    expect(screen.getByTestId('tree-node-state-icon-unidade-un-1')).toBeInTheDocument()
  })

  it('shows a toast and keeps the unidade unchanged when reactivation conflicts', async () => {
    server.use(
      http.get('http://localhost/api/v1/condominios/cond-1/estrutura', () =>
        HttpResponse.json({
          condominioId: 'cond-1',
          nomeFantasia: 'Residencial Sol',
          geradoEm: '2026-04-20T10:00:00Z',
          blocos: [
            {
              id: 'bloco-1',
              nome: 'Bloco A',
              ativo: true,
              andares: [{ andar: 1, unidades: [{ id: 'un-1', numero: '101', ativo: false }] }],
            },
          ],
        }),
      ),
      http.post('http://localhost/api/v1/condominios/cond-1/blocos/bloco-1/unidades/un-1:reativar', () =>
        HttpResponse.json(
          {
            type: 'about:blank',
            title: 'Conflict',
            status: 409,
            detail: 'Ja existe outra unidade ativa com a mesma tripla.',
          },
          { status: 409, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    )

    const user = userEvent.setup()
    renderWithQuery(
      <Routes>
        <Route path="/estrutura" element={<EstruturaPage />} />
      </Routes>,
    )

    await user.click(await screen.findByLabelText('Mostrar inativos'))
    await user.click(await screen.findByRole('treeitem', { name: /Bloco A/i }))
    await user.click(await screen.findByRole('treeitem', { name: /Andar 1/i }))
    await screen.findByRole('treeitem', { name: /Unidade 101/i })
    await user.click(screen.getByRole('button', { name: 'Ações da unidade 101' }))
    await user.click(screen.getByRole('menuitem', { name: 'Reativar' }))
    await user.click(screen.getByRole('button', { name: 'Reativar unidade' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Ja existe outra unidade ativa com a mesma tripla')
    expect(screen.getByRole('treeitem', { name: /Unidade 101/i })).toBeInTheDocument()
    expect(screen.getByTestId('tree-node-state-icon-unidade-un-1')).toBeInTheDocument()
  })
})
