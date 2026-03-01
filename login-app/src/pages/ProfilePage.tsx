import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError, authApi } from '../lib/api'
import { useAuth } from '../context/useAuth'
import type { User } from '../types/auth'

/**
 * Protected profile page for viewing and refreshing current user data.
 */
export function ProfilePage() {
  const navigate = useNavigate()
  const { user, token, logout, refreshProfile } = useAuth()
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [users, setUsers] = useState<User[]>([])
  const [isLoadingUsers, setIsLoadingUsers] = useState(false)
  const [usersError, setUsersError] = useState<string | null>(null)

  const isAdmin = Boolean(
    user?.roles?.some((role) => role.toLowerCase() === 'admin'),
  )

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

  useEffect(() => {
    if (!user || !token || !isAdmin) {
      setUsers([])
      setUsersError(null)
      return
    }

    const loadUsers = async () => {
      setIsLoadingUsers(true)
      setUsersError(null)

      try {
        const response = await authApi.users(token)
        setUsers(response.data)
      } catch (err) {
        if (err instanceof ApiError) {
          setUsersError(err.message)
        } else {
          setUsersError('Unable to load users right now.')
        }
      } finally {
        setIsLoadingUsers(false)
      }
    }

    void loadUsers()
  }, [isAdmin, token, user])

  return (
    <main className="page profile-page">
      <section className={`card ${isAdmin ? 'wide' : ''}`.trim()}>
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
            <div>
              <dt>Roles</dt>
              <dd>{user.roles.join(', ') || 'None'}</dd>
            </div>
          </dl>
        )}

        {isAdmin && (
          <section className="admin-users">
            <h2>All users</h2>
            <p className="muted small">Visible to admins only.</p>

            {isLoadingUsers && <p>Loading users...</p>}
            {usersError && <p className="error">{usersError}</p>}

            {!isLoadingUsers && !usersError && (
              <>
                {users.length === 0 ? (
                  <p className="muted">No users found.</p>
                ) : (
                  <div className="users-table-wrap">
                    <table className="users-table">
                      <thead>
                        <tr>
                          <th>ID</th>
                          <th>Name</th>
                          <th>Email</th>
                          <th>Roles</th>
                        </tr>
                      </thead>
                      <tbody>
                        {users.map((listedUser) => (
                          <tr key={listedUser.id}>
                            <td>{listedUser.id}</td>
                            <td>{listedUser.firstName} {listedUser.lastName}</td>
                            <td>{listedUser.email}</td>
                            <td>{listedUser.roles.join(', ') || 'None'}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}

                <button
                  type="button"
                  onClick={async () => {
                    if (!token) {
                      return
                    }

                    setIsLoadingUsers(true)
                    setUsersError(null)

                    try {
                      const response = await authApi.users(token)
                      setUsers(response.data)
                    } catch (err) {
                      if (err instanceof ApiError) {
                        setUsersError(err.message)
                      } else {
                        setUsersError('Unable to refresh user list.')
                      }
                    } finally {
                      setIsLoadingUsers(false)
                    }
                  }}
                  disabled={isLoadingUsers}
                >
                  {isLoadingUsers ? 'Refreshing users...' : 'Refresh users'}
                </button>
              </>
            )}
          </section>
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