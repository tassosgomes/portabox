export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  userId?: string
  id?: string
  email?: string
  name?: string
  role?: string | null
  tenantId?: string | null
}

export interface PasswordSetupRequest {
  token: string
  password: string
}
