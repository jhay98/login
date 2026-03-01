import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ApiError } from '../lib/api'
import { useAuth } from '../context/useAuth'

/**
 * Local register form state.
 */
interface FormState {
  firstName: string
  lastName: string
  email: string
  password: string
  confirmPassword: string
}

/**
 * Validation errors keyed by field name.
 */
interface FormErrors {
  firstName?: string
  lastName?: string
  email?: string
  password?: string
  confirmPassword?: string
}

/**
 * Performs client-side registration form validation.
 */
function validate(form: FormState): FormErrors {
  const errors: FormErrors = {}
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

  if (!form.firstName.trim()) errors.firstName = 'First name is required.'
  if (!form.lastName.trim()) errors.lastName = 'Last name is required.'

  if (!form.email.trim()) {
    errors.email = 'Email is required.'
  } else if (!emailRegex.test(form.email.trim())) {
    errors.email = 'Enter a valid email address.'
  }

  if (!form.password) {
    errors.password = 'Password is required.'
  } else {
    if (form.password.length < 8) errors.password = 'Password must be at least 8 characters.'
    if (!/[A-Z]/.test(form.password)) errors.password = 'Password must include an uppercase letter.'
    if (!/[a-z]/.test(form.password)) errors.password = 'Password must include a lowercase letter.'
    if (!/[0-9]/.test(form.password)) errors.password = 'Password must include a number.'
    if (!/[^A-Za-z0-9]/.test(form.password)) errors.password = 'Password must include a special character.'
  }

  if (!form.confirmPassword) {
    errors.confirmPassword = 'Please confirm your password.'
  } else if (form.password !== form.confirmPassword) {
    errors.confirmPassword = 'Passwords do not match.'
  }

  return errors
}

/**
 * Account registration page.
 */
export function RegisterPage() {
  const navigate = useNavigate()
  const { register } = useAuth()

  const [form, setForm] = useState<FormState>({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    confirmPassword: '',
  })
  const [errors, setErrors] = useState<FormErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  /**
   * Handles registration form submission.
   */
  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setFormError(null)

    const nextErrors = validate(form)
    setErrors(nextErrors)

    if (Object.keys(nextErrors).length > 0) {
      return
    }

    setIsSubmitting(true)
    try {
      await register({
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        email: form.email.trim().toLowerCase(),
        password: form.password,
      })

      navigate('/login', { replace: true, state: { registered: true } })
    } catch (error) {
      if (error instanceof ApiError) {
        setFormError(error.message)
      } else {
        setFormError('Unable to register right now. Please try again.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="page auth-page">
      <section className="card">
        <h1>Register</h1>
        <p className="muted">Create your account.</p>

        {formError && <p className="error">{formError}</p>}

        <form onSubmit={onSubmit} noValidate>
          <label htmlFor="firstName">First name</label>
          <input
            id="firstName"
            value={form.firstName}
            onChange={(e) => {
              setForm((prev) => ({ ...prev, firstName: e.target.value }))
              setErrors((prev) => ({ ...prev, firstName: undefined }))
            }}
            disabled={isSubmitting}
          />
          {errors.firstName && <p className="field-error">{errors.firstName}</p>}

          <label htmlFor="lastName">Last name</label>
          <input
            id="lastName"
            value={form.lastName}
            onChange={(e) => {
              setForm((prev) => ({ ...prev, lastName: e.target.value }))
              setErrors((prev) => ({ ...prev, lastName: undefined }))
            }}
            disabled={isSubmitting}
          />
          {errors.lastName && <p className="field-error">{errors.lastName}</p>}

          <label htmlFor="email">Email</label>
          <input
            id="email"
            type="email"
            autoComplete="email"
            value={form.email}
            onChange={(e) => {
              setForm((prev) => ({ ...prev, email: e.target.value }))
              setErrors((prev) => ({ ...prev, email: undefined }))
            }}
            disabled={isSubmitting}
          />
          {errors.email && <p className="field-error">{errors.email}</p>}

          <label htmlFor="password">Password</label>
          <input
            id="password"
            type="password"
            autoComplete="new-password"
            value={form.password}
            onChange={(e) => {
              setForm((prev) => ({ ...prev, password: e.target.value }))
              setErrors((prev) => ({ ...prev, password: undefined }))
            }}
            disabled={isSubmitting}
          />
          {errors.password && <p className="field-error">{errors.password}</p>}

          <label htmlFor="confirmPassword">Confirm password</label>
          <input
            id="confirmPassword"
            type="password"
            autoComplete="new-password"
            value={form.confirmPassword}
            onChange={(e) => {
              setForm((prev) => ({ ...prev, confirmPassword: e.target.value }))
              setErrors((prev) => ({ ...prev, confirmPassword: undefined }))
            }}
            disabled={isSubmitting}
          />
          {errors.confirmPassword && <p className="field-error">{errors.confirmPassword}</p>}

          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Creating account...' : 'Create account'}
          </button>
        </form>

        <p className="muted small">
          Already have an account? <Link to="/login">Sign in</Link>
        </p>
      </section>
    </main>
  )
}