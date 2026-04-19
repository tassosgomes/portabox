import { describe, it, expect, vi, beforeAll, afterAll, afterEach } from 'vitest'
import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { AuthProvider } from '@/features/auth/AuthContext'
import { AppRoutes } from './routes'

const BASE = 'http://localhost/api'

const server = setupServer(
  http.get(`${BASE}/v1/auth/me`, () => {
    return HttpResponse.json(null, { status: 401 })
  }),
)

beforeAll(() => server.listen({ onUnhandledRequest: 'warn' }))
afterAll(() => server.close())
afterEach(() => {
  server.resetHandlers()
  vi.clearAllMocks()
  vi.useRealTimers()
})

function renderApp(initialPath = '/') {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </MemoryRouter>,
  )
}

describe('Auth integration', () => {
  it('redirects unauthenticated user to /login when accessing protected route', async () => {
    renderApp('/')

    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Bem-vindo ao PortaBox')
    })
  })

  it('shows login form with email and password fields on /login', async () => {
    renderApp('/login')

    await waitFor(() => {
      expect(screen.getByLabelText('E-mail')).toBeInTheDocument()
      expect(screen.getByLabelText('Senha')).toBeInTheDocument()
    })
  })

  describe('Setup password flow', () => {
    it('happy path: submit valid password → success message → redirect to /login after 2s', async () => {
      server.use(
        http.post(`${BASE}/v1/auth/password-setup`, () => new HttpResponse(null, { status: 204 })),
      )

      renderApp('/setup-password?token=valid-token-xyz')

      await screen.findByLabelText('Nova senha')

      fireEvent.change(screen.getByLabelText('Nova senha'), { target: { value: 'Senha12345!' } })
      fireEvent.change(screen.getByLabelText('Confirmar senha'), { target: { value: 'Senha12345!' } })
      fireEvent.submit(screen.getByRole('button', { name: 'Definir senha' }).closest('form')!)

      await screen.findByRole('status')
      expect(screen.getByRole('status')).toHaveTextContent('Senha definida com sucesso')

      await waitFor(() => {
        expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Bem-vindo ao PortaBox')
      }, { timeout: 3000 })
    })

    it('invalid token: shows generic message + "Voltar para login" button', async () => {
      server.use(
        http.post(`${BASE}/v1/auth/password-setup`, () =>
          HttpResponse.json({ title: 'Bad Request' }, { status: 400 }),
        ),
      )

      const user = userEvent.setup()
      renderApp('/setup-password?token=bad-token')

      await waitFor(() => {
        expect(screen.getByLabelText('Nova senha')).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText('Nova senha'), 'Senha12345!')
      await user.type(screen.getByLabelText('Confirmar senha'), 'Senha12345!')
      await user.click(screen.getByRole('button', { name: 'Definir senha' }))

      await waitFor(() => {
        expect(screen.getByRole('alert')).toHaveTextContent(
          'Link inválido ou expirado. Entre em contato com a equipe do condomínio para receber um novo link.',
        )
      })

      expect(screen.getByRole('link', { name: 'Voltar para login' })).toBeInTheDocument()
    })
  })

  describe('Login flow', () => {
    it('happy path: login with credentials → reaches / and shows user name', async () => {
      server.use(
        http.post(`${BASE}/v1/auth/login`, () =>
          HttpResponse.json({
            id: 'u1',
            email: 'sindico@example.com',
            name: 'Maria Costa',
            role: 'Sindico',
          }),
        ),
        http.get(`${BASE}/v1/auth/me`, () =>
          HttpResponse.json({
            id: 'u1',
            email: 'sindico@example.com',
            name: 'Maria Costa',
            role: 'Sindico',
          }),
        ),
      )

      const user = userEvent.setup()
      renderApp('/login')

      await waitFor(() => {
        expect(screen.getByLabelText('E-mail')).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText('E-mail'), 'sindico@example.com')
      await user.type(screen.getByLabelText('Senha'), 'Senha12345!')
      await user.click(screen.getByRole('button', { name: 'Entrar' }))

      await waitFor(() => {
        expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
          'Bem-vindo, Maria Costa',
        )
      })
    })

    it('shows error on invalid credentials', async () => {
      server.use(
        http.post(`${BASE}/v1/auth/login`, () =>
          HttpResponse.json({ title: 'Unauthorized' }, { status: 401 }),
        ),
      )

      const user = userEvent.setup()
      renderApp('/login')

      await waitFor(() => {
        expect(screen.getByLabelText('E-mail')).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText('E-mail'), 'bad@example.com')
      await user.type(screen.getByLabelText('Senha'), 'wrongpass')
      await user.click(screen.getByRole('button', { name: 'Entrar' }))

      await waitFor(() => {
        expect(screen.getByRole('alert')).toHaveTextContent('E-mail ou senha inválidos')
      })
    })

    it('shows layout with logo and name + logout after login', async () => {
      server.use(
        http.get(`${BASE}/v1/auth/me`, () =>
          HttpResponse.json({
            id: 'u1',
            email: 'sindico@example.com',
            name: 'Maria Costa',
            role: 'Sindico',
          }),
        ),
      )

      renderApp('/')

      await waitFor(() => {
        expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
          'Bem-vindo, Maria Costa',
        )
      })

      expect(screen.getByRole('banner')).toBeInTheDocument()
      expect(screen.getByAltText('PortaBox')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Sair' })).toBeInTheDocument()
    })
  })
})
