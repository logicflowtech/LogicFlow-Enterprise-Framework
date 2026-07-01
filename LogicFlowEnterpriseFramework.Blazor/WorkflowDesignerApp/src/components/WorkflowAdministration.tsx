import { useEffect, useMemo, useState } from 'react'
import {
  getWorkflowDefinitionVersions,
  type ExternalApiEndpoint,
  type Role,
  type User,
  type UserGroup,
  type WorkflowDefinition,
  type WorkflowValidation,
  type WorkflowVersion,
} from '../api'
import { formatDate } from '../format'
import { WorkflowDesigner, type ViewportActions } from '../WorkflowDesigner'
import { ToastStack } from './ToastStack'

type DefinitionView = 'list' | 'editor'
const workflowLibraryPageSizeStorageKey = 'logicflow.workflow-library.page-size'

type WorkflowAdministrationProps = {
  appMode: 'library' | 'designer'
  busy: boolean
  canEditDraft: boolean
  definitions: WorkflowDefinition[]
  externalApiEndpoints: ExternalApiEndpoint[]
  definitionSearch: string
  definitionStatusFilter: string
  definitionView: DefinitionView
  draftJson: string
  effectiveFromUtc: string
  effectiveToUtc: string
  filteredDefinitions: WorkflowDefinition[]
  onCreateDefinition: (name: string, description: string) => Promise<void>
  onPublish: (publishMessage: string) => Promise<void>
  onOpenDesigner: (definitionId: string) => void
  onSaveDraft: () => void
  onSelectDefinition: (definitionId: string) => void
  onShowList: () => void
  roles: Role[]
  selectedDefinition: WorkflowDefinition | null
  selectedUser: User | null
  selectedUserId: string
  userGroups: UserGroup[]
  users: User[]
  setDefinitionSearch: (value: string) => void
  setDefinitionStatusFilter: (value: string) => void
  setDraftJson: (value: string) => void
  setEffectiveFromUtc: (value: string) => void
  setEffectiveToUtc: (value: string) => void
  validation: WorkflowValidation | null
}

