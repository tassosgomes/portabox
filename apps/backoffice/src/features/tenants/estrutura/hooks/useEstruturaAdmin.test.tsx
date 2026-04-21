import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getEstruturaAdmin, queryKeys } from '@portabox/api-client'
import type { Estrutura } from '@portabox/api-client'
import { useEstruturaAdmin } from './useEstruturaAdmin'

vi.mock('@portabox/api-client', async () => {
  const actual = await vi.importActual<typeof import('@portabox/api-client')>('@portabox/api-client')
  return {
    ...actual,
    getEstruturaAdmin: vi.fn(),
  }
})

const estruturaFixture: Estrutura = {
  condominioId: 'cond-1',
  nomeFantasia: 'Residencial Sol',
  geradoEm: '2026-04-20T10:00:00Z',
  blocos: [],
}

function createWrapper(client: QueryClient) {
  return function Wrapper({ children }: { children: React.ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>
  }
}

describe('useEstruturaAdmin', () => {
  beforeEach(() => {
    vi.mocked(getEstruturaAdmin).mockReset()
  })

  it('calls getEstruturaAdmin with condominioId/includeInactive and isolates cache key from sindico', async () => {
    const client = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    })

    vi.mocked(getEstruturaAdmin).mockResolvedValue(estruturaFixture)

    const { rerender } = renderHook(
      ({ includeInactive }) => useEstruturaAdmin('cond-1', includeInactive),
      {
        initialProps: { includeInactive: false },
        wrapper: createWrapper(client),
      },
    )

    await waitFor(() => {
      expect(vi.mocked(getEstruturaAdmin)).toHaveBeenCalledWith('cond-1', false)
    })

    expect(client.getQueryData(queryKeys.estruturaAdmin('cond-1'))).toEqual(estruturaFixture)
    expect(client.getQueryData(queryKeys.estrutura('cond-1'))).toBeUndefined()

    vi.mocked(getEstruturaAdmin).mockResolvedValue({
      ...estruturaFixture,
      blocos: [
        {
          id: 'bloco-1',
          nome: 'Bloco A',
          ativo: true,
          andares: [],
        },
      ],
    })

    rerender({ includeInactive: true })

    await waitFor(() => {
      expect(vi.mocked(getEstruturaAdmin)).toHaveBeenLastCalledWith('cond-1', true)
    })
  })
})
