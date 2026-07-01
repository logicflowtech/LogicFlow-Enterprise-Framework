import { useEffect, useState } from 'react'
import type { PlatformEmailSettings, UpdatePlatformEmailSettingsInput } from '../api'

type PlatformModuleKey =
  | 'email'
  | 'notification-defaults'
  | 'branding'
  | 'authentication'
  | 'runtime'
  | 'audit'

type PlatformSettingsProps = {
  busy: boolean
  emailSettings: PlatformEmailSettings | null
  onRefresh: () => void
  onSaveEmailSettings: (payload: UpdatePlatformEmailSettingsInput) => void
}

type EmailFormState = {
  isEnabled: boolean
  host: string
  port: string
  userName: string
  password: string
  enableSsl: boolean
  senderName: string
  senderEmail: string
  replyToEmail: string
  timeoutSeconds: string
}

const emptyForm: EmailFormState = {
  isEnabled: false,
  host: '',
  port: '587',
  userName: '',
  password: '',
  enableSsl: true,
  senderName: '',
  senderEmail: '',
  replyToEmail: '',
  timeoutSeconds: '30',
}

const platformModules: Array<{
  key: PlatformModuleKey
  category: string
  title: string
  description: string
  status: 'Live' | 'Planned'
}> = [
  {
    key: 'email',
    category: 'Delivery',
    title: 'Email Engine',
    description: 'SMTP delivery, sender defaults, and transport security.',
    status: 'Live',
  },
  {
    key: 'notification-defaults',
    category: 'Delivery',
    title: 'Notification Defaults',
    description: 'Default channels, templates, and reminder behavior.',
    status: 'Planned',
  },
  {
    key: 'branding',
    category: 'Experience',
    title: 'Branding',
    description: 'Mail identity, logos, product name, and visual defaults.',
    status: 'Planned',
  },
  {
    key: 'authentication',
    category: 'Security',
    title: 'Authentication',
    description: 'Session policies, password standards, and SSO controls.',
    status: 'Planned',
  },
  {
    key: 'runtime',
    category: 'Operations',
    title: 'Runtime Controls',
    description: 'Execution defaults, retries, throttling, and engine behavior.',
    status: 'Planned',
  },
  {
    key: 'audit',
    category: 'Operations',
    title: 'Audit & Retention',
    description: 'Log retention, archival policy, and platform trace settings.',
    status: 'Planned',
  },
]

