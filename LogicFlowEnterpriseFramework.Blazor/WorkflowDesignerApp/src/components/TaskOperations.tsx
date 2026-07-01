import { useState } from 'react'
import type { User, WorkflowInstanceDetail, WorkflowTask } from '../api'
import { formatDate, formatValue } from '../format'

type TaskOperationsProps = {
  busy: boolean
  canCancel: boolean
  comment: string
  delegateTargetUserId: string
  detail: WorkflowInstanceDetail | null
  filteredTasks: WorkflowTask[]
  onAddComment: () => void
  onApprove: () => void
  onCancel: () => void
  onClaim: () => void
  onDelegate: () => void
  onReassign: () => void
  onReject: () => void
  onSelectTask: (taskId: string) => void
  onUnclaim: () => void
  reassignTargetUserId: string
  selectedTaskId: string
  setComment: (value: string) => void
  setDelegateTargetUserId: (value: string) => void
  setReassignTargetUserId: (value: string) => void
  setTaskSearch: (value: string) => void
  setTaskStatusFilter: (value: string) => void
  taskSearch: string
  tasks: WorkflowTask[]
  taskStatusFilter: string
  users: User[]
}

export function TaskOperations({
  busy,
  canCancel,
  comment,
  delegateTargetUserId,
  detail,
  filteredTasks,
  onAddComment,
  onApprove,
  onCancel,
  onClaim,
  onDelegate,
  onReassign,
  onReject,
  onSelectTask,
  onUnclaim,
  reassignTargetUserId,
  selectedTaskId,
  setComment,
  setDelegateTargetUserId,
  setReassignTargetUserId,
  setTaskSearch,
  setTaskStatusFilter,
  taskSearch,
  tasks,
  taskStatusFilter,
  users,
}: TaskOperationsProps) {
  const selectedDetailTask = detail?.tasks.find((task) => task.id === selectedTaskId) ?? filteredTasks.find((task) => task.id === selectedTaskId) ?? null
  const pendingCount = tasks.filter((task) => task.status === 'Pending').length
  const claimedCount = tasks.filter((task) => task.status === 'Claimed').length
  const [detailTab, setDetailTab] = useState<'overview' | 'variables' | 'audit' | 'comments' | 'history'>('overview')
  const selectedTaskComments = detail?.taskComments.filter((item) => item.workflowTaskId === selectedTaskId) ?? []
  const selectedTaskAssignments = detail?.taskAssignments.filter((item) => item.workflowTaskId === selectedTaskId) ?? []
  const availableActions = selectedDetailTask?.availableActions ?? []
  const hasAction = (code: string) => availableActions.some((action) => action.code === code)
  const primaryDecisionLabel = selectedDetailTask?.taskMode === 'approval' ? 'Approve' : 'Complete'
  const secondaryDecisionLabel = selectedDetailTask?.taskMode === 'approval' ? 'Reject' : 'Send Back'

  return (
    <>
    <section className="work-area workflow-list-area">
      <header className="page-heading task-operations-heading">
        <div className="task-operations-heading-copy">
          <div className="page-eyebrow">Task administration</div>
          <h2><span className="title-icon subtle" aria-hidden="true"><TaskHeaderIcon /></span>Task Operations</h2>
          <p>Review assigned work, claim tasks, and complete running workflow instances.</p>
        </div>
        <div className="page-heading-meta">
          <div className="heading-stat">
            <strong>{filteredTasks.length}</strong>
            <span>Visible tasks</span>
          </div>
          <div className="heading-stat">
            <strong>{tasks.length}</strong>
            <span>Inbox total</span>
          </div>
          <div className="heading-stat">
            <strong>{pendingCount}</strong>
            <span>Pending</span>
          </div>
          <div className="heading-stat">
            <strong>{claimedCount}</strong>
            <span>Claimed</span>
          </div>
        </div>
      </header>

      <section className="panel workflow-table-panel workflow-library-panel task-inbox-library-panel">
        <div className="panel-header task-inbox-panel-header workflow-library-panel-header">
          <div className="workflow-library-panel-copy">
            <h2><span className="title-icon subtle" aria-hidden="true"><InboxIcon /></span>Task Inbox</h2>
            <p>{filteredTasks.length} of {tasks.length} active task{tasks.length === 1 ? '' : 's'}</p>
          </div>
          <div className="workflow-library-panel-meta">
            <span className="workflow-panel-pill">Select a task to inspect the live instance</span>
            <span className="workflow-panel-pill">Claim, approve, reject, or cancel from the detail panel</span>
          </div>
        </div>

        <div className="list-toolbar task-list-toolbar">
          <label className="toolbar-search-field">
            <span>Search tasks</span>
            <input
              aria-label="Search tasks"
              placeholder="Search by task, owner, or workflow"
              value={taskSearch}
              onChange={(event) => setTaskSearch(event.target.value)}
            />
          </label>
          <label className="toolbar-filter-field">
            <span>Status</span>
            <select
              aria-label="Filter task status"
              value={taskStatusFilter}
              onChange={(event) => setTaskStatusFilter(event.target.value)}
            >
              <option>All</option>
              <option>Pending</option>
              <option>Claimed</option>
            </select>
          </label>
        </div>

        <div className="workflow-table task-list-table">
          <div className="workflow-table-header workflow-table-grid task-grid">
            <span>Status</span>
            <span>Task</span>
            <span>Assignment</span>
            <span>Activity</span>
          </div>
          {filteredTasks.length === 0 && (
            <div className="empty-state-card compact">
              <div className="empty-state-icon">TK</div>
              <strong>No matching tasks</strong>
              <p>Adjust the current search or status filter to bring active work back into view.</p>
            </div>
          )}
          {filteredTasks.map((task) => (
            <button
              key={task.id}
              className={task.id === selectedTaskId ? 'workflow-table-row workflow-table-grid task-grid active' : 'workflow-table-row workflow-table-grid task-grid'}
              type="button"
              onClick={() => onSelectTask(task.id)}
            >
              <div className="workflow-status-cell">
                <span className={`status-pill ${task.status.toLowerCase()}`}>{task.status}</span>
                <small>{task.status === 'Claimed' ? 'In progress' : 'Awaiting action'}</small>
              </div>
              <div className="row-stack task-name-cell">
                <strong>{task.taskName}</strong>
                <small>{task.entityId || task.workflowInstanceId}</small>
              </div>
              <div className="row-stack task-assignment-cell">
                <strong>{task.assignedToDisplayName || task.assignmentType}</strong>
                <div className="workflow-version-tags task-assignment-tags">
                  <span>{task.taskMode}</span>
                  {task.priority && <span>{task.priority}</span>}
                  {task.claimedBy && <span>Claimed</span>}
                </div>
              </div>
              <div className="row-stack task-activity-cell">
                <strong>{formatDate(task.createdAtUtc)}</strong>
                <small>{task.claimedAtUtc ? `Claimed ${formatDate(task.claimedAtUtc)}` : 'Not claimed yet'}</small>
              </div>
            </button>
          ))}
        </div>
      </section>
    </section>
    {detail && selectedDetailTask && (
      <section className="modal-backdrop" role="presentation">
        <div className="modal-dialog modal-dialog--wide task-detail-modal" role="dialog" aria-modal="true" aria-labelledby="task-detail-title" onClick={(event) => event.stopPropagation()}>
          <div className="modal-header task-detail-modal-header">
            <div>
              <h3 id="task-detail-title"><span className="title-icon subtle" aria-hidden="true"><DetailIcon /></span>{selectedDetailTask.taskName}</h3>
              <p>{detail.instance.title || detail.instance.businessKey || detail.instance.id}</p>
            </div>
            <button className="modal-close-button" type="button" onClick={() => onSelectTask('')} disabled={busy}>Close</button>
          </div>
          <div className="modal-body task-detail-modal-body">
            <div className="detail-hero task-detail-hero">
              <div className="detail-hero-main">
                <div className="detail-hero-topline">
                  <span className={`status-pill ${selectedDetailTask.status.toLowerCase()}`}>{selectedDetailTask.status}</span>
                  <span className={`status-pill ${detail.instance.status.toLowerCase()}`}>{detail.instance.status}</span>
                  <span className="task-detail-inline-meta">Current node {detail.instance.currentNodeId || '-'}</span>
                  <span className="task-detail-inline-meta">Started {formatDate(detail.instance.startedAtUtc)}</span>
                </div>
                <div className="task-detail-meta-rail">
                  <span><strong>Instance</strong>{detail.instance.businessKey || detail.instance.id}</span>
                  <span><strong>Started by</strong>{detail.instance.startedByDisplayName || detail.instance.startedBy}</span>
                  <span><strong>Variables</strong>{detail.variables.length}</span>
                </div>
              </div>
            </div>

            <div className="task-detail-summary-grid" aria-label="Task summary metrics">
              <div className="task-detail-summary-metric">
                <span>Current node</span>
                <strong>{detail.instance.currentNodeId || '-'}</strong>
              </div>
              <div className="task-detail-summary-metric">
                <span>Task mode</span>
                <strong>{selectedDetailTask.taskMode}</strong>
              </div>
              <div className="task-detail-summary-metric">
                <span>Priority / SLA</span>
                <strong>{selectedDetailTask.priority || '-'} / {selectedDetailTask.slaStatus || '-'}</strong>
              </div>
              <div className="task-detail-summary-metric">
                <span>Entity</span>
                <strong>{selectedDetailTask.entityId || '-'}</strong>
              </div>
            </div>

            <label className="field">
              <span>Comment / reason</span>
              <textarea value={comment} onChange={(event) => setComment(event.target.value)} rows={3} />
            </label>

            <div className="task-detail-summary-grid" aria-label="Task routing">
              <label className="field">
                <span>Delegate to user</span>
                <select value={delegateTargetUserId} onChange={(event) => setDelegateTargetUserId(event.target.value)} disabled={busy}>
                  <option value="">Select user</option>
                  {users.map((user) => (
                    <option key={user.id} value={user.id}>{user.displayName}</option>
                  ))}
                </select>
              </label>
              <label className="field">
                <span>Reassign to user</span>
                <select value={reassignTargetUserId} onChange={(event) => setReassignTargetUserId(event.target.value)} disabled={busy}>
                  <option value="">Select user</option>
                  {users.map((user) => (
                    <option key={user.id} value={user.id}>{user.displayName}</option>
                  ))}
                </select>
              </label>
            </div>

            <div className="action-bar grouped task-detail-action-bar">
              <div className="action-group utility task-detail-utility-actions">
                <button className="secondary-action" type="button" onClick={onClaim} disabled={!hasAction('claim') || busy}>
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><ClaimIcon /></span>Claim</span>
                </button>
                <button className="secondary-action" type="button" onClick={onUnclaim} disabled={!hasAction('unclaim') || busy}>
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><UnclaimIcon /></span>Unclaim</span>
                </button>
                <button className="secondary-action" type="button" onClick={onAddComment} disabled={!hasAction('comment') || busy || !comment.trim()}>
                  <span className="button-label">Add Comment</span>
                </button>
                <button className="secondary-action" type="button" onClick={onDelegate} disabled={!hasAction('delegate') || busy || !delegateTargetUserId}>
                  <span className="button-label">Delegate</span>
                </button>
                <button className="secondary-action" type="button" onClick={onReassign} disabled={!hasAction('reassign') || busy || !reassignTargetUserId}>
                  <span className="button-label">Reassign</span>
                </button>
              </div>
              <div className="action-group decision task-detail-decision-actions">
                <button className="primary-action" type="button" onClick={onApprove} disabled={!hasAction('approve') || busy}>
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><ApproveIcon /></span>{primaryDecisionLabel}</span>
                </button>
                <button className="danger-action" type="button" onClick={onReject} disabled={!hasAction('reject') || busy}>
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><RejectIcon /></span>{secondaryDecisionLabel}</span>
                </button>
                <button className="danger-ghost" type="button" onClick={onCancel} disabled={!canCancel || busy}>
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><CancelIcon /></span>Cancel workflow</span>
                </button>
              </div>
            </div>

            <div className="detail-tabs" role="tablist" aria-label="Task detail sections">
              <button className={detailTab === 'overview' ? 'active' : ''} type="button" onClick={() => setDetailTab('overview')}>Overview</button>
              <button className={detailTab === 'variables' ? 'active' : ''} type="button" onClick={() => setDetailTab('variables')}>Variables</button>
              <button className={detailTab === 'audit' ? 'active' : ''} type="button" onClick={() => setDetailTab('audit')}>Audit</button>
              <button className={detailTab === 'comments' ? 'active' : ''} type="button" onClick={() => setDetailTab('comments')}>Comments</button>
              <button className={detailTab === 'history' ? 'active' : ''} type="button" onClick={() => setDetailTab('history')}>History</button>
            </div>

            {detailTab === 'overview' && (
              <section>
                <div className="section-heading">
                  <h3><span className="title-icon subtle" aria-hidden="true"><TasksSectionIcon /></span>Tasks</h3>
                  <span>{detail.tasks.length} item{detail.tasks.length === 1 ? '' : 's'}</span>
                </div>
                <div className="compact-list">
                  {detail.tasks.map((task) => (
                    <div key={task.id} className="compact-row">
                      <span className={`status-pill ${task.status.toLowerCase()}`}>{task.status}</span>
                      <div className="row-stack">
                        <strong>{task.taskName}</strong>
                        <small>{task.completedBy || task.claimedBy || task.assignedToDisplayName || task.assignmentType}</small>
                      </div>
                    </div>
                  ))}
                </div>
              </section>
            )}

            {detailTab === 'variables' && (
              <section>
                <div className="section-heading">
                  <h3><span className="title-icon subtle" aria-hidden="true"><VariablesIcon /></span>Variables</h3>
                  <span>{detail.variables.length} captured</span>
                </div>
                <div className="compact-list">
                  {detail.variables.length === 0 ? (
                    <div className="empty-state-card compact">
                      <div className="empty-state-icon">{} </div>
                      <strong>No variables yet</strong>
                      <p>This workflow instance has not stored any runtime variables.</p>
                    </div>
                  ) : (
                    detail.variables.map((variable) => (
                      <div key={variable.id} className="compact-row variable-row">
                        <strong>{variable.name}</strong>
                        <span>{formatValue(variable.value)}</span>
                      </div>
                    ))
                  )}
                </div>
              </section>
            )}

            {detailTab === 'audit' && (
              <section>
                <div className="section-heading">
                  <h3><span className="title-icon subtle" aria-hidden="true"><TimelineIcon /></span>Audit Timeline</h3>
                  <span>{detail.auditLogs.length} event{detail.auditLogs.length === 1 ? '' : 's'}</span>
                </div>
                {detail.auditLogs.length === 0 ? (
                  <div className="empty-state-card compact">
                    <div className="empty-state-icon">::</div>
                    <strong>No timeline events</strong>
                    <p>Activity logs will appear here as the workflow progresses.</p>
                  </div>
                ) : (
                  <div className="timeline">
                    {detail.auditLogs.map((log) => (
                      <div key={log.id} className="timeline-row">
                        <span></span>
                        <div>
                          <strong>{log.action}</strong>
                          <p>{log.message || `${log.fromNodeId || '-'} -> ${log.toNodeId || '-'}`}</p>
                          <small>{formatDate(log.createdAtUtc)} {log.performedByDisplayName ? `by ${log.performedByDisplayName}` : ''}</small>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </section>
            )}

            {detailTab === 'comments' && (
              <section>
                <div className="section-heading">
                  <h3>Task Comments</h3>
                  <span>{selectedTaskComments.length} item{selectedTaskComments.length === 1 ? '' : 's'}</span>
                </div>
                {selectedTaskComments.length === 0 ? (
                  <div className="empty-state-card compact">
                    <div className="empty-state-icon">::</div>
                    <strong>No comments</strong>
                    <p>Task comments will appear here.</p>
                  </div>
                ) : (
                  <div className="timeline">
                    {selectedTaskComments.map((item) => (
                      <div key={item.id} className="timeline-row">
                        <span></span>
                        <div>
                          <strong>{item.commentType}</strong>
                          <p>{item.body}</p>
                          <small>{formatDate(item.createdAtUtc)} by {item.createdBy}</small>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </section>
            )}

            {detailTab === 'history' && (
              <section>
                <div className="section-heading">
                  <h3>Assignment History</h3>
                  <span>{selectedTaskAssignments.length} event{selectedTaskAssignments.length === 1 ? '' : 's'}</span>
                </div>
                {selectedTaskAssignments.length === 0 ? (
                  <div className="empty-state-card compact">
                    <div className="empty-state-icon">::</div>
                    <strong>No history</strong>
                    <p>Delegation and reassignment history will appear here.</p>
                  </div>
                ) : (
                  <div className="timeline">
                    {selectedTaskAssignments.map((item) => (
                      <div key={item.id} className="timeline-row">
                        <span></span>
                        <div>
                          <strong>{item.actionType}</strong>
                          <p>{item.reason || 'No reason recorded.'}</p>
                          <small>{formatDate(item.createdAtUtc)} by {item.performedBy}</small>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </section>
            )}
          </div>
        </div>
      </section>
    )}
    </>
  )
}

function TaskHeaderIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="2.5" width="10" height="11" rx="1.5" />
      <path d="M5.5 5.5h5" />
      <path d="M5.5 8h5" />
      <path d="M5.5 10.5h3.4" />
    </svg>
  )
}

function InboxIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3 4.5h10v7H3z" />
      <path d="M3 9h2.5l1 1.5h3l1-1.5H13" />
    </svg>
  )
}

function DetailIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <rect x="2.5" y="3" width="11" height="10" rx="1.5" />
      <path d="M5.2 6h5.6" />
      <path d="M5.2 8.4h5.6" />
      <path d="M5.2 10.8h3.4" />
    </svg>
  )
}

function ClaimIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3.2 8.2 6.1 11 12.8 4.5" />
    </svg>
  )
}

function UnclaimIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3.5 3.5 12.5 12.5" />
      <path d="M12.5 3.5 3.5 12.5" />
    </svg>
  )
}

function ApproveIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3.2 8.2 6.1 11 12.8 4.5" />
    </svg>
  )
}

function RejectIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <path d="M4 4 12 12" />
      <path d="M12 4 4 12" />
    </svg>
  )
}

function CancelIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="8" cy="8" r="5.5" />
      <path d="M5.4 10.6 10.6 5.4" />
    </svg>
  )
}

function TasksSectionIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M4 4.5h8" />
      <path d="M4 8h8" />
      <path d="M4 11.5h5" />
    </svg>
  )
}

function VariablesIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M6 3.5c-1.3 0-2 .7-2 2v1c0 .8-.3 1.4-1 1.5.7.1 1 .7 1 1.5v1c0 1.3.7 2 2 2" />
      <path d="M10 3.5c1.3 0 2 .7 2 2v1c0 .8.3 1.4 1 1.5-.7.1-1 .7-1 1.5v1c0 1.3-.7 2-2 2" />
    </svg>
  )
}

function TimelineIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="4" cy="4" r="1.2" />
      <circle cx="12" cy="8" r="1.2" />
      <circle cx="6" cy="12" r="1.2" />
      <path d="M5 4.6 10.8 7.4" />
      <path d="M11.4 9 6.7 11.2" />
    </svg>
  )
}
