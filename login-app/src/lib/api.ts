import type {
  ApiErrorShape,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  User,
} from '../types/auth'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api'

export class ApiError extends Error {
  status: number
  details: string[]

  constructor(status: number, message: string, details: string[] = []) {
    super(message)
    this.status = status
    this.details = details
  }
}

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

export const authApi = {
  register(payload: RegisterRequest) {
    return request<User>('/auth/register', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  login(payload: LoginRequest) {
    return request<LoginResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  me(token: string) {
    return request<User>('/auth/me', {
      method: 'GET',
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
  },
}