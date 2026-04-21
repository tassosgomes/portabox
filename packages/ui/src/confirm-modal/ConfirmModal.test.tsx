import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ConfirmModal } from './ConfirmModal'

describe('ConfirmModal', () => {
  it('calls onConfirm when confirm button is clicked', async () => {
    const user = userEvent.setup()
    const onConfirm = vi.fn()

    render(
      <ConfirmModal
        open={true}
        title="Inativar unidade"
        description="A unidade deixará de aparecer no fluxo principal."
        confirmLabel="Inativar"
        cancelLabel="Cancelar"
        onConfirm={onConfirm}
        onCancel={() => {}}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'Inativar' }))
    expect(onConfirm).toHaveBeenCalledOnce()
  })

  it('calls onCancel when cancel button is clicked', async () => {
    const user = userEvent.setup()
    const onCancel = vi.fn()

    render(
      <ConfirmModal
        open={true}
        title="Inativar unidade"
        description="A unidade deixará de aparecer no fluxo principal."
        confirmLabel="Inativar"
        cancelLabel="Cancelar"
        onConfirm={() => {}}
        onCancel={onCancel}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'Cancelar' }))
    expect(onCancel).toHaveBeenCalledOnce()
  })

  it('closes on Escape through onCancel', async () => {
    const user = userEvent.setup()
    const onCancel = vi.fn()

    render(
      <ConfirmModal
        open={true}
        title="Inativar unidade"
        description="A unidade deixará de aparecer no fluxo principal."
        confirmLabel="Inativar"
        cancelLabel="Cancelar"
        onConfirm={() => {}}
        onCancel={onCancel}
      />,
    )

    await user.keyboard('{Escape}')
    expect(onCancel).toHaveBeenCalledOnce()
  })

  it('uses the danger button variant when danger=true', () => {
    render(
      <ConfirmModal
        open={true}
        title="Inativar unidade"
        description="A unidade deixará de aparecer no fluxo principal."
        confirmLabel="Excluir"
        cancelLabel="Cancelar"
        danger={true}
        onConfirm={() => {}}
        onCancel={() => {}}
      />,
    )

    expect(screen.getByRole('button', { name: 'Excluir' }).className).toMatch(/danger/)
  })

  it('matches the danger snapshot variant', () => {
    const { asFragment } = render(
      <ConfirmModal
        open={true}
        title="Inativar unidade"
        description="A unidade deixará de aparecer no fluxo principal."
        confirmLabel="Excluir"
        cancelLabel="Cancelar"
        danger={true}
        onConfirm={() => {}}
        onCancel={() => {}}
      />,
    )

    expect(asFragment()).toMatchSnapshot('confirm-modal-danger')
  })
})
