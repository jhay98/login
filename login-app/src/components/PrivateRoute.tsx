import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../context/useAuth'

/**
 * Props for {@link PrivateRoute}.
 */
interface PrivateRouteProps {
  /** Content rendered when authentication succeeds. */
  children: ReactNode
}

/**
 * Protects child routes and redirects unauthenticated users to login.
 */
export function PrivateRoute({ children }: PrivateRouteProps) {
  const { isAuthenticated, isInitializing } = useAuth()
  const location = useLocation()

  if (isInitializing) {
    return <div className="page"><p>Loading...</p></div>
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  return <>{children}</>
}