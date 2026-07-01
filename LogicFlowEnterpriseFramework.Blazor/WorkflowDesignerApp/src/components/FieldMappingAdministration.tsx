import { useEffect, useMemo, useState } from 'react'
import type {
  ExternalDatabaseConnection,
  ExternalDatabaseObject,
  ExternalDatabaseObjectDetail,
  ExternalDatabaseSchema,
  ExternalFieldMapping,
  ExternalSystem,
  UpsertExternalFieldMappingInput,
} from '../api'
import { formatDate } from '../format'

type FieldMappingAdministrationProps = {
  busy: boolean
  databaseConnections: ExternalDatabaseConnection[]
  externalSystems: ExternalSystem[]
  mappings: ExternalFieldMapping[]
  onLoadDatabaseObjectDetail: (connectionId: string, schema: string, objectName: string) => Promise<ExternalDatabaseObjectDetail | null>
  onLoadDatabaseObjects: (connectionId: string, schema?: string, type?: string) => Promise<ExternalDatabaseObject[]>
  onLoadDatabaseSchemas: (connectionId: string) => Promise<ExternalDatabaseSchema[]>
  onRefresh: () => void
  onSave: (payload: UpsertExternalFieldMappingInput) => Promise<void>
}

const emptyForm: UpsertExternalFieldMappingInput = {
  id: '',
  externalSystemId: '',
  localEntityType: 'User',
  localField: '',
  externalField: '',
  transformRule: '',
  isKey: false,
  isEnabled: true,
}

