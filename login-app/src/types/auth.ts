/** Public user profile returned by the API. */
export interface User {
  id: number
  email: string
  firstName: string
  lastName: string
  createdAt: string
}

/** Login request payload. */
export interface LoginRequest {
  email: string
  password: string
}

/** Registration request payload. */
export interface RegisterRequest {
  email: string
  password: string
  firstName: string
  lastName: string
}

/** Login response payload containing token and user. */
export interface LoginResponse {
  token: string
  user: User
}

/** Error body shape used by backend API responses. */
export interface ApiErrorShape {
  message?: string
  errors?: string[]
}