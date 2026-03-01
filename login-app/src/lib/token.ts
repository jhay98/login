/**
 * Decodes a base64url-encoded JWT segment.
 */
function decodeBase64Url(value: string): string {
  const normalized = value.replace(/-/g, '+').replace(/_/g, '/')
  const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=')
  return atob(padded)
}

/**
 * Extracts JWT expiration as a Unix epoch value in milliseconds.
 */
export function getTokenExpiryEpochMs(token: string): number | null {
  try {
    const parts = token.split('.')
    if (parts.length !== 3) {
      return null
    }

    const payload = JSON.parse(decodeBase64Url(parts[1])) as { exp?: number }
    if (!payload.exp) {
      return null
    }

    return payload.exp * 1000
  } catch {
    return null
  }
}

/**
 * Indicates whether a JWT token is expired or invalid.
 */
export function isTokenExpired(token: string, skewSeconds = 10): boolean {
  const exp = getTokenExpiryEpochMs(token)
  if (!exp) {
    return true
  }

  const now = Date.now()
  return now >= exp - skewSeconds * 1000
}