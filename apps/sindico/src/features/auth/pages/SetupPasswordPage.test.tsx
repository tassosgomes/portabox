import { describe, it, expect, vi, afterEach } from 'vitest'
import { render, screen, fireEvent, act } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { SetupPasswordPage } from './SetupPasswordPage'
import { ApiHttpError } from '@/shared/api/client'

const mockPost = vi.fn()
const mockNavigate = vi.fn()

vi.mock('@/shared/api/client', () => ({
  apiClient: {
    post: (...args: unknown[]) => mockPost(...args),
  },
  ApiHttpError: class ApiHttpError extends Error {
    constructor(
      public status: number,
      public body: unknown,
    ) {
      super(`HTTP ${status}`)
      this.name = 'ApiHttpError'
    }
  },
}))

vi.mock('react-router-dom', async (importOriginal) => {
  const mod = await importOriginal<typeof import('react-router-dom')>()
  return {
    ...mod,
    useNavigate: () => mockNavigate,
    useSearchParams: () => [new URLSearchParams('token=abc123'), vi.fn()],
  }
})

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/setup-password?token=abc123']}>
      <SetupPasswordPage />
    </MemoryRouter>,
  )
}

afterEach(() => {
  vi.clearAllMocks()
  vi.useRealTimers()
})

describe('SetupPasswordPage', () => {
  it('renders password and confirm fields', () => {
    renderPage()
    expect(screen.getByLabelText('Nova senha')).toBeInTheDocument()
    expect(screen.getByLabelText('Confirmar senha')).toBeInTheDocument()
  })

  it('disables submit when password is too short', () => {
    renderPage()
    fireEvent.change(screen.getByLabelText('Nova senha'), { target: { value: 'short' } })
    fireEvent.change(screen.getByLabelText('Confirmar senha'), { target: { value: 'short' } })
    expect(screen.getByRole('button', { name: 'Definir senha' })).toBeDisabled()
  })

  it('disables submit when password has no digit', () => {
    renderPage()
    fireEvent.change(screen.getByLabelText('Nova senha'), { target: { value: 'SenhaLongaSemNumero' } })
    fireEvent.change(screen.getByLabelText('Confirmar senha'), { target: { value: 'SenhaLongaSemNumero' } })
    expect(screen.getByRole('button', { name: 'Definir senha' })).toBeDisabled()
  })

  it('disables submit when password has no letter', () => {
    renderPage()
    fireEvent.change(screen.getByLabelText('Nova senha'), { target: { value: '1234567890' } })
    fireEvent.change(screen.getByLabelText('Confirmar senha'), { target: { value: '1234567890' } })
    expect(screen.getByRole('button', { name: 'Definir senha' })).toBeDisabled()
  })

  it('disables submit when passwords do not match', () => {
    renderPage()
    fireEvent.change(screen.getByLabelText('Nova senha'), { target: { value: 'Senha12345!' } })
    fireEvent.change(screen.getByLabelText('Confirmar senha'), { target: { value: 'Senha99999!' } })
    expect(screen.getByRole('button', { name: 'Definir senha' })).toBeDisabled()
  })

  it('enables submit when password meets policy and passwords match', () => {
    renderPage()
    fireEvent.change(screen.getByLabelText('Nova senha'), { target: { value: 'Senha12345!' } })
    fireEvent.change(screen.getByLabelText('Confirmar senha'), { target: { value: 'Senha12345!' } })
    expect(screen.getByRole('button', { name: 'Definir senha' })).not.toBeDisabled()
  })

  it('shows generic message on 400 without revealing details', async () => {
    mockPost.mockRejectedValueOnce(new ApiHttpError(400, null))
    renderPage()

    await act(async () => {
      fireEvent.change(screen.getByLabelText('Nova senha'), { target: { value: 'Senha12345!' } })
      fireEvent.change(screen.getByLabelText('Confirmar senha'), { target: { value: 'Senha12345!' } })
      fireEvent.submit(screen.getByRole('button', { name: 'Definir senha' }).closest('form')!)
    })

    expect(screen.getByRole('alert')).toHaveTextContent(
      'Link inválido ou expirado. Entre em contato com a equipe do condomínio para receber um novo link.',
    )
    expect(screen.getByRole('alert')).not.toHaveTextContent('consumido')
    expect(screen.getByRole('alert')).not.toHaveTextContent('usado')
    expect(screen.getByRole('alert')).not.toHaveTextContent('inválido por')
  })

  it('shows "Voltar para login" link on 400 error', async () => {
    mockPost.mockRejectedValueOnce(new ApiHttpError(400, null))
    renderPage()

    await act(async () => {
      fireEvent.change(screen.getByLabelText('Nova senha'), { target: { value: 'Senha12345!' } })
      fireEvent.change(screen.getByLabelText('Confirmar senha'), { target: { value: 'Senha12345!' } })
      fireEvent.submit(screen.getByRole('button', { name: 'Definir senha' }).closest('form')!)
    })

    expect(screen.getByRole('link', { name: 'Voltar para login' })).toBeInTheDocument()
  })

  it('shows success message and redirects to /login after 2s on 200', async () => {
    vi.useFakeTimers()
    mockPost.mockResolvedValueOnce(undefined)
    renderPage()

    await act(async () => {
      fireEvent.change(screen.getByLabelText('Nova senha'), { target: { value: 'Senha12345!' } })
      fireEvent.change(screen.getByLabelText('Confirmar senha'), { target: { value: 'Senha12345!' } })
      fireEvent.submit(screen.getByRole('button', { name: 'Definir senha' }).closest('form')!)
    })

    expect(screen.getByRole('status')).toHaveTextContent('Senha definida com sucesso')

    act(() => {
      vi.advanceTimersByTime(2000)
    })

    expect(mockNavigate).toHaveBeenCalledWith('/login', { replace: true })
  })
})
