export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  userId: string
  role: string | null
  tenantId: string | null
}

export interface MeResponse {
  userId: string
  email: string
  roles: string[]
  tenantId: string | null
}

export interface ApiError {
  type: string
  title: string
  status: number
  detail?: string
  traceId?: string
}
