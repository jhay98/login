export interface User {
  id: number
  email: string
  firstName: string
  lastName: string
  createdAt: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  email: string
  password: string
  firstName: string
  lastName: string
}

export interface LoginResponse {
  token: string
  user: User
}

export interface ApiErrorShape {
  message?: string
  errors?: string[]
}