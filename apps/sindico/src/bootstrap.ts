import { configure, getRuntimeEnv } from '@portabox/api-client'

const AUTH_TOKEN_STORAGE_KEY = 'portabox.token'

export function configureApiClient() {
  const baseUrl = getRuntimeEnv('VITE_API_URL')
  if (!baseUrl) {
    throw new Error('VITE_API_URL is not configured')
  }

  configure({
    baseUrl,
    getAuthToken: () => localStorage.getItem(AUTH_TOKEN_STORAGE_KEY),
  })
}
