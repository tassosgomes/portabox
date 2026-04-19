import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from 'react'
import { apiClient } from '@/shared/api/client'
import type { LoginRequest, LoginResponse } from '@/shared/api/types'

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

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({
    user: null,
    isAuthenticated: false,
    isLoading: true,
  })

  useEffect(() => {
    apiClient
      .get<AuthUser>('/v1/auth/me', { noRedirectOn401: true })
      .then((user) => {
        setState({ user, isAuthenticated: true, isLoading: false })
      })
      .catch(() => {
        setState({ user: null, isAuthenticated: false, isLoading: false })
      })
  }, [])

  const login = useCallback(async (credentials: LoginRequest): Promise<void> => {
    const res = await apiClient.post<LoginResponse>('/v1/auth/login', credentials)
    setState({
      user: { id: res.id, email: res.email, name: res.name, role: res.role },
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

export function useAuthContext(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuthContext must be used within AuthProvider')
  return ctx
}
