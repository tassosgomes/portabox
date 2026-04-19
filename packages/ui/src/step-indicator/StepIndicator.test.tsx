import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { StepIndicator } from './StepIndicator'

const STEPS = [
  { label: 'Dados do condomínio' },
  { label: 'Consentimento LGPD' },
  { label: 'Síndico responsável' },
]

describe('StepIndicator', () => {
  it('renders all steps', () => {
    render(<StepIndicator steps={STEPS} currentStep={1} />)
    expect(screen.getByText('Dados do condomínio')).toBeInTheDocument()
    expect(screen.getByText('Consentimento LGPD')).toBeInTheDocument()
    expect(screen.getByText('Síndico responsável')).toBeInTheDocument()
  })

  it('marks current step with aria-current=step', () => {
    render(<StepIndicator steps={STEPS} currentStep={2} />)
    const items = screen.getAllByRole('listitem')
    expect(items[1]).toHaveAttribute('aria-current', 'step')
  })

  it('does not set aria-current on non-current steps', () => {
    render(<StepIndicator steps={STEPS} currentStep={1} />)
    const items = screen.getAllByRole('listitem')
    expect(items[1]).not.toHaveAttribute('aria-current')
    expect(items[2]).not.toHaveAttribute('aria-current')
  })

  it('applies current class to current step', () => {
    render(<StepIndicator steps={STEPS} currentStep={2} />)
    const items = screen.getAllByRole('listitem')
    expect(items[1].className).toMatch(/current/)
  })

  it('applies completed class to past steps', () => {
    render(<StepIndicator steps={STEPS} currentStep={3} />)
    const items = screen.getAllByRole('listitem')
    expect(items[0].className).toMatch(/completed/)
    expect(items[1].className).toMatch(/completed/)
  })

  it('shows checkmark for completed steps', () => {
    render(<StepIndicator steps={STEPS} currentStep={3} />)
    const circles = document.querySelectorAll('[aria-hidden="true"]')
    expect(circles[0].textContent).toBe('✓')
    expect(circles[1].textContent).toBe('✓')
  })

  it('shows step numbers for non-completed steps', () => {
    render(<StepIndicator steps={STEPS} currentStep={2} />)
    const circles = document.querySelectorAll('[aria-hidden="true"]')
    expect(circles[1].textContent).toBe('2')
    expect(circles[2].textContent).toBe('3')
  })

  it('renders nav element with label', () => {
    render(<StepIndicator steps={STEPS} currentStep={1} />)
    expect(screen.getByRole('navigation', { name: 'Progresso do formulário' })).toBeInTheDocument()
  })

  it('renders step descriptions when provided', () => {
    const stepsWithDesc = [
      { label: 'Etapa 1', description: 'Preencha os dados' },
      { label: 'Etapa 2' },
    ]
    render(<StepIndicator steps={stepsWithDesc} currentStep={1} />)
    expect(screen.getByText('Preencha os dados')).toBeInTheDocument()
  })

  it('renders correct number of list items', () => {
    render(<StepIndicator steps={STEPS} currentStep={1} />)
    expect(screen.getAllByRole('listitem')).toHaveLength(3)
  })
})
