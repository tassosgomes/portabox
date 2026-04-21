import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { UnidadeForm } from '../components/UnidadeForm'

describe('UnidadeForm', () => {
  it('validates invalid numero values inline', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()

    render(
      <UnidadeForm open blocoNome="Bloco A" onClose={vi.fn()} onSubmit={onSubmit} />,
    )

    await user.clear(screen.getByLabelText('Número'))
    await user.click(screen.getByRole('button', { name: 'Adicionar unidade' }))
    expect(await screen.findByText('Informe o numero da unidade.')).toBeInTheDocument()

    await user.type(screen.getByLabelText('Número'), '1AB')
    await user.click(screen.getByRole('button', { name: 'Adicionar unidade' }))
    expect(await screen.findByText(/Use ate 4 digitos/i)).toBeInTheDocument()

    await user.clear(screen.getByLabelText('Número'))
    await user.type(screen.getByLabelText('Número'), '12345')
    await user.click(screen.getByRole('button', { name: 'Adicionar unidade' }))
    expect(await screen.findByText(/Use ate 4 digitos/i)).toBeInTheDocument()

    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('validates andar lower than zero inline', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()

    render(
      <UnidadeForm open blocoNome="Bloco A" onClose={vi.fn()} onSubmit={onSubmit} />,
    )

    const andarInput = screen.getByLabelText('Andar') as HTMLInputElement
    fireEvent.change(andarInput, { target: { value: '-1', valueAsNumber: -1 } })
    fireEvent.blur(andarInput)
    expect(andarInput.value).toBe('-1')
    await user.type(screen.getByLabelText('Número'), '101')
    await user.click(screen.getByRole('button', { name: 'Adicionar unidade' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/andar/i)
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('normalizes numero to uppercase before submit', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn().mockResolvedValue('close')

    render(
      <UnidadeForm open blocoNome="Bloco A" onClose={vi.fn()} onSubmit={onSubmit} />,
    )

    await user.clear(screen.getByLabelText('Andar'))
    await user.type(screen.getByLabelText('Andar'), '10')
    await user.type(screen.getByLabelText('Número'), '101a')
    await user.click(screen.getByRole('button', { name: 'Adicionar unidade' }))

    expect(onSubmit).toHaveBeenCalledWith({ andar: 10, numero: '101A' })
  })

  it('keeps the modal workflow ready for batch creation after success', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()
    const onSubmit = vi.fn().mockResolvedValue('keep-open')

    render(
      <UnidadeForm open blocoNome="Bloco A" keepOpenOnSuccess onClose={onClose} onSubmit={onSubmit} />,
    )

    await user.clear(screen.getByLabelText('Andar'))
    await user.type(screen.getByLabelText('Andar'), '5')
    await user.type(screen.getByLabelText('Número'), '501')
    await user.click(screen.getByRole('button', { name: 'Salvar e continuar' }))

    expect(onSubmit).toHaveBeenCalledWith({ andar: 5, numero: '501' })
    expect(onClose).not.toHaveBeenCalled()
    expect(screen.getByLabelText('Andar')).toHaveFocus()
    expect(screen.getByLabelText('Número')).toHaveValue('')
  })
})
