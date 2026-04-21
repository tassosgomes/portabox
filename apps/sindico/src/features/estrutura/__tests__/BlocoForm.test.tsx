import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { BlocoForm } from '../components/BlocoForm'

describe('BlocoForm', () => {
  it('validates empty and oversized names inline', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()

    render(
      <BlocoForm open mode="create" onClose={vi.fn()} onSubmit={onSubmit} />,
    )

    await user.click(screen.getByRole('button', { name: 'Criar bloco' }))
    expect(await screen.findByText('Informe o nome do bloco.')).toBeInTheDocument()

    await user.type(screen.getByLabelText('Nome do bloco'), 'A'.repeat(51))
    await user.click(screen.getByRole('button', { name: 'Criar bloco' }))

    expect(await screen.findByText('Use no máximo 50 caracteres.')).toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('submits trimmed values successfully', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn().mockResolvedValue(undefined)

    render(
      <BlocoForm open mode="rename" initialNome="Bloco A" onClose={vi.fn()} onSubmit={onSubmit} />,
    )

    const input = screen.getByLabelText('Nome do bloco')
    await user.clear(input)
    await user.type(input, '  Torre Alfa  ')
    await user.click(screen.getByRole('button', { name: 'Salvar nome' }))

    expect(onSubmit).toHaveBeenCalledWith({ nome: 'Torre Alfa' })
  })
})
