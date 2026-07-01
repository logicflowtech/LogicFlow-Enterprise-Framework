type ApiResponse<T> = {
  succeeded: boolean
  data?: T
  message?: string | null
}

export class ApiRequestError extends Error {
  status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiRequestError'
    this.status = status
  }
}

export type WorkflowHostSession = {
  accessToken: string
  apiBaseUrl?: string
  hostMode?: 'library' | 'designer'
  definitionId?: string
  returnUrl?: string
  user: User
}

export type User = {
  id: string
  userName: string
  displayName: string
  email?: string | null
  status: string
  permissions?: string[]
}

export type Role = {
  id: string
  code: string
  name: string
  status: string
}

export type UserGroup = {
  id: string
  code: string
  name: string
  assignmentMode: string
  status: string
  memberUserIds: string[]
  roleIds: string[]
}

export type ExternalApiEndpoint = {
  id: string
  name: string
  status?: string
  httpMethod?: string
}

export type WorkflowDefinition = {
  id: string
  name: string
  description?: string | null
  status: string
  draftDefinitionJson?: string | null
  definitionRowVersion: string
  draftRowVersion?: string | null
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export type WorkflowValidation = {
  isValid: boolean
  errors: string[]
}

export type WorkflowVersion = {
  id: string
  workflowDefinitionId: string
  versionNumber: number
  status: string
  effectiveFromUtc?: string | null
  effectiveToUtc?: string | null
  publishedBy?: string | null
  publishedAtUtc?: string | null
  publishMessage?: string | null
}

export type WorkflowTaskAction = {
  code: string
  label: string
  style: string
  requiresComment: boolean
}

export type WorkflowTask = {
  id: string
  workflowInstanceId: string
  nodeId: string
  taskName: string
  status: string
  taskMode: string
  priority?: string | null
  entityType?: string | null
  entityId?: string | null
  assignedToUserId?: string | null
  assignedToGroupId?: string | null
  assignedToRoleId?: string | null
  assignmentType: string
  assignedToDisplayName?: string | null
  claimRequired: boolean
  queueKey?: string | null
  formKey?: string | null
  listViewKey?: string | null
  detailViewKey?: string | null
  displayMetadataJson?: string | null
  dueAtUtc?: string | null
  reminderAtUtc?: string | null
  escalationAtUtc?: string | null
  escalationPolicyKey?: string | null
  slaStatus?: string | null
  escalatedAtUtc?: string | null
  createdAtUtc: string
  claimedBy?: string | null
  claimedByUserId?: string | null
  claimedAtUtc?: string | null
  completedBy?: string | null
  completedByUserId?: string | null
  completedAtUtc?: string | null
  completionAction?: string | null
  availableActions: WorkflowTaskAction[]
  comment?: string | null
}

export type WorkflowVariable = {
  id: string
  workflowInstanceId: string
  name: string
  value?: string | null
  dataType: string
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export type WorkflowAuditLog = {
  id: number
  workflowInstanceId: string
  workflowTaskId?: string | null
  action: string
  fromNodeId?: string | null
  toNodeId?: string | null
  performedByUserId?: string | null
  performedBy?: string | null
  performedByDisplayName?: string | null
  message?: string | null
  createdAtUtc: string
}

export type WorkflowTaskComment = {
  id: string
  workflowTaskId: string
  commentType: string
  body: string
  visibility: string
  createdBy: string
  createdByUserId?: string | null
  createdAtUtc: string
}

export type WorkflowTaskAssignment = {
  id: string
  workflowTaskId: string
  actionType: string
  reason?: string | null
  performedBy: string
  performedByUserId?: string | null
  createdAtUtc: string
}

export type WorkflowInstance = {
  id: string
  workflowDefinitionId: string
  workflowVersionId: string
  businessKey?: string | null
  title?: string | null
  status: string
  currentNodeId?: string | null
  startedByUserId?: string | null
  startedBy: string
  startedByDisplayName?: string | null
  startedAtUtc: string
  completedAtUtc?: string | null
  cancelledAtUtc?: string | null
  failedAtUtc?: string | null
}

export type WorkflowVersionDetail = WorkflowVersion & {
  definitionJson: string
}

export type WorkflowInstanceDetail = {
  instance: WorkflowInstance
  version: WorkflowVersionDetail
  tasks: WorkflowTask[]
  variables: WorkflowVariable[]
  auditLogs: WorkflowAuditLog[]
  taskComments: WorkflowTaskComment[]
  taskAssignments: WorkflowTaskAssignment[]
}

export type WorkflowTaskDetail = {
  task: WorkflowTask
  comments: WorkflowTaskComment[]
  assignments: WorkflowTaskAssignment[]
}

type WorkflowHostBridge = {
  session?: WorkflowHostSession | null
  getSession?: () => WorkflowHostSession | null
}

export function getWorkflowHostSession() {
  const bridge = (window as Window & { logicFlowWorkflowHost?: WorkflowHostBridge }).logicFlowWorkflowHost
  if (!bridge) {
    return null
  }

  try {
    const session = typeof bridge.getSession === 'function'
      ? bridge.getSession()
      : bridge.session ?? null

    return session ? session as WorkflowHostSession : null
  } catch {
    return null
  }
}

async function request<T>(path: string, session: WorkflowHostSession, options?: RequestInit): Promise<T> {
  const apiBaseUrl = session.apiBaseUrl ?? import.meta.env.VITE_API_BASE_URL ?? ''
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${session.accessToken}`,
      ...(options?.headers ?? {}),
    },
  })

  const body = await response.text()
  let payload: ApiResponse<T> | undefined

  if (body) {
    try {
      payload = JSON.parse(body) as ApiResponse<T>
    } catch {
      throw new ApiRequestError(body || `Request failed with ${response.status}`, response.status)
    }
  }

  if (!response.ok || !payload?.succeeded) {
    if (response.status === 403) {
      throw new ApiRequestError(payload?.message || 'You do not have permission to perform this workflow action.', response.status)
    }

    throw new ApiRequestError(payload?.message || `Request failed with ${response.status}`, response.status)
  }

  return payload.data as T
}

export function getUsers(session: WorkflowHostSession) {
  return request<Array<{
    id: string
    userName: string
    displayName: string
    email?: string | null
    isActive: boolean
  }>>('/api/workflow/lookups/users', session).then((items) =>
    items.map((item) => ({
      id: item.id,
      userName: item.userName,
      displayName: item.displayName,
      email: item.email,
      status: item.isActive ? 'Active' : 'Inactive',
    })),
  )
}

export function getRoles(session: WorkflowHostSession) {
  return request<Array<{ id: string; code: string; name: string; isActive: boolean }>>('/api/workflow/lookups/roles', session).then((items) =>
    items.map((item) => ({
      id: item.id,
      code: item.code,
      name: item.name,
      status: item.isActive ? 'Active' : 'Inactive',
    })),
  )
}

export function getUserGroups(session: WorkflowHostSession) {
  return request<Array<{ id: string; code: string; name: string; isActive: boolean }>>('/api/workflow/lookups/groups', session).then((items) =>
    items.map((item) => ({
      id: item.id,
      code: item.code,
      name: item.name,
      assignmentMode: 'Claim',
      status: item.isActive ? 'Active' : 'Inactive',
      memberUserIds: [],
      roleIds: [],
    })),
  )
}

export function getWorkflowDefinitions(session: WorkflowHostSession) {
  return request<WorkflowDefinition[]>('/api/workflow-definitions', session)
}

export function getWorkflowDefinitionVersions(definitionId: string, _user: User) {
  const session = getWorkflowHostSession()
  if (!session) {
    return Promise.reject(new Error('Workflow host session is not available.'))
  }

  return request<WorkflowVersion[]>(`/api/workflow-definitions/${definitionId}/versions`, session)
}

export function createWorkflowDefinition(session: WorkflowHostSession, name: string, description: string, draftDefinitionJson: string) {
  return request<WorkflowDefinition>('/api/workflow-definitions', session, {
    method: 'POST',
    body: JSON.stringify({ name, description, draftDefinitionJson }),
  })
}

export function updateWorkflowDraft(
  definitionId: string,
  session: WorkflowHostSession,
  name: string,
  description: string,
  draftDefinitionJson: string,
  definitionRowVersion: string,
  draftRowVersion?: string | null,
) {
  return request<WorkflowDefinition>(`/api/workflow-definitions/${definitionId}/draft`, session, {
    method: 'PUT',
    body: JSON.stringify({ name, description, draftDefinitionJson, definitionRowVersion, draftRowVersion: draftRowVersion || null }),
  })
}

export function validateWorkflowDraft(definitionId: string, session: WorkflowHostSession) {
  return request<WorkflowValidation>(`/api/workflow-definitions/${definitionId}/validate`, session, {
    method: 'POST',
  })
}

export function publishWorkflowDefinition(
  definitionId: string,
  session: WorkflowHostSession,
  effectiveFromUtc?: string,
  effectiveToUtc?: string,
  publishMessage?: string,
  definitionRowVersion?: string,
  draftRowVersion?: string | null,
) {
  return request<WorkflowVersion>(`/api/workflow-definitions/${definitionId}/publish`, session, {
    method: 'POST',
    body: JSON.stringify({
      effectiveFromUtc: effectiveFromUtc || null,
      effectiveToUtc: effectiveToUtc || null,
      publishMessage: publishMessage?.trim() || null,
      definitionRowVersion: definitionRowVersion || null,
      draftRowVersion: draftRowVersion || null,
    }),
  })
}

export function getMyTasks(session: WorkflowHostSession) {
  return request<WorkflowTask[]>('/api/tasks/my', session)
}

export function getTask(taskId: string, session: WorkflowHostSession) {
  return request<WorkflowTask>(`/api/tasks/${taskId}`, session)
}

export function getTaskDetail(taskId: string, session: WorkflowHostSession) {
  return request<WorkflowTaskDetail>(`/api/tasks/${taskId}/detail`, session)
}

export function claimTask(taskId: string, session: WorkflowHostSession) {
  return request<WorkflowTask>(`/api/tasks/${taskId}/claim`, session, {
    method: 'POST',
  })
}

export function unclaimTask(taskId: string, session: WorkflowHostSession) {
  return request<WorkflowTask>(`/api/tasks/${taskId}/unclaim`, session, {
    method: 'POST',
  })
}

export function approveTask(taskId: string, session: WorkflowHostSession, comment?: string) {
  return request<WorkflowTask>(`/api/tasks/${taskId}/approve`, session, {
    method: 'POST',
    body: JSON.stringify({ comment: comment?.trim() || null }),
  })
}

export function rejectTask(taskId: string, session: WorkflowHostSession, comment?: string) {
  return request<WorkflowTask>(`/api/tasks/${taskId}/reject`, session, {
    method: 'POST',
    body: JSON.stringify({ comment: comment?.trim() || null }),
  })
}

export function delegateTask(taskId: string, session: WorkflowHostSession, targetUserId: string, reason?: string) {
  return request<WorkflowTask>(`/api/tasks/${taskId}/delegate`, session, {
    method: 'POST',
    body: JSON.stringify({ targetUserId, reason: reason?.trim() || null }),
  })
}

export function reassignTask(taskId: string, session: WorkflowHostSession, requestBody: {
  targetUserId?: string | null
  targetGroupId?: string | null
  targetRoleId?: string | null
  reason?: string
}) {
  return request<WorkflowTask>(`/api/tasks/${taskId}/reassign`, session, {
    method: 'POST',
    body: JSON.stringify({
      targetUserId: requestBody.targetUserId || null,
      targetGroupId: requestBody.targetGroupId || null,
      targetRoleId: requestBody.targetRoleId || null,
      reason: requestBody.reason?.trim() || null,
    }),
  })
}

export function addTaskComment(taskId: string, session: WorkflowHostSession, comment: string, visibility = 'Internal') {
  return request<WorkflowTaskComment>(`/api/tasks/${taskId}/comment`, session, {
    method: 'POST',
    body: JSON.stringify({ comment, visibility }),
  })
}

export function getWorkflowInstances(session: WorkflowHostSession, options?: {
  workflowDefinitionId?: string
  status?: string
  search?: string
  take?: number
}) {
  const params = new URLSearchParams()
  params.set('take', String(options?.take ?? 100))
  if (options?.workflowDefinitionId) {
    params.set('workflowDefinitionId', options.workflowDefinitionId)
  }
  if (options?.status) {
    params.set('status', options.status)
  }
  if (options?.search) {
    params.set('search', options.search)
  }

  return request<Array<WorkflowInstance & {
    workflowDefinitionName: string
    workflowVersionNumber: number
  }>>(`/api/workflow-instances?${params.toString()}`, session)
}

export function getWorkflowInstanceDetail(instanceId: string, session: WorkflowHostSession) {
  return request<WorkflowInstanceDetail>(`/api/workflow-instances/${instanceId}/detail`, session)
}

export function cancelWorkflowInstance(instanceId: string, session: WorkflowHostSession, reason?: string) {
  return request<WorkflowInstance>(`/api/workflow-instances/${instanceId}/cancel`, session, {
    method: 'POST',
    body: JSON.stringify({ reason: reason?.trim() || null }),
  })
}
