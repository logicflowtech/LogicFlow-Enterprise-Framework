import { useEffect, useMemo, useState } from 'react'
import type {
  ExternalDatabaseLookupIntegration,
  ExternalDatabaseLookupOption,
  ExternalIdentityMapping,
  ExternalSystem,
  Role,
  UpsertExternalIdentityMappingInput,
  UpsertRoleInput,
  UpsertUserGroupInput,
  UpsertUserInput,
  User,
  UserGroup,
} from '../api'
import { formatDate } from '../format'

type IdentityView = 'users' | 'roles' | 'groups'

type UserManagementProps = {
  activeView: IdentityView
  busy: boolean
  externalSystems: ExternalSystem[]
  onLoadLookupIntegrations: (externalSystemId: string) => Promise<ExternalDatabaseLookupIntegration[]>
  onLoadLookupOptions: (integrationId: string, search?: string) => Promise<ExternalDatabaseLookupOption[]>
  onLoadMappings: (entityType: string, localEntityId: string) => Promise<ExternalIdentityMapping[]>
  onRefresh: () => void
  onDeleteExternalMapping: (id: string) => Promise<void>
  onSaveExternalMapping: (payload: UpsertExternalIdentityMappingInput) => Promise<void>
  onSaveRole: (payload: UpsertRoleInput) => Promise<void>
  onSaveUserGroup: (payload: UpsertUserGroupInput) => Promise<void>
  onSaveUser: (payload: UpsertUserInput) => Promise<void>
  roles: Role[]
  selectedUser: User | null
  userGroups: UserGroup[]
  users: User[]
}

const emptyForm: UpsertUserInput = {
  primaryExternalSystemId: '',
  externalUserId: '',
  userName: '',
  displayName: '',
  email: '',
  mobilePhone: '',
  jobTitle: '',
  departmentCode: '',
  managerUserId: '',
  status: 'Active',
}

const emptyRoleForm: UpsertRoleInput = {
  primaryExternalSystemId: '',
  externalRoleId: '',
  code: '',
  name: '',
  description: '',
  status: 'Active',
}

const emptyGroupForm: UpsertUserGroupInput = {
  primaryExternalSystemId: '',
  externalGroupId: '',
  code: '',
  name: '',
  description: '',
  assignmentMode: 'Claim',
  managerUserId: '',
  escalationRoleId: '',
  escalationUserId: '',
  status: 'Active',
  memberUserIds: [],
  roleIds: [],
}

const emptyMappingForm: UpsertExternalIdentityMappingInput = {
  id: '',
  localEntityType: 'User',
  localEntityId: '',
  externalSystemId: '',
  externalEntityType: '',
  externalEntityId: '',
  externalEntityCode: '',
  syncMode: 'ReferenceOnly',
  isPrimary: true,
}

