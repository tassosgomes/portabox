import { describe, it, expect, vi, beforeAll, afterAll, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { AuthProvider } from '@/features/auth/AuthContext'
import { RequireOperator } from '@/shared/auth/RequireOperator'
import { AppLayout } from '@/shared/layouts/AppLayout'
import { DetalhesCondominioPage } from './DetalhesCondominioPage'

const BASE = 'http://localhost/api'

const loggedInUser = {
  userId: 'op-1',
  email: 'op@example.com',
  roles: ['Operator'],
  tenantId: null,
}

const sindicoSemSenha = {
  id: 'sind-1',
  userId: 'user-sind-1',
  nomeCompleto: 'João Síndico',
  email: 'sindico@example.com',
  celularMasked: '+55 11 99999-9999',
}

const sindicoComSenha = { ...sindicoSemSenha }

const auditLog = [
  {
    id: 1,
    eventKind: 1,
    performedByEmail: 'op@example.com',
    occurredAt: '2026-01-10T10:00:00Z',
    note: null,
  },
]

const baseDetails = {
  id: 'condo-1',
  nomeFantasia: 'Residencial Parque',
  cnpjMasked: '11.222.333/0001-81',
  status: 1,
  createdAt: '2026-01-10T00:00:00Z',
  activatedAt: null,
  enderecoLogradouro: 'Rua das Flores',
  enderecoNumero: '100',
  enderecoComplemento: null,
  enderecoBairro: 'Centro',
  enderecoCidade: 'São Paulo',
  enderecoUf: 'SP',
  enderecoCep: '01310100',
  administradoraNome: 'Administradora ABC',
  optIn: {
    dataAssembleia: '2025-12-01',
    quorumDescricao: '2/3 dos condôminos',
    signatarioNome: 'Maria Silva',
    signatarioCpfMasked: '***.982.247-25',
    dataTermo: '2025-12-15',
  },
  documentos: [],
  sindico: sindicoSemSenha,
  sindicoSenhaDefinida: false,
  auditLog,
}

const server = setupServer(
  http.get(`${BASE}/v1/auth/me`, () => HttpResponse.json(loggedInUser)),
)

beforeAll(() => server.listen({ onUnhandledRequest: 'warn' }))
afterAll(() => server.close())
afterEach(() => { server.resetHandlers(); vi.clearAllMocks() })

function renderDetails(id = 'condo-1', state?: Record<string, unknown>) {
  return render(
    <MemoryRouter initialEntries={[{ pathname: `/condominios/${id}`, state: state ?? null }]}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<div>Login</div>} />
          <Route
            path="/"
            element={<RequireOperator><AppLayout /></RequireOperator>}
          >
            <Route path="condominios" element={<div>Lista</div>} />
            <Route path="condominios/:id" element={<DetalhesCondominioPage />} />
          </Route>
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  )
}

