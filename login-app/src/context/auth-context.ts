import { createContext } from 'react'
import type { LoginRequest, RegisterRequest, User } from '../types/auth'

export type SessionNotice = 'expired' | null

export interface AuthContextValue {
  user: User | null
  token: string | null
  isAuthenticated: boolean
  isInitializing: boolean
  sessionNotice: SessionNotice
  clearSessionNotice: () => void
  login: (payload: LoginRequest) => Promise<void>
  register: (payload: RegisterRequest) => Promise<User>
  logout: (reason?: SessionNotice) => void
  refreshProfile: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)