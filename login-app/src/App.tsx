import { Navigate, Route, Routes } from 'react-router-dom'
import './App.css'
import { PrivateRoute } from './components/PrivateRoute'
import { useAuth } from './context/AuthContext'
import { LoginPage } from './pages/LoginPage'
import { ProfilePage } from './pages/ProfilePage'
import { RegisterPage } from './pages/RegisterPage'

function HomeRedirect() {
  const { isAuthenticated, isInitializing } = useAuth()

  if (isInitializing) {
    return <div className="page"><p>Loading...</p></div>
  }

  return <Navigate to={isAuthenticated ? '/profile' : '/login'} replace />
}

function App() {
  return (
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
  )
}

export default App