export function WorkflowAdministration({
  appMode,
  busy,
  canEditDraft,
  definitions,
  definitionSearch,
  definitionStatusFilter,
  definitionView,
  draftJson,
  effectiveFromUtc,
  effectiveToUtc,
  filteredDefinitions,
  externalApiEndpoints,
  onCreateDefinition,
  onOpenDesigner,
  onPublish,
  onSaveDraft,
  onSelectDefinition,
  onShowList,
  roles,
  selectedDefinition,
  selectedUser,
  selectedUserId,
  userGroups,
  users,
  setDefinitionSearch,
  setDefinitionStatusFilter,
  setDraftJson,
  setEffectiveFromUtc,
  setEffectiveToUtc,
  validation,
}: WorkflowAdministrationProps) {
  const [sortBy, setSortBy] = useState<'status' | 'name' | 'updated'>('updated')
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc')
  const [openActionMenuId, setOpenActionMenuId] = useState<string | null>(null)
  const [isPublishModalOpen, setIsPublishModalOpen] = useState(false)
  const [publishMessage, setPublishMessage] = useState('')
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false)
  const [createName, setCreateName] = useState('New Workflow')
  const [createDescription, setCreateDescription] = useState('')
  const [isVersionsModalOpen, setIsVersionsModalOpen] = useState(false)
  const [versionDefinition, setVersionDefinition] = useState<WorkflowDefinition | null>(null)
  const [versions, setVersions] = useState<WorkflowVersion[]>([])
  const [isLoadingVersions, setIsLoadingVersions] = useState(false)
  const [versionsError, setVersionsError] = useState('')
  const [viewportActions, setViewportActions] = useState<ViewportActions | null>(null)
  const [currentPage, setCurrentPage] = useState(1)
  const [pageSize, setPageSize] = useState(() => getStoredWorkflowLibraryPageSize())

  useEffect(() => {
    if (!isPublishModalOpen) {
      setPublishMessage('')
    }
  }, [isPublishModalOpen])

  useEffect(() => {
    if (!isCreateModalOpen) {
      setCreateName('New Workflow')
      setCreateDescription('')
    }
  }, [isCreateModalOpen])

  useEffect(() => {
    if (!openActionMenuId) return

    function handleDocumentPointerDown(event: PointerEvent) {
      const target = event.target
      if (target instanceof Element && target.closest('[data-workflow-action-menu="true"]')) {
        return
      }

      setOpenActionMenuId(null)
    }

    document.addEventListener('pointerdown', handleDocumentPointerDown)
    return () => document.removeEventListener('pointerdown', handleDocumentPointerDown)
  }, [openActionMenuId])

  useEffect(() => {
    if (!versionsError) {
      return
    }

    const timeout = window.setTimeout(() => setVersionsError(''), 5200)
    return () => window.clearTimeout(timeout)
  }, [versionsError])

  async function handlePublishSubmit() {
    await onPublish(publishMessage)
    setIsPublishModalOpen(false)
  }

  async function handleCreateSubmit() {
    await onCreateDefinition(createName.trim() || 'New Workflow', createDescription.trim())
    setIsCreateModalOpen(false)
  }

  async function openVersions(definition: WorkflowDefinition) {
    if (!selectedUser) return

    setVersionDefinition(definition)
    setVersions([])
    setVersionsError('')
    setIsVersionsModalOpen(true)
    setIsLoadingVersions(true)

    try {
      const items = await getWorkflowDefinitionVersions(definition.id, selectedUser)
      setVersions(items)
    } catch (error) {
      setVersionsError(error instanceof Error ? error.message : 'Unable to load versions.')
    } finally {
      setIsLoadingVersions(false)
    }
  }

  function openDesigner(definitionId: string) {
    onOpenDesigner(definitionId)
  }

  const displayDefinitions = useMemo(() => [...filteredDefinitions].sort((left, right) => {
    const multiplier = sortDirection === 'asc' ? 1 : -1

    if (sortBy === 'name') {
      return left.name.localeCompare(right.name) * multiplier
    }

    if (sortBy === 'status') {
      return left.status.localeCompare(right.status) * multiplier
    }

    const leftDate = new Date(left.updatedAtUtc || left.createdAtUtc).getTime()
    const rightDate = new Date(right.updatedAtUtc || right.createdAtUtc).getTime()
    return (leftDate - rightDate) * multiplier
  }), [filteredDefinitions, sortBy, sortDirection])

  const totalPages = Math.max(1, Math.ceil(displayDefinitions.length / pageSize))
  const pagedDefinitions = useMemo(() => {
    const startIndex = (currentPage - 1) * pageSize
    return displayDefinitions.slice(startIndex, startIndex + pageSize)
  }, [currentPage, displayDefinitions, pageSize])
  const pageStart = displayDefinitions.length === 0 ? 0 : ((currentPage - 1) * pageSize) + 1
  const pageEnd = Math.min(currentPage * pageSize, displayDefinitions.length)
  const visiblePageNumbers = useMemo(() => {
    if (totalPages <= 5) {
      return Array.from({ length: totalPages }, (_, index) => index + 1)
    }

    const startPage = Math.max(1, Math.min(currentPage - 2, totalPages - 4))
    return Array.from({ length: 5 }, (_, index) => startPage + index)
  }, [currentPage, totalPages])

  function toggleSort(nextSortBy: 'status' | 'name' | 'updated') {
    if (sortBy === nextSortBy) {
      setSortDirection((current) => current === 'asc' ? 'desc' : 'asc')
      return
    }

    setSortBy(nextSortBy)
    setSortDirection(nextSortBy === 'name' ? 'asc' : 'desc')
  }

  useEffect(() => {
    setCurrentPage(1)
  }, [definitionSearch, definitionStatusFilter, sortBy, sortDirection])

  useEffect(() => {
    if (currentPage > totalPages) {
      setCurrentPage(totalPages)
    }
  }, [currentPage, totalPages])

  useEffect(() => {
    if (typeof window === 'undefined') {
      return
    }

    window.localStorage.setItem(workflowLibraryPageSizeStorageKey, String(pageSize))
  }, [pageSize])

  if (appMode === 'library' && definitionView === 'list') {
    return (
      <>
        <ToastStack error={versionsError} onDismissError={() => setVersionsError('')} />
        <section className="work-area workflow-list-area">
          <header className="page-heading workflow-library-heading">
            <div className="workflow-library-heading-copy">
              <div className="page-eyebrow">Workflow</div>
              <h2><span className="title-icon subtle" aria-hidden="true"><LibraryIcon /></span>Workflow</h2>
              <p>Manage definitions, drafts, and releases from one operating surface.</p>
            </div>
            <div className="page-heading-meta workflow-library-stats">
              <div className="heading-stat">
                <strong>{definitions.length}</strong>
                <span>Workflows</span>
              </div>
              <div className="heading-stat">
                <strong>{definitions.filter((definition) => definition.status === 'Draft').length}</strong>
                <span>Drafts</span>
              </div>
              <div className="heading-stat">
                <strong>{definitions.filter((definition) => definition.status === 'Published').length}</strong>
                <span>Live</span>
              </div>
              <button className="primary-action workflow-create-button" type="button" onClick={() => setIsCreateModalOpen(true)} disabled={busy || !selectedUser}>
                <span className="button-label"><span className="button-icon" aria-hidden="true"><CreateIcon /></span>New workflow</span>
              </button>
            </div>
          </header>

          <section className="panel workflow-table-panel workflow-library-panel">
            <div className="panel-header workflow-library-panel-header">
              <div className="workflow-library-panel-copy">
                <h2><span className="title-icon subtle" aria-hidden="true"><DefinitionIcon /></span>Workflow Definitions</h2>
                <p>
                  Showing {pagedDefinitions.length} workflow{pagedDefinitions.length === 1 ? '' : 's'} on this page,
                  {` ${filteredDefinitions.length} of ${definitions.length} total in the current view.`}
                </p>
              </div>
              <div className="workflow-library-panel-meta">
                <span className="workflow-panel-pill">Versioned publishing</span>
                <span className="workflow-panel-pill">Open designer</span>
              </div>
            </div>

            <div className="list-toolbar workflow-list-toolbar workflow-library-toolbar">
              <label className="toolbar-search-field">
                <span>Search</span>
                <input
                  aria-label="Search workflows"
                  placeholder="Name, description, or status"
                  value={definitionSearch}
                  onChange={(event) => setDefinitionSearch(event.target.value)}
                />
              </label>
              <label className="toolbar-filter-field">
                <span>Status</span>
                <select
                  aria-label="Filter workflow status"
                  value={definitionStatusFilter}
                  onChange={(event) => setDefinitionStatusFilter(event.target.value)}
                >
                  <option>All</option>
                  <option>Draft</option>
                  <option>Published</option>
                  <option>Archived</option>
                </select>
              </label>
            </div>

            <div className="workflow-table">
              <div className="workflow-table-header workflow-table-grid workflow-management-grid">
                <button className={`table-sort-button ${sortBy === 'status' ? `active ${sortDirection}` : 'inactive'}`} type="button" onClick={() => toggleSort('status')}>
                  <span>Status</span>
                  <strong>{sortBy === 'status' ? (sortDirection === 'asc' ? '↑' : '↓') : '↕'}</strong>
                </button>
                <button className={`table-sort-button ${sortBy === 'name' ? `active ${sortDirection}` : 'inactive'}`} type="button" onClick={() => toggleSort('name')}>
                  <span>Workflow</span>
                  <strong>{sortBy === 'name' ? (sortDirection === 'asc' ? '↑' : '↓') : '↕'}</strong>
                </button>
                <span>Versioning</span>
                <button className={`table-sort-button ${sortBy === 'updated' ? `active ${sortDirection}` : 'inactive'}`} type="button" onClick={() => toggleSort('updated')}>
                  <span>Updated</span>
                  <strong>{sortBy === 'updated' ? (sortDirection === 'asc' ? '↑' : '↓') : '↕'}</strong>
                </button>
                <span>Actions</span>
              </div>
              {busy && displayDefinitions.length === 0 && (
                <div className="workflow-skeleton-list" aria-hidden="true">
                  {Array.from({ length: 5 }).map((_, index) => (
                    <div key={index} className="workflow-table-row workflow-table-grid workflow-management-grid workflow-skeleton-row">
                      <div className="workflow-status-cell">
                        <span className="skeleton-block skeleton-pill" />
                        <span className="skeleton-block skeleton-text short" />
                      </div>
                      <div className="row-stack">
                        <span className="skeleton-block skeleton-text medium" />
                        <span className="skeleton-block skeleton-text long" />
                        <span className="skeleton-block skeleton-text short" />
                      </div>
                      <div className="row-stack">
                        <span className="skeleton-block skeleton-text medium" />
                        <div className="workflow-version-tags">
                          <span className="skeleton-block skeleton-tag" />
                          <span className="skeleton-block skeleton-tag" />
                        </div>
                      </div>
                      <div className="row-stack">
                        <span className="skeleton-block skeleton-text medium" />
                        <span className="skeleton-block skeleton-text short" />
                      </div>
                      <div className="table-action-row workflow-library-action-row">
                        <span className="skeleton-block skeleton-button" />
                        <span className="skeleton-block skeleton-button compact" />
                      </div>
                    </div>
                  ))}
                </div>
              )}
              {!busy && displayDefinitions.length === 0 && (
                <div className="empty-state-card compact">
                  <div className="empty-state-icon">WL</div>
                  <strong>No matching workflows</strong>
                  <p>Try widening the search terms or switching the status filter to find saved definitions.</p>
                </div>
              )}
              {pagedDefinitions.map((definition) => (
                <div
                  key={definition.id}
                  className="workflow-table-row workflow-table-grid workflow-management-grid"
                >
                  <div className="workflow-status-cell">
                    <span className={`status-pill ${definition.status.toLowerCase()}`}>{definition.status}</span>
                    <small>{definition.status === 'Draft' ? 'Editable' : definition.status === 'Published' ? 'Live' : 'Inactive'}</small>
                  </div>
                  <div className="row-stack workflow-name-cell">
                    <strong>{definition.name}</strong>
                    <small>{definition.description || 'No description'}</small>
                    <small className="workflow-id-text">{definition.id}</small>
                  </div>
                  <div className="row-stack version-summary-cell">
                    <strong>{definition.status === 'Draft' ? 'Draft v1 ready for design' : 'Release history available'}</strong>
                    <div className="workflow-version-tags">
                      <span>{definition.status === 'Draft' ? 'Editable draft' : 'Published lineage'}</span>
                    </div>
                  </div>
                  <div className="row-stack workflow-time-cell">
                    <strong>{formatDate(definition.updatedAtUtc || definition.createdAtUtc)}</strong>
                    <small>Created {formatDate(definition.createdAtUtc)}</small>
                  </div>
                  <div className="table-action-row workflow-library-action-row">
                    <div className="workflow-action-cluster">
                      <button className="table-action-button designer-action-button" type="button" onClick={() => openDesigner(definition.id)} disabled={busy || !selectedUser}>
                        <span className="button-label"><span className="button-icon" aria-hidden="true"><EditIcon /></span>Design</span>
                      </button>
                      <div className="table-action-menu-shell" data-workflow-action-menu="true">
                        <button
                          className="table-action-button icon-action-button overflow-action-button"
                          type="button"
                          onClick={(event) => {
                            event.stopPropagation()
                            setOpenActionMenuId((current) => current === definition.id ? null : definition.id)
                          }}
                          disabled={busy || !selectedUser}
                          aria-label={`More actions for ${definition.name}`}
                          aria-expanded={openActionMenuId === definition.id}
                          title={`More actions for ${definition.name}`}
                        >
                          <span className="button-icon" aria-hidden="true"><MoreIcon /></span>
                        </button>
                        {openActionMenuId === definition.id && (
                          <div className="table-action-menu workflow-action-menu">
                            <button
                              type="button"
                              onClick={() => {
                                setOpenActionMenuId(null)
                                void openVersions(definition)
                              }}
                              disabled={busy || !selectedUser}
                            >
                              <span className="button-label"><span className="button-icon" aria-hidden="true"><HistoryIcon /></span>History</span>
                            </button>
                            <button
                              className="publish-action-button"
                              type="button"
                              onClick={() => {
                                setOpenActionMenuId(null)
                                onSelectDefinition(definition.id)
                                setIsPublishModalOpen(true)
                              }}
                              disabled={busy || !selectedUser}
                            >
                              <span className="button-label"><span className="button-icon" aria-hidden="true"><PublishIcon /></span>Publish</span>
                            </button>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
            {!busy && displayDefinitions.length > 0 && (
              <div className="page-toolbar workflow-library-pagination">
                <div className="workflow-library-pagination-summary">
                  <span>Showing {pageStart}-{pageEnd} of {displayDefinitions.length}</span>
                </div>
                <label className="workflow-library-page-size">
                  <span>Rows</span>
                  <select
                    value={pageSize}
                    onChange={(event) => {
                      setPageSize(Number(event.target.value))
                      setCurrentPage(1)
                    }}
                  >
                    <option value={10}>10</option>
                    <option value={25}>25</option>
                    <option value={50}>50</option>
                  </select>
                </label>
                <div className="workflow-library-pagination-controls">
                  <button type="button" onClick={() => setCurrentPage(1)} disabled={currentPage === 1}>
                    First
                  </button>
                  <button type="button" onClick={() => setCurrentPage((page) => Math.max(1, page - 1))} disabled={currentPage === 1}>
                    Previous
                  </button>
                  <div className="workflow-library-pagination-pages">
                    {visiblePageNumbers.map((pageNumber) => (
                      <button
                        key={pageNumber}
                        type="button"
                        className={pageNumber === currentPage ? 'is-active' : ''}
                        onClick={() => setCurrentPage(pageNumber)}
                        aria-current={pageNumber === currentPage ? 'page' : undefined}
                      >
                        {pageNumber}
                      </button>
                    ))}
                  </div>
                  <span>Page {currentPage} / {totalPages}</span>
                  <button type="button" onClick={() => setCurrentPage((page) => Math.min(totalPages, page + 1))} disabled={currentPage === totalPages}>
                    Next
                  </button>
                  <button type="button" onClick={() => setCurrentPage(totalPages)} disabled={currentPage === totalPages}>
                    Last
                  </button>
                </div>
              </div>
            )}
          </section>
        </section>

        {isCreateModalOpen && (
          <section className="modal-backdrop" role="presentation">
            <div className="modal-dialog modal-dialog--medium" role="dialog" aria-modal="true" aria-labelledby="create-workflow-title" onClick={(event) => event.stopPropagation()}>
              <div className="modal-header">
                <div>
                  <h3 id="create-workflow-title"><span className="title-icon subtle" aria-hidden="true"><CreateIcon /></span>Create workflow</h3>
                  <p>New workflows start as draft so versioning and designer edits stay controlled from the table.</p>
                </div>
                <button className="modal-close-button" type="button" onClick={() => setIsCreateModalOpen(false)} disabled={busy}>Close</button>
              </div>
              <div className="modal-body">
                <div className="modal-intro-strip">
                  <div className="modal-intro-copy">
                    <strong>Draft workspace</strong>
                    <span>Create the definition first, then continue versioning and design from the workflow table.</span>
                  </div>
                  <div className="modal-intro-metrics">
                    <div className="modal-intro-metric">
                      <span>Status</span>
                      <strong>Draft</strong>
                    </div>
                    <div className="modal-intro-metric">
                      <span>Lifecycle</span>
                      <strong>Table-managed</strong>
                    </div>
                  </div>
                </div>
                <div className="form-grid one-column modal-section-card">
                  <label className="field">
                    <span>Workflow name</span>
                    <input value={createName} onChange={(event) => setCreateName(event.target.value)} placeholder="Enter workflow name" />
                  </label>
                  <label className="field">
                    <span>Description</span>
                    <textarea
                      className="code-editor small publish-message-input"
                      value={createDescription}
                      onChange={(event) => setCreateDescription(event.target.value)}
                      rows={4}
                      placeholder="Describe the purpose of this workflow."
                    />
                  </label>
                </div>
              </div>
              <div className="modal-footer">
                <div className="modal-footer-actions">
                  <button className="secondary-action" type="button" onClick={() => setIsCreateModalOpen(false)} disabled={busy}>Cancel</button>
                  <button className="primary-action" type="button" onClick={() => void handleCreateSubmit()} disabled={busy || !selectedUser}>
                    <span className="button-label"><span className="button-icon">CR</span>Create draft</span>
                  </button>
                </div>
              </div>
            </div>
          </section>
        )}

        {isVersionsModalOpen && (
          <section className="modal-backdrop" role="presentation">
            <div className="modal-dialog modal-dialog--large versions-modal" role="dialog" aria-modal="true" aria-labelledby="versions-modal-title" onClick={(event) => event.stopPropagation()}>
              <div className="modal-header">
                <div>
                  <h3 id="versions-modal-title"><span className="title-icon subtle">VS</span>{versionDefinition?.name || 'Workflow'} versions</h3>
                  <p>Published releases stay visible here while the workflow table remains the primary management surface.</p>
                </div>
                <button className="modal-close-button" type="button" onClick={() => setIsVersionsModalOpen(false)} disabled={busy}>Close</button>
              </div>
              <div className="modal-body">
                <div className="modal-intro-strip">
                  <div className="modal-intro-copy">
                    <strong>Release lineage</strong>
                    <span>Use this view for publish history while draft editing remains inside the designer workspace.</span>
                  </div>
                  <div className="modal-intro-metrics">
                    <div className="modal-intro-metric">
                      <span>Versions</span>
                      <strong>{versions.length}</strong>
                    </div>
                    <div className="modal-intro-metric">
                      <span>Definition</span>
                      <strong>{versionDefinition ? 'Selected' : 'None'}</strong>
                    </div>
                  </div>
                </div>
                {isLoadingVersions && <div className="info"><strong>Loading versions...</strong></div>}
                {!isLoadingVersions && !versionsError && versions.length === 0 && (
                  <div className="info">
                    <strong>No published versions yet.</strong>
                    <span>This workflow currently exists only as a draft.</span>
                  </div>
                )}
                {!isLoadingVersions && !versionsError && versions.length > 0 && (
                  <div className="version-history-list">
                    {versions.map((version) => (
                      <article key={version.id} className="version-history-card">
                        <div className="version-history-header">
                          <div className="row-stack">
                            <strong>v{version.versionNumber}</strong>
                            <small>{formatDate(version.publishedAtUtc || version.effectiveFromUtc)}</small>
                          </div>
                          <span className={`status-pill ${version.status.toLowerCase()}`}>{version.status}</span>
                        </div>
                        <div className="version-history-meta">
                          <span>Effective from {formatDate(version.effectiveFromUtc)}</span>
                          <span>{version.effectiveToUtc ? `Effective to ${formatDate(version.effectiveToUtc)}` : 'No end date'}</span>
                          <span>{version.publishedBy ? `Published by ${version.publishedBy}` : 'Publisher unavailable'}</span>
                        </div>
                        <p>{version.publishMessage || 'No publish message recorded for this version.'}</p>
                      </article>
                    ))}
                  </div>
                )}
              </div>
              <div className="modal-footer">
                <div className="modal-footer-actions">
                  {versionDefinition && (
                    <button className="secondary-action" type="button" onClick={() => {
                      setIsVersionsModalOpen(false)
                      openDesigner(versionDefinition.id)
                    }} disabled={busy || !selectedUser}>
                      <span className="button-label"><span className="button-icon">DG</span>Open draft designer</span>
                    </button>
                  )}
                  <button className="primary-action" type="button" onClick={() => setIsVersionsModalOpen(false)}>
                    <span className="button-label"><span className="button-icon">OK</span>Done</span>
                  </button>
                </div>
              </div>
            </div>
          </section>
        )}
      </>
    )
  }

  return (
    <>
      <ToastStack error={versionsError} onDismissError={() => setVersionsError('')} />
      <section className="work-area workflow-editor-area">
        {validation && (
          !validation.isValid && (
            <div className="validation invalid designer-inline-validation designer-inline-validation--compact">
              <strong>Validation errors</strong>
              {validation.errors.map((item) => <span key={item}>{item}</span>)}
            </div>
          )
        )}

        <div className="designer-fullscreen-workspace">
          <main className="designer-canvas-stage">
            <div className="designer-surface">
              <div className="designer-surface-header">
                <div className="designer-surface-header-left">
                  <div className="designer-surface-title">
                    <strong>{selectedDefinition ? selectedDefinition.name : 'Workflow draft'}</strong>
                  </div>
                  <div className="designer-surface-meta">
                    {selectedDefinition?.status && (
                      <span className="designer-commandbar-pill">{selectedDefinition.status}</span>
                    )}
                    {validation && (
                      <span className={`designer-commandbar-pill ${validation.isValid ? 'is-valid' : 'is-invalid'}`}>
                        {validation.isValid ? 'Valid' : `${validation.errors.length} issue${validation.errors.length === 1 ? '' : 's'}`}
                      </span>
                    )}
                  </div>
                </div>
                <div className="designer-surface-header-center">
                  <div className="designer-surface-viewport-controls">
                    <button type="button" onClick={() => viewportActions?.reset()} disabled={!viewportActions}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><ResetViewIcon /></span>Reset</span>
                    </button>
                    <button type="button" onClick={() => viewportActions?.zoomOut()} disabled={!viewportActions}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><ZoomOutIcon /></span>Zoom -</span>
                    </button>
                    <button type="button" onClick={() => viewportActions?.zoomIn()} disabled={!viewportActions}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><ZoomInIcon /></span>Zoom +</span>
                    </button>
                  </div>
                </div>
                <div className="designer-surface-tools">
                  <div className="designer-surface-actions">
                    <button className="secondary-action designer-back-button" type="button" onClick={onShowList}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><BackIcon /></span>Back</span>
                    </button>
                    <button className="secondary-action designer-save-button" type="button" onClick={onSaveDraft} disabled={busy || !selectedUser || !selectedDefinition || !canEditDraft}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><SaveIcon /></span>Save</span>
                    </button>
                    <button className="primary-action designer-publish-button" type="button" onClick={() => setIsPublishModalOpen(true)} disabled={busy || !selectedUser || !selectedDefinition || validation?.isValid === false}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><PublishIcon /></span>Publish Live</span>
                    </button>
                  </div>
                </div>
              </div>
              <div className="designer-surface-body">
                <WorkflowDesigner value={draftJson} onChange={setDraftJson} onViewportActionsChange={setViewportActions} readonly={!canEditDraft} externalApiEndpoints={externalApiEndpoints} roles={roles} selectedUserId={selectedUserId} userGroups={userGroups} users={users} />
              </div>
              <div className="designer-surface-statusbar">
                <span><CanvasIcon /> Visual designer</span>
                <span><StatusDot tone={validation?.isValid === false ? 'danger' : 'ok'} /> {validation ? (validation.isValid ? 'Definition valid' : `${validation.errors.length} validation issue${validation.errors.length === 1 ? '' : 's'}`) : 'Validation pending'}</span>
                <span><StatusDot tone={canEditDraft ? 'ok' : 'muted'} /> {canEditDraft ? 'Draft editable' : 'Designer locked'}</span>
              </div>
            </div>
          </main>
        </div>
      </section>

      {isPublishModalOpen && (
        <section className="modal-backdrop" role="presentation">
          <div className="modal-dialog modal-dialog--medium publish-modal" role="dialog" aria-modal="true" aria-labelledby="publish-modal-title" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3 id="publish-modal-title"><span className="title-icon subtle" aria-hidden="true"><PublishIcon /></span>Publish workflow</h3>
                <p>Set the effective window and add a release note for this version.</p>
              </div>
              <button className="modal-close-button" type="button" onClick={() => setIsPublishModalOpen(false)} disabled={busy}>Close</button>
            </div>
            <div className="modal-body">
              <div className="modal-intro-strip">
                <div className="modal-intro-copy">
                  <strong>Release control</strong>
                  <span>Publishing creates a versioned release with an effective date window and an audit-friendly message.</span>
                </div>
                <div className="modal-intro-metrics">
                  <div className="modal-intro-metric">
                    <span>Workflow</span>
                    <strong>{selectedDefinition?.name || 'Draft'}</strong>
                  </div>
                  <div className="modal-intro-metric">
                    <span>Result</span>
                    <strong>New version</strong>
                  </div>
                </div>
              </div>
              <div className="form-grid one-column modal-section-card">
                <label className="field">
                  <span>Effective from</span>
                  <input type="datetime-local" value={effectiveFromUtc} onChange={(event) => setEffectiveFromUtc(event.target.value)} />
                </label>
                <label className="field">
                  <span>Effective to</span>
                  <input type="datetime-local" value={effectiveToUtc} onChange={(event) => setEffectiveToUtc(event.target.value)} />
                </label>
                <label className="field">
                  <span>Publish message</span>
                  <textarea
                    className="code-editor small publish-message-input"
                    value={publishMessage}
                    onChange={(event) => setPublishMessage(event.target.value)}
                    rows={5}
                    placeholder="Summarize what changed in this release."
                  />
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <div className="modal-footer-actions">
                <button className="secondary-action" type="button" onClick={() => setIsPublishModalOpen(false)} disabled={busy}>Cancel</button>
                <button className="primary-action designer-publish-button publish-confirm-button" type="button" onClick={() => void handlePublishSubmit()} disabled={busy || !selectedUser || !selectedDefinition || validation?.isValid === false}>
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><PublishIcon /></span>Publish Release</span>
                </button>
              </div>
            </div>
          </div>
        </section>
      )}
    </>
  )

  return (
    <>
      <section className="work-area workflow-editor-area">
        <header className="page-heading workflow-library-heading designer-page-heading">
          <div className="designer-topbar-brandbar">
            <button className="designer-nav-button" type="button" onClick={onShowList}>
              <span className="button-label"><span className="button-icon" aria-hidden="true"><BackIcon /></span>Back to list</span>
            </button>

            <div className="designer-topbar-main">
              <div className="designer-topbar-brand">
                <div>
                  <small className="designer-topbar-eyebrow">Workflow designer</small>
                  <strong>{selectedDefinition?.name ?? 'Workflow draft'}</strong>
                  <span>
                    {selectedDefinition?.id || 'Draft workflow workspace'}
                    {selectedDefinition?.status ? ` - ${selectedDefinition?.status}` : ''}
                  </span>
                </div>
              </div>

              <div className="designer-topbar-actions">
                <div className="designer-command-group">
                  <button className="designer-save-button" type="button" onClick={onSaveDraft} disabled={busy || !selectedUser || !selectedDefinition || !canEditDraft}>
                    <span className="button-label"><span className="button-icon" aria-hidden="true"><SaveIcon /></span>Save draft</span>
                  </button>
                </div>
                <button className="primary-action designer-publish-button" type="button" onClick={() => setIsPublishModalOpen(true)} disabled={busy || !selectedUser || !selectedDefinition || validation?.isValid === false}>
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><PublishIcon /></span>Publish Live</span>
                </button>
              </div>
            </div>
          </div>

          <div className="designer-topbar-subbar">
            <div className="designer-topbar-meta designer-topbar-meta-line">
              <span>{canEditDraft ? 'Editable' : 'Read only'}</span>
              <span>&bull;</span>
              <span>{selectedDefinition?.status === 'Draft' ? 'Draft workflow' : selectedDefinition?.status ? `${selectedDefinition?.status} workflow` : 'Workflow draft'}</span>
              {selectedDefinition?.updatedAtUtc && (
                <>
                  <span>&bull;</span>
                  <span>Updated {formatDate(selectedDefinition?.updatedAtUtc ?? '')}</span>
                </>
              )}
            </div>
            <div className="designer-topbar-meta designer-topbar-meta-line">
              <span>Canvas workspace</span>
            </div>
          </div>
        </header>

        {validation && (
          <div className={validation?.isValid ? 'validation valid designer-inline-validation' : 'validation invalid designer-inline-validation'}>
            <strong>{validation?.isValid ? 'Valid workflow definition' : 'Validation errors'}</strong>
            {!validation?.isValid && validation?.errors.map((item) => <span key={item}>{item}</span>)}
          </div>
        )}

        <div className="designer-fullscreen-workspace">
          <main className="designer-canvas-stage">
            <div className="designer-surface">
              <div className="designer-surface-header">
                <div className="designer-surface-header-left">
                  <div className="designer-surface-title">
                    <strong>Canvas workspace</strong>
                    <span>Build the flow visually and configure steps from the docked editor.</span>
                  </div>
                  <div className="designer-surface-meta">
                    <span>{canEditDraft ? 'Editable draft' : 'Read only'}</span>
                    <span>{validation?.isValid ? 'Validated' : validation ? 'Needs attention' : 'Not validated'}</span>
                  </div>
                </div>
                <div className="designer-surface-header-center">
                  <div className="designer-surface-viewport-controls">
                    <button type="button" onClick={() => viewportActions?.reset()} disabled={!viewportActions}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><ResetViewIcon /></span>Reset view</span>
                    </button>
                    <button type="button" onClick={() => viewportActions?.zoomOut()} disabled={!viewportActions}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><ZoomOutIcon /></span>Zoom out</span>
                    </button>
                    <button type="button" onClick={() => viewportActions?.zoomIn()} disabled={!viewportActions}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><ZoomInIcon /></span>Zoom in</span>
                    </button>
                  </div>
                </div>
                <div className="designer-surface-tools">
                  <div className="designer-surface-actions">
                    <button className="secondary-action designer-back-button" type="button" onClick={onShowList}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><BackIcon /></span>Back to list</span>
                    </button>
                    <button className="secondary-action designer-save-button" type="button" onClick={onSaveDraft} disabled={busy || !selectedUser || !selectedDefinition || !canEditDraft}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><SaveIcon /></span>Save draft</span>
                    </button>
                    <button className="primary-action designer-publish-button" type="button" onClick={() => setIsPublishModalOpen(true)} disabled={busy || !selectedUser || !selectedDefinition || validation?.isValid === false}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><PublishIcon /></span>Publish Live</span>
                    </button>
                  </div>
                </div>
              </div>
              <div className="designer-surface-body">
              <WorkflowDesigner value={draftJson} onChange={setDraftJson} onViewportActionsChange={setViewportActions} readonly={!canEditDraft} externalApiEndpoints={externalApiEndpoints} roles={roles} selectedUserId={selectedUserId} userGroups={userGroups} users={users} />
              </div>
              <div className="designer-surface-statusbar">
                <span><CanvasIcon /> Visual designer</span>
                <span><StatusDot tone={validation?.isValid === false ? 'danger' : 'ok'} /> {validation?.isValid ? 'Definition valid' : validation ? `${validation?.errors?.length ?? 0} validation issue${(validation?.errors?.length ?? 0) === 1 ? '' : 's'}` : 'Validation pending'}</span>
                <span><StatusDot tone={canEditDraft ? 'ok' : 'muted'} /> {canEditDraft ? 'Draft editable' : 'Designer locked'}</span>
              </div>
            </div>
          </main>
        </div>
      </section>

      {isPublishModalOpen && (
        <section className="modal-backdrop" role="presentation">
          <div className="modal-dialog modal-dialog--medium publish-modal" role="dialog" aria-modal="true" aria-labelledby="publish-modal-title" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3 id="publish-modal-title"><span className="title-icon subtle" aria-hidden="true"><PublishIcon /></span>Publish workflow</h3>
                <p>Set the effective window and add a release note for this version.</p>
              </div>
              <button className="modal-close-button" type="button" onClick={() => setIsPublishModalOpen(false)} disabled={busy}>Close</button>
            </div>
            <div className="modal-body">
              <div className="modal-intro-strip">
                <div className="modal-intro-copy">
                  <strong>Release control</strong>
                  <span>Publishing creates a versioned release with an effective date window and an audit-friendly message.</span>
                </div>
                <div className="modal-intro-metrics">
                  <div className="modal-intro-metric">
                    <span>Workflow</span>
                    <strong>{selectedDefinition?.name || 'Draft'}</strong>
                  </div>
                  <div className="modal-intro-metric">
                    <span>Result</span>
                    <strong>New version</strong>
                  </div>
                </div>
              </div>
              <div className="form-grid one-column modal-section-card">
                <label className="field">
                  <span>Effective from</span>
                  <input type="datetime-local" value={effectiveFromUtc} onChange={(event) => setEffectiveFromUtc(event.target.value)} />
                </label>
                <label className="field">
                  <span>Effective to</span>
                  <input type="datetime-local" value={effectiveToUtc} onChange={(event) => setEffectiveToUtc(event.target.value)} />
                </label>
                <label className="field">
                  <span>Publish message</span>
                  <textarea
                    className="code-editor small publish-message-input"
                    value={publishMessage}
                    onChange={(event) => setPublishMessage(event.target.value)}
                    rows={5}
                    placeholder="Summarize what changed in this release."
                  />
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <div className="modal-footer-actions">
                <button className="secondary-action" type="button" onClick={() => setIsPublishModalOpen(false)} disabled={busy}>Cancel</button>
                <button className="primary-action designer-publish-button publish-confirm-button" type="button" onClick={() => void handlePublishSubmit()} disabled={busy || !selectedUser || !selectedDefinition || validation?.isValid === false}>
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><PublishIcon /></span>Publish Release</span>
                </button>
              </div>
            </div>
          </div>
        </section>
      )}
    </>
  )
}

function getStoredWorkflowLibraryPageSize() {
  if (typeof window === 'undefined') {
    return 10
  }

  const storedValue = Number.parseInt(window.localStorage.getItem(workflowLibraryPageSizeStorageKey) ?? '', 10)
  return [10, 25, 50].includes(storedValue) ? storedValue : 10
}

function EditIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <path d="M2.5 11.5 11 3a1.8 1.8 0 1 1 2.5 2.5L5 14H2.5v-2.5Z" />
    </svg>
  )
}

function BackIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M7 3.5 2.5 8 7 12.5" />
      <path d="M3 8h10" />
    </svg>
  )
}

function SaveIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3 3.5h8l2 2v7H3v-9Z" />
      <path d="M5.2 3.5v3h5.2v-3" />
      <path d="M5.4 12v-3h5.2v3" />
    </svg>
  )
}

function CanvasIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <rect x="2.5" y="3" width="11" height="10" rx="1.5" />
      <path d="M5.2 6.1h5.6" />
      <path d="M5.2 8.4h5.6" />
      <path d="M5.2 10.7h3.2" />
    </svg>
  )
}

function ResetViewIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3.5 8a4.5 4.5 0 1 0 1.2-3.1" />
      <path d="M3.5 4v2.6h2.6" />
    </svg>
  )
}

function ZoomOutIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="7" cy="7" r="3.8" />
      <path d="M4.8 7h4.4" />
      <path d="m10 10 2.5 2.5" />
    </svg>
  )
}

function ZoomInIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="7" cy="7" r="3.8" />
      <path d="M4.8 7h4.4" />
      <path d="M7 4.8v4.4" />
      <path d="m10 10 2.5 2.5" />
    </svg>
  )
}

function StatusDot({ tone }: { tone: 'ok' | 'danger' | 'muted' }) {
  return <span className={`designer-status-dot ${tone}`} aria-hidden="true" />
}

function LibraryIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3 3.5h8.5a1.5 1.5 0 0 1 1.5 1.5v7.5H4.5A1.5 1.5 0 0 0 3 14V3.5Z" />
      <path d="M3 12.5h8.5" />
      <path d="M6 6h4" />
      <path d="M6 8.5h3.2" />
    </svg>
  )
}

function DefinitionIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="2.5" width="10" height="11" rx="1.5" />
      <path d="M5.5 5.5h5" />
      <path d="M5.5 8h5" />
      <path d="M5.5 10.5h3.5" />
    </svg>
  )
}

function CreateIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <path d="M8 3.2v9.6" />
      <path d="M3.2 8h9.6" />
    </svg>
  )
}

function MoreIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="currentColor">
      <circle cx="3" cy="8" r="1.2" />
      <circle cx="8" cy="8" r="1.2" />
      <circle cx="13" cy="8" r="1.2" />
    </svg>
  )
}

function HistoryIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <path d="M2.5 8a5.5 5.5 0 1 0 1.6-3.9" />
      <path d="M2.5 2.8v2.8h2.8" />
      <path d="M8 4.8V8l2.2 1.4" />
    </svg>
  )
}

function PublishIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <path d="M8 11.8V3.4" />
      <path d="m4.8 6.6 3.2-3.2 3.2 3.2" />
      <path d="M3 12.8h10" />
    </svg>
  )
}
