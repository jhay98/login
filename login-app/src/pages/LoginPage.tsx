import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { ApiError } from '../lib/api'
import { useAuth } from '../context/useAuth'

function validateEmail(email: string): string | null {
  if (!email.trim()) {
    return 'Email is required.'
  }

  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  if (!emailRegex.test(email)) {
    return 'Enter a valid email address.'
  }

  return null
}

type LoginLocationState = {
  from?: {
    pathname?: string
  }
  registered?: boolean
}

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { login, sessionNotice, clearSessionNotice, isAuthenticated } = useAuth()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [emailError, setEmailError] = useState<string | null>(null)
  const [passwordError, setPasswordError] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const state = (location.state ?? {}) as LoginLocationState
  const destination = state.from?.pathname || '/profile'

  const sessionMessage = useMemo(() => {
    if (sessionNotice === 'expired') {
      return 'Your session expired. Please sign in again.'
    }
    return null
  }, [sessionNotice])

  useEffect(() => {
    if (!sessionNotice) {
      return
    }

    clearSessionNotice()
  }, [clearSessionNotice, sessionNotice])

  useEffect(() => {
    if (isAuthenticated) {
      navigate('/profile', { replace: true })
    }
  }, [isAuthenticated, navigate])

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setFormError(null)

    const normalizedEmail = email.trim().toLowerCase()
    const nextEmailError = validateEmail(normalizedEmail)
    const nextPasswordError = password ? null : 'Password is required.'

    setEmailError(nextEmailError)
    setPasswordError(nextPasswordError)

    if (nextEmailError || nextPasswordError) {
      return
    }

    setIsSubmitting(true)
    try {
      await login({ email: normalizedEmail, password })
      navigate(destination, { replace: true })
    } catch (error) {
      if (error instanceof ApiError) {
        setFormError(error.message)
      } else {
        setFormError('Unable to sign in right now. Please try again.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="page auth-page">
      <section className="card">
        <h1>Login</h1>
        <p className="muted">Sign in to continue.</p>

        {state.registered && <p className="success">Registration successful. Please log in.</p>}
        {sessionMessage && <p className="error">{sessionMessage}</p>}
        {formError && <p className="error">{formError}</p>}

        <form onSubmit={onSubmit} noValidate>
          <label htmlFor="email">Email</label>
          <input
            id="email"
            type="email"
            autoComplete="email"
            value={email}
            onChange={(e) => {
              setEmail(e.target.value)
              setEmailError(null)
            }}
            disabled={isSubmitting}
          />
          {emailError && <p className="field-error">{emailError}</p>}

          <label htmlFor="password">Password</label>
          <input
            id="password"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => {
              setPassword(e.target.value)
              setPasswordError(null)
            }}
            disabled={isSubmitting}
          />
          {passwordError && <p className="field-error">{passwordError}</p>}

          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Signing in...' : 'Sign in'}
          </button>
        </form>

        <p className="muted small">
          No account? <Link to="/register">Create one</Link>
        </p>
      </section>
    </main>
  )
}