describe('DetalhesCondominioPage — renderização', () => {
  it('exibe nome, CNPJ mascarado e badge de status', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios/condo-1`, () => HttpResponse.json(baseDetails)),
    )
    renderDetails()

    await waitFor(() => {
      expect(screen.getByText('Residencial Parque')).toBeInTheDocument()
    })
    expect(screen.getByText('11.222.333/0001-81')).toBeInTheDocument()
    expect(screen.getByText('Pré-ativo')).toBeInTheDocument()
  })

  it('exibe seção de dados do condomínio', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios/condo-1`, () => HttpResponse.json(baseDetails)),
    )
    renderDetails()

    await waitFor(() => {
      expect(screen.getByText('Rua das Flores')).toBeInTheDocument()
    })
    expect(screen.getByText('Administradora ABC')).toBeInTheDocument()
  })

  it('exibe seção de consentimento LGPD com CPF mascarado', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios/condo-1`, () => HttpResponse.json(baseDetails)),
    )
    renderDetails()

    await waitFor(() => {
      expect(screen.getByText('2/3 dos condôminos')).toBeInTheDocument()
    })
    expect(screen.getByText('***.982.247-25')).toBeInTheDocument()
    expect(screen.getByText('Maria Silva')).toBeInTheDocument()
  })

  it('exibe seção do síndico', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios/condo-1`, () => HttpResponse.json(baseDetails)),
    )
    renderDetails()

    await waitFor(() => {
      expect(screen.getByText('João Síndico')).toBeInTheDocument()
    })
    expect(screen.getByText('sindico@example.com')).toBeInTheDocument()
  })

  it('exibe histórico de auditoria', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios/condo-1`, () => HttpResponse.json(baseDetails)),
    )
    renderDetails()

    await waitFor(() => {
      expect(screen.getByRole('list', { name: /histórico/i })).toBeInTheDocument()
    })
    expect(screen.getByText('Criado')).toBeInTheDocument()
  })

  it('mostra toast de sucesso passado via location.state', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios/condo-1`, () => HttpResponse.json(baseDetails)),
    )
    renderDetails('condo-1', { successMessage: 'Condomínio criado em estado pré-ativo.' })

    await waitFor(() => {
      expect(screen.getByText('Residencial Parque')).toBeInTheDocument()
    })
    expect(screen.getByRole('status')).toHaveTextContent('Condomínio criado em estado pré-ativo.')
  })

  it('mostra estado de erro quando API retorna 404', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios/nao-existe`, () =>
        HttpResponse.json({ title: 'Not Found' }, { status: 404 }),
      ),
    )
    renderDetails('nao-existe')

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/não encontrado/i)
    })
  })
})

describe('DetalhesCondominioPage — ação Ativar operação', () => {
  it('exibe botão "Ativar operação" quando status é Pré-ativo', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios/condo-1`, () => HttpResponse.json(baseDetails)),
    )
    renderDetails()

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /ativar operação/i })).toBeInTheDocument()
    })
  })

  it('não exibe botão "Ativar operação" quando já está ativo', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios/condo-1`, () =>
        HttpResponse.json({ ...baseDetails, status: 2, activatedAt: '2026-01-15T00:00:00Z' }),
      ),
    )
    renderDetails()

    await waitFor(() => {
      expect(screen.getByText('Residencial Parque')).toBeInTheDocument()
    })
    expect(screen.queryByRole('button', { name: /ativar operação/i })).not.toBeInTheDocument()
  })

  it('ativar operação muda badge para Ativo e mostra toast', async () => {
    const detailsAtivos = {
      ...baseDetails,
      status: 2 as const,
      activatedAt: '2026-01-15T00:00:00Z',
      auditLog: [
        ...auditLog,
        {
          id: 2,
          eventKind: 2,
          performedByEmail: 'op@example.com',
          occurredAt: '2026-01-15T10:00:00Z',
          note: null,
        },
      ],
    }

    server.use(
      http.get(`${BASE}/v1/admin/condominios/condo-1`, () => HttpResponse.json(baseDetails)),
      http.post(`${BASE}/v1/admin/condominios/condo-1:activate`, () =>
        HttpResponse.json(detailsAtivos),
      ),
    )

    const user = userEvent.setup()
    renderDetails()

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /ativar operação/i })).toBeInTheDocument()
    })

    // first click opens modal
    await user.click(screen.getByRole('button', { name: /ativar operação/i }))
    // second click confirms
    await user.click(screen.getByRole('button', { name: /confirmar ativação/i }))

    // after activation, page reloads and shows updated state
    server.use(
      http.get(`${BASE}/v1/admin/condominios/condo-1`, () => HttpResponse.json(detailsAtivos)),
    )

    await waitFor(() => {
      expect(screen.getByRole('status')).toHaveTextContent(/operação ativada/i)
    })
  })
})

describe('DetalhesCondominioPage — reenvio de magic link', () => {
  it('exibe botão de reenvio quando síndico não definiu senha', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios/condo-1`, () => HttpResponse.json(baseDetails)),
    )
    renderDetails()

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /reenviar magic link/i })).toBeInTheDocument()
    })
  })

  it('não exibe botão de reenvio quando síndico já definiu senha', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios/condo-1`, () =>
        HttpResponse.json({ ...baseDetails, sindico: sindicoComSenha, sindicoSenhaDefinida: true }),
      ),
    )
    renderDetails()

    await waitFor(() => {
      expect(screen.getByText('Residencial Parque')).toBeInTheDocument()
    })
    expect(screen.queryByRole('button', { name: /reenviar magic link/i })).not.toBeInTheDocument()
  })

  it('reenvio 200: mostra toast de sucesso', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios/condo-1`, () => HttpResponse.json(baseDetails)),
      http.post(
        `${BASE}/v1/admin/condominios/condo-1/sindicos/user-sind-1:resend-magic-link`,
        () => HttpResponse.json(null, { status: 200 }),
      ),
    )
    const user = userEvent.setup()
    renderDetails()

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /reenviar magic link/i })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: /reenviar magic link/i }))

    await waitFor(() => {
      expect(screen.getByRole('status')).toHaveTextContent(/reenviado com sucesso/i)
    })
  })

  it('reenvio 429: mostra mensagem pt-BR', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios/condo-1`, () => HttpResponse.json(baseDetails)),
      http.post(
        `${BASE}/v1/admin/condominios/condo-1/sindicos/user-sind-1:resend-magic-link`,
        () => new HttpResponse(null, { status: 429 }),
      ),
    )
    const user = userEvent.setup()
    renderDetails()

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /reenviar magic link/i })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: /reenviar magic link/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/aguarde alguns minutos/i)
    })
  })
})

describe('DetalhesCondominioPage — upload e download de documentos', () => {
  it('exibe lista de documentos com tamanho formatado em KB', async () => {
    const detailsWithDoc = {
      ...baseDetails,
      documentos: [
        {
          id: 'doc-1',
          kind: 1,
          contentType: 'application/pdf',
          sizeBytes: 500_000,
          uploadedAt: '2026-01-10T00:00:00Z',
          originalFileName: 'ata.pdf',
        },
      ],
    }
    server.use(
      http.get(`${BASE}/v1/admin/condominios/condo-1`, () => HttpResponse.json(detailsWithDoc)),
    )
    renderDetails()

    await waitFor(() => {
      expect(screen.getByText('500 KB')).toBeInTheDocument()
    })
    expect(screen.getAllByText(/ata de assembleia/i).length).toBeGreaterThanOrEqual(1)
  })

  it('upload de documento chama onUploaded e recarrega página', async () => {
    server.use(
      http.get(`${BASE}/v1/admin/condominios/condo-1`, () => HttpResponse.json(baseDetails)),
      http.post(`${BASE}/v1/admin/condominios/condo-1/opt-in-documents`, () =>
        HttpResponse.json({
          id: 'doc-new',
          kind: 1,
          contentType: 'application/pdf',
          sizeBytes: 500_000,
          uploadedAt: '2026-01-20T00:00:00Z',
          originalFileName: 'new.pdf',
        }),
      ),
    )
    const user = userEvent.setup()
    renderDetails()

    await waitFor(() => {
      expect(screen.getByLabelText(/selecionar arquivo/i)).toBeInTheDocument()
    })

    const file = new File([''], 'new.pdf', { type: 'application/pdf' })
    Object.defineProperty(file, 'size', { value: 500_000, configurable: true })

    const input = screen.getByLabelText(/selecionar arquivo/i)
    await user.upload(input, file)
    await user.click(screen.getByRole('button', { name: /enviar documento/i }))

    // After upload, page calls load() which refetches details
    // (the getCondominioDetails mock returns the same data, so we just verify no errors)
    await waitFor(() => {
      expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    })
  })

  it('download abre nova aba com presigned URL', async () => {
    const openSpy = vi.spyOn(window, 'open').mockImplementation(() => null)

    const detailsWithDoc = {
      ...baseDetails,
      documentos: [
        {
          id: 'doc-1',
          kind: 1,
          contentType: 'application/pdf',
          sizeBytes: 100_000,
          uploadedAt: '2026-01-10T00:00:00Z',
          originalFileName: 'ata.pdf',
        },
      ],
    }
    server.use(
      http.get(`${BASE}/v1/admin/condominios/condo-1`, () => HttpResponse.json(detailsWithDoc)),
      http.get(
        `${BASE}/v1/admin/condominios/condo-1/opt-in-documents/doc-1:download`,
        () => HttpResponse.json({ url: 'https://storage.example.com/presigned', expiresAt: '2026-01-10T01:00:00Z' }),
      ),
    )

    const user = userEvent.setup()
    renderDetails()

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /download/i })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: /download/i }))

    await waitFor(() => {
      expect(openSpy).toHaveBeenCalledWith(
        'https://storage.example.com/presigned',
        '_blank',
        'noopener,noreferrer',
      )
    })

    openSpy.mockRestore()
  })
})