export function PlatformSettings({ busy, emailSettings, onRefresh, onSaveEmailSettings }: PlatformSettingsProps) {
  const [form, setForm] = useState<EmailFormState>(emptyForm)
  const [activeModule, setActiveModule] = useState<PlatformModuleKey>('email')

  useEffect(() => {
    if (!emailSettings) return

    setForm({
      isEnabled: emailSettings.isEnabled,
      host: emailSettings.host ?? '',
      port: String(emailSettings.port || 587),
      userName: emailSettings.userName ?? '',
      password: '',
      enableSsl: emailSettings.enableSsl,
      senderName: emailSettings.senderName ?? '',
      senderEmail: emailSettings.senderEmail ?? '',
      replyToEmail: emailSettings.replyToEmail ?? '',
      timeoutSeconds: String(emailSettings.timeoutSeconds || 30),
    })
  }, [emailSettings])

  const hasExistingPassword = Boolean(emailSettings?.hasPassword)

  function handleSave() {
    onSaveEmailSettings({
      isEnabled: form.isEnabled,
      host: form.host,
      port: Number(form.port) || 587,
      userName: form.userName,
      password: form.password ? form.password : undefined,
      enableSsl: form.enableSsl,
      senderName: form.senderName,
      senderEmail: form.senderEmail,
      replyToEmail: form.replyToEmail,
      timeoutSeconds: Number(form.timeoutSeconds) || 30,
    })
  }

  const activeModuleDefinition = platformModules.find((module) => module.key === activeModule) ?? platformModules[0]

  return (
    <section className="work-area workflow-list-area">
      <header className="page-heading">
        <div className="workflow-library-heading-copy">
          <div className="page-eyebrow">Configuration</div>
          <h2><span className="title-icon subtle" aria-hidden="true">PL</span>Platform Settings</h2>
          <p>Manage platform-level delivery settings for outbound workflow email notifications and system mail.</p>
        </div>
      </header>

      <section className="panel platform-settings-panel">
        <div className="platform-settings-shell">
          <aside className="platform-settings-nav">
            <section className="platform-settings-nav-card">
              <div className="platform-settings-nav-header">
                <span>Settings Modules</span>
                <strong>Platform configuration catalog</strong>
              </div>
              <div className="platform-settings-nav-list">
                {platformModules.map((module) => (
                  <button
                    key={module.key}
                    className={module.key === activeModule ? 'active' : ''}
                    type="button"
                    onClick={() => setActiveModule(module.key)}
                  >
                    <span className="platform-settings-nav-meta">
                      <small>{module.category}</small>
                      <strong>{module.title}</strong>
                    </span>
                    <span className={`platform-settings-nav-status ${module.status === 'Live' ? 'live' : ''}`}>{module.status}</span>
                  </button>
                ))}
              </div>
            </section>

            <section className="platform-settings-nav-card platform-settings-roadmap">
              <span>UX Direction</span>
              <strong>Each platform area now behaves like a dedicated settings module.</strong>
              <p>
                This allows future controls to live in their own workspace without making Platform Settings become one long mixed form.
              </p>
            </section>
          </aside>

          <div className="platform-settings-content">
            <section className="platform-settings-card platform-settings-hero">
              <div className="platform-settings-card-header">
                <div>
                  <span>{activeModuleDefinition.category}</span>
                  <strong>{activeModuleDefinition.title}</strong>
                  <p>{activeModuleDefinition.description}</p>
                </div>
                <div className="platform-settings-hero-actions">
                  <button className="secondary-action" type="button" onClick={onRefresh} disabled={busy}>
                    Refresh
                  </button>
                  {activeModule === 'email' ? (
                    <button
                      className="primary-action"
                      type="button"
                      onClick={handleSave}
                      disabled={busy || !form.host.trim() || !form.senderEmail.trim()}
                    >
                      Save SMTP Setup
                    </button>
                  ) : null}
                </div>
              </div>

              {activeModule === 'email' ? (
                <div className="platform-settings-metrics">
                  <div className="platform-settings-metric">
                    <span>Protocol</span>
                    <strong>SMTP</strong>
                  </div>
                  <div className="platform-settings-metric">
                    <span>Security</span>
                    <strong>{form.enableSsl ? 'TLS / SSL' : 'Plain / StartTLS by server'}</strong>
                  </div>
                  <div className="platform-settings-metric">
                    <span>Password</span>
                    <strong>{hasExistingPassword ? 'Configured' : 'Not set'}</strong>
                  </div>
                  <div className="platform-settings-metric">
                    <span>Last Updated</span>
                    <strong>{emailSettings?.updatedAtUtc ? new Date(emailSettings.updatedAtUtc).toLocaleString() : 'Not saved yet'}</strong>
                  </div>
                </div>
              ) : (
                <div className="platform-settings-metrics">
                  <div className="platform-settings-metric">
                    <span>Module Status</span>
                    <strong>{activeModuleDefinition.status}</strong>
                  </div>
                  <div className="platform-settings-metric">
                    <span>Category</span>
                    <strong>{activeModuleDefinition.category}</strong>
                  </div>
                  <div className="platform-settings-metric">
                    <span>Delivery Model</span>
                    <strong>Dedicated module workspace</strong>
                  </div>
                  <div className="platform-settings-metric">
                    <span>Purpose</span>
                    <strong>Reserved for future enterprise controls</strong>
                  </div>
                </div>
              )}
            </section>

            {activeModule === 'email' ? (
              <>
                <section className="platform-settings-card">
                  <div className="platform-settings-card-header">
                    <div>
                      <span>Email Engine</span>
                      <strong>SMTP Delivery Setup</strong>
                    </div>
                    <label className="platform-toggle">
                      <input
                        type="checkbox"
                        checked={form.isEnabled}
                        onChange={(event) => setForm((current) => ({ ...current, isEnabled: event.target.checked }))}
                        disabled={busy}
                      />
                      <span>{form.isEnabled ? 'Enabled' : 'Disabled'}</span>
                    </label>
                  </div>
                </section>

                <div className="platform-settings-grid">
                  <section className="platform-settings-card">
                    <div className="section-heading">
                      <h3>Server</h3>
                      <span>Connection endpoint</span>
                    </div>
                    <div className="form-grid">
                      <label className="field">
                        <span>SMTP Host</span>
                        <input value={form.host} onChange={(event) => setForm((current) => ({ ...current, host: event.target.value }))} disabled={busy} />
                      </label>
                      <label className="field">
                        <span>Port</span>
                        <input type="number" min="1" max="65535" value={form.port} onChange={(event) => setForm((current) => ({ ...current, port: event.target.value }))} disabled={busy} />
                      </label>
                      <label className="field">
                        <span>User Name</span>
                        <input value={form.userName} onChange={(event) => setForm((current) => ({ ...current, userName: event.target.value }))} disabled={busy} />
                      </label>
                      <label className="field">
                        <span>{hasExistingPassword ? 'Password (leave blank to keep current)' : 'Password'}</span>
                        <input type="password" value={form.password} onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))} disabled={busy} />
                      </label>
                      <label className="field">
                        <span>Timeout Seconds</span>
                        <input type="number" min="1" max="300" value={form.timeoutSeconds} onChange={(event) => setForm((current) => ({ ...current, timeoutSeconds: event.target.value }))} disabled={busy} />
                      </label>
                      <label className="field platform-checkbox-field">
                        <span>Transport Security</span>
                        <label className="platform-inline-checkbox">
                          <input
                            type="checkbox"
                            checked={form.enableSsl}
                            onChange={(event) => setForm((current) => ({ ...current, enableSsl: event.target.checked }))}
                            disabled={busy}
                          />
                          <strong>Enable SSL / TLS</strong>
                        </label>
                      </label>
                    </div>
                  </section>

                  <section className="platform-settings-card">
                    <div className="section-heading">
                      <h3>Sender Defaults</h3>
                      <span>Outbound identity</span>
                    </div>
                    <div className="form-grid">
                      <label className="field">
                        <span>From Name</span>
                        <input value={form.senderName} onChange={(event) => setForm((current) => ({ ...current, senderName: event.target.value }))} disabled={busy} />
                      </label>
                      <label className="field">
                        <span>From Email</span>
                        <input type="email" value={form.senderEmail} onChange={(event) => setForm((current) => ({ ...current, senderEmail: event.target.value }))} disabled={busy} />
                      </label>
                      <label className="field">
                        <span>Reply-To Email</span>
                        <input type="email" value={form.replyToEmail} onChange={(event) => setForm((current) => ({ ...current, replyToEmail: event.target.value }))} disabled={busy} />
                      </label>
                    </div>
                  </section>
                </div>

                <section className="platform-settings-card platform-settings-note">
                  <div className="section-heading">
                    <h3>Usage</h3>
                    <span>Workflow mail delivery</span>
                  </div>
                  <p>
                    This SMTP engine will be used by workflow notifications, escalation alerts, and future outbound approval emails.
                    Keep the sender mailbox aligned with your organization domain and mail relay policy.
                  </p>
                </section>
              </>
            ) : (
              <section className="platform-settings-card platform-settings-placeholder">
                <div className="section-heading">
                  <h3>{activeModuleDefinition.title}</h3>
                  <span>{activeModuleDefinition.status}</span>
                </div>
                <p>{activeModuleDefinition.description}</p>
                <div className="platform-settings-placeholder-grid">
                  <div className="platform-settings-metric">
                    <span>Future Scope</span>
                    <strong>Dedicated form, validation, and save workflow</strong>
                  </div>
                  <div className="platform-settings-metric">
                    <span>Recommended UX</span>
                    <strong>Keep each module isolated by concern</strong>
                  </div>
                  <div className="platform-settings-metric">
                    <span>Enterprise Pattern</span>
                    <strong>Shared catalog on the left, focused editor on the right</strong>
                  </div>
                </div>
              </section>
            )}
          </div>
        </div>
      </section>
    </section>
  )
}
