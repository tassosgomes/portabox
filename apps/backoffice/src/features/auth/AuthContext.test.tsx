import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, act, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { AuthProvider, useAuthContext } from './AuthContext'

const mockGet = vi.fn()
const mockPost = vi.fn()

vi.mock('@/shared/api/client', () => ({
  apiClient: {
    get: (...args: unknown[]) => mockGet(...args),
    post: (...args: unknown[]) => mockPost(...args),
    put: vi.fn(),
    delete: vi.fn(),
  },
  ApiHttpError: class ApiHttpError extends Error {
    constructor(
      public status: number,
      public body: unknown,
    ) {
      super(`HTTP ${status}`)
    }
  },
}))

function TestConsumer() {
  const { user, isAuthenticated, isLoading } = useAuthContext()
  return (
    <div>
      <span data-testid="authenticated">{String(isAuthenticated)}</span>
      <span data-testid="loading">{String(isLoading)}</span>
      <span data-testid="user-name">{user?.name ?? 'none'}</span>
    </div>
  )
}

function LoginConsumer() {
  const { login, isAuthenticated } = useAuthContext()
  return (
    <div>
      <span data-testid="authenticated">{String(isAuthenticated)}</span>
      <button
        type="button"
        onClick={() => void login({ email: 'op@example.com', password: 'S3cr3t!' })}
      >
        Login
      </button>
    </div>
  )
}

function LogoutConsumer() {
  const { logout, isAuthenticated } = useAuthContext()
  return (
    <div>
      <span data-testid="authenticated">{String(isAuthenticated)}</span>
      <button type="button" onClick={() => void logout()}>
        Logout
      </button>
    </div>
  )
}

describe('AuthContext', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('starts loading and resolves to unauthenticated when /me fails', async () => {
    mockGet.mockRejectedValue(new Error('401'))

    render(
      <MemoryRouter>
        <AuthProvider>
          <TestConsumer />
        </AuthProvider>
      </MemoryRouter>,
    )

    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('false')
    })
    expect(screen.getByTestId('authenticated')).toHaveTextContent('false')
  })

  it('returns isAuthenticated=true after login with status 200', async () => {
    mockGet.mockRejectedValueOnce(new Error('401')).mockResolvedValueOnce({
      userId: 'u1',
      email: 'op@example.com',
      roles: ['Operator'],
      tenantId: null,
    })
    mockPost.mockResolvedValue({ userId: 'u1', role: 'Operator', tenantId: null })

    render(
      <MemoryRouter>
        <AuthProvider>
          <LoginConsumer />
        </AuthProvider>
      </MemoryRouter>,
    )

    await waitFor(() => {
      expect(screen.getByTestId('authenticated')).toHaveTextContent('false')
    })

    await act(async () => {
      screen.getByRole('button', { name: 'Login' }).click()
    })

    await waitFor(() => {
      expect(screen.getByTestId('authenticated')).toHaveTextContent('true')
    })
  })

  it('returns isAuthenticated=false after logout with status 204', async () => {
    mockGet.mockResolvedValue({
      userId: 'u1',
      email: 'op@example.com',
      roles: ['Operator'],
      tenantId: null,
    })
    mockPost.mockResolvedValue(undefined)

    render(
      <MemoryRouter>
        <AuthProvider>
          <LogoutConsumer />
        </AuthProvider>
      </MemoryRouter>,
    )

    await waitFor(() => {
      expect(screen.getByTestId('authenticated')).toHaveTextContent('true')
    })

    await act(async () => {
      screen.getByRole('button', { name: 'Logout' }).click()
    })

    await waitFor(() => {
      expect(screen.getByTestId('authenticated')).toHaveTextContent('false')
    })
  })
})
