import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook, waitFor } from '@testing-library/react'
import { ApiError, criarBloco, queryKeys, type Estrutura } from '@portabox/api-client'
import type { ReactNode } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { useCriarBloco } from '../hooks/useCriarBloco'

vi.mock('@portabox/api-client', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@portabox/api-client')>()
  return {
    ...actual,
    criarBloco: vi.fn(),
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

describe('useCriarBloco', () => {
  it('calls criarBloco with payload and invalidates estrutura on success', async () => {
    vi.mocked(criarBloco).mockResolvedValue({
      id: 'bloco-1',
      condominioId: 'cond-1',
      nome: 'Bloco A',
      ativo: true,
      inativadoEm: null,
    })

    const { Wrapper, invalidateQueries } = createWrapper()
    const { result } = renderHook(() => useCriarBloco('cond-1'), { wrapper: Wrapper })

    await result.current.mutateAsync({ nome: 'Bloco A' })

    expect(criarBloco).toHaveBeenCalledWith({ condominioId: 'cond-1', nome: 'Bloco A' })
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.estrutura('cond-1') })
  })

  it('applies optimistic update and reverts on error', async () => {
    let rejectRequest: ((reason?: unknown) => void) | undefined
    vi.mocked(criarBloco).mockImplementation(
      () => new Promise((_, reject) => {
        rejectRequest = reject
      }),
    )

    const initialData: Estrutura = {
      condominioId: 'cond-1',
      nomeFantasia: 'Residencial Sol',
      geradoEm: '2026-04-20T10:00:00Z',
      blocos: [],
    }
    const { Wrapper, queryClient } = createWrapper()
    queryClient.setQueryData(queryKeys.estrutura('cond-1'), initialData)

    const { result } = renderHook(() => useCriarBloco('cond-1'), { wrapper: Wrapper })
    const mutationPromise = result.current.mutateAsync({ nome: 'Novo bloco' })

    await waitFor(() => {
      const optimisticData = queryClient.getQueryData<Estrutura>(queryKeys.estrutura('cond-1'))
      expect(optimisticData?.blocos).toHaveLength(1)
      expect(optimisticData?.blocos[0]?.nome).toBe('Novo bloco')
    })

    rejectRequest?.(new ApiError({ type: 'about:blank', title: 'Conflict', status: 409, detail: 'Duplicado' }))
    await expect(mutationPromise).rejects.toBeInstanceOf(ApiError)

    await waitFor(() => {
      expect(queryClient.getQueryData(queryKeys.estrutura('cond-1'))).toEqual(initialData)
    })
  })
})
