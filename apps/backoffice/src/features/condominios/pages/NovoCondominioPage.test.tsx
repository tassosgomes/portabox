import { describe, it, expect, beforeAll, afterAll, afterEach } from 'vitest'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { http, HttpResponse, delay } from 'msw'
import { setupServer } from 'msw/node'
import { AuthProvider } from '@/features/auth/AuthContext'
import { RequireOperator } from '@/shared/auth/RequireOperator'
import { AppLayout } from '@/shared/layouts/AppLayout'
import { NovoCondominioPage } from './NovoCondominioPage'

const BASE = 'http://localhost/api'

const VALID_CNPJ = '11.222.333/0001-81'
const VALID_CPF = '529.982.247-25'

const loggedInUser = {
  userId: 'op-1',
  email: 'op@example.com',
  roles: ['Operator'],
  tenantId: null,
}

function DetailsPageStub() {
  const location = useLocation()
  const msg = (location.state as { successMessage?: string } | null)?.successMessage
  return (
    <div>
      <div data-testid="details-page">Detalhes do condomínio</div>
      {msg && <div role="status">{msg}</div>}
    </div>
  )
}

function renderWizard(initialPath = '/condominios/novo') {
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
            <Route path="condominios" element={<h2>Condomínios</h2>} />
            <Route path="condominios/novo" element={<NovoCondominioPage />} />
            <Route path="condominios/:id" element={<DetailsPageStub />} />
          </Route>
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  )
}

const server = setupServer(
  http.get(`${BASE}/v1/auth/me`, () => HttpResponse.json(loggedInUser)),
)

beforeAll(() => server.listen({ onUnhandledRequest: 'warn' }))
afterAll(() => server.close())
afterEach(() => server.resetHandlers())

async function waitForWizard() {
  await waitFor(() => {
    expect(screen.getByText('Novo condomínio')).toBeInTheDocument()
  })
}

async function fillStep1(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText(/nome fantasia/i), 'Residencial Parque')
  await user.type(screen.getByLabelText(/cnpj/i), VALID_CNPJ)
  await user.click(screen.getByRole('button', { name: 'Avançar' }))
}

async function fillStep2(user: ReturnType<typeof userEvent.setup>) {
  await waitFor(() => expect(screen.getByLabelText(/data da assembleia/i)).toBeInTheDocument())
  // use fireEvent for date input since userEvent.type doesn't work well with type="date"
  const { fireEvent } = await import('@testing-library/react')
  fireEvent.change(screen.getByLabelText(/data da assembleia/i), { target: { value: '2024-01-10' } })
  await user.type(screen.getByLabelText(/descrição do quórum/i), '2/3 dos condôminos presentes')
  await user.type(screen.getByLabelText(/nome do signatário/i), 'João Silva')
  await user.type(screen.getByLabelText(/cpf do signatário/i), VALID_CPF)
  fireEvent.change(screen.getByLabelText(/data do termo/i), { target: { value: '2024-01-10' } })
  await user.click(screen.getByRole('button', { name: 'Avançar' }))
}

async function fillStep3(user: ReturnType<typeof userEvent.setup>) {
  await waitFor(() => expect(screen.getByLabelText(/nome completo/i)).toBeInTheDocument())
  await user.type(screen.getByLabelText(/nome completo/i), 'Maria Sindico')
  await user.type(screen.getByLabelText(/e-mail/i), 'sindico@example.com')
  await user.type(screen.getByLabelText(/celular/i), '+5511999999999')
  await user.click(screen.getByRole('button', { name: 'Avançar' }))
}

describe('NovoCondominioPage — renderização inicial', () => {
  it('mostra etapa 1 ativa com StepIndicator', async () => {
    renderWizard()
    await waitForWizard()

    expect(screen.getByText('Dados do condomínio')).toBeInTheDocument()
    expect(screen.getByText('Consentimento LGPD')).toBeInTheDocument()
    expect(screen.getByText('Síndico responsável')).toBeInTheDocument()
    expect(screen.getByLabelText(/nome fantasia/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/cnpj/i)).toBeInTheDocument()
  })
})

