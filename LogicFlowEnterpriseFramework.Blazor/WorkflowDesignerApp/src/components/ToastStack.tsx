type ToastStackProps = {
  error?: string
  notice?: string
  onDismissError?: () => void
  onDismissNotice?: () => void
}

export function ToastStack({ error = '', notice = '', onDismissError, onDismissNotice }: ToastStackProps) {
  if (!notice && !error) {
    return null
  }

  return (
    <div className="toast-stack">
      {notice && (
        <div className="notice toast-notification" role="status" aria-live="polite">
          <div className="toast-notification-copy">
            <strong>Success</strong>
            <span>{notice}</span>
          </div>
          <button className="toast-close-button" type="button" aria-label="Close success notification" onClick={onDismissNotice}>
            <CloseIcon />
          </button>
        </div>
      )}
      {error && (
        <div className="error toast-notification" role="alert" aria-live="assertive">
          <div className="toast-notification-copy">
            <strong>Failed</strong>
            <span>{error}</span>
          </div>
          <button className="toast-close-button" type="button" aria-label="Close error notification" onClick={onDismissError}>
            <CloseIcon />
          </button>
        </div>
      )}
    </div>
  )
}

function CloseIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round">
      <path d="M4 4l8 8" />
      <path d="M12 4 4 12" />
    </svg>
  )
}
