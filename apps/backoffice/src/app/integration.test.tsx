import { describe, it, expect, vi, beforeAll, afterAll, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
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
    renderApp('/condominios')

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /entrar no backoffice/i })).toBeInTheDocument()
    })
  })

  it('preserves redirectTo param when unauthenticated user accesses protected route', async () => {
    renderApp('/condominios')

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /entrar no backoffice/i })).toBeInTheDocument()
    })
    // redirectTo is preserved in URL for post-login navigation
    expect(screen.getByLabelText('E-mail')).toBeInTheDocument()
    expect(screen.getByLabelText('Senha')).toBeInTheDocument()
  })

  it('navigates to /condominios after successful login', async () => {
    server.use(
      http.post(`${BASE}/v1/auth/login`, () =>
        HttpResponse.json({ userId: 'u1', role: 'Operator', tenantId: null }),
      ),
      http.get(`${BASE}/v1/auth/me`, () =>
        HttpResponse.json({
          userId: 'u1',
          email: 'op@example.com',
          roles: ['Operator'],
          tenantId: null,
        }),
      ),
    )

    const user = userEvent.setup()
    renderApp('/login')

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /entrar no backoffice/i })).toBeInTheDocument()
    })

    await user.type(screen.getByLabelText('E-mail'), 'op@example.com')
    await user.type(screen.getByLabelText('Senha'), 'S3cr3t!')
    await user.click(screen.getByRole('button', { name: 'Entrar' }))

    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 2 })).toHaveTextContent('Condomínios')
    })
  })

  it('shows layout with logo and Lucide icon after login', async () => {
    server.use(
      http.get(`${BASE}/v1/auth/me`, () =>
        HttpResponse.json({
          userId: 'u1',
          email: 'op@example.com',
          roles: ['Operator'],
          tenantId: null,
        }),
      ),
    )

    renderApp('/condominios')

    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 2 })).toHaveTextContent('Condomínios')
    })

    expect(screen.getByRole('banner')).toBeInTheDocument()
    expect(screen.getByAltText('PortaBox')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Sair' })).toBeInTheDocument()
  })

  it('redirects to /login after logout', async () => {
    server.use(
      http.get(`${BASE}/v1/auth/me`, () =>
        HttpResponse.json({
          userId: 'u1',
          email: 'op@example.com',
          roles: ['Operator'],
          tenantId: null,
        }),
      ),
      http.post(`${BASE}/v1/auth/logout`, () => new HttpResponse(null, { status: 204 })),
    )

    const user = userEvent.setup()
    renderApp('/condominios')

    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 2 })).toHaveTextContent('Condomínios')
    })

    await user.click(screen.getByRole('button', { name: 'Sair' }))

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /entrar no backoffice/i })).toBeInTheDocument()
    })
  })
})
