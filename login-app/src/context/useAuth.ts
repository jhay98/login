import { useContext } from 'react'
import { AuthContext } from './auth-context'

/**
 * Returns authentication context value.
 *
 * @throws Error If used outside {@link AuthProvider}.
 */
export function useAuth() {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }

  return context
}