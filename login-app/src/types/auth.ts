/** Public user profile returned by the API. */
export interface User {
  id: number
  email: string
  firstName: string
  lastName: string
  createdAt: string
  roles: string[]
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

/** Generic response payload with refreshed token and endpoint data. */
export interface RefreshTokenResponse<T> {
  token: string
  data: T
}

/** Activity event returned by the activity API. */
export interface ActivityEvent {
  id: number
  userId: number
  eventType: string
  ipAddress: string | null
  userAgent: string | null
  metadata: string | null
  occurredAtUtc: string
}

/** Request payload for creating an activity event. */
export interface CreateActivityRequest {
  userId: number
  eventType: string
  ipAddress?: string | null
  userAgent?: string | null
  metadata?: string | null
}

/** Error body shape used by backend API responses. */
export interface ApiErrorShape {
  message?: string
  errors?: string[]
}