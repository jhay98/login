interface SessionTimeoutDialogProps {
  onOk: () => void
}

/**
 * Modal dialog displayed when the current authenticated session has timed out.
 */
export function SessionTimeoutDialog({ onOk }: SessionTimeoutDialogProps) {
  return (
    <div className="session-timeout-overlay" role="presentation">
      <section className="session-timeout-modal" role="dialog" aria-modal="true" aria-labelledby="session-timeout-title">
        <h2 id="session-timeout-title">Session timed out</h2>
        <p>Your session has timed out. Please sign in again.</p>
        <div className="session-timeout-actions">
          <button type="button" onClick={onOk}>OK</button>
        </div>
      </section>
    </div>
  )
}
