import { configure } from '@portabox/api-client'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { configureApiClient } from './bootstrap'

vi.mock('@portabox/api-client', () => ({
  configure: vi.fn(),
  getRuntimeEnv: vi.fn((key: string) =>
    key === 'VITE_API_URL' ? 'http://localhost/api/v1' : undefined,
  ),
}))

describe('configureApiClient', () => {
  beforeEach(() => {
    vi.mocked(configure).mockClear()
    localStorage.clear()
  })

  it('configures the shared api client exactly once per bootstrap call', () => {
    configureApiClient()

    expect(configure).toHaveBeenCalledTimes(1)
  })

  it('passes baseUrl and auth token getter', () => {
    localStorage.setItem('portabox.token', 'token-123')

    configureApiClient()

    const options = vi.mocked(configure).mock.calls[0]?.[0]
    const getAuthToken = options?.getAuthToken

    expect(options?.baseUrl).toBe('http://localhost/api/v1')
    expect(getAuthToken?.()).toBe('token-123')
  })
})
