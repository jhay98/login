import { beforeEach, describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { RegisterPage } from './RegisterPage'

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

describe('RegisterPage integration', () => {
  beforeEach(() => {
    mockUseAuth.mockReset()
    mockNavigate.mockReset()
  })

  it('validates required fields and blocks submission', async () => {
    const register = vi.fn().mockResolvedValue(undefined)

    mockUseAuth.mockReturnValue({ register })

    render(
      <MemoryRouter initialEntries={['/register']}>
        <Routes>
          <Route path="/register" element={<RegisterPage />} />
        </Routes>
      </MemoryRouter>,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Create account' }))

    expect(await screen.findByText('First name is required.')).toBeInTheDocument()
    expect(screen.getByText('Last name is required.')).toBeInTheDocument()
    expect(screen.getByText('Email is required.')).toBeInTheDocument()
    expect(screen.getByText('Password is required.')).toBeInTheDocument()
    expect(screen.getByText('Please confirm your password.')).toBeInTheDocument()
    expect(register).not.toHaveBeenCalled()
  })

  it('submits normalized payload and redirects to login with success state', async () => {
    const register = vi.fn().mockResolvedValue({
      id: 1,
      firstName: 'John',
      lastName: 'Doe',
      email: 'john@example.com',
      createdAt: '2026-03-01T00:00:00Z',
    })

    mockUseAuth.mockReturnValue({ register })

    render(
      <MemoryRouter initialEntries={['/register']}>
        <Routes>
          <Route path="/register" element={<RegisterPage />} />
        </Routes>
      </MemoryRouter>,
    )

    fireEvent.change(screen.getByLabelText('First name'), { target: { value: '  John  ' } })
    fireEvent.change(screen.getByLabelText('Last name'), { target: { value: '  Doe  ' } })
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: '  JOHN@Example.COM  ' } })
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'Password1!' } })
    fireEvent.change(screen.getByLabelText('Confirm password'), { target: { value: 'Password1!' } })

    fireEvent.click(screen.getByRole('button', { name: 'Create account' }))

    await waitFor(() => {
      expect(register).toHaveBeenCalledWith({
        firstName: 'John',
        lastName: 'Doe',
        email: 'john@example.com',
        password: 'Password1!',
      })
    })

    expect(mockNavigate).toHaveBeenCalledWith('/login', {
      replace: true,
      state: { registered: true },
    })
  })
})
