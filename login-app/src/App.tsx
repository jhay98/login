import { Navigate, Route, Routes, useNavigate } from 'react-router-dom'
import './App.css'
import { PrivateRoute } from './components/PrivateRoute'
import { SessionTimeoutDialog } from './components/SessionTimeoutDialog'
import { useAuth } from './context/useAuth'
import { LoginPage } from './pages/LoginPage'
import { ProfilePage } from './pages/ProfilePage'
import { RegisterPage } from './pages/RegisterPage'

/**
 * Redirects the root route to either login or profile depending on auth state.
 */
function HomeRedirect() {
  const { isAuthenticated, isInitializing } = useAuth()

  if (isInitializing) {
    return <div className="page"><p>Loading...</p></div>
  }

  return <Navigate to={isAuthenticated ? '/profile' : '/login'} replace />
}

/**
 * Root application router.
 */
function App() {
  const navigate = useNavigate()
  const { sessionNotice, acknowledgeSessionNotice } = useAuth()

  return (
    <>
      <Routes>
        <Route path="/" element={<HomeRedirect />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/profile"
          element={
            <PrivateRoute>
              <ProfilePage />
            </PrivateRoute>
          }
        />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>

      {sessionNotice === 'expired' && (
        <SessionTimeoutDialog
          onOk={() => {
            acknowledgeSessionNotice()
            navigate('/login', { replace: true })
          }}
        />
      )}
    </>
  )
}

export default App
