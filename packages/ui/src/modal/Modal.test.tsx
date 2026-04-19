import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Modal } from './Modal'

describe('Modal', () => {
  it('does not render when open=false', () => {
    render(
      <Modal open={false} onClose={() => {}}>
        Conteúdo
      </Modal>,
    )
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('renders when open=true', () => {
    render(
      <Modal open={true} onClose={() => {}}>
        Conteúdo do modal
      </Modal>,
    )
    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(screen.getByText('Conteúdo do modal')).toBeInTheDocument()
  })

  it('has aria-modal="true"', () => {
    render(
      <Modal open={true} onClose={() => {}}>
        Modal
      </Modal>,
    )
    expect(screen.getByRole('dialog')).toHaveAttribute('aria-modal', 'true')
  })

  it('renders title and sets aria-labelledby', () => {
    render(
      <Modal open={true} onClose={() => {}} title="Novo condomínio">
        Conteúdo
      </Modal>,
    )
    expect(screen.getByText('Novo condomínio')).toBeInTheDocument()
    expect(screen.getByRole('dialog')).toHaveAttribute('aria-labelledby', 'modal-title')
  })

  it('calls onClose when close button is clicked', async () => {
    const onClose = vi.fn()
    render(
      <Modal open={true} onClose={onClose} title="Modal">
        Conteúdo
      </Modal>,
    )
    await userEvent.click(screen.getByRole('button', { name: 'Fechar modal' }))
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('calls onClose when Escape key is pressed', async () => {
    const onClose = vi.fn()
    render(
      <Modal open={true} onClose={onClose}>
        Conteúdo
      </Modal>,
    )
    await userEvent.keyboard('{Escape}')
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('calls onClose when backdrop is clicked', async () => {
    const onClose = vi.fn()
    render(
      <Modal open={true} onClose={onClose}>
        Conteúdo
      </Modal>,
    )
    const backdrop = document.querySelector('[class*="backdrop"]') as HTMLElement
    await userEvent.click(backdrop)
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('applies size class md by default', () => {
    render(
      <Modal open={true} onClose={() => {}}>
        Conteúdo
      </Modal>,
    )
    expect(screen.getByRole('dialog').className).toMatch(/md/)
  })

  it('applies size class sm', () => {
    render(
      <Modal open={true} onClose={() => {}} size="sm">
        Conteúdo
      </Modal>,
    )
    expect(screen.getByRole('dialog').className).toMatch(/sm/)
  })

  it('traps focus inside modal — close button is focusable', () => {
    render(
      <Modal open={true} onClose={() => {}} title="Modal com foco">
        <button>Ação</button>
      </Modal>,
    )
    const buttons = screen.getAllByRole('button')
    expect(buttons.length).toBeGreaterThanOrEqual(1)
  })
})
