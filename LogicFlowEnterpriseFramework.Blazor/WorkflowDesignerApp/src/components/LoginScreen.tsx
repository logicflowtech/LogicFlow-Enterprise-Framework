import { useState } from 'react'
import { ToastStack } from './ToastStack'

type LoginScreenProps = {
  busy: boolean
  error: string
  onLogin: (userName: string, password: string) => Promise<void>
}

export function LoginScreen({ busy, error, onLogin }: LoginScreenProps) {
  const [userName, setUserName] = useState('superadmin')
  const [password, setPassword] = useState('Admin@123')

  return (
    <main className="login-shell">
      <ToastStack error={error} />
      <section className="login-panel">
        <div className="login-branding">
          <div className="login-eyebrow">Enterprise workflow platform</div>
          <h1>LogicFlow Control Center</h1>
          <p>Sign in to administer workflows, review operational tasks, and manage enterprise routing controls.</p>
        </div>

        <div className="login-card">
          <div className="login-card-header">
            <strong>Secure sign in</strong>
            <span>Seeded administrator access is ready for first-time setup.</span>
          </div>

          <form
            className="login-form"
            onSubmit={(event) => {
              event.preventDefault()
              void onLogin(userName, password)
            }}
          >
            <label className="field">
              <span>Username</span>
              <input value={userName} onChange={(event) => setUserName(event.target.value)} autoComplete="username" />
            </label>
            <label className="field">
              <span>Password</span>
              <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} autoComplete="current-password" />
            </label>
            <button className="primary-action login-submit-button" type="submit" disabled={busy || !userName.trim() || !password.trim()}>
              Sign in
            </button>
          </form>

          <div className="login-seed-note">
            <strong>Seeded super admin</strong>
            <span>User name `superadmin` with initial password `Admin@123` is created automatically.</span>
          </div>
        </div>
      </section>
    </main>
  )
}
