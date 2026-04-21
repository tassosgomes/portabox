import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook, waitFor } from '@testing-library/react'
import { getEstrutura, queryKeys } from '@portabox/api-client'
import type { ReactNode } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useEstrutura } from '../hooks/useEstrutura'

vi.mock('@portabox/api-client', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@portabox/api-client')>()
  return {
    ...actual,
    getEstrutura: vi.fn(),
  }
})

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })

  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  }

  return { Wrapper, queryClient }
}

describe('useEstrutura', () => {
  beforeEach(() => {
    vi.mocked(getEstrutura).mockReset()
  })

  it('calls getEstrutura with includeInactive and uses queryKeys.estrutura(id)', async () => {
    vi.mocked(getEstrutura).mockResolvedValue({
      condominioId: 'cond-1',
      nomeFantasia: 'Residencial Sol',
      blocos: [],
      geradoEm: '2026-04-20T10:00:00Z',
    })

    const { Wrapper, queryClient } = createWrapper()
    const { result } = renderHook(() => useEstrutura('cond-1', false), { wrapper: Wrapper })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(getEstrutura).toHaveBeenCalledWith('cond-1', false)
    expect(queryClient.getQueryCache().find({ queryKey: queryKeys.estrutura('cond-1') })).toBeTruthy()
  })

  it('refetches when includeInactive changes without changing the query key', async () => {
    vi.mocked(getEstrutura)
      .mockResolvedValueOnce({
        condominioId: 'cond-1',
        nomeFantasia: 'Residencial Sol',
        blocos: [],
        geradoEm: '2026-04-20T10:00:00Z',
      })
      .mockResolvedValueOnce({
        condominioId: 'cond-1',
        nomeFantasia: 'Residencial Sol',
        blocos: [],
        geradoEm: '2026-04-20T10:01:00Z',
      })

    const { Wrapper } = createWrapper()
    const { rerender } = renderHook(
      ({ includeInactive }) => useEstrutura('cond-1', includeInactive),
      {
        initialProps: { includeInactive: false },
        wrapper: Wrapper,
      },
    )

    await waitFor(() => {
      expect(getEstrutura).toHaveBeenCalledTimes(1)
    })

    rerender({ includeInactive: true })

    await waitFor(() => {
      expect(getEstrutura).toHaveBeenNthCalledWith(2, 'cond-1', true)
    })
  })
})