export function FieldMappingAdministration({
  busy,
  databaseConnections,
  externalSystems,
  mappings,
  onLoadDatabaseObjectDetail,
  onLoadDatabaseObjects,
  onLoadDatabaseSchemas,
  onRefresh,
  onSave,
}: FieldMappingAdministrationProps) {
  const [search, setSearch] = useState('')
  const [systemFilter, setSystemFilter] = useState('All')
  const [entityFilter, setEntityFilter] = useState('All')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [form, setForm] = useState<UpsertExternalFieldMappingInput>(emptyForm)
  const [selectedConnectionId, setSelectedConnectionId] = useState('')
  const [selectedSchema, setSelectedSchema] = useState('')
  const [selectedObjectName, setSelectedObjectName] = useState('')
  const [databaseSchemas, setDatabaseSchemas] = useState<ExternalDatabaseSchema[]>([])
  const [databaseObjects, setDatabaseObjects] = useState<ExternalDatabaseObject[]>([])
  const [selectedObjectDetail, setSelectedObjectDetail] = useState<ExternalDatabaseObjectDetail | null>(null)

  useEffect(() => {
    if (!isModalOpen) {
      setForm({ ...emptyForm, externalSystemId: externalSystems[0]?.id ?? '' })
      setSelectedConnectionId('')
      setSelectedSchema('')
      setSelectedObjectName('')
      setDatabaseSchemas([])
      setDatabaseObjects([])
      setSelectedObjectDetail(null)
    }
  }, [externalSystems, isModalOpen])

  const availableConnections = useMemo(
    () => databaseConnections.filter((connection) => connection.externalSystemId === form.externalSystemId),
    [databaseConnections, form.externalSystemId],
  )

  const localFieldOptions = useMemo(
    () => getLocalFieldOptions(form.localEntityType),
    [form.localEntityType],
  )

  useEffect(() => {
    if (!isModalOpen || !form.externalSystemId) return

    const nextConnectionId = availableConnections.some((connection) => connection.id === selectedConnectionId)
      ? selectedConnectionId
      : availableConnections[0]?.id ?? ''

    setSelectedConnectionId(nextConnectionId)
  }, [availableConnections, form.externalSystemId, isModalOpen, selectedConnectionId])

  useEffect(() => {
    if (!isModalOpen || !selectedConnectionId) {
      setDatabaseSchemas([])
      setDatabaseObjects([])
      setSelectedObjectDetail(null)
      return
    }

    let isCancelled = false

    void Promise.all([
      onLoadDatabaseSchemas(selectedConnectionId),
      onLoadDatabaseObjects(selectedConnectionId, selectedSchema || undefined, 'All'),
    ]).then(([schemas, objects]) => {
      if (isCancelled) return
      setDatabaseSchemas(schemas)
      setDatabaseObjects(objects)
    })

    return () => {
      isCancelled = true
    }
  }, [isModalOpen, onLoadDatabaseObjects, onLoadDatabaseSchemas, selectedConnectionId, selectedSchema])

  const filteredMappings = useMemo(
    () => mappings.filter((mapping) => {
      const term = search.trim().toLowerCase()
      const matchesSystem = systemFilter === 'All' || mapping.externalSystemId === systemFilter
      const matchesEntity = entityFilter === 'All' || mapping.localEntityType === entityFilter
      const matchesSearch = !term
        || mapping.localField.toLowerCase().includes(term)
        || mapping.externalField.toLowerCase().includes(term)
        || (mapping.transformRule ?? '').toLowerCase().includes(term)

      return matchesSystem && matchesEntity && matchesSearch
    }),
    [entityFilter, mappings, search, systemFilter],
  )

  function openCreate() {
    setForm({
      ...emptyForm,
      externalSystemId: externalSystems[0]?.id ?? '',
    })
    setIsModalOpen(true)
  }

  function openEdit(mapping: ExternalFieldMapping) {
    setForm({
      id: mapping.id,
      externalSystemId: mapping.externalSystemId,
      localEntityType: mapping.localEntityType,
      localField: mapping.localField,
      externalField: mapping.externalField,
      transformRule: mapping.transformRule ?? '',
      isKey: mapping.isKey,
      isEnabled: mapping.isEnabled,
    })
    setIsModalOpen(true)
  }

  async function handleSubmit() {
    await onSave(form)
    setIsModalOpen(false)
  }

  async function handleSelectDatabaseObject(objectName: string) {
    if (!selectedConnectionId || !selectedSchema || !objectName) return

    setSelectedObjectName(objectName)
    const detail = await onLoadDatabaseObjectDetail(selectedConnectionId, selectedSchema, objectName)
    setSelectedObjectDetail(detail)
  }

  function applyExternalField(columnName: string) {
    setForm((current) => ({ ...current, externalField: columnName }))
  }

  return (
    <>
      <section className="work-area workflow-list-area">
        <header className="page-heading">
          <div className="workflow-library-heading-copy">
            <div className="page-eyebrow">Configuration</div>
            <h2><span className="title-icon subtle" aria-hidden="true">FM</span>Field Mapping</h2>
            <p>Define how external identity fields map into local user, role, and group records across connected systems.</p>
          </div>
          <div className="page-heading-meta">
            <div className="heading-stat">
              <strong>{filteredMappings.length}</strong>
              <span>Visible mappings</span>
            </div>
            <div className="heading-stat">
              <strong>{mappings.filter((mapping) => mapping.isEnabled).length}</strong>
              <span>Enabled</span>
            </div>
            <div className="heading-stat">
              <strong>{mappings.filter((mapping) => mapping.isKey).length}</strong>
              <span>Key fields</span>
            </div>
            <button className="primary-action workflow-create-button" type="button" onClick={openCreate} disabled={busy || externalSystems.length === 0}>
              <span className="button-label"><span className="button-icon" aria-hidden="true"><CreateIcon /></span>New mapping</span>
            </button>
          </div>
        </header>

        <section className="panel workflow-table-panel user-directory-panel">
          <div className="panel-header workflow-library-panel-header">
            <div className="workflow-library-panel-copy">
              <h2><span className="title-icon subtle" aria-hidden="true">FM</span>Mapping Directory</h2>
              <p>Keep field ownership explicit between external systems and local workflow identity records.</p>
            </div>
            <div className="workflow-library-panel-meta">
              <span className="workflow-panel-pill">Use key fields to mark external identifiers</span>
              <span className="workflow-panel-pill">Transform rules are optional expression hints</span>
              <button className="table-action-button history-action-button" type="button" onClick={onRefresh} disabled={busy}>
                <span className="button-label"><span className="button-icon" aria-hidden="true"><RefreshInlineIcon /></span>Refresh</span>
              </button>
            </div>
          </div>

          <div className="list-toolbar workflow-list-toolbar workflow-library-toolbar field-mapping-toolbar">
            <label className="toolbar-search-field">
              <span>Search mappings</span>
              <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search local field, external field, or transform rule" />
            </label>
            <label className="toolbar-filter-field">
              <span>System</span>
              <select value={systemFilter} onChange={(event) => setSystemFilter(event.target.value)}>
                <option value="All">All</option>
                {externalSystems.map((system) => (
                  <option key={system.id} value={system.id}>{system.code}</option>
                ))}
              </select>
            </label>
            <label className="toolbar-filter-field">
              <span>Entity</span>
              <select value={entityFilter} onChange={(event) => setEntityFilter(event.target.value)}>
                <option value="All">All</option>
                <option value="User">User</option>
                <option value="Role">Role</option>
                <option value="UserGroup">User Group</option>
              </select>
            </label>
          </div>

          <div className="workflow-table">
            <div className="workflow-table-header workflow-table-grid field-mapping-grid">
              <span>Status</span>
              <span>External system</span>
              <span>Local field</span>
              <span>External field</span>
              <span>Transform</span>
              <span>Updated</span>
              <span>Actions</span>
            </div>
            {filteredMappings.length === 0 && (
              <div className="empty-state-card compact">
                <div className="empty-state-icon">FM</div>
                <strong>No field mappings</strong>
                <p>Create the first mapping to control how external identity data flows into local records.</p>
              </div>
            )}
            {filteredMappings.map((mapping) => (
              <div key={mapping.id} className="workflow-table-row workflow-table-grid field-mapping-grid">
                <div className="workflow-status-cell">
                  <span className={`status-pill ${mapping.isEnabled ? 'active' : 'inactive'}`}>{mapping.isEnabled ? 'Enabled' : 'Disabled'}</span>
                  <small>{mapping.isKey ? 'Key field' : 'Attribute'}</small>
                </div>
                <div className="row-stack workflow-name-cell">
                  <strong>{resolveExternalSystemName(mapping.externalSystemId, externalSystems)}</strong>
                  <small>{mapping.localEntityType}</small>
                </div>
                <div className="row-stack workflow-time-cell">
                  <strong>{mapping.localField}</strong>
                  <small>Local target</small>
                </div>
                <div className="row-stack workflow-time-cell">
                  <strong>{mapping.externalField}</strong>
                  <small>External source</small>
                </div>
                <div className="row-stack workflow-time-cell">
                  <strong>{mapping.transformRule || 'Direct'}</strong>
                  <small>{mapping.isKey ? 'Used for identity keying' : 'Standard mapping'}</small>
                </div>
                <div className="row-stack workflow-time-cell">
                  <strong>{mapping.updatedAtUtc ? formatDate(mapping.updatedAtUtc) : formatDate(mapping.createdAtUtc)}</strong>
                  <small>{mapping.id}</small>
                </div>
                <div className="table-action-row user-action-row">
                  <button className="table-action-button designer-action-button" type="button" onClick={() => openEdit(mapping)} disabled={busy}>
                    <span className="button-label"><span className="button-icon" aria-hidden="true"><EditIcon /></span>Edit</span>
                  </button>
                </div>
              </div>
            ))}
          </div>
        </section>
      </section>

      {isModalOpen && (
      <section className="modal-backdrop" role="presentation">
        <div className="modal-dialog modal-dialog--large" role="dialog" aria-modal="true" aria-labelledby="field-mapping-title" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3 id="field-mapping-title"><span className="title-icon subtle" aria-hidden="true">FM</span>Field Mapping</h3>
                <p>Define how one external source field maps into a local identity attribute.</p>
              </div>
              <button className="modal-close-button" type="button" onClick={() => setIsModalOpen(false)} disabled={busy}>Close</button>
            </div>
            <div className="modal-body">
              <div className="form-grid modal-section-card">
                <label className="field">
                  <span>External system</span>
                  <select value={form.externalSystemId} onChange={(event) => setForm((current) => ({ ...current, externalSystemId: event.target.value }))}>
                    {externalSystems.map((system) => (
                      <option key={system.id} value={system.id}>{system.code} - {system.name}</option>
                    ))}
                  </select>
                </label>
                <label className="field">
                  <span>Local entity</span>
                  <select value={form.localEntityType} onChange={(event) => setForm((current) => ({ ...current, localEntityType: event.target.value, localField: '' }))}>
                    <option value="User">User</option>
                    <option value="Role">Role</option>
                    <option value="UserGroup">User Group</option>
                  </select>
                </label>
                <label className="field">
                  <span>Local field</span>
                  <select value={form.localField} onChange={(event) => setForm((current) => ({ ...current, localField: event.target.value }))}>
                    <option value="">Select local field</option>
                    {localFieldOptions.map((fieldName) => (
                      <option key={fieldName} value={fieldName}>{fieldName}</option>
                    ))}
                  </select>
                </label>
                <label className="field">
                  <span>External field</span>
                  <input value={form.externalField} onChange={(event) => setForm((current) => ({ ...current, externalField: event.target.value }))} />
                  <small className="field-hint">You can type manually, or choose from a connected database source below.</small>
                </label>
                <label className="field">
                  <span>Transform rule</span>
                  <input value={form.transformRule ?? ''} onChange={(event) => setForm((current) => ({ ...current, transformRule: event.target.value }))} />
                </label>
                <label className="field platform-checkbox-field">
                  <span>Mapping options</span>
                  <label className="platform-inline-checkbox">
                    <input type="checkbox" checked={form.isKey} onChange={(event) => setForm((current) => ({ ...current, isKey: event.target.checked }))} />
                    <strong>Use as key field</strong>
                  </label>
                </label>
                <label className="field platform-checkbox-field">
                  <span>Status</span>
                  <label className="platform-inline-checkbox">
                    <input type="checkbox" checked={form.isEnabled} onChange={(event) => setForm((current) => ({ ...current, isEnabled: event.target.checked }))} />
                    <strong>Enabled</strong>
                  </label>
                </label>
                <div className="field procedure-parameter-field">
                  <span>Choose external field from database</span>
                  {availableConnections.length === 0 ? (
                    <div className="empty-state compact">No database connection is registered under this external system yet.</div>
                  ) : (
                    <div className="field-mapping-source-card">
                      <div className="field-mapping-source-header">
                        <div>
                          <strong>Database source picker</strong>
                          <p>Pick a connection, narrow to a schema and source object, then choose the external column to map.</p>
                        </div>
                        <div className="field-mapping-source-state">
                          <span>{selectedConnectionId ? 'Connected source selected' : 'Awaiting source selection'}</span>
                          <strong>{selectedSchema && selectedObjectName ? `${selectedSchema}.${selectedObjectName}` : 'No object loaded'}</strong>
                        </div>
                      </div>
                      <div className="field-mapping-source-controls">
                        <label className="field">
                          <span>Connection</span>
                          <select value={selectedConnectionId} onChange={(event) => {
                            setSelectedConnectionId(event.target.value)
                            setSelectedSchema('')
                            setSelectedObjectName('')
                            setSelectedObjectDetail(null)
                          }}>
                            <option value="">Select DB connection</option>
                            {availableConnections.map((connection) => (
                              <option key={connection.id} value={connection.id}>{connection.code} - {connection.name}</option>
                            ))}
                          </select>
                        </label>
                        <label className="field">
                          <span>Schema</span>
                          <select value={selectedSchema} onChange={(event) => {
                            setSelectedSchema(event.target.value)
                            setSelectedObjectName('')
                            setSelectedObjectDetail(null)
                          }} disabled={!selectedConnectionId}>
                            <option value="">Select schema</option>
                            {databaseSchemas.map((schema) => (
                              <option key={schema.schemaName} value={schema.schemaName}>{schema.schemaName}</option>
                            ))}
                          </select>
                        </label>
                        <label className="field">
                          <span>Table or view</span>
                          <select value={selectedObjectName} onChange={(event) => void handleSelectDatabaseObject(event.target.value)} disabled={!selectedSchema}>
                            <option value="">Select table or view</option>
                            {databaseObjects
                              .filter((item) => item.objectType === 'Table' || item.objectType === 'View')
                              .map((item) => (
                                <option key={`${item.schemaName}.${item.objectName}`} value={item.objectName}>{item.objectName} ({item.objectType})</option>
                              ))}
                          </select>
                        </label>
                      </div>
                      {selectedObjectDetail?.columns.length ? (
                        <div className="field-mapping-column-picker">
                          <div className="field-mapping-column-picker-header">
                            <strong>Available external columns</strong>
                            <span>{selectedObjectDetail.columns.length} fields loaded</span>
                          </div>
                          <div className="field-mapping-column-grid">
                            {selectedObjectDetail.columns.map((column) => (
                              <label key={column.columnName} className={form.externalField === column.columnName ? 'active' : ''}>
                                <input
                                  type="radio"
                                  name="field-mapping-external-column"
                                  checked={form.externalField === column.columnName}
                                  onChange={() => applyExternalField(column.columnName)}
                                />
                                <strong>{column.columnName}</strong>
                                <small>{column.dataType}{column.isPrimaryKey ? ' • Primary key' : ''}</small>
                              </label>
                            ))}
                          </div>
                        </div>
                      ) : (
                        <div className="field-mapping-source-empty">Select a schema and table/view to load external fields.</div>
                      )}
                    </div>
                  )}
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <div className="modal-footer-actions">
                <button className="secondary-action" type="button" onClick={() => setIsModalOpen(false)} disabled={busy}>Cancel</button>
                <button className="primary-action" type="button" onClick={() => void handleSubmit()} disabled={busy || !form.externalSystemId || !form.localField.trim() || !form.externalField.trim()}>
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><SaveIcon /></span>Save mapping</span>
                </button>
              </div>
            </div>
          </div>
        </section>
      )}
    </>
  )
}

function resolveExternalSystemName(systemId: string, systems: ExternalSystem[]) {
  return systems.find((system) => system.id === systemId)?.name ?? 'Unknown system'
}

function getLocalFieldOptions(entityType: string) {
  switch (entityType) {
    case 'User':
      return ['userName', 'displayName', 'email', 'mobilePhone', 'jobTitle', 'departmentCode', 'externalUserId', 'status']
    case 'Role':
      return ['code', 'name', 'description', 'externalRoleId', 'status']
    case 'UserGroup':
      return ['code', 'name', 'description', 'externalGroupId', 'status']
    default:
      return []
  }
}

function RefreshInlineIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M13 8a5 5 0 1 1-1.4-3.5" />
      <path d="M13 3.5v3.3H9.7" />
    </svg>
  )
}

function EditIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="m11.8 2.9 1.3 1.3" />
      <path d="m4.1 10.6 6.8-6.8 1.3 1.3-6.8 6.8-2 .6.7-1.9Z" />
    </svg>
  )
}

function SaveIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3.5 3.5h7l2 2V12.5h-9z" />
      <path d="M5 3.5v3h5v-3" />
      <path d="M5.3 12.5V9.4h5.4v3.1" />
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
