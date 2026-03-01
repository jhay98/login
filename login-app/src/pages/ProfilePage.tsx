import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '../lib/api'
import { useAuth } from '../context/useAuth'

/**
 * Protected profile page for viewing and refreshing current user data.
 */
export function ProfilePage() {
  const navigate = useNavigate()
  const { user, logout, refreshProfile } = useAuth()
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (user) {
      return
    }

    const load = async () => {
      setIsRefreshing(true)
      setError(null)

      try {
        await refreshProfile()
      } catch (err) {
        if (err instanceof ApiError) {
          setError(err.message)
        } else {
          setError('Unable to load profile right now.')
        }
      } finally {
        setIsRefreshing(false)
      }
    }

    void load()
  }, [refreshProfile, user])

  return (
    <main className="page profile-page">
      <section className="card">
        <h1>User profile</h1>
        <p className="muted">This page is protected.</p>

        {isRefreshing && <p>Loading profile...</p>}
        {error && <p className="error">{error}</p>}

        {user && (
          <dl className="profile-grid">
            <div>
              <dt>ID</dt>
              <dd>{user.id}</dd>
            </div>
            <div>
              <dt>First name</dt>
              <dd>{user.firstName}</dd>
            </div>
            <div>
              <dt>Last name</dt>
              <dd>{user.lastName}</dd>
            </div>
            <div>
              <dt>Email</dt>
              <dd>{user.email}</dd>
            </div>
          </dl>
        )}

        <div className="actions">
          <button
            type="button"
            onClick={async () => {
              setIsRefreshing(true)
              setError(null)

              try {
                await refreshProfile()
              } catch (err) {
                if (err instanceof ApiError) {
                  setError(err.message)
                } else {
                  setError('Unable to refresh profile.')
                }
              } finally {
                setIsRefreshing(false)
              }
            }}
            disabled={isRefreshing}
          >
            {isRefreshing ? 'Refreshing...' : 'Refresh'}
          </button>

          <button
            type="button"
            className="secondary"
            onClick={() => {
              logout(null)
              navigate('/login', { replace: true })
            }}
          >
            Log out
          </button>
        </div>
      </section>
    </main>
  )
}