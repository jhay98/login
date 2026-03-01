import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { ApiError, authApi } from '../lib/api'
import { isTokenExpired } from '../lib/token'
import type { LoginRequest, RegisterRequest, User } from '../types/auth'
import { AuthContext, type AuthContextValue, type SessionNotice } from './auth-context'

const SESSION_TOKEN_KEY = 'auth_token'

function persistToken(token: string | null) {
  if (!token) {
    sessionStorage.removeItem(SESSION_TOKEN_KEY)
    return
  }

  sessionStorage.setItem(SESSION_TOKEN_KEY, token)
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [token, setToken] = useState<string | null>(null)
  const [isInitializing, setIsInitializing] = useState(true)
  const [sessionNotice, setSessionNotice] = useState<SessionNotice>(null)

  const logout = useCallback((reason: SessionNotice = null) => {
    setUser(null)
    setToken(null)
    setSessionNotice(reason)
    persistToken(null)
  }, [])

  const refreshProfileForToken = useCallback(async (activeToken: string) => {
    if (isTokenExpired(activeToken)) {
      logout('expired')
      return
    }

    try {
      const me = await authApi.me(activeToken)
      setUser(me)
      setToken(activeToken)
      persistToken(activeToken)
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout('expired')
        return
      }

      throw error
    }
  }, [logout])

  const refreshProfile = useCallback(async () => {
    if (!token) {
      logout(null)
      return
    }

    await refreshProfileForToken(token)
  }, [logout, refreshProfileForToken, token])

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
  }, [logout])

  const clearSessionNotice = useCallback(() => {
    setSessionNotice(null)
  }, [])

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
      clearSessionNotice,
      login,
      register,
      logout,
      refreshProfile,
    }),
    [clearSessionNotice, isInitializing, login, logout, refreshProfile, register, sessionNotice, token, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}