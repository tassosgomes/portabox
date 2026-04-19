import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Input } from './Input'

describe('Input', () => {
  it('renders without label', () => {
    render(<Input placeholder="Digite aqui" />)
    expect(screen.getByPlaceholderText('Digite aqui')).toBeInTheDocument()
  })

  it('renders with label', () => {
    render(<Input label="Nome completo" />)
    expect(screen.getByLabelText('Nome completo')).toBeInTheDocument()
  })

  it('shows hint text when provided', () => {
    render(<Input label="E-mail" hint="Formato: nome@exemplo.com" />)
    expect(screen.getByText('Formato: nome@exemplo.com')).toBeInTheDocument()
  })

  it('shows error message when error prop is set', () => {
    render(<Input label="E-mail" error="E-mail inválido" />)
    expect(screen.getByText('E-mail inválido')).toBeInTheDocument()
  })

  it('sets aria-invalid true when error is present', () => {
    render(<Input label="PIN" error="PIN não confere" />)
    expect(screen.getByLabelText('PIN')).toHaveAttribute('aria-invalid', 'true')
  })

  it('sets aria-invalid false when no error', () => {
    render(<Input label="Morador" />)
    expect(screen.getByLabelText('Morador')).toHaveAttribute('aria-invalid', 'false')
  })

  it('applies error CSS class when error is present', () => {
    render(<Input label="Campo" error="Obrigatório" />)
    expect(screen.getByLabelText('Campo').className).toMatch(/inputError/)
  })

  it('error message has role=alert', () => {
    render(<Input error="Campo obrigatório" />)
    expect(screen.getByRole('alert')).toHaveTextContent('Campo obrigatório')
  })

  it('hides hint when error is shown', () => {
    render(<Input label="Campo" hint="Dica aqui" error="Erro aqui" />)
    expect(screen.queryByText('Dica aqui')).not.toBeInTheDocument()
    expect(screen.getByText('Erro aqui')).toBeInTheDocument()
  })

  it('forwards ref', () => {
    const ref = { current: null as HTMLInputElement | null }
    render(<Input ref={ref} />)
    expect(ref.current).toBeInstanceOf(HTMLInputElement)
  })
})
