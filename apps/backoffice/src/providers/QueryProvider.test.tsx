import { useQuery, useQueryClient } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { queryClient } from '@/lib/queryClient'
import { QueryProvider } from './QueryProvider'

afterEach(() => {
  queryClient.clear()
})

function QueryClientProbe() {
  const client = useQueryClient()

  return <span>{client === queryClient ? 'query-client-ready' : 'missing-query-client'}</span>
}

function QueryConsumer() {
  const { data } = useQuery({
    queryKey: ['test-query'],
    queryFn: async () => 'loaded-from-query',
  })

  return <span>{data ?? 'loading'}</span>
}

describe('QueryProvider', () => {
  it('provides QueryClient context to children', () => {
    render(
      <QueryProvider>
        <QueryClientProbe />
      </QueryProvider>,
    )

    expect(screen.getByText('query-client-ready')).toBeInTheDocument()
  })

  it('uses the expected default query options', () => {
    expect(queryClient.getDefaultOptions().queries?.staleTime).toBe(30_000)
    expect(queryClient.getDefaultOptions().queries?.retry).toBe(1)
  })

  it('does not mount devtools in test mode', () => {
    const { container } = render(
      <QueryProvider>
        <div>content</div>
      </QueryProvider>,
    )

    expect(container.querySelector('.tsqd-parent-container')).not.toBeInTheDocument()
  })

  it('supports useQuery consumers with cache', async () => {
    render(
      <QueryProvider>
        <QueryConsumer />
      </QueryProvider>,
    )

    await waitFor(() => {
      expect(screen.getByText('loaded-from-query')).toBeInTheDocument()
    })

    expect(queryClient.getQueryData(['test-query'])).toBe('loaded-from-query')
  })
})
