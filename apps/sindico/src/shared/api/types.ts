export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  id: string
  email: string
  name: string
  role: string
}

export interface PasswordSetupRequest {
  token: string
  password: string
}
