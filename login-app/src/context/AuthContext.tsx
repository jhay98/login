import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { ApiError, authApi } from '../lib/api'
import { isTokenExpired } from '../lib/token'
import type { LoginRequest, RegisterRequest, User } from '../types/auth'
import { AuthContext, type AuthContextValue, type SessionNotice } from './auth-context'

/** Storage key used for persisting the JWT between refreshes. */
const SESSION_TOKEN_KEY = 'auth_token'

/**
 * Persists or removes the session token in browser session storage.
 */
function persistToken(token: string | null) {
  if (!token) {
    sessionStorage.removeItem(SESSION_TOKEN_KEY)
    return
  }

  sessionStorage.setItem(SESSION_TOKEN_KEY, token)
}

/**
 * Props for {@link AuthProvider}.
 */
interface AuthProviderProps {
  /** Child elements that consume authentication state. */
  children: ReactNode
}

/**
 * Provides authentication state and actions to descendants.
 */
export function AuthProvider({ children }: AuthProviderProps) {
  const [user, setUser] = useState<User | null>(null)
  const [token, setToken] = useState<string | null>(null)
  const [isInitializing, setIsInitializing] = useState(true)
  const [sessionNotice, setSessionNotice] = useState<SessionNotice>(null)

  /**
   * Clears session state and optionally records a session notice reason.
   */
  const logout = useCallback((reason: SessionNotice = null) => {
    setUser(null)
    setToken(null)
    setSessionNotice(reason)
    persistToken(null)
  }, [])

  /**
   * Loads profile information for a token and normalizes invalid sessions.
   */
  const refreshProfileForToken = useCallback(async (activeToken: string) => {
    if (isTokenExpired(activeToken)) {
      logout('expired')
      return
    }

    try {
      const refreshed = await authApi.me(activeToken)

      if (isTokenExpired(refreshed.token)) {
        logout('expired')
        return
      }

      setUser(refreshed.data)
      setToken(refreshed.token)
      setSessionNotice(null)
      persistToken(refreshed.token)
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout('expired')
        return
      }

      throw error
    }
  }, [logout])

  /**
   * Refreshes profile information for the active token.
   */
  const refreshProfile = useCallback(async () => {
    if (!token) {
      return
    }

    await refreshProfileForToken(token)
  }, [refreshProfileForToken, token])

  /**
   * Signs the user in and persists the received token.
   */
  const login = useCallback(async (payload: LoginRequest) => {
    const response = await authApi.login(payload)

    if (isTokenExpired(response.token)) {
      logout('expired')
      throw new ApiError(401, 'Received an expired session token. Please log in again.')
    }

    setToken(response.token)
    setUser(response.user)
    setSessionNotice(null)
    persistToken(response.token)

    void authApi.recordActivity(response.token, {
      userId: response.user.id,
      eventType: 'user_login',
      userAgent: navigator.userAgent,
      metadata: 'source=login-app',
    }).catch(() => {
      // Do not block login when activity capture fails.
    })
  }, [logout])

  /**
   * Clears one-time session notice state.
   */
  const acknowledgeSessionNotice = useCallback(() => {
    setSessionNotice(null)
  }, [])

  /**
   * Registers a new user account.
   */
  const register = useCallback(async (payload: RegisterRequest) => {
    return authApi.register(payload)
  }, [])

  useEffect(() => {
    const initializeAuth = async () => {
      const storedToken = sessionStorage.getItem(SESSION_TOKEN_KEY)

      if (!storedToken) {
        setIsInitializing(false)
        return
      }

      try {
        await refreshProfileForToken(storedToken)
      } catch {
        logout(null)
      } finally {
        setIsInitializing(false)
      }
    }

    void initializeAuth()
  }, [logout, refreshProfileForToken])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      isAuthenticated: Boolean(token && user),
      isInitializing,
      sessionNotice,
      acknowledgeSessionNotice,
      login,
      register,
      logout,
      refreshProfile,
    }),
    [acknowledgeSessionNotice, isInitializing, login, logout, refreshProfile, register, sessionNotice, token, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}