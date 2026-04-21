import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook, waitFor } from '@testing-library/react'
import { ApiError, criarUnidade, queryKeys, type Estrutura } from '@portabox/api-client'
import type { ReactNode } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { useCriarUnidade } from '../hooks/useCriarUnidade'

vi.mock('@portabox/api-client', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@portabox/api-client')>()
  return {
    ...actual,
    criarUnidade: vi.fn(),
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

  return { Wrapper, queryClient, invalidateQueries }
}

describe('useCriarUnidade', () => {
  it('calls criarUnidade with payload and invalidates estrutura on success', async () => {
    vi.mocked(criarUnidade).mockResolvedValue({
      id: 'un-1',
      blocoId: 'bloco-1',
      andar: 2,
      numero: '201A',
      ativo: true,
      inativadoEm: null,
    })

    const { Wrapper, invalidateQueries } = createWrapper()
    const { result } = renderHook(() => useCriarUnidade('cond-1'), { wrapper: Wrapper })

    await result.current.mutateAsync({ blocoId: 'bloco-1', andar: 2, numero: '201A' })

    expect(criarUnidade).toHaveBeenCalledWith({
      condominioId: 'cond-1',
      blocoId: 'bloco-1',
      andar: 2,
      numero: '201A',
    })
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.estrutura('cond-1') })
  })

  it('applies optimistic update and reverts on error', async () => {
    let rejectRequest: ((reason?: unknown) => void) | undefined
    vi.mocked(criarUnidade).mockImplementation(
      () => new Promise((_, reject) => {
        rejectRequest = reject
      }),
    )

    const initialData: Estrutura = {
      condominioId: 'cond-1',
      nomeFantasia: 'Residencial Sol',
      geradoEm: '2026-04-20T10:00:00Z',
      blocos: [
        {
          id: 'bloco-1',
          nome: 'Bloco A',
          ativo: true,
          andares: [{ andar: 1, unidades: [{ id: 'un-1', numero: '101', ativo: true }] }],
        },
      ],
    }

    const { Wrapper, queryClient } = createWrapper()
    queryClient.setQueryData(queryKeys.estrutura('cond-1'), initialData)

    const { result } = renderHook(() => useCriarUnidade('cond-1'), { wrapper: Wrapper })
    const mutationPromise = result.current.mutateAsync({ blocoId: 'bloco-1', andar: 2, numero: '201a' })

    await waitFor(() => {
      const optimisticData = queryClient.getQueryData<Estrutura>(queryKeys.estrutura('cond-1'))
      expect(optimisticData?.blocos[0]?.andares[1]?.andar).toBe(2)
      expect(optimisticData?.blocos[0]?.andares[1]?.unidades[0]?.numero).toBe('201A')
    })

    rejectRequest?.(new ApiError({ type: 'about:blank', title: 'Conflict', status: 409, detail: 'Duplicada' }))
    await expect(mutationPromise).rejects.toBeInstanceOf(ApiError)

    await waitFor(() => {
      expect(queryClient.getQueryData(queryKeys.estrutura('cond-1'))).toEqual(initialData)
    })
  })
})
