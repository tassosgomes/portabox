import {
  useCallback,
  useEffect,
  useState,
  type ReactNode,
} from 'react'
import { apiClient } from '@/shared/api/client'
import type { LoginRequest, LoginResponse } from '@/shared/api/types'
import { AuthContext, type AuthState, type AuthUser } from './context'

type MeResponse = {
  userId?: string
  id?: string
  email?: string
  name?: string
  role?: string
  roles?: string[]
  tenantId?: string | null
}

function normalizeAuthUser(payload: MeResponse): AuthUser {
  const id = payload.userId ?? payload.id
  const email = payload.email

  if (!id || !email) {
    throw new Error('Auth payload is missing required fields')
  }

  const role = payload.role ?? payload.roles?.[0] ?? 'Sindico'
  const fallbackName = email.split('@')[0] ?? email

  return {
    id,
    email,
    name: payload.name ?? fallbackName,
    role,
    tenantId: payload.tenantId ?? null,
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({
    user: null,
    isAuthenticated: false,
    isLoading: true,
  })

  useEffect(() => {
    apiClient
      .get<MeResponse>('/v1/auth/me', { noRedirectOn401: true })
      .then((payload) => {
        setState({ user: normalizeAuthUser(payload), isAuthenticated: true, isLoading: false })
      })
      .catch(() => {
        setState({ user: null, isAuthenticated: false, isLoading: false })
      })
  }, [])

  const login = useCallback(async (credentials: LoginRequest): Promise<void> => {
    await apiClient.post<LoginResponse>('/v1/auth/login', credentials, { noRedirectOn401: true })
    const me = await apiClient.get<MeResponse>('/v1/auth/me', { noRedirectOn401: true })
    const user = normalizeAuthUser(me)

    setState({
      user,
      isAuthenticated: true,
      isLoading: false,
    })
  }, [])

  const logout = useCallback(async (): Promise<void> => {
    await apiClient.post<void>('/v1/auth/logout')
    setState({ user: null, isAuthenticated: false, isLoading: false })
  }, [])

  return (
    <AuthContext.Provider value={{ ...state, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}
