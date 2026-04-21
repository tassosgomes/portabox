import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook } from '@testing-library/react'
import { inativarUnidade, queryKeys, reativarUnidade } from '@portabox/api-client'
import type { ReactNode } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { useInativarUnidade } from '../hooks/useInativarUnidade'
import { useReativarUnidade } from '../hooks/useReativarUnidade'

vi.mock('@portabox/api-client', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@portabox/api-client')>()
  return {
    ...actual,
    inativarUnidade: vi.fn(),
    reativarUnidade: vi.fn(),
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

describe('unidade mutation hooks', () => {
  it('inactivates a unidade and invalidates the estrutura query', async () => {
    vi.mocked(inativarUnidade).mockResolvedValue({
      id: 'un-1',
      blocoId: 'bloco-1',
      andar: 1,
      numero: '101',
      ativo: false,
      inativadoEm: '2026-04-20T10:00:00Z',
    })

    const { Wrapper, invalidateQueries } = createWrapper()
    const { result } = renderHook(() => useInativarUnidade('cond-1'), { wrapper: Wrapper })

    await result.current.mutateAsync({ blocoId: 'bloco-1', unidadeId: 'un-1' })

    expect(inativarUnidade).toHaveBeenCalledWith({ condominioId: 'cond-1', blocoId: 'bloco-1', unidadeId: 'un-1' })
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.estrutura('cond-1') })
  })

  it('reativates a unidade and invalidates the estrutura query', async () => {
    vi.mocked(reativarUnidade).mockResolvedValue({
      id: 'un-1',
      blocoId: 'bloco-1',
      andar: 1,
      numero: '101',
      ativo: true,
      inativadoEm: null,
    })

    const { Wrapper, invalidateQueries } = createWrapper()
    const { result } = renderHook(() => useReativarUnidade('cond-1'), { wrapper: Wrapper })

    await result.current.mutateAsync({ blocoId: 'bloco-1', unidadeId: 'un-1' })

    expect(reativarUnidade).toHaveBeenCalledWith({ condominioId: 'cond-1', blocoId: 'bloco-1', unidadeId: 'un-1' })
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.estrutura('cond-1') })
  })
})
