import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook } from '@testing-library/react'
import {
  inativarBloco,
  queryKeys,
  reativarBloco,
  renomearBloco,
} from '@portabox/api-client'
import type { ReactNode } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { useInativarBloco } from '../hooks/useInativarBloco'
import { useReativarBloco } from '../hooks/useReativarBloco'
import { useRenomearBloco } from '../hooks/useRenomearBloco'

vi.mock('@portabox/api-client', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@portabox/api-client')>()
  return {
    ...actual,
    renomearBloco: vi.fn(),
    inativarBloco: vi.fn(),
    reativarBloco: vi.fn(),
  }
})

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  const invalidateQueries = vi.fn().mockResolvedValue(undefined)
  queryClient.invalidateQueries = invalidateQueries as typeof queryClient.invalidateQueries

  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  }

  return { Wrapper, invalidateQueries }
}

describe('bloco mutation hooks', () => {
  it('renames a bloco and invalidates the estrutura query', async () => {
    vi.mocked(renomearBloco).mockResolvedValue({
      id: 'bloco-1',
      condominioId: 'cond-1',
      nome: 'Torre Alfa',
      ativo: true,
      inativadoEm: null,
    })

    const { Wrapper, invalidateQueries } = createWrapper()
    const { result } = renderHook(() => useRenomearBloco('cond-1'), { wrapper: Wrapper })

    await result.current.mutateAsync({ blocoId: 'bloco-1', nome: 'Torre Alfa' })

    expect(renomearBloco).toHaveBeenCalledWith({ condominioId: 'cond-1', blocoId: 'bloco-1', nome: 'Torre Alfa' })
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.estrutura('cond-1') })
  })

  it('inativates a bloco and invalidates the estrutura query', async () => {
    vi.mocked(inativarBloco).mockResolvedValue({
      id: 'bloco-1',
      condominioId: 'cond-1',
      nome: 'Bloco A',
      ativo: false,
      inativadoEm: '2026-04-20T10:00:00Z',
    })

    const { Wrapper, invalidateQueries } = createWrapper()
    const { result } = renderHook(() => useInativarBloco('cond-1'), { wrapper: Wrapper })

    await result.current.mutateAsync({ blocoId: 'bloco-1' })

    expect(inativarBloco).toHaveBeenCalledWith({ condominioId: 'cond-1', blocoId: 'bloco-1' })
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.estrutura('cond-1') })
  })

  it('surfaces reactivation conflicts from the API', async () => {
    const conflict = new Error('Ja existe outro bloco ativo com esse nome. Inative o duplicado antes de reativar este.')
    vi.mocked(reativarBloco).mockRejectedValue(conflict)

    const { Wrapper } = createWrapper()
    const { result } = renderHook(() => useReativarBloco('cond-1'), { wrapper: Wrapper })

    await expect(result.current.mutateAsync({ blocoId: 'bloco-1' })).rejects.toThrow(
      'Ja existe outro bloco ativo com esse nome. Inative o duplicado antes de reativar este.',
    )
  })
})
