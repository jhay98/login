import type {
  ApiErrorShape,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  User,
} from '../types/auth'

/** Base URL for backend API requests. */
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api'

/**
 * Represents a non-success API response.
 */
export class ApiError extends Error {
  /** HTTP status code returned by the backend. */
  status: number
  /** Optional error details returned by the backend. */
  details: string[]

  /**
   * Creates an API error object.
   */
  constructor(status: number, message: string, details: string[] = []) {
    super(message)
    this.status = status
    this.details = details
  }
}

/**
 * Parses a failed fetch response into a typed {@link ApiError}.
 */
async function parseError(response: Response): Promise<ApiError> {
  let body: ApiErrorShape | null = null

  try {
    body = (await response.json()) as ApiErrorShape
  } catch {
    body = null
  }

  const fallback = response.statusText || 'Request failed'
  const message = body?.message?.trim() || fallback
  const details = Array.isArray(body?.errors) ? body.errors : []

  return new ApiError(response.status, message, details)
}

/**
 * Performs a JSON HTTP request against the backend API.
 */
async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
  })

  if (!response.ok) {
    throw await parseError(response)
  }

  return (await response.json()) as T
}

/**
 * Authentication API methods.
 */
export const authApi = {
  /** Registers a new user. */
  register(payload: RegisterRequest) {
    return request<User>('/auth/register', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  /** Authenticates a user and returns token plus profile. */
  login(payload: LoginRequest) {
    return request<LoginResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  /** Retrieves the current user profile for a bearer token. */
  me(token: string) {
    return request<User>('/auth/me', {
      method: 'GET',
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
  },
}