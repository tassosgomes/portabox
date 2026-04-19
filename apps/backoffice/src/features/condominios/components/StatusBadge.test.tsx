import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { StatusBadge } from './StatusBadge'

describe('StatusBadge', () => {
  it('mostra "Pré-ativo" para status 1', () => {
    render(<StatusBadge status={1} />)
    expect(screen.getByText('Pré-ativo')).toBeInTheDocument()
  })

  it('mostra "Ativo" para status 2', () => {
    render(<StatusBadge status={2} />)
    expect(screen.getByText('Ativo')).toBeInTheDocument()
  })

  it('aplica classe pre-ativo para status 1', () => {
    render(<StatusBadge status={1} />)
    const badge = screen.getByText('Pré-ativo').closest('span')
    expect(badge).toHaveClass('pre-ativo')
  })

  it('aplica classe ativo para status 2', () => {
    render(<StatusBadge status={2} />)
    const badge = screen.getByText('Ativo').closest('span')
    expect(badge).toHaveClass('ativo')
  })
})
