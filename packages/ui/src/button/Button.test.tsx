import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Button } from './Button'

describe('Button', () => {
  it('renders children', () => {
    render(<Button>Criar condomínio</Button>)
    expect(screen.getByRole('button', { name: 'Criar condomínio' })).toBeInTheDocument()
  })

  it('has primary variant class by default', () => {
    render(<Button>Primário</Button>)
    const btn = screen.getByRole('button')
    expect(btn.className).toMatch(/primary/)
  })

  it('applies secondary variant class', () => {
    render(<Button variant="secondary">Secundário</Button>)
    expect(screen.getByRole('button').className).toMatch(/secondary/)
  })

  it('applies danger variant class', () => {
    render(<Button variant="danger">Excluir</Button>)
    expect(screen.getByRole('button').className).toMatch(/danger/)
  })

  it('applies ghost variant class', () => {
    render(<Button variant="ghost">Cancelar</Button>)
    expect(screen.getByRole('button').className).toMatch(/ghost/)
  })

  it('is disabled when disabled prop is true', () => {
    render(<Button disabled>Desabilitado</Button>)
    expect(screen.getByRole('button')).toBeDisabled()
  })

  it('shows aria-disabled when loading', () => {
    render(<Button loading>Carregando</Button>)
    const btn = screen.getByRole('button')
    expect(btn).toHaveAttribute('aria-disabled', 'true')
    expect(btn).toBeDisabled()
  })

  it('renders loading spinner when loading is true', () => {
    render(<Button loading>Salvando</Button>)
    expect(document.querySelector('[aria-hidden="true"]')).toBeInTheDocument()
  })

  it('calls onClick when clicked', async () => {
    const handler = vi.fn()
    render(<Button onClick={handler}>Clique</Button>)
    await userEvent.click(screen.getByRole('button'))
    expect(handler).toHaveBeenCalledOnce()
  })

  it('does not call onClick when disabled', async () => {
    const handler = vi.fn()
    render(
      <Button disabled onClick={handler}>
        Desabilitado
      </Button>,
    )
    await userEvent.click(screen.getByRole('button'))
    expect(handler).not.toHaveBeenCalled()
  })

  it('applies sm size class', () => {
    render(<Button size="sm">Pequeno</Button>)
    expect(screen.getByRole('button').className).toMatch(/sm/)
  })

  it('applies lg size class', () => {
    render(<Button size="lg">Grande</Button>)
    expect(screen.getByRole('button').className).toMatch(/lg/)
  })

  it('merges custom className', () => {
    render(<Button className="my-custom">Botão</Button>)
    expect(screen.getByRole('button').className).toMatch(/my-custom/)
  })
})
