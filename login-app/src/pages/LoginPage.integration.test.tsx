import { beforeEach, describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { LoginPage } from './LoginPage'

const { mockUseAuth, mockNavigate } = vi.hoisted(() => ({
  mockUseAuth: vi.fn(),
  mockNavigate: vi.fn(),
}))

vi.mock('../context/useAuth', () => ({
  useAuth: () => mockUseAuth(),
}))

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

type AuthState = {
  login: (payload: { email: string; password: string }) => Promise<void>
  isAuthenticated: boolean
}

function renderLogin(state?: object) {
  render(
    <MemoryRouter initialEntries={[{ pathname: '/login', state }]}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('LoginPage integration', () => {
  beforeEach(() => {
    mockUseAuth.mockReset()
    mockNavigate.mockReset()
  })

  it('shows field-level validation errors before submit', async () => {
    const login = vi.fn().mockResolvedValue(undefined)

    const authState: AuthState = {
      login,
      isAuthenticated: false,
    }

    mockUseAuth.mockReturnValue(authState)
    renderLogin()

    fireEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByText('Email is required.')).toBeInTheDocument()
    expect(screen.getByText('Password is required.')).toBeInTheDocument()
    expect(login).not.toHaveBeenCalled()
  })

  it('submits normalized credentials and navigates to prior destination', async () => {
    const login = vi.fn().mockResolvedValue(undefined)

    mockUseAuth.mockReturnValue({
      login,
      isAuthenticated: false,
    } as AuthState)

    renderLogin({ from: { pathname: '/profile' } })

    fireEvent.change(screen.getByLabelText('Email'), {
      target: { value: '  USER@Example.COM  ' },
    })
    fireEvent.change(screen.getByLabelText('Password'), {
      target: { value: 'Secret123!' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    await waitFor(() => {
      expect(login).toHaveBeenCalledWith({
        email: 'user@example.com',
        password: 'Secret123!',
      })
    })

    expect(mockNavigate).toHaveBeenCalledWith('/profile', { replace: true })
  })
})
