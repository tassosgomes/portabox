import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { apiClient, ApiHttpError } from './client'

const mockFetch = vi.fn()
vi.stubGlobal('fetch', mockFetch)

function makeResponse(status: number, body?: unknown, headers?: Record<string, string>) {
  const responseHeaders = new Headers(headers)
  return {
    ok: status >= 200 && status < 300,
    status,
    headers: responseHeaders,
    json: () => Promise.resolve(body),
  } as Response
}

describe('apiClient', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    Object.defineProperty(document, 'cookie', {
      writable: true,
      value: '',
    })
    Object.defineProperty(window, 'location', {
      writable: true,
      value: { pathname: '/condominios', search: '', href: '' },
    })
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('sends GET request with credentials: include', async () => {
    mockFetch.mockResolvedValue(makeResponse(200, { ok: true }))

    await apiClient.get('/v1/test')

    const [, init] = mockFetch.mock.calls[0] as [string, RequestInit]
    expect(init.credentials).toBe('include')
    expect(init.method).toBe('GET')
  })

  it('adds X-XSRF-TOKEN header from XSRF-TOKEN cookie on POST', async () => {
    document.cookie = 'XSRF-TOKEN=test-token-123'
    mockFetch.mockResolvedValue(makeResponse(200, {}))

    await apiClient.post('/v1/test', { data: 1 })

    const [, init] = mockFetch.mock.calls[0] as [string, RequestInit]
    const headers = init.headers as Headers
    expect(headers.get('X-XSRF-TOKEN')).toBe('test-token-123')
  })

  it('does not add X-XSRF-TOKEN header on GET', async () => {
    document.cookie = 'XSRF-TOKEN=test-token-123'
    mockFetch.mockResolvedValue(makeResponse(200, {}))

    await apiClient.get('/v1/test')

    const [, init] = mockFetch.mock.calls[0] as [string, RequestInit]
    const headers = init.headers as Headers
    expect(headers.get('X-XSRF-TOKEN')).toBeNull()
  })

  it('redirects to /login when response is 401', async () => {
    mockFetch.mockResolvedValue(makeResponse(401, null))

    await expect(apiClient.get('/v1/protected')).rejects.toThrow(ApiHttpError)

    expect(window.location.href).toContain('/login?redirectTo=')
  })

  it('does not redirect when noRedirectOn401 is true', async () => {
    mockFetch.mockResolvedValue(makeResponse(401, null))
    const hrefBefore = window.location.href

    await expect(
      apiClient.get('/v1/auth/me', { noRedirectOn401: true }),
    ).rejects.toMatchObject({ status: 401 })

    expect(window.location.href).toBe(hrefBefore)
  })

  it('throws ApiHttpError with status when response is not ok', async () => {
    mockFetch.mockResolvedValue(
      makeResponse(422, { title: 'Validation error', status: 422 }),
    )

    await expect(apiClient.post('/v1/test', {})).rejects.toMatchObject({
      status: 422,
    })
  })

  it('returns undefined for 204 responses', async () => {
    mockFetch.mockResolvedValue(makeResponse(204))

    const result = await apiClient.post('/v1/logout')
    expect(result).toBeUndefined()
  })
})
