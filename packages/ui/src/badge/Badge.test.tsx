import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Badge } from './Badge'

describe('Badge', () => {
  it('renders default label for pre-ativo', () => {
    render(<Badge status="pre-ativo" />)
    expect(screen.getByText('Pré-ativo')).toBeInTheDocument()
  })

  it('renders default label for ativo', () => {
    render(<Badge status="ativo" />)
    expect(screen.getByText('Ativo')).toBeInTheDocument()
  })

  it('renders default label for inativo', () => {
    render(<Badge status="inativo" />)
    expect(screen.getByText('Inativo')).toBeInTheDocument()
  })

  it('renders custom label when provided', () => {
    render(<Badge status="pendente" label="Aguardando setup" />)
    expect(screen.getByText('Aguardando setup')).toBeInTheDocument()
  })

  it('applies status CSS class', () => {
    const { container } = render(<Badge status="ativo" />)
    expect(container.firstChild).toHaveClass('ativo')
  })

  it('applies badge base class', () => {
    const { container } = render(<Badge status="erro" />)
    expect(container.firstChild).toHaveClass('badge')
  })

  it('renders dot with aria-hidden', () => {
    render(<Badge status="pendente" />)
    const dot = document.querySelector('[aria-hidden="true"]')
    expect(dot).toBeInTheDocument()
  })

  it('renders all status variants', () => {
    const statuses = ['pre-ativo', 'ativo', 'inativo', 'pendente', 'processando', 'erro', 'info'] as const
    statuses.forEach((status) => {
      const { container } = render(<Badge status={status} />)
      expect(container.firstChild).toHaveClass(status)
    })
  })
})