describe('NovoCondominioPage — validações de etapa 1', () => {
  it('impede avançar e mostra "CNPJ inválido" quando CNPJ não está preenchido', async () => {
    const user = userEvent.setup()
    renderWizard()
    await waitForWizard()

    await user.type(screen.getByLabelText(/nome fantasia/i), 'Residencial Teste')
    // leave CNPJ empty
    await user.click(screen.getByRole('button', { name: 'Avançar' }))

    expect(screen.getByText('CNPJ inválido')).toBeInTheDocument()
    expect(screen.getByLabelText(/nome fantasia/i)).toBeInTheDocument() // still on step 1
  })

  it('impede avançar quando CNPJ tem dígito verificador inválido', async () => {
    const user = userEvent.setup()
    renderWizard()
    await waitForWizard()

    await user.type(screen.getByLabelText(/nome fantasia/i), 'Residencial Teste')
    await user.type(screen.getByLabelText(/cnpj/i), '11.222.333/0001-00')
    await user.click(screen.getByRole('button', { name: 'Avançar' }))

    expect(screen.getByText('CNPJ inválido')).toBeInTheDocument()
  })

  it('avança para etapa 2 quando CNPJ válido', async () => {
    const user = userEvent.setup()
    renderWizard()
    await waitForWizard()

    await fillStep1(user)

    await waitFor(() => {
      expect(screen.getByLabelText(/data da assembleia/i)).toBeInTheDocument()
    })
  })

  it('preserva dados ao voltar para etapa 1 a partir da etapa 2', async () => {
    const user = userEvent.setup()
    renderWizard()
    await waitForWizard()

    await fillStep1(user)
    await waitFor(() => expect(screen.getByRole('button', { name: 'Voltar' })).toBeInTheDocument())
    await user.click(screen.getByRole('button', { name: 'Voltar' }))

    await waitFor(() => {
      expect(screen.getByLabelText(/nome fantasia/i)).toHaveValue('Residencial Parque')
    })
  })
})

describe('NovoCondominioPage — validações de etapa 2', () => {
  it('impede avançar quando CPF do signatário é inválido', async () => {
    const user = userEvent.setup()
    renderWizard()
    await waitForWizard()

    await fillStep1(user)
    await waitFor(() => expect(screen.getByLabelText(/data da assembleia/i)).toBeInTheDocument())

    const { fireEvent } = await import('@testing-library/react')
    fireEvent.change(screen.getByLabelText(/data da assembleia/i), { target: { value: '2024-01-10' } })
    await user.type(screen.getByLabelText(/descrição do quórum/i), 'Quórum válido')
    await user.type(screen.getByLabelText(/nome do signatário/i), 'João Silva')
    await user.type(screen.getByLabelText(/cpf do signatário/i), '111.111.111-11') // invalid
    fireEvent.change(screen.getByLabelText(/data do termo/i), { target: { value: '2024-01-10' } })
    await user.click(screen.getByRole('button', { name: 'Avançar' }))

    expect(screen.getByText('CPF inválido')).toBeInTheDocument()
  })
})

describe('NovoCondominioPage — validações de etapa 3', () => {
  it('impede avançar quando celular não está em formato E.164', async () => {
    const user = userEvent.setup()
    renderWizard()
    await waitForWizard()

    await fillStep1(user)
    await fillStep2(user)
    await waitFor(() => expect(screen.getByLabelText(/nome completo/i)).toBeInTheDocument())

    await user.type(screen.getByLabelText(/nome completo/i), 'Maria Sindico')
    await user.type(screen.getByLabelText(/e-mail/i), 'sindico@example.com')
    await user.type(screen.getByLabelText(/celular/i), '11999999999') // missing + prefix
    await user.click(screen.getByRole('button', { name: 'Avançar' }))

    expect(screen.getByText(/formato E\.164/i)).toBeInTheDocument()
  })
})

