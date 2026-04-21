import { ApiError } from './errors'
import { getRuntimeEnv } from './runtime-env'

type ConfigureOptions = {
  baseUrl: string
  getAuthToken?: () => string | null
}

const configuration: Required<ConfigureOptions> = {
  baseUrl: '',
  getAuthToken: () => null,
}

export function configure(options: ConfigureOptions): void {
  configuration.baseUrl = options.baseUrl.replace(/\/$/, '')
  configuration.getAuthToken = options.getAuthToken ?? (() => null)
}

function resolveBaseUrl(): string {
  if (configuration.baseUrl) {
    return configuration.baseUrl
  }

  const fallback =
    getRuntimeEnv('VITE_API_BASE_URL') ?? getRuntimeEnv('PORTABOX_API_BASE_URL')
  if (!fallback) {
    throw new Error('API base URL is not configured')
  }

  return fallback.replace(/\/$/, '')
}

function buildHeaders(init?: RequestInit): Headers {
  const headers = new Headers(init?.headers)
  const token = configuration.getAuthToken()

  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json, application/problem+json')
  }

  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const bodyIsFormData = typeof FormData !== 'undefined' && init?.body instanceof FormData
  const bodyExists = init?.body !== undefined && init.body !== null
  if (bodyExists && !bodyIsFormData && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  return headers
}

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${resolveBaseUrl()}${path}`, {
    ...init,
    credentials: init?.credentials ?? 'include',
    headers: buildHeaders(init),
  })

  if (!response.ok) {
    throw await ApiError.fromResponse(response)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}
