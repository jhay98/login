import { createContext } from 'react'
import type { LoginRequest, RegisterRequest, User } from '../types/auth'

/**
 * Reason shown to the user when a session was cleared.
 */
export type SessionNotice = 'expired' | null

/**
 * Shape of authentication state and actions exposed by context.
 */
export interface AuthContextValue {
  /** Currently authenticated user, when available. */
  user: User | null
  /** Active JWT access token, when available. */
  token: string | null
  /** Whether a valid session currently exists. */
  isAuthenticated: boolean
  /** Whether provider is still restoring session state. */
  isInitializing: boolean
  /** Optional one-time session message. */
  sessionNotice: SessionNotice
  /** Clears the current session notice message. */
  clearSessionNotice: () => void
  /** Authenticates and stores a user session. */
  login: (payload: LoginRequest) => Promise<void>
  /** Registers a new account. */
  register: (payload: RegisterRequest) => Promise<User>
  /** Clears the local session and optionally sets a notice reason. */
  logout: (reason?: SessionNotice) => void
  /** Reloads the user profile for the current token. */
  refreshProfile: () => Promise<void>
}

/**
 * Authentication context. Use {@link useAuth} for consumption.
 */
export const AuthContext = createContext<AuthContextValue | undefined>(undefined)