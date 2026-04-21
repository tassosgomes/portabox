import { getRuntimeEnv } from '@portabox/api-client'

const API_BASE = getRuntimeEnv('VITE_API_BASE_URL') ?? '/api'

function getXsrfToken(): string | undefined {
  const match = document.cookie
    .split('; ')
    .find((row) => row.startsWith('XSRF-TOKEN='))
  return match ? decodeURIComponent(match.split('=')[1]) : undefined
}

function isMutating(method: string): boolean {
  return ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method.toUpperCase())
}

export class ApiHttpError extends Error {
  constructor(
    public readonly status: number,
    public readonly body: unknown,
  ) {
    super(`HTTP ${status}`)
    this.name = 'ApiHttpError'
  }
}

interface RequestOptions extends RequestInit {
  noRedirectOn401?: boolean
}

async function request<T>(path: string, init: RequestOptions = {}): Promise<T> {
  const { noRedirectOn401, ...fetchInit } = init
  const method = (fetchInit.method ?? 'GET').toUpperCase()
  const headers = new Headers(fetchInit.headers)

  if (!headers.has('Content-Type') && fetchInit.body && !(fetchInit.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json')
  }

  if (isMutating(method)) {
    const xsrf = getXsrfToken()
    if (xsrf) {
      headers.set('X-XSRF-TOKEN', xsrf)
    }
  }

  const res = await fetch(`${API_BASE}${path}`, {
    ...fetchInit,
    headers,
    credentials: 'include',
  })

  if (res.status === 401) {
    if (!noRedirectOn401) {
      const redirectTo = encodeURIComponent(
        window.location.pathname + window.location.search,
      )
      window.location.href = `/login?redirectTo=${redirectTo}`
    }
    throw new ApiHttpError(401, null)
  }

  if (!res.ok) {
    let body: unknown
    try {
      body = await res.json()
    } catch {
      body = null
    }
    throw new ApiHttpError(res.status, body)
  }

  if (res.status === 204 || res.headers.get('content-length') === '0') {
    return undefined as T
  }

  return res.json() as Promise<T>
}

export const apiClient = {
  get<T>(path: string, init?: RequestOptions): Promise<T> {
    return request<T>(path, { ...init, method: 'GET' })
  },
  post<T>(path: string, body?: unknown, init?: RequestOptions): Promise<T> {
    return request<T>(path, {
      ...init,
      method: 'POST',
      body: body !== undefined ? JSON.stringify(body) : undefined,
    })
  },
}
