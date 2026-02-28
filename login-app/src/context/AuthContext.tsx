import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { ApiError, authApi } from '../lib/api'
import { isTokenExpired } from '../lib/token'
import type { LoginRequest, RegisterRequest, User } from '../types/auth'

const SESSION_TOKEN_KEY = 'auth_token'

type SessionNotice = 'expired' | null

interface AuthContextValue {
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

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

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

  function logout(reason: SessionNotice = null) {
    setUser(null)
    setToken(null)
    setSessionNotice(reason)
    persistToken(null)
  }

  async function refreshProfile(nextToken?: string) {
    const activeToken = nextToken ?? token

    if (!activeToken) {
      logout(null)
      return
    }

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
  }

  async function login(payload: LoginRequest) {
    const response = await authApi.login(payload)

    if (isTokenExpired(response.token)) {
      logout('expired')
      throw new ApiError(401, 'Received an expired session token. Please log in again.')
    }

    setToken(response.token)
    setUser(response.user)
    setSessionNotice(null)
    persistToken(response.token)
  }

  function clearSessionNotice() {
    setSessionNotice(null)
  }

  useEffect(() => {
    const initializeAuth = async () => {
      const storedToken = sessionStorage.getItem(SESSION_TOKEN_KEY)

      if (!storedToken) {
        setIsInitializing(false)
        return
      }

      try {
        await refreshProfile(storedToken)
      } catch {
        logout(null)
      } finally {
        setIsInitializing(false)
      }
    }

    void initializeAuth()
  }, [])

  async function register(payload: RegisterRequest) {
    return authApi.register(payload)
  }

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
      refreshProfile: async () => refreshProfile(),
    }),
    [isInitializing, sessionNotice, token, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }

  return context
}