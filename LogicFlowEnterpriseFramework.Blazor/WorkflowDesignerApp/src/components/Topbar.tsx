import { useEffect, useRef, useState } from 'react'
import type { User } from '../api'
import logoImage from '../assets/logo.png'
import { ToastStack } from './ToastStack'

type WorkspaceTab = 'dashboard' | 'tasks' | 'definitions' | 'monitoring' | 'users' | 'configuration'
type IdentityView = 'users' | 'roles' | 'groups'
type ConfigurationTarget = 'external-systems' | 'field-mapping' | 'platform-settings'

type TopbarProps = {
  activeTab: WorkspaceTab
  busy: boolean
  currentUser: User | null
  definitionCount: number
  error: string
  identityView: IdentityView
  notice: string
  onDismissError: () => void
  onDismissNotice: () => void
  onConfigurationSelect: (target: ConfigurationTarget) => void
  onUserManagementSelect: (view: IdentityView) => void
  onLogout: () => void
  onRefresh: () => void
  onTabChange: (tab: WorkspaceTab) => void
  taskCount: number
}

export function Topbar({
  activeTab,
  busy,
  currentUser,
  definitionCount,
  error,
  identityView,
  notice,
  onDismissError,
  onDismissNotice,
  onConfigurationSelect,
  onUserManagementSelect,
  onLogout,
  onRefresh,
  onTabChange,
  taskCount,
}: TopbarProps) {
  const currentSection = activeTab === 'dashboard'
    ? 'Dashboard'
    : activeTab === 'tasks'
    ? 'Task inbox'
    : activeTab === 'definitions'
      ? 'Workflow'
      : activeTab === 'monitoring'
        ? 'Monitoring'
      : activeTab === 'users'
        ? identityView === 'roles'
          ? 'Roles'
          : identityView === 'groups'
            ? 'User groups'
            : 'User management'
      : activeTab === 'configuration'
        ? 'Configuration'
        : 'Workspace'
  const [workspaceMenuOpen, setWorkspaceMenuOpen] = useState(false)
  const [userMenuOpen, setUserMenuOpen] = useState(false)
  const [configurationMenuOpen, setConfigurationMenuOpen] = useState(false)
  const [identityMenuOpen, setIdentityMenuOpen] = useState(false)
  const workspaceMenuRef = useRef<HTMLDivElement | null>(null)
  const userMenuRef = useRef<HTMLDivElement | null>(null)
  const configurationMenuRef = useRef<HTMLDivElement | null>(null)
  const identityMenuRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    function handlePointerDown(event: MouseEvent) {
      const target = event.target as Node
      if (workspaceMenuRef.current && !workspaceMenuRef.current.contains(target)) {
        setWorkspaceMenuOpen(false)
      }
      if (userMenuRef.current && !userMenuRef.current.contains(target)) {
        setUserMenuOpen(false)
      }
      if (configurationMenuRef.current && !configurationMenuRef.current.contains(target)) {
        setConfigurationMenuOpen(false)
      }
      if (identityMenuRef.current && !identityMenuRef.current.contains(target)) {
        setIdentityMenuOpen(false)
      }
    }

    function handleEscape(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setWorkspaceMenuOpen(false)
        setUserMenuOpen(false)
        setConfigurationMenuOpen(false)
        setIdentityMenuOpen(false)
      }
    }

    window.addEventListener('mousedown', handlePointerDown)
    window.addEventListener('keydown', handleEscape)
    return () => {
      window.removeEventListener('mousedown', handlePointerDown)
      window.removeEventListener('keydown', handleEscape)
    }
  }, [])

  return (
    <header className="topbar">
      <div className="topbar-brandbar">
        <div className="brand">
          <img className="brand-mark" src={logoImage} alt="LogicFlow" />
        </div>

        <div className="topbar-nav-rail">
          <nav className="nav-buttons workspace-tabs" aria-label="Workspace">
            <button className={activeTab === 'dashboard' ? 'active' : ''} type="button" onClick={() => onTabChange('dashboard')}>
              <span className="button-label"><span className="button-icon" aria-hidden="true"><DashboardIcon /></span>Dashboard</span>
            </button>
            <button className={activeTab === 'tasks' ? 'active' : ''} type="button" onClick={() => onTabChange('tasks')}>
              <span className="button-label"><span className="button-icon" aria-hidden="true"><TasksIcon /></span>Inbox</span>
            </button>
            <button className={activeTab === 'definitions' ? 'active' : ''} type="button" onClick={() => onTabChange('definitions')}>
              <span className="button-label"><span className="button-icon" aria-hidden="true"><WorkflowIcon /></span>Workflows</span>
            </button>
            <button className={activeTab === 'monitoring' ? 'active' : ''} type="button" onClick={() => onTabChange('monitoring')}>
              <span className="button-label"><span className="button-icon" aria-hidden="true"><MonitoringIcon /></span>Monitor</span>
            </button>
            <div className="nav-menu-shell" ref={identityMenuRef}>
              <button
                className={`nav-menu-trigger ${activeTab === 'users' ? 'active' : ''}`}
                type="button"
                aria-haspopup="menu"
                aria-expanded={identityMenuOpen}
                onClick={() => {
                  setIdentityMenuOpen((current) => !current)
                  setConfigurationMenuOpen(false)
                  setWorkspaceMenuOpen(false)
                  setUserMenuOpen(false)
                }}
              >
                <span className="button-label"><span className="button-icon" aria-hidden="true"><UsersIcon /></span>Users <span className="nav-menu-caret" aria-hidden="true"><CaretIcon /></span></span>
              </button>
              {identityMenuOpen && (
                <div className="nav-menu-dropdown" role="menu">
                  <div className="nav-menu-dropdown-header">
                    <span>User management</span>
                    <strong>Select a user workspace</strong>
                  </div>
                  <button
                    className={activeTab === 'users' && identityView === 'users' ? 'active' : ''}
                    type="button"
                    role="menuitem"
                    onClick={() => {
                      onUserManagementSelect('users')
                      setIdentityMenuOpen(false)
                    }}
                  >
                    <span className="nav-menu-item-copy">
                      <strong>Users</strong>
                    </span>
                  </button>
                  <button
                    className={activeTab === 'users' && identityView === 'roles' ? 'active' : ''}
                    type="button"
                    role="menuitem"
                    onClick={() => {
                      onUserManagementSelect('roles')
                      setIdentityMenuOpen(false)
                    }}
                  >
                    <span className="nav-menu-item-copy">
                      <strong>Roles</strong>
                    </span>
                  </button>
                  <button
                    className={activeTab === 'users' && identityView === 'groups' ? 'active' : ''}
                    type="button"
                    role="menuitem"
                    onClick={() => {
                      onUserManagementSelect('groups')
                      setIdentityMenuOpen(false)
                    }}
                  >
                    <span className="nav-menu-item-copy">
                      <strong>User Groups</strong>
                    </span>
                  </button>
                </div>
              )}
            </div>
            <div className="nav-menu-shell" ref={configurationMenuRef}>
              <button
                className={`nav-menu-trigger ${activeTab === 'configuration' ? 'active' : ''}`}
                type="button"
                aria-haspopup="menu"
                aria-expanded={configurationMenuOpen}
                onClick={() => {
                  setConfigurationMenuOpen((current) => !current)
                  setWorkspaceMenuOpen(false)
                  setUserMenuOpen(false)
                }}
              >
                <span className="button-label"><span className="button-icon" aria-hidden="true"><ConfigurationIcon /></span>Config <span className="nav-menu-caret" aria-hidden="true"><CaretIcon /></span></span>
              </button>
              {configurationMenuOpen && (
                <div className="nav-menu-dropdown" role="menu">
                  <div className="nav-menu-dropdown-header">
                    <span>Configuration</span>
                    <strong>Select a configuration workspace</strong>
                  </div>
                  <button
                    type="button"
                    role="menuitem"
                    onClick={() => {
                      onConfigurationSelect('field-mapping')
                      setConfigurationMenuOpen(false)
                    }}
                  >
                    <span className="nav-menu-item-copy">
                      <strong>Field Mapping</strong>
                    </span>
                  </button>
                  <button
                    type="button"
                    role="menuitem"
                    onClick={() => {
                      onConfigurationSelect('external-systems')
                      setConfigurationMenuOpen(false)
                    }}
                  >
                    <span className="nav-menu-item-copy">
                      <strong>External Systems</strong>
                    </span>
                  </button>
                  <button
                    type="button"
                    role="menuitem"
                    onClick={() => {
                      onConfigurationSelect('platform-settings')
                      setConfigurationMenuOpen(false)
                    }}
                  >
                    <span className="nav-menu-item-copy">
                      <strong>Platform Settings</strong>
                    </span>
                  </button>
                </div>
              )}
            </div>
          </nav>
        </div>

        <div className="topbar-actions">
          <div className="topbar-menu-shell" ref={workspaceMenuRef}>
            <button
              className={`topbar-menu-trigger ${workspaceMenuOpen ? 'open' : ''}`}
              type="button"
              aria-haspopup="menu"
              aria-expanded={workspaceMenuOpen}
              onClick={() => {
                setWorkspaceMenuOpen((current) => !current)
                setUserMenuOpen(false)
              }}
            >
              <span className="topbar-menu-trigger-copy">
                <span className="topbar-menu-trigger-label">Workspace</span>
                <strong>Active workspace</strong>
              </span>
              <span className="topbar-menu-caret" aria-hidden="true"><CaretIcon /></span>
            </button>

            {workspaceMenuOpen && (
              <div className="topbar-dropdown-menu topbar-dropdown-menu--workspace" role="menu">
                <div className="topbar-dropdown-header">
                  <span>Workspace</span>
                  <strong>Active workspace</strong>
                </div>
                <div className="topbar-dropdown-metrics">
                  <div className="topbar-dropdown-metric">
                    <strong>{taskCount}</strong>
                    <span>Tasks</span>
                  </div>
                  <div className="topbar-dropdown-metric">
                    <strong>{definitionCount}</strong>
                    <span>Flows</span>
                  </div>
                </div>
                <button className="topbar-dropdown-action" type="button" role="menuitem" onClick={() => {
                  setWorkspaceMenuOpen(false)
                  onRefresh()
                }} disabled={busy || !currentUser}>
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><RefreshIcon /></span>Refresh workspace</span>
                </button>
              </div>
            )}
          </div>

          <div className="topbar-menu-shell" ref={userMenuRef}>
            <button
              className={`topbar-menu-trigger topbar-menu-trigger--user ${userMenuOpen ? 'open' : ''}`}
              type="button"
              aria-haspopup="menu"
              aria-expanded={userMenuOpen}
              onClick={() => {
                setUserMenuOpen((current) => !current)
                setWorkspaceMenuOpen(false)
              }}
            >
              <span className="topbar-menu-trigger-copy">
                <span className="topbar-menu-trigger-label">Signed in as</span>
                <strong>{currentUser?.displayName || currentUser?.userName || 'Authenticated user'}</strong>
              </span>
              <span className="topbar-menu-caret" aria-hidden="true"><CaretIcon /></span>
            </button>

            {userMenuOpen && (
              <div className="topbar-dropdown-menu topbar-dropdown-menu--user" role="menu">
                <div className="topbar-dropdown-header">
                  <span>Signed in as</span>
                  <strong>{currentUser?.displayName || currentUser?.userName || 'Authenticated user'}</strong>
                </div>
                <button className="topbar-dropdown-action" type="button" role="menuitem" onClick={() => {
                  setUserMenuOpen(false)
                  onLogout()
                }} disabled={busy}>
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><LogoutIcon /></span>Sign out</span>
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="topbar-subbar">
        <div className="topbar-main">
          <div className="breadcrumb structured-breadcrumb">
            <strong>Operations</strong>
            <span>/</span>
            <small>{currentSection}</small>
          </div>
          <div className="topbar-caption">Enterprise workflow operations console</div>
        </div>
      </div>

      <ToastStack error={error} notice={notice} onDismissError={onDismissError} onDismissNotice={onDismissNotice} />
    </header>
  )
}

function TasksIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3 4.5h10" />
      <path d="M3 8h10" />
      <path d="M3 11.5h6.5" />
      <path d="m10.8 11.4 1.4 1.4 2.3-3.1" />
    </svg>
  )
}

function DashboardIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <path d="M2.8 8.4h2.5v4.1H2.8z" />
      <path d="M6.8 5.6h2.5v6.9H6.8z" />
      <path d="M10.8 3.4h2.5v9.1h-2.5z" />
    </svg>
  )
}

function WorkflowIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="3.5" cy="4" r="1.4" />
      <circle cx="12.5" cy="4" r="1.4" />
      <circle cx="8" cy="12" r="1.4" />
      <path d="M4.9 4h6.2" />
      <path d="M4.5 5.2 7.2 10" />
      <path d="m11.5 5.2-2.7 4.8" />
    </svg>
  )
}

function RefreshIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <path d="M13 8a5 5 0 1 1-1.4-3.5" />
      <path d="M13 3.5v3.3H9.7" />
    </svg>
  )
}

function ConfigurationIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="8" cy="8" r="2.1" />
      <path d="M8 2.6v1.3" />
      <path d="M8 12.1v1.3" />
      <path d="m12.1 3.9-.9.9" />
      <path d="m4.8 11.2-.9.9" />
      <path d="M13.4 8h-1.3" />
      <path d="M3.9 8H2.6" />
      <path d="m12.1 12.1-.9-.9" />
      <path d="m4.8 4.8-.9-.9" />
    </svg>
  )
}

function MonitoringIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <path d="M2.8 3.8h10.4v7.4H2.8z" />
      <path d="M6.1 12.2h3.8" />
      <path d="M8 11.2v1" />
      <path d="M4.5 8.8 6.4 7l1.5 1.2 3-3" />
    </svg>
  )
}

function UsersIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <path d="M5.2 7.2a1.9 1.9 0 1 0 0-3.8 1.9 1.9 0 0 0 0 3.8Z" />
      <path d="M10.8 6.5a1.5 1.5 0 1 0 0-3" />
      <path d="M2.9 12.3c.4-1.5 1.7-2.4 3.5-2.4 1.8 0 3.1.9 3.5 2.4" />
      <path d="M10 9.8c1.3.1 2.3.8 2.8 2.1" />
    </svg>
  )
}

function LogoutIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <path d="M6 3.5H3.5v9H6" />
      <path d="M9 5.2 12 8l-3 2.8" />
      <path d="M4 8h8" />
    </svg>
  )
}

function CaretIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <path d="m4.5 6.5 3.5 3.5 3.5-3.5" />
    </svg>
  )
}