export function UserManagement({
  activeView,
  busy,
  externalSystems,
  onLoadLookupIntegrations,
  onLoadLookupOptions,
  onLoadMappings,
  onRefresh,
  onDeleteExternalMapping,
  onSaveExternalMapping,
  onSaveRole,
  onSaveUser,
  onSaveUserGroup,
  roles,
  selectedUser,
  userGroups,
  users,
}: UserManagementProps) {
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('All')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [isRoleModalOpen, setIsRoleModalOpen] = useState(false)
  const [isGroupModalOpen, setIsGroupModalOpen] = useState(false)
  const [isMappingsModalOpen, setIsMappingsModalOpen] = useState(false)
  const [openActionMenuId, setOpenActionMenuId] = useState<string | null>(null)
  const [sortBy, setSortBy] = useState<'status' | 'name' | 'synced'>('name')
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc')
  const [form, setForm] = useState<UpsertUserInput>(emptyForm)
  const [roleForm, setRoleForm] = useState<UpsertRoleInput>(emptyRoleForm)
  const [groupForm, setGroupForm] = useState<UpsertUserGroupInput>(emptyGroupForm)
  const [mappingForm, setMappingForm] = useState<UpsertExternalIdentityMappingInput>(emptyMappingForm)
  const [mappings, setMappings] = useState<ExternalIdentityMapping[]>([])
  const [mappingEntity, setMappingEntity] = useState<{ type: 'User' | 'Role' | 'UserGroup'; id: string; name: string } | null>(null)
  const [lookupIntegrations, setLookupIntegrations] = useState<ExternalDatabaseLookupIntegration[]>([])
  const [selectedLookupIntegrationId, setSelectedLookupIntegrationId] = useState('')
  const [lookupOptions, setLookupOptions] = useState<ExternalDatabaseLookupOption[]>([])
  const [lookupSearch, setLookupSearch] = useState('')

  const filteredUsers = useMemo(
    () => users.filter((user) => {
      const term = search.trim().toLowerCase()
      const matchesStatus = statusFilter === 'All' || user.status === statusFilter
      const matchesSearch = !term
        || user.displayName.toLowerCase().includes(term)
        || user.userName.toLowerCase().includes(term)
        || (user.email ?? '').toLowerCase().includes(term)
        || (user.externalUserId ?? '').toLowerCase().includes(term)

      return matchesStatus && matchesSearch
    }),
    [search, statusFilter, users],
  )

  const filteredRoles = useMemo(
    () => roles.filter((role) => {
      const term = search.trim().toLowerCase()
      const matchesStatus = statusFilter === 'All' || role.status === statusFilter
      const matchesSearch = !term
        || role.name.toLowerCase().includes(term)
        || role.code.toLowerCase().includes(term)
        || (role.externalRoleId ?? '').toLowerCase().includes(term)

      return matchesStatus && matchesSearch
    }),
    [roles, search, statusFilter],
  )

  const filteredGroups = useMemo(
    () => userGroups.filter((group) => {
      const term = search.trim().toLowerCase()
      const matchesStatus = statusFilter === 'All' || group.status === statusFilter
      const matchesSearch = !term
        || group.name.toLowerCase().includes(term)
        || group.code.toLowerCase().includes(term)
        || (group.externalGroupId ?? '').toLowerCase().includes(term)
        || group.assignmentMode.toLowerCase().includes(term)

      return matchesStatus && matchesSearch
    }),
    [search, statusFilter, userGroups],
  )

  const activeUsers = useMemo(
    () => users.filter((user) => user.status === 'Active').length,
    [users],
  )

  const inactiveUsers = useMemo(
    () => users.filter((user) => user.status === 'Inactive').length,
    [users],
  )

  useEffect(() => {
    if (!isModalOpen) {
      setForm(emptyForm)
    }
  }, [isModalOpen])

  useEffect(() => {
    if (!isRoleModalOpen) {
      setRoleForm(emptyRoleForm)
    }
  }, [isRoleModalOpen])

  useEffect(() => {
    if (!isGroupModalOpen) {
      setGroupForm(emptyGroupForm)
    }
  }, [isGroupModalOpen])

  useEffect(() => {
    if (!isMappingsModalOpen) {
      setMappings([])
      setMappingEntity(null)
      setMappingForm(emptyMappingForm)
      setLookupIntegrations([])
      setSelectedLookupIntegrationId('')
      setLookupOptions([])
      setLookupSearch('')
    }
  }, [isMappingsModalOpen])

  useEffect(() => {
    setSearch('')
    setStatusFilter('All')
    setOpenActionMenuId(null)
  }, [activeView])

  useEffect(() => {
    if (!openActionMenuId) return

    function handleWindowClick() {
      setOpenActionMenuId(null)
    }

    window.addEventListener('click', handleWindowClick)
    return () => window.removeEventListener('click', handleWindowClick)
  }, [openActionMenuId])

  function openCreateModal() {
    setForm({
      ...emptyForm,
      primaryExternalSystemId: externalSystems[0]?.id ?? '',
      managerUserId: selectedUser?.id ?? '',
    })
    setIsModalOpen(true)
  }

  function openCreateRoleModal() {
    setRoleForm({
      ...emptyRoleForm,
      primaryExternalSystemId: externalSystems[0]?.id ?? '',
    })
    setIsRoleModalOpen(true)
  }

  function openEditRoleModal(role: Role) {
    setRoleForm({
      primaryExternalSystemId: role.primaryExternalSystemId ?? externalSystems[0]?.id ?? '',
      externalRoleId: role.externalRoleId ?? '',
      code: role.code,
      name: role.name,
      description: role.description ?? '',
      status: role.status,
    })
    setIsRoleModalOpen(true)
  }

  function openCreateGroupModal() {
    setGroupForm({
      ...emptyGroupForm,
      primaryExternalSystemId: externalSystems[0]?.id ?? '',
    })
    setIsGroupModalOpen(true)
  }

  function openEditGroupModal(group: UserGroup) {
    setGroupForm({
      primaryExternalSystemId: group.primaryExternalSystemId ?? externalSystems[0]?.id ?? '',
      externalGroupId: group.externalGroupId ?? '',
      code: group.code,
      name: group.name,
      description: group.description ?? '',
      assignmentMode: group.assignmentMode,
      managerUserId: '',
      escalationRoleId: '',
      escalationUserId: '',
      status: group.status,
      memberUserIds: group.memberUserIds,
      roleIds: group.roleIds,
    })
    setIsGroupModalOpen(true)
  }

  function openEditModal(user: User) {
    setForm({
      ...emptyForm,
      primaryExternalSystemId: user.primaryExternalSystemId ?? externalSystems[0]?.id ?? '',
      externalUserId: user.externalUserId ?? '',
      userName: user.userName,
      displayName: user.displayName,
      email: user.email ?? '',
      status: user.status,
    })
    setIsModalOpen(true)
  }

  async function openMappingsModal(type: 'User' | 'Role' | 'UserGroup', id: string, name: string) {
    const externalSystemId = externalSystems[0]?.id ?? ''
    const availableIntegrations = externalSystemId
      ? await onLoadLookupIntegrations(externalSystemId)
      : []
    const initialLookupIntegrationId = availableIntegrations[0]?.id ?? ''
    const initialLookupOptions = initialLookupIntegrationId
      ? await onLoadLookupOptions(initialLookupIntegrationId)
      : []

    setMappingEntity({ type, id, name })
    setMappings(await onLoadMappings(type, id))
    setLookupIntegrations(availableIntegrations)
    setSelectedLookupIntegrationId(initialLookupIntegrationId)
    setLookupOptions(initialLookupOptions)
    setLookupSearch('')
    setMappingForm({
      ...emptyMappingForm,
      localEntityType: type,
      localEntityId: id,
      externalSystemId,
      externalEntityType: type,
      externalEntityCode: initialLookupOptions[0]?.display ?? '',
      isPrimary: true,
    })
    setIsMappingsModalOpen(true)
  }

  async function handleSubmit() {
    await onSaveUser(form)
    setIsModalOpen(false)
  }

  async function handleRoleSubmit() {
    await onSaveRole(roleForm)
    setIsRoleModalOpen(false)
  }

  async function handleGroupSubmit() {
    await onSaveUserGroup(groupForm)
    setIsGroupModalOpen(false)
  }

  async function handleMappingSubmit() {
    await onSaveExternalMapping(mappingForm)
    if (mappingEntity) {
      setMappings(await onLoadMappings(mappingEntity.type, mappingEntity.id))
      setMappingForm({
        ...emptyMappingForm,
        localEntityType: mappingEntity.type,
        localEntityId: mappingEntity.id,
        externalSystemId: externalSystems[0]?.id ?? '',
        externalEntityType: mappingEntity.type,
        isPrimary: true,
      })
    }
  }

  async function handleDeleteMapping(id: string) {
    await onDeleteExternalMapping(id)
    if (mappingEntity) {
      setMappings(await onLoadMappings(mappingEntity.type, mappingEntity.id))
    }
  }

  async function handleSetPrimaryMapping(mapping: ExternalIdentityMapping) {
    await onSaveExternalMapping({
      id: mapping.id,
      localEntityType: mapping.localEntityType,
      localEntityId: mapping.localEntityId,
      externalSystemId: mapping.externalSystemId,
      externalEntityType: mapping.externalEntityType,
      externalEntityId: mapping.externalEntityId,
      externalEntityCode: mapping.externalEntityCode ?? '',
      syncMode: mapping.syncMode,
      isPrimary: true,
    })
    if (mappingEntity) {
      setMappings(await onLoadMappings(mappingEntity.type, mappingEntity.id))
    }
  }

  function editMapping(mapping: ExternalIdentityMapping) {
    setMappingForm({
      id: mapping.id,
      localEntityType: mapping.localEntityType,
      localEntityId: mapping.localEntityId,
      externalSystemId: mapping.externalSystemId,
      externalEntityType: mapping.externalEntityType,
      externalEntityId: mapping.externalEntityId,
      externalEntityCode: mapping.externalEntityCode ?? '',
      syncMode: mapping.syncMode,
      isPrimary: mapping.isPrimary,
    })
  }

  async function handleMappingSystemChange(externalSystemId: string) {
    const availableIntegrations = externalSystemId
      ? await onLoadLookupIntegrations(externalSystemId)
      : []
    const nextIntegrationId = availableIntegrations[0]?.id ?? ''
    const nextOptions = nextIntegrationId
      ? await onLoadLookupOptions(nextIntegrationId)
      : []

    setLookupIntegrations(availableIntegrations)
    setSelectedLookupIntegrationId(nextIntegrationId)
    setLookupOptions(nextOptions)
    setLookupSearch('')
    setMappingForm((current) => ({
      ...current,
      externalSystemId,
      externalEntityCode: nextOptions.find((option) => option.id === current.externalEntityId)?.display ?? current.externalEntityCode ?? '',
    }))
  }

  async function handleLookupIntegrationChange(integrationId: string) {
    setSelectedLookupIntegrationId(integrationId)
    setLookupSearch('')
    const nextOptions = integrationId
      ? await onLoadLookupOptions(integrationId)
      : []
    setLookupOptions(nextOptions)
  }

  async function handleLookupSearch() {
    if (!selectedLookupIntegrationId) return
    setLookupOptions(await onLoadLookupOptions(selectedLookupIntegrationId, lookupSearch))
  }

  function handleLookupOptionChange(externalEntityId: string) {
    const selectedOption = lookupOptions.find((option) => option.id === externalEntityId)
    setMappingForm((current) => ({
      ...current,
      externalEntityId,
      externalEntityCode: selectedOption?.display ?? current.externalEntityCode ?? '',
    }))
  }

  function toggleSort(nextSortBy: 'status' | 'name' | 'synced') {
    if (sortBy === nextSortBy) {
      setSortDirection((current) => current === 'asc' ? 'desc' : 'asc')
      return
    }

    setSortBy(nextSortBy)
    setSortDirection(nextSortBy === 'name' ? 'asc' : 'desc')
  }

  const displayUsers = [...filteredUsers].sort((left, right) => {
    const multiplier = sortDirection === 'asc' ? 1 : -1

    if (sortBy === 'name') {
      return left.displayName.localeCompare(right.displayName) * multiplier
    }

    if (sortBy === 'status') {
      return left.status.localeCompare(right.status) * multiplier
    }

    const leftTime = left.lastSyncedAtUtc ? new Date(left.lastSyncedAtUtc).getTime() : 0
    const rightTime = right.lastSyncedAtUtc ? new Date(right.lastSyncedAtUtc).getTime() : 0
    return (leftTime - rightTime) * multiplier
  })

  const displayRoles = [...filteredRoles].sort((left, right) => left.code.localeCompare(right.code))
  const displayGroups = [...filteredGroups].sort((left, right) => left.code.localeCompare(right.code))

  const pageTitle = activeView === 'roles' ? 'Role Management' : activeView === 'groups' ? 'User Group Management' : 'User Management'
  const pageEyebrow = activeView === 'roles' ? 'Role administration' : activeView === 'groups' ? 'Group administration' : 'Identity administration'
  const pageDescription = activeView === 'roles'
    ? 'Review workflow roles used for assignment, access inheritance, and enterprise approval routing.'
    : activeView === 'groups'
      ? 'Manage user groups that bundle members and inherited roles for enterprise workflow assignment.'
      : 'Manage internal workflow users, sync identity details, and control who can act inside the platform.'
  const visibleCount = activeView === 'roles' ? filteredRoles.length : activeView === 'groups' ? filteredGroups.length : filteredUsers.length
  const totalCount = activeView === 'roles' ? roles.length : activeView === 'groups' ? userGroups.length : users.length
  const primaryCountLabel = activeView === 'roles' ? 'Visible roles' : activeView === 'groups' ? 'Visible groups' : 'Visible users'
  const secondaryCount = activeView === 'roles' ? roles.filter((role) => role.status === 'Active').length : activeView === 'groups' ? userGroups.filter((group) => group.assignmentMode === 'Claim').length : activeUsers
  const secondaryCountLabel = activeView === 'roles' ? 'Active roles' : activeView === 'groups' ? 'Claim groups' : 'Active users'
  const tertiaryCount = activeView === 'roles' ? roles.filter((role) => role.status === 'Inactive').length : activeView === 'groups' ? userGroups.reduce((sum, group) => sum + group.memberUserIds.length, 0) : inactiveUsers
  const tertiaryCountLabel = activeView === 'roles' ? 'Inactive roles' : activeView === 'groups' ? 'Mapped members' : 'Inactive users'
  const quaternaryCount = activeView === 'groups' ? userGroups.reduce((sum, group) => sum + group.roleIds.length, 0) : externalSystems.length
  const quaternaryCountLabel = activeView === 'groups' ? 'Inherited roles' : 'External systems'
  const searchPlaceholder = activeView === 'roles'
    ? 'Search by role name, code, or external role ID'
    : activeView === 'groups'
      ? 'Search by group name, code, assignment mode, or external group ID'
      : 'Search by name, username, email, or external ID'

  return (
    <>
      <section className="work-area workflow-list-area">
        <header className="page-heading user-management-heading">
          <div className="workflow-library-heading-copy">
            <div className="page-eyebrow">{pageEyebrow}</div>
            <h2><span className="title-icon subtle" aria-hidden="true">{activeView === 'roles' ? <ShieldIcon /> : activeView === 'groups' ? <GroupIcon /> : <UsersIcon />}</span>{pageTitle}</h2>
            <p>{pageDescription}</p>
          </div>
          <div className="page-heading-meta">
            <div className="heading-stat">
              <strong>{visibleCount}</strong>
              <span>{primaryCountLabel}</span>
            </div>
            <div className="heading-stat">
              <strong>{secondaryCount}</strong>
              <span>{secondaryCountLabel}</span>
            </div>
            <div className="heading-stat">
              <strong>{tertiaryCount}</strong>
              <span>{tertiaryCountLabel}</span>
            </div>
            <div className="heading-stat">
              <strong>{quaternaryCount}</strong>
              <span>{quaternaryCountLabel}</span>
            </div>
            {activeView === 'users' ? (
              <button className="primary-action workflow-create-button" type="button" onClick={openCreateModal} disabled={busy || !selectedUser || externalSystems.length === 0}>
                <span className="button-label"><span className="button-icon" aria-hidden="true"><CreateIcon /></span>Sync user</span>
              </button>
            ) : activeView === 'roles' ? (
              <button className="primary-action workflow-create-button" type="button" onClick={openCreateRoleModal} disabled={busy || !selectedUser}>
                <span className="button-label"><span className="button-icon" aria-hidden="true"><CreateIcon /></span>New role</span>
              </button>
            ) : (
              <button className="primary-action workflow-create-button" type="button" onClick={openCreateGroupModal} disabled={busy || !selectedUser}>
                <span className="button-label"><span className="button-icon" aria-hidden="true"><CreateIcon /></span>New group</span>
              </button>
            )}
          </div>
        </header>

        <section className="panel workflow-table-panel user-directory-panel">
          <div className="panel-header workflow-library-panel-header">
            <div className="workflow-library-panel-copy">
              <h2><span className="title-icon subtle" aria-hidden="true">{activeView === 'roles' ? <ShieldIcon /> : activeView === 'groups' ? <GroupIcon /> : <DirectoryIcon />}</span>{activeView === 'roles' ? 'Role Directory' : activeView === 'groups' ? 'User Group Directory' : 'User Directory'}</h2>
              <p>{visibleCount} of {totalCount} records visible in the current view.</p>
            </div>
            <div className="workflow-library-panel-meta">
              <span className="workflow-panel-pill">{activeView === 'roles' ? 'Workflow roles are used for assignment and access' : activeView === 'groups' ? 'Groups can inherit roles for human task routing' : 'Identity sync uses external system mapping'}</span>
              <span className="workflow-panel-pill">{activeView === 'roles' ? 'Roles are maintained from the workflow administration domain' : activeView === 'groups' ? 'Group membership and role inheritance are shown in one list' : 'Use an admin acting user for create or edit'}</span>
              <button className="table-action-button history-action-button" type="button" onClick={onRefresh} disabled={busy}>
                <span className="button-label"><span className="button-icon" aria-hidden="true"><RefreshInlineIcon /></span>Refresh</span>
              </button>
            </div>
          </div>

          <div className="list-toolbar workflow-list-toolbar workflow-library-toolbar">
            <label className="toolbar-search-field">
              <span>Search records</span>
              <input
                aria-label="Search identity records"
                placeholder={searchPlaceholder}
                value={search}
                onChange={(event) => setSearch(event.target.value)}
              />
            </label>
            <label className="toolbar-filter-field">
              <span>Status</span>
              <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
                <option>All</option>
                <option>Active</option>
                <option>Inactive</option>
              </select>
            </label>
          </div>

          {activeView === 'users' ? (
            <div className="workflow-table">
              <div className="workflow-table-header workflow-table-grid user-management-grid">
                <button className={`table-sort-button ${sortBy === 'status' ? `active ${sortDirection}` : 'inactive'}`} type="button" onClick={() => toggleSort('status')}>
                  <span>Status</span>
                  <strong>{sortBy === 'status' ? (sortDirection === 'asc' ? 'â†‘' : 'â†“') : 'â†•'}</strong>
                </button>
                <button className={`table-sort-button ${sortBy === 'name' ? `active ${sortDirection}` : 'inactive'}`} type="button" onClick={() => toggleSort('name')}>
                  <span>User</span>
                  <strong>{sortBy === 'name' ? (sortDirection === 'asc' ? 'â†‘' : 'â†“') : 'â†•'}</strong>
                </button>
                <span>Identity mapping</span>
                <button className={`table-sort-button ${sortBy === 'synced' ? `active ${sortDirection}` : 'inactive'}`} type="button" onClick={() => toggleSort('synced')}>
                  <span>Last synced</span>
                  <strong>{sortBy === 'synced' ? (sortDirection === 'asc' ? 'â†‘' : 'â†“') : 'â†•'}</strong>
                </button>
                <span>Actions</span>
              </div>
              {filteredUsers.length === 0 && (
                <div className="empty-state-card compact">
                  <div className="empty-state-icon">UM</div>
                  <strong>No matching users</strong>
                  <p>Adjust the filters or sync a new user into the directory.</p>
                </div>
              )}
              {displayUsers.map((user) => (
                <div key={user.id} className="workflow-table-row workflow-table-grid user-management-grid">
                  <div className="workflow-status-cell">
                    <span className={`status-pill ${user.status.toLowerCase()}`}>{user.status}</span>
                    <small>{user.id === selectedUser?.id ? 'Acting user' : 'Directory user'}</small>
                  </div>
                  <div className="row-stack workflow-name-cell">
                    <strong>{user.displayName}</strong>
                    <small>{user.userName}</small>
                    <small>{user.email || 'No email recorded'}</small>
                  </div>
                  <div className="row-stack version-summary-cell user-identity-cell">
                    <strong>{user.externalUserId || 'Not mapped'}</strong>
                    <div className="workflow-version-tags user-version-tags">
                      <span>{resolveExternalSystemName(user.primaryExternalSystemId, externalSystems)}</span>
                      <span>{user.externalUserId ? 'Mapped' : 'Pending mapping'}</span>
                      {user.id === selectedUser?.id && <span>Current actor</span>}
                    </div>
                  </div>
                  <div className="row-stack workflow-time-cell">
                    <strong>{user.lastSyncedAtUtc ? formatDate(user.lastSyncedAtUtc) : 'Never'}</strong>
                    <small>{user.id}</small>
                  </div>
                  <div className="table-action-row user-action-row">
                    <div className="table-action-menu-shell" onClick={(event) => event.stopPropagation()}>
                      <button
                        className="table-action-button icon-action-button overflow-action-button"
                        type="button"
                        onClick={() => setOpenActionMenuId((current) => current === user.id ? null : user.id)}
                        disabled={busy || !selectedUser || externalSystems.length === 0}
                        aria-label={`More actions for ${user.displayName}`}
                        title={`More actions for ${user.displayName}`}
                      >
                        <span className="button-icon" aria-hidden="true"><MoreIcon /></span>
                      </button>
                      {openActionMenuId === user.id && (
                        <div className="table-action-menu">
                          <button
                            type="button"
                            onClick={() => {
                              setOpenActionMenuId(null)
                              openEditModal(user)
                            }}
                            disabled={busy || !selectedUser || externalSystems.length === 0}
                          >
                            <span className="button-label"><span className="button-icon" aria-hidden="true"><EditIcon /></span>Edit user</span>
                          </button>
                          <button
                            type="button"
                            onClick={() => {
                              setOpenActionMenuId(null)
                              void openMappingsModal('User', user.id, user.displayName)
                            }}
                            disabled={busy}
                          >
                            <span className="button-label"><span className="button-icon" aria-hidden="true"><LinkIcon /></span>Mappings</span>
                          </button>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          ) : activeView === 'roles' ? (
            <div className="workflow-table">
              <div className="workflow-table-header workflow-table-grid role-management-grid">
                <span>Status</span>
                <span>Role</span>
                <span>External mapping</span>
                <span>Last synced</span>
                <span>Actions</span>
              </div>
              {filteredRoles.length === 0 && (
                <div className="empty-state-card compact">
                  <div className="empty-state-icon">RL</div>
                  <strong>No matching roles</strong>
                  <p>Adjust the filters to review workflow assignment roles.</p>
                </div>
              )}
              {displayRoles.map((role) => (
                <div key={role.id} className="workflow-table-row workflow-table-grid role-management-grid">
                  <div className="workflow-status-cell">
                    <span className={`status-pill ${role.status.toLowerCase()}`}>{role.status}</span>
                    <small>{role.externalRoleId ? 'Mapped role' : 'Local role'}</small>
                  </div>
                  <div className="row-stack workflow-name-cell">
                    <strong>{role.name}</strong>
                    <small>{role.code}</small>
                  </div>
                  <div className="row-stack version-summary-cell">
                    <strong>{role.externalRoleId || 'Not mapped'}</strong>
                    <div className="workflow-version-tags user-version-tags">
                      <span>{resolveExternalSystemName(role.primaryExternalSystemId ?? undefined, externalSystems)}</span>
                    </div>
                  </div>
                  <div className="row-stack workflow-time-cell">
                    <strong>{role.lastSyncedAtUtc ? formatDate(role.lastSyncedAtUtc) : 'Never'}</strong>
                    <small>{role.id}</small>
                  </div>
                  <div className="table-action-row user-action-row">
                    <button className="table-action-button designer-action-button" type="button" onClick={() => openEditRoleModal(role)} disabled={busy}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><EditIcon /></span>Edit</span>
                    </button>
                    <button className="table-action-button history-action-button" type="button" onClick={() => void openMappingsModal('Role', role.id, role.name)} disabled={busy}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><LinkIcon /></span>Mappings</span>
                    </button>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="workflow-table">
              <div className="workflow-table-header workflow-table-grid group-management-grid">
                <span>Status</span>
                <span>User group</span>
                <span>Assignment</span>
                <span>Members</span>
                <span>Inherited roles</span>
                <span>Last synced</span>
                <span>Actions</span>
              </div>
              {filteredGroups.length === 0 && (
                <div className="empty-state-card compact">
                  <div className="empty-state-icon">UG</div>
                  <strong>No matching user groups</strong>
                  <p>Adjust the filters to review workflow assignment groups.</p>
                </div>
              )}
              {displayGroups.map((group) => (
                <div key={group.id} className="workflow-table-row workflow-table-grid group-management-grid">
                  <div className="workflow-status-cell">
                    <span className={`status-pill ${group.status.toLowerCase()}`}>{group.status}</span>
                    <small>{group.externalGroupId ? 'Mapped group' : 'Local group'}</small>
                  </div>
                  <div className="row-stack workflow-name-cell">
                    <strong>{group.name}</strong>
                    <small>{group.code}</small>
                    <small>{group.description || 'No description recorded'}</small>
                  </div>
                  <div className="row-stack version-summary-cell">
                    <strong>{group.assignmentMode}</strong>
                    <div className="workflow-version-tags user-version-tags">
                      <span>{resolveExternalSystemName(group.primaryExternalSystemId ?? undefined, externalSystems)}</span>
                      <span>{group.externalGroupId || 'Not mapped'}</span>
                    </div>
                  </div>
                  <div className="row-stack workflow-time-cell">
                    <strong>{group.memberUserIds.length}</strong>
                    <small>Members in group</small>
                  </div>
                  <div className="row-stack workflow-time-cell">
                    <strong>{group.roleIds.length}</strong>
                    <small>Inherited roles</small>
                  </div>
                  <div className="row-stack workflow-time-cell">
                    <strong>{group.lastSyncedAtUtc ? formatDate(group.lastSyncedAtUtc) : 'Never'}</strong>
                    <small>{group.id}</small>
                  </div>
                  <div className="table-action-row user-action-row">
                    <button className="table-action-button designer-action-button" type="button" onClick={() => openEditGroupModal(group)} disabled={busy}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><EditIcon /></span>Edit</span>
                    </button>
                    <button className="table-action-button history-action-button" type="button" onClick={() => void openMappingsModal('UserGroup', group.id, group.name)} disabled={busy}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><LinkIcon /></span>Mappings</span>
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      </section>

      {activeView === 'users' && isModalOpen && (
        <section className="modal-backdrop" role="presentation">
          <div className="modal-dialog modal-dialog--large" role="dialog" aria-modal="true" aria-labelledby="sync-user-title" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3 id="sync-user-title"><span className="title-icon subtle" aria-hidden="true"><UsersIcon /></span>Sync user</h3>
                <p>Create or update a workflow user against a mapped external system identity.</p>
              </div>
              <button className="modal-close-button" type="button" onClick={() => setIsModalOpen(false)} disabled={busy}>Close</button>
            </div>
            <div className="modal-body">
              <div className="modal-intro-strip">
                <div className="modal-intro-copy">
                  <strong>Directory synchronization</strong>
                  <span>Maintain workflow-ready users with external identity mapping, operational status, and core profile details.</span>
                </div>
                <div className="modal-intro-metrics">
                  <div className="modal-intro-metric">
                    <span>Systems</span>
                    <strong>{externalSystems.length}</strong>
                  </div>
                  <div className="modal-intro-metric">
                    <span>Mode</span>
                    <strong>{form.externalUserId || form.userName ? 'Update / create' : 'New record'}</strong>
                  </div>
                </div>
              </div>
              <div className="form-grid modal-section-card">
                <label className="field">
                  <span>External system</span>
                  <select value={form.primaryExternalSystemId} onChange={(event) => setForm((current) => ({ ...current, primaryExternalSystemId: event.target.value }))}>
                    {externalSystems.map((system) => (
                      <option key={system.id} value={system.id}>{system.code} - {system.name}</option>
                    ))}
                  </select>
                </label>
                <label className="field">
                  <span>External user ID</span>
                  <input value={form.externalUserId} onChange={(event) => setForm((current) => ({ ...current, externalUserId: event.target.value }))} />
                </label>
                <label className="field">
                  <span>Username</span>
                  <input value={form.userName} onChange={(event) => setForm((current) => ({ ...current, userName: event.target.value }))} />
                </label>
                <label className="field">
                  <span>Display name</span>
                  <input value={form.displayName} onChange={(event) => setForm((current) => ({ ...current, displayName: event.target.value }))} />
                </label>
                <label className="field">
                  <span>Email</span>
                  <input value={form.email ?? ''} onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))} />
                </label>
                <label className="field">
                  <span>Status</span>
                  <select value={form.status} onChange={(event) => setForm((current) => ({ ...current, status: event.target.value }))}>
                    <option>Active</option>
                    <option>Inactive</option>
                  </select>
                </label>
                <label className="field">
                  <span>Job title</span>
                  <input value={form.jobTitle ?? ''} onChange={(event) => setForm((current) => ({ ...current, jobTitle: event.target.value }))} />
                </label>
                <label className="field">
                  <span>Department code</span>
                  <input value={form.departmentCode ?? ''} onChange={(event) => setForm((current) => ({ ...current, departmentCode: event.target.value }))} />
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <div className="modal-footer-actions">
                <button className="secondary-action" type="button" onClick={() => setIsModalOpen(false)} disabled={busy}>Cancel</button>
                <button
                  className="primary-action"
                  type="button"
                  onClick={() => void handleSubmit()}
                  disabled={busy || !selectedUser || !form.primaryExternalSystemId || !form.externalUserId.trim() || !form.userName.trim() || !form.displayName.trim()}
                >
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><SaveIcon /></span>Save user</span>
                </button>
              </div>
            </div>
          </div>
        </section>
      )}

      {activeView === 'roles' && isRoleModalOpen && (
        <section className="modal-backdrop" role="presentation">
          <div className="modal-dialog modal-dialog--medium" role="dialog" aria-modal="true" aria-labelledby="role-modal-title" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3 id="role-modal-title"><span className="title-icon subtle" aria-hidden="true"><ShieldIcon /></span>Role</h3>
                <p>Create or update workflow roles with optional primary external mapping details.</p>
              </div>
              <button className="modal-close-button" type="button" onClick={() => setIsRoleModalOpen(false)} disabled={busy}>Close</button>
            </div>
            <div className="modal-body">
              <div className="form-grid modal-section-card">
                <label className="field">
                  <span>Code</span>
                  <input value={roleForm.code} onChange={(event) => setRoleForm((current) => ({ ...current, code: event.target.value }))} />
                </label>
                <label className="field">
                  <span>Name</span>
                  <input value={roleForm.name} onChange={(event) => setRoleForm((current) => ({ ...current, name: event.target.value }))} />
                </label>
                <label className="field">
                  <span>External system</span>
                  <select value={roleForm.primaryExternalSystemId ?? ''} onChange={(event) => setRoleForm((current) => ({ ...current, primaryExternalSystemId: event.target.value }))}>
                    <option value="">No primary external system</option>
                    {externalSystems.map((system) => (
                      <option key={system.id} value={system.id}>{system.code} - {system.name}</option>
                    ))}
                  </select>
                </label>
                <label className="field">
                  <span>External role ID</span>
                  <input value={roleForm.externalRoleId ?? ''} onChange={(event) => setRoleForm((current) => ({ ...current, externalRoleId: event.target.value }))} />
                </label>
                <label className="field">
                  <span>Description</span>
                  <input value={roleForm.description ?? ''} onChange={(event) => setRoleForm((current) => ({ ...current, description: event.target.value }))} />
                </label>
                <label className="field">
                  <span>Status</span>
                  <select value={roleForm.status} onChange={(event) => setRoleForm((current) => ({ ...current, status: event.target.value }))}>
                    <option>Active</option>
                    <option>Inactive</option>
                  </select>
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <div className="modal-footer-actions">
                <button className="secondary-action" type="button" onClick={() => setIsRoleModalOpen(false)} disabled={busy}>Cancel</button>
                <button className="primary-action" type="button" onClick={() => void handleRoleSubmit()} disabled={busy || !roleForm.code.trim() || !roleForm.name.trim()}>
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><SaveIcon /></span>Save role</span>
                </button>
              </div>
            </div>
          </div>
        </section>
      )}

      {activeView === 'groups' && isGroupModalOpen && (
        <section className="modal-backdrop" role="presentation">
          <div className="modal-dialog modal-dialog--large" role="dialog" aria-modal="true" aria-labelledby="group-modal-title" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3 id="group-modal-title"><span className="title-icon subtle" aria-hidden="true"><GroupIcon /></span>User Group</h3>
                <p>Create or update groups with inherited roles, members, and optional primary external mapping details.</p>
              </div>
              <button className="modal-close-button" type="button" onClick={() => setIsGroupModalOpen(false)} disabled={busy}>Close</button>
            </div>
            <div className="modal-body">
              <div className="form-grid modal-section-card">
                <label className="field">
                  <span>Code</span>
                  <input value={groupForm.code} onChange={(event) => setGroupForm((current) => ({ ...current, code: event.target.value }))} />
                </label>
                <label className="field">
                  <span>Name</span>
                  <input value={groupForm.name} onChange={(event) => setGroupForm((current) => ({ ...current, name: event.target.value }))} />
                </label>
                <label className="field">
                  <span>External system</span>
                  <select value={groupForm.primaryExternalSystemId ?? ''} onChange={(event) => setGroupForm((current) => ({ ...current, primaryExternalSystemId: event.target.value }))}>
                    <option value="">No primary external system</option>
                    {externalSystems.map((system) => (
                      <option key={system.id} value={system.id}>{system.code} - {system.name}</option>
                    ))}
                  </select>
                </label>
                <label className="field">
                  <span>External group ID</span>
                  <input value={groupForm.externalGroupId ?? ''} onChange={(event) => setGroupForm((current) => ({ ...current, externalGroupId: event.target.value }))} />
                </label>
                <label className="field">
                  <span>Description</span>
                  <input value={groupForm.description ?? ''} onChange={(event) => setGroupForm((current) => ({ ...current, description: event.target.value }))} />
                </label>
                <label className="field">
                  <span>Assignment mode</span>
                  <select value={groupForm.assignmentMode} onChange={(event) => setGroupForm((current) => ({ ...current, assignmentMode: event.target.value }))}>
                    <option>Claim</option>
                    <option>Direct</option>
                    <option>RoundRobin</option>
                  </select>
                </label>
                <label className="field">
                  <span>Status</span>
                  <select value={groupForm.status} onChange={(event) => setGroupForm((current) => ({ ...current, status: event.target.value }))}>
                    <option>Active</option>
                    <option>Inactive</option>
                  </select>
                </label>
                <label className="field">
                  <span>Manager</span>
                  <select value={groupForm.managerUserId ?? ''} onChange={(event) => setGroupForm((current) => ({ ...current, managerUserId: event.target.value }))}>
                    <option value="">No manager</option>
                    {users.map((user) => (
                      <option key={user.id} value={user.id}>{user.displayName || user.userName}</option>
                    ))}
                  </select>
                </label>
                <label className="field">
                  <span>Members</span>
                  <select multiple value={groupForm.memberUserIds} onChange={(event) => setGroupForm((current) => ({ ...current, memberUserIds: Array.from(event.target.selectedOptions).map((option) => option.value) }))}>
                    {users.map((user) => (
                      <option key={user.id} value={user.id}>{user.displayName || user.userName}</option>
                    ))}
                  </select>
                </label>
                <label className="field">
                  <span>Inherited roles</span>
                  <select multiple value={groupForm.roleIds} onChange={(event) => setGroupForm((current) => ({ ...current, roleIds: Array.from(event.target.selectedOptions).map((option) => option.value) }))}>
                    {roles.map((role) => (
                      <option key={role.id} value={role.id}>{role.name} ({role.code})</option>
                    ))}
                  </select>
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <div className="modal-footer-actions">
                <button className="secondary-action" type="button" onClick={() => setIsGroupModalOpen(false)} disabled={busy}>Cancel</button>
                <button className="primary-action" type="button" onClick={() => void handleGroupSubmit()} disabled={busy || !groupForm.code.trim() || !groupForm.name.trim()}>
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><SaveIcon /></span>Save group</span>
                </button>
              </div>
            </div>
          </div>
        </section>
      )}

      {isMappingsModalOpen && mappingEntity && (
        <section className="modal-backdrop" role="presentation">
          <div className="modal-dialog modal-dialog--large" role="dialog" aria-modal="true" aria-labelledby="mappings-modal-title" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3 id="mappings-modal-title"><span className="title-icon subtle" aria-hidden="true"><LinkIcon /></span>External Mappings</h3>
                <p>Manage external system keys for {mappingEntity.name}. Use a primary mapping to align the local directory record with a source system.</p>
              </div>
              <button className="modal-close-button" type="button" onClick={() => setIsMappingsModalOpen(false)} disabled={busy}>Close</button>
            </div>
            <div className="modal-body">
              <div className="workflow-table">
                <div className="workflow-table-header workflow-table-grid mapping-management-grid">
                  <span>Primary</span>
                  <span>System</span>
                  <span>Entity</span>
                  <span>Sync mode</span>
                  <span>Last status</span>
                  <span>Actions</span>
                </div>
                {mappings.length === 0 && (
                  <div className="empty-state-card compact">
                    <div className="empty-state-icon">MP</div>
                    <strong>No mappings configured</strong>
                    <p>Create the first mapping for this record below.</p>
                  </div>
                )}
                {mappings.map((mapping) => (
                  <div key={mapping.id} className="workflow-table-row workflow-table-grid mapping-management-grid">
                    <div className="workflow-status-cell">
                      <span className={`status-pill ${mapping.isPrimary ? 'active' : 'inactive'}`}>{mapping.isPrimary ? 'Primary' : 'Linked'}</span>
                    </div>
                    <div className="row-stack workflow-name-cell">
                      <strong>{resolveExternalSystemName(mapping.externalSystemId, externalSystems)}</strong>
                      <small>{mapping.externalEntityCode || 'No code'}</small>
                    </div>
                    <div className="row-stack workflow-time-cell">
                      <strong>{mapping.externalEntityType}</strong>
                      <small>{mapping.externalEntityId}</small>
                    </div>
                    <div className="row-stack workflow-time-cell">
                      <strong>{mapping.syncMode}</strong>
                      <small>{mapping.lastSyncedAtUtc ? formatDate(mapping.lastSyncedAtUtc) : 'Not synced'}</small>
                    </div>
                    <div className="row-stack workflow-time-cell">
                      <strong>{mapping.lastSyncStatus || 'Configured'}</strong>
                      <small>{mapping.lastSyncMessage || 'No sync message'}</small>
                    </div>
                    <div className="table-action-row user-action-row">
                      <button className="table-action-button designer-action-button" type="button" onClick={() => editMapping(mapping)} disabled={busy}>
                        <span className="button-label"><span className="button-icon" aria-hidden="true"><EditIcon /></span>Edit</span>
                      </button>
                      {!mapping.isPrimary && (
                        <button className="table-action-button history-action-button" type="button" onClick={() => void handleSetPrimaryMapping(mapping)} disabled={busy}>
                          <span className="button-label"><span className="button-icon" aria-hidden="true"><PrimaryIcon /></span>Primary</span>
                        </button>
                      )}
                      <button className="table-action-button danger-ghost" type="button" onClick={() => void handleDeleteMapping(mapping.id)} disabled={busy}>
                        <span className="button-label"><span className="button-icon" aria-hidden="true"><DeleteIcon /></span>Remove</span>
                      </button>
                    </div>
                  </div>
                ))}
              </div>
              <div className="form-grid modal-section-card">
                <label className="field">
                  <span>External system</span>
                  <select value={mappingForm.externalSystemId} onChange={(event) => void handleMappingSystemChange(event.target.value)}>
                    {externalSystems.map((system) => (
                      <option key={system.id} value={system.id}>{system.code} - {system.name}</option>
                    ))}
                  </select>
                </label>
                <label className="field">
                  <span>External entity type</span>
                  <input value={mappingForm.externalEntityType} onChange={(event) => setMappingForm((current) => ({ ...current, externalEntityType: event.target.value }))} />
                </label>
                <label className="field">
                  <span>Lookup integration</span>
                  <select value={selectedLookupIntegrationId} onChange={(event) => void handleLookupIntegrationChange(event.target.value)} disabled={lookupIntegrations.length === 0}>
                    {lookupIntegrations.length === 0 ? (
                      <option value="">No database lookup available</option>
                    ) : (
                      lookupIntegrations.map((integration) => (
                        <option key={integration.id} value={integration.id}>{integration.code} - {integration.name}</option>
                      ))
                    )}
                  </select>
                </label>
                <label className="field">
                  <span>Lookup search</span>
                  <div className="field-inline-actions">
                    <input
                      value={lookupSearch}
                      onChange={(event) => setLookupSearch(event.target.value)}
                      placeholder="Search connected database"
                      disabled={!selectedLookupIntegrationId}
                    />
                    <button className="table-action-button history-action-button" type="button" onClick={() => void handleLookupSearch()} disabled={busy || !selectedLookupIntegrationId}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><RefreshInlineIcon /></span>Search</span>
                    </button>
                  </div>
                </label>
                <label className="field">
                  <span>External entity ID</span>
                  {lookupOptions.length > 0 ? (
                    <select value={mappingForm.externalEntityId} onChange={(event) => handleLookupOptionChange(event.target.value)}>
                      <option value="">Select external record</option>
                      {lookupOptions.map((option) => (
                        <option key={option.id} value={option.id}>{option.display} ({option.id})</option>
                      ))}
                    </select>
                  ) : (
                    <input value={mappingForm.externalEntityId} onChange={(event) => setMappingForm((current) => ({ ...current, externalEntityId: event.target.value }))} />
                  )}
                </label>
                <label className="field">
                  <span>External entity code</span>
                  <input value={mappingForm.externalEntityCode ?? ''} onChange={(event) => setMappingForm((current) => ({ ...current, externalEntityCode: event.target.value }))} />
                </label>
                <label className="field">
                  <span>Sync mode</span>
                  <select value={mappingForm.syncMode} onChange={(event) => setMappingForm((current) => ({ ...current, syncMode: event.target.value }))}>
                    <option value="ReferenceOnly">Reference Only</option>
                    <option value="Import">Import</option>
                    <option value="LocalOverride">Local Override</option>
                  </select>
                </label>
                <label className="field platform-checkbox-field">
                  <span>Primary mapping</span>
                  <label className="platform-inline-checkbox">
                    <input type="checkbox" checked={mappingForm.isPrimary} onChange={(event) => setMappingForm((current) => ({ ...current, isPrimary: event.target.checked }))} />
                    <strong>Set as primary</strong>
                  </label>
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <div className="modal-footer-actions">
                <button className="secondary-action" type="button" onClick={() => setIsMappingsModalOpen(false)} disabled={busy}>Cancel</button>
                <button className="primary-action" type="button" onClick={() => void handleMappingSubmit()} disabled={busy || !mappingForm.externalSystemId || !mappingForm.externalEntityType.trim() || !mappingForm.externalEntityId.trim()}>
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

function resolveExternalSystemName(systemId: string | undefined, systems: ExternalSystem[]) {
  return systems.find((system) => system.id === systemId)?.name ?? 'No external system'
}

function UsersIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M5.2 7.4a1.9 1.9 0 1 0 0-3.8 1.9 1.9 0 0 0 0 3.8Z" />
      <path d="M10.9 6.8a1.5 1.5 0 1 0 0-3" />
      <path d="M2.9 12.4c.4-1.6 1.7-2.5 3.5-2.5 1.8 0 3.1.9 3.5 2.5" />
      <path d="M10 9.8c1.3.1 2.3.9 2.8 2.2" />
    </svg>
  )
}

function DirectoryIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M4 2.8h8v10.4H4z" />
      <path d="M6 5.4h4" />
      <path d="M6 8h4" />
      <path d="M6 10.6h2.5" />
    </svg>
  )
}

function ShieldIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M8 2.5 12 4v3.2c0 2.8-1.5 4.8-4 6.3-2.5-1.5-4-3.5-4-6.3V4z" />
      <path d="m6.4 7.8 1.1 1.1 2.1-2.3" />
    </svg>
  )
}

function GroupIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M5.1 6.9a1.8 1.8 0 1 0 0-3.6 1.8 1.8 0 0 0 0 3.6Z" />
      <path d="M10.9 6.1a1.4 1.4 0 1 0 0-2.8" />
      <path d="M2.7 12.3c.4-1.5 1.7-2.3 3.4-2.3 1.7 0 2.9.8 3.4 2.3" />
      <path d="M9.8 9.9c1.2.1 2.2.8 2.7 2" />
      <path d="M11.8 12.6h1.5" />
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

function LinkIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M6.2 9.8 9.8 6.2" />
      <path d="M5 11.5H4a2.5 2.5 0 1 1 0-5h1.5" />
      <path d="M11 4.5H12a2.5 2.5 0 1 1 0 5h-1.5" />
    </svg>
  )
}

function PrimaryIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M8 2.8 9.7 6.1l3.7.5-2.7 2.5.7 3.6L8 10.9l-3.4 1.8.7-3.6-2.7-2.5 3.7-.5L8 2.8Z" />
    </svg>
  )
}

function DeleteIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3.8 4.6h8.4" />
      <path d="M6.2 4.6V3.4h3.6v1.2" />
      <path d="M5 4.6v7.2h6V4.6" />
      <path d="M6.8 6.3v3.8" />
      <path d="M9.2 6.3v3.8" />
    </svg>
  )
}

function MoreIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="currentColor">
      <circle cx="3.5" cy="8" r="1.2" />
      <circle cx="8" cy="8" r="1.2" />
      <circle cx="12.5" cy="8" r="1.2" />
    </svg>
  )
}

function RefreshInlineIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M13 8a5 5 0 1 1-1.4-3.5" />
      <path d="M13 3.5v3.3H9.7" />
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
