import { describe, expect, it } from 'vitest'
import type { Estrutura } from '@portabox/api-client'
import { toTreeItems } from '../mappers/toTreeItems'

const estruturaFixture: Estrutura = {
  condominioId: 'cond-1',
  nomeFantasia: 'Residencial Sol',
  geradoEm: '2026-04-20T10:00:00Z',
  blocos: [
    {
      id: 'bloco-1',
      nome: 'Bloco A',
      ativo: true,
      andares: [
        {
          andar: 1,
          unidades: [
            { id: 'un-1', numero: '101', ativo: true },
            { id: 'un-2', numero: '102', ativo: false },
          ],
        },
      ],
    },
    {
      id: 'bloco-2',
      nome: 'Bloco B',
      ativo: false,
      andares: [
        {
          andar: 0,
          unidades: [{ id: 'un-3', numero: '001', ativo: true }],
        },
      ],
    },
  ],
}

describe('toTreeItems', () => {
  it('converts Estrutura into a nested TreeItem[] with inactive states', () => {
    const [root] = toTreeItems(estruturaFixture)
    const [blocoA, blocoB] = root.children ?? []
    const unidadeInativa = blocoA?.children?.[0]?.children?.[1]

    expect(root.label).toBe('Residencial Sol')
    expect(blocoA?.label).toBe('Bloco A · 1 unidade ativa')
    expect(blocoA?.state).toBe('default')
    expect(blocoB?.state).toBe('inactive')
    expect(unidadeInativa?.label).toBe('Unidade 102')
    expect(unidadeInativa?.state).toBe('inactive')
    expect(blocoA?.actions).toBeTruthy()
    expect(unidadeInativa?.actions).toBeTruthy()
  })

  it('calculates active unit badge correctly for each bloco', () => {
    const [root] = toTreeItems(estruturaFixture)
    const [blocoA, blocoB] = root.children ?? []

    expect(blocoA?.label).toContain('1 unidade ativa')
    expect(blocoB?.label).toContain('1 unidade ativa')
  })

  it('marks the selected bloco with an info badge', () => {
    const [root] = toTreeItems(estruturaFixture, { selectedBlocoId: 'bloco-1' })
    const [blocoA] = root.children ?? []

    expect(blocoA?.badge).toBeTruthy()
  })
})
