import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Card } from './Card'

describe('Card', () => {
  it('renders children', () => {
    render(<Card>Conteúdo do card</Card>)
    expect(screen.getByText('Conteúdo do card')).toBeInTheDocument()
  })

  it('applies default md padding class', () => {
    const { container } = render(<Card>Conteúdo</Card>)
    expect(container.firstChild).toHaveClass('pad-md')
  })

  it('applies sm padding class', () => {
    const { container } = render(<Card padding="sm">Conteúdo</Card>)
    expect(container.firstChild).toHaveClass('pad-sm')
  })

  it('applies lg padding class', () => {
    const { container } = render(<Card padding="lg">Conteúdo</Card>)
    expect(container.firstChild).toHaveClass('pad-lg')
  })

  it('applies card base class', () => {
    const { container } = render(<Card>Conteúdo</Card>)
    expect(container.firstChild).toHaveClass('card')
  })

  it('merges custom className', () => {
    const { container } = render(<Card className="my-class">Conteúdo</Card>)
    expect(container.firstChild).toHaveClass('my-class')
  })

  it('passes through HTML attributes', () => {
    render(<Card data-testid="my-card">Conteúdo</Card>)
    expect(screen.getByTestId('my-card')).toBeInTheDocument()
  })
})
