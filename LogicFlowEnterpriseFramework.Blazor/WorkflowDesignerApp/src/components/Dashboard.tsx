import type { User, UserGroup, WorkflowDefinition, WorkflowTask } from '../api'
import { formatDate } from '../format'

type DashboardProps = {
  definitions: WorkflowDefinition[]
  tasks: WorkflowTask[]
  userGroups: UserGroup[]
  users: User[]
}

export function Dashboard({ definitions, tasks, userGroups, users }: DashboardProps) {
  const activeFlows = definitions.filter((definition) => definition.status === 'Active').length
  const draftFlows = definitions.filter((definition) => definition.status === 'Draft').length
  const activeUsers = users.filter((user) => user.status === 'Active').length
  const pendingTasks = tasks.filter((task) => task.status === 'Pending').length
  const claimedTasks = tasks.filter((task) => task.status === 'Claimed').length

  const latestDefinitions = [...definitions]
    .sort((left, right) => new Date(right.updatedAtUtc || right.createdAtUtc).getTime() - new Date(left.updatedAtUtc || left.createdAtUtc).getTime())
    .slice(0, 5)

  const latestTasks = [...tasks]
    .sort((left, right) => new Date(right.createdAtUtc).getTime() - new Date(left.createdAtUtc).getTime())
    .slice(0, 5)

  return (
    <section className="work-area workflow-list-area">
      <header className="page-heading">
        <div className="workflow-library-heading-copy">
          <div className="page-eyebrow">Operations dashboard</div>
          <h2><span className="title-icon subtle" aria-hidden="true"><DashboardIcon /></span>Dashboard</h2>
          <p>Monitor workflow activity, configuration readiness, and current operational load from one landing page.</p>
        </div>
        <div className="page-heading-meta">
          <div className="heading-stat">
            <strong>{definitions.length}</strong>
            <span>Total workflows</span>
          </div>
          <div className="heading-stat">
            <strong>{pendingTasks}</strong>
            <span>Pending tasks</span>
          </div>
          <div className="heading-stat">
            <strong>{activeUsers}</strong>
            <span>Active users</span>
          </div>
        </div>
      </header>

      <section className="dashboard-metric-grid">
        <article className="dashboard-metric-card">
          <span>Active releases</span>
          <strong>{activeFlows}</strong>
          <small>Live workflow versions currently available for execution.</small>
        </article>
        <article className="dashboard-metric-card">
          <span>Draft workflows</span>
          <strong>{draftFlows}</strong>
          <small>Definitions that still require design edits or publication.</small>
        </article>
        <article className="dashboard-metric-card">
          <span>Claimed tasks</span>
          <strong>{claimedTasks}</strong>
          <small>Items already being handled by an assigned workflow actor.</small>
        </article>
        <article className="dashboard-metric-card">
          <span>User groups</span>
          <strong>{userGroups.length}</strong>
          <small>Configured assignment groups with inherited routing roles.</small>
        </article>
      </section>

      <section className="dashboard-summary-grid">
        <article className="panel">
          <div className="panel-header">
            <div>
              <h2><span className="title-icon subtle" aria-hidden="true"><WorkflowMiniIcon /></span>Recent Workflow Changes</h2>
              <p>Latest workflow definitions touched in the library.</p>
            </div>
          </div>
          <div className="dashboard-list">
            {latestDefinitions.length === 0 ? (
              <div className="empty-state">No workflows available yet.</div>
            ) : latestDefinitions.map((definition) => (
              <div key={definition.id} className="dashboard-list-row">
                <div className="row-stack">
                  <strong>{definition.name}</strong>
                  <small>{definition.description || 'No description recorded'}</small>
                </div>
                <div className="dashboard-inline-meta">
                  <span className={`status-pill ${definition.status.toLowerCase()}`}>{definition.status}</span>
                  <small>{formatDate(definition.updatedAtUtc || definition.createdAtUtc)}</small>
                </div>
              </div>
            ))}
          </div>
        </article>

        <article className="panel">
          <div className="panel-header">
            <div>
              <h2><span className="title-icon subtle" aria-hidden="true"><TaskMiniIcon /></span>Latest Inbox Activity</h2>
              <p>Most recent tasks currently visible to the signed-in user.</p>
            </div>
          </div>
          <div className="dashboard-list">
            {latestTasks.length === 0 ? (
              <div className="empty-state">No tasks available right now.</div>
            ) : latestTasks.map((task) => (
              <div key={task.id} className="dashboard-list-row">
                <div className="row-stack">
                  <strong>{task.taskName}</strong>
                  <small>{task.assignmentType} assignment</small>
                </div>
                <div className="dashboard-inline-meta">
                  <span className={`status-pill ${task.status.toLowerCase()}`}>{task.status}</span>
                  <small>{formatDate(task.createdAtUtc)}</small>
                </div>
              </div>
            ))}
          </div>
        </article>
      </section>
    </section>
  )
}

function DashboardIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M2.8 8.4h2.5v4.1H2.8z" />
      <path d="M6.8 5.6h2.5v6.9H6.8z" />
      <path d="M10.8 3.4h2.5v9.1h-2.5z" />
    </svg>
  )
}

function WorkflowMiniIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="3.5" cy="4" r="1.3" />
      <circle cx="12.5" cy="4" r="1.3" />
      <circle cx="8" cy="12" r="1.3" />
      <path d="M4.8 4h6.4" />
      <path d="M4.4 5.1 7.1 10" />
      <path d="m11.6 5.1-2.7 4.9" />
    </svg>
  )
}

function TaskMiniIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3 4.5h10" />
      <path d="M3 8h10" />
      <path d="M3 11.5h6.5" />
      <path d="m10.8 11.4 1.4 1.4 2.3-3.1" />
    </svg>
  )
}
