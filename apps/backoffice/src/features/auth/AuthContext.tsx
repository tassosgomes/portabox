import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from 'react'
import { apiClient } from '@/shared/api/client'
import type { LoginRequest, LoginResponse, MeResponse } from '@/shared/api/types'

export interface AuthUser {
  id: string
  email: string
  name: string
  role: string
}

interface AuthState {
  user: AuthUser | null
  isAuthenticated: boolean
  isLoading: boolean
}

interface AuthContextValue extends AuthState {
  login: (credentials: LoginRequest) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

function nameFromEmail(email: string): string {
  const local = email.split('@')[0] ?? email
  if (!local) return email
  return local
    .split(/[._-]+/)
    .filter(Boolean)
    .map((token) => token.charAt(0).toUpperCase() + token.slice(1))
    .join(' ')
}

function primaryRole(roles: string[], fallback?: string | null): string {
  if (roles.length > 0) return roles[0]!
  return fallback ?? ''
}

function userFromMe(me: MeResponse): AuthUser {
  return {
    id: me.userId,
    email: me.email,
    name: nameFromEmail(me.email),
    role: primaryRole(me.roles),
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
      .then((me) => {
        setState({ user: userFromMe(me), isAuthenticated: true, isLoading: false })
      })
      .catch(() => {
        setState({ user: null, isAuthenticated: false, isLoading: false })
      })
  }, [])

  const login = useCallback(async (credentials: LoginRequest): Promise<void> => {
    await apiClient.post<LoginResponse>('/v1/auth/login', credentials)
    const me = await apiClient.get<MeResponse>('/v1/auth/me', { noRedirectOn401: true })
    setState({ user: userFromMe(me), isAuthenticated: true, isLoading: false })
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

export function useAuthContext(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuthContext must be used within AuthProvider')
  return ctx
}