describe('NovoCondominioPage — tela de revisão', () => {
  it('mostra todos os dados consolidados', async () => {
    const user = userEvent.setup()
    renderWizard()
    await waitForWizard()

    await fillStep1(user)
    await fillStep2(user)
    await fillStep3(user)

    await waitFor(() => {
      expect(screen.getByText('Residencial Parque')).toBeInTheDocument()
    })

    expect(screen.getByText('11.222.333/0001-81')).toBeInTheDocument()
    expect(screen.getByText('Maria Sindico')).toBeInTheDocument()
    expect(screen.getByText('sindico@example.com')).toBeInTheDocument()
    expect(screen.getByText('+5511999999999')).toBeInTheDocument()
    expect(screen.getByText('João Silva')).toBeInTheDocument()
    expect(screen.getByText('529.982.247-25')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Criar condomínio' })).toBeInTheDocument()
  })
})

describe('NovoCondominioPage — submit com sucesso', () => {
  it('redireciona para /condominios/{id} e exibe mensagem de sucesso via location.state', async () => {
    server.use(
      http.post(`${BASE}/v1/admin/condominios`, () =>
        HttpResponse.json({ id: 'condo-abc' }, { status: 201 }),
      ),
    )

    const user = userEvent.setup()
    renderWizard()
    await waitForWizard()

    await fillStep1(user)
    await fillStep2(user)
    await fillStep3(user)

    await waitFor(() => expect(screen.getByRole('button', { name: 'Criar condomínio' })).toBeInTheDocument())
    await user.click(screen.getByRole('button', { name: 'Criar condomínio' }))

    await waitFor(() => {
      expect(screen.getByTestId('details-page')).toBeInTheDocument()
    })

    expect(screen.getByRole('status')).toHaveTextContent(
      /condomínio criado em estado pré-ativo/i,
    )
  })

  it('desabilita o botão primário durante o submit', async () => {
    server.use(
      http.post(`${BASE}/v1/admin/condominios`, async () => {
        await delay(200)
        return HttpResponse.json({ id: 'condo-xyz' }, { status: 201 })
      }),
    )

    const user = userEvent.setup()
    renderWizard()
    await waitForWizard()

    await fillStep1(user)
    await fillStep2(user)
    await fillStep3(user)

    await waitFor(() => expect(screen.getByRole('button', { name: 'Criar condomínio' })).toBeInTheDocument())
    await user.click(screen.getByRole('button', { name: 'Criar condomínio' }))

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /criar condomínio/i })).toBeDisabled()
    })
  })
})

describe('NovoCondominioPage — erros de submit', () => {
  it('exibe mensagem de CNPJ duplicado em 409 com nome e data do tenant existente', async () => {
    server.use(
      http.post(`${BASE}/v1/admin/condominios`, () =>
        HttpResponse.json(
          {
            status: 409,
            title: 'Conflict',
            extensions: {
              nomeExistente: 'Condomínio Existente Ltda',
              criadoEm: '2024-03-15',
            },
          },
          { status: 409 },
        ),
      ),
    )

    const user = userEvent.setup()
    renderWizard()
    await waitForWizard()

    await fillStep1(user)
    await fillStep2(user)
    await fillStep3(user)

    await waitFor(() => expect(screen.getByRole('button', { name: 'Criar condomínio' })).toBeInTheDocument())
    await user.click(screen.getByRole('button', { name: 'Criar condomínio' }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(
        /Este CNPJ já está cadastrado como "Condomínio Existente Ltda"/,
      )
    })
  })

  it('exibe toast de erro genérico em 500', async () => {
    server.use(
      http.post(`${BASE}/v1/admin/condominios`, () =>
        HttpResponse.json({ status: 500, title: 'Internal Server Error' }, { status: 500 }),
      ),
    )

    const user = userEvent.setup()
    renderWizard()
    await waitForWizard()

    await fillStep1(user)
    await fillStep2(user)
    await fillStep3(user)

    await waitFor(() => expect(screen.getByRole('button', { name: 'Criar condomínio' })).toBeInTheDocument())
    await user.click(screen.getByRole('button', { name: 'Criar condomínio' }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/erro ao criar condomínio/i)
    })
  })
})

describe('NovoCondominioPage — navegação entre etapas', () => {
  it('permite navegar de volta preservando dados de todas as etapas', async () => {
    const user = userEvent.setup()
    renderWizard()
    await waitForWizard()

    await fillStep1(user)
    await fillStep2(user)
    await fillStep3(user)

    // on review, go back to step 3
    await waitFor(() => expect(screen.getByRole('button', { name: 'Criar condomínio' })).toBeInTheDocument())

    const buttons = screen.getAllByRole('button', { name: 'Voltar' })
    await user.click(buttons[0])

    await waitFor(() => {
      expect(screen.getByLabelText(/nome completo/i)).toHaveValue('Maria Sindico')
    })
    expect(screen.getByLabelText(/e-mail/i)).toHaveValue('sindico@example.com')
  })

  it('step indicator mostra todas etapas concluídas na tela de revisão', async () => {
    const user = userEvent.setup()
    renderWizard()
    await waitForWizard()

    await fillStep1(user)
    await fillStep2(user)
    await fillStep3(user)

    await waitFor(() => {
      const nav = screen.getByRole('navigation', { name: /progresso/i })
      const items = within(nav).getAllByRole('listitem')
      expect(items).toHaveLength(3)
    })
  })
})
