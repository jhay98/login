import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError, authApi } from '../lib/api'
import { useAuth } from '../context/useAuth'
import type { ActivityEvent, User } from '../types/auth'

/**
 * Formats an ISO UTC date string into local date/time text.
 */
function formatDateTime(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  return date.toLocaleString()
}

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
  const [activities, setActivities] = useState<ActivityEvent[]>([])
  const [isLoadingActivities, setIsLoadingActivities] = useState(false)
  const [activitiesError, setActivitiesError] = useState<string | null>(null)
  const hasRecordedProfileViewRef = useRef(false)

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
      setActivities([])
      setActivitiesError(null)
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

  useEffect(() => {
    if (!user || !token || !isAdmin) {
      return
    }

    const loadRecentActivity = async () => {
      setIsLoadingActivities(true)
      setActivitiesError(null)

      try {
        const response = await authApi.recentActivity(token, 25)
        setActivities(response.data)
      } catch (err) {
        if (err instanceof ApiError) {
          setActivitiesError(err.message)
        } else {
          setActivitiesError('Unable to load activity right now.')
        }
      } finally {
        setIsLoadingActivities(false)
      }
    }

    void loadRecentActivity()
  }, [isAdmin, token, user])

  useEffect(() => {
    if (!user || !token || hasRecordedProfileViewRef.current) {
      return
    }

    hasRecordedProfileViewRef.current = true

    void authApi.recordActivity(token, {
      userId: user.id,
      eventType: 'profile_viewed',
      userAgent: navigator.userAgent,
      metadata: isAdmin ? 'source=login-app;role=admin' : 'source=login-app',
    }).catch(() => {
      // Ignore activity capture errors in UI flow.
    })
  }, [isAdmin, token, user])

  return (
    <main className="page profile-page">
      <section className={`card ${isAdmin ? 'wide' : ''}`.trim()}>
        <h1>User profile</h1>
        <p className="muted">This page is protected.</p>

        {isRefreshing && <p>Loading profile...</p>}
        {error && <p className="error">{error}</p>}

        {user && (
          <section className="profile-section">
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
          </section>
        )}

        {isAdmin && (
          <>
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

            <section className="admin-activity">
              <h2>Recent activity</h2>
              <p className="muted small">Last 25 events. Visible to admins only.</p>

              {isLoadingActivities && <p>Loading activity...</p>}
              {activitiesError && <p className="error">{activitiesError}</p>}

              {!isLoadingActivities && !activitiesError && (
                <>
                  {activities.length === 0 ? (
                    <p className="muted">No recent activity found.</p>
                  ) : (
                    <ul className="activity-list" aria-label="Recent activity">
                      {activities.map((activity) => (
                        <li key={activity.id}>
                          <div className="activity-row-top">
                            <span className="activity-event">{activity.eventType}</span>
                            <span className="muted small">{formatDateTime(activity.occurredAtUtc)}</span>
                          </div>
                          <p className="muted small">
                            User #{activity.userId}
                            {activity.metadata ? ` · ${activity.metadata}` : ''}
                          </p>
                        </li>
                      ))}
                    </ul>
                  )}

                  <button
                    type="button"
                    onClick={async () => {
                      if (!token) {
                        return
                      }

                      setIsLoadingActivities(true)
                      setActivitiesError(null)

                      try {
                        const response = await authApi.recentActivity(token, 25)
                        setActivities(response.data)
                      } catch (err) {
                        if (err instanceof ApiError) {
                          setActivitiesError(err.message)
                        } else {
                          setActivitiesError('Unable to refresh activity list.')
                        }
                      } finally {
                        setIsLoadingActivities(false)
                      }
                    }}
                    disabled={isLoadingActivities}
                  >
                    {isLoadingActivities ? 'Refreshing activity...' : 'Refresh activity'}
                  </button>
                </>
              )}
            </section>
          </>
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
            onClick={async () => {
              if (token && user) {
                try {
                  await authApi.recordActivity(token, {
                    userId: user.id,
                    eventType: 'user_logout',
                    userAgent: navigator.userAgent,
                    metadata: 'source=login-app',
                  })
                } catch {
                  // Ignore activity capture errors in UI flow.
                }
              }

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