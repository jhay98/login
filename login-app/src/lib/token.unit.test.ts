import { describe, expect, it, vi, afterEach } from 'vitest'
import { getTokenExpiryEpochMs, isTokenExpired } from './token'

function toBase64Url(input: string): string {
  return btoa(input).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '')
}

function buildToken(payload: object): string {
  const header = toBase64Url(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))
  const body = toBase64Url(JSON.stringify(payload))
  return `${header}.${body}.signature`
}

describe('token helpers', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('extracts token expiry from a valid JWT payload', () => {
    const token = buildToken({ exp: 1_800_000_000 })

    expect(getTokenExpiryEpochMs(token)).toBe(1_800_000_000_000)
  })

  it('returns null when token format is invalid', () => {
    expect(getTokenExpiryEpochMs('invalid-token')).toBeNull()
  })

  it('returns true for expired tokens', () => {
    const now = 1_700_000_000_000
    vi.spyOn(Date, 'now').mockReturnValue(now)

    const token = buildToken({ exp: Math.floor((now - 30_000) / 1000) })

    expect(isTokenExpired(token)).toBe(true)
  })

  it('returns false for non-expired tokens', () => {
    const now = 1_700_000_000_000
    vi.spyOn(Date, 'now').mockReturnValue(now)

    const token = buildToken({ exp: Math.floor((now + 120_000) / 1000) })

    expect(isTokenExpired(token)).toBe(false)
  })
})
