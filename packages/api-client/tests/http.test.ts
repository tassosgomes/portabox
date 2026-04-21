import { afterEach, describe, expect, it, vi } from 'vitest'

import { ApiError } from '../src/errors'
import { apiFetch, configure } from '../src/http'

describe('apiFetch', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    configure({ baseUrl: 'http://localhost/api/v1', getAuthToken: () => null })
  })

  it('calls fetch with absolute url and authorization header when token exists', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ ok: true }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    configure({ baseUrl: 'http://localhost/api/v1/', getAuthToken: () => 'token-123' })

    await apiFetch('/condominios/1/estrutura')

    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost/api/v1/condominios/1/estrutura',
      expect.objectContaining({
        credentials: 'include',
        headers: expect.any(Headers),
      }),
    )

    const headers = fetchMock.mock.calls[0]?.[1]?.headers as Headers
    expect(headers.get('Authorization')).toBe('Bearer token-123')
    expect(headers.get('Accept')).toBe('application/json, application/problem+json')
  })

  it('returns undefined on 204 without parsing json', async () => {
    const jsonSpy = vi.fn()
    const response = { ok: true, status: 204, json: jsonSpy } as unknown as Response
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(response))

    configure({ baseUrl: 'http://localhost/api/v1', getAuthToken: () => null })

    const result = await apiFetch('/condominios/1/estrutura')

    expect(result).toBeUndefined()
    expect(jsonSpy).not.toHaveBeenCalled()
  })

  it('throws ApiError with problem details fields on non-ok response', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify({
            type: 'https://portabox.app/problems/conflict',
            title: 'Conflito canônico',
            status: 409,
            detail: 'Já existe bloco com esse nome.',
          }),
          {
            status: 409,
            headers: { 'Content-Type': 'application/problem+json' },
          },
        ),
      ),
    )

    configure({ baseUrl: 'http://localhost/api/v1', getAuthToken: () => null })

    await expect(apiFetch('/condominios/1/blocos')).rejects.toMatchObject<ApiError>({
      status: 409,
      title: 'Conflito canônico',
      detail: 'Já existe bloco com esse nome.',
      type: 'https://portabox.app/problems/conflict',
    })
  })

  it('uses environment base url when configure was not called', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ ok: true }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)
    process.env.PORTABOX_API_BASE_URL = 'http://env-base/api'
    configure({ baseUrl: '', getAuthToken: () => null })

    await apiFetch('/health')

    expect(fetchMock).toHaveBeenCalledWith(
      'http://env-base/api/health',
      expect.objectContaining({ credentials: 'include', headers: expect.any(Headers) }),
    )

    delete process.env.PORTABOX_API_BASE_URL
  })

  it('does not set content type automatically for FormData bodies', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ ok: true }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)
    configure({ baseUrl: 'http://localhost/api/v1', getAuthToken: () => null })

    const formData = new FormData()
    formData.set('nome', 'Bloco A')

    await apiFetch('/upload', { method: 'POST', body: formData })

    const headers = fetchMock.mock.calls[0]?.[1]?.headers as Headers
    expect(headers.get('Content-Type')).toBeNull()
  })

  it('falls back to status text for non-json error responses', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response('forbidden', {
          status: 403,
          statusText: 'Forbidden',
          headers: { 'Content-Type': 'text/plain' },
        }),
      ),
    )

    configure({ baseUrl: 'http://localhost/api/v1', getAuthToken: () => null })

    await expect(apiFetch('/condominios/1/blocos')).rejects.toMatchObject<ApiError>({
      status: 403,
      title: 'Forbidden',
      type: 'about:blank',
    })
  })
})
