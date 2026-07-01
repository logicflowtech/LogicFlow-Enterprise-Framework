import { useEffect, useMemo, useState } from 'react'
import { ToastStack } from './components/ToastStack'
import { Dashboard } from './components/Dashboard'
import { TaskOperations } from './components/TaskOperations'
import { WorkflowAdministration } from './components/WorkflowAdministration'
import {
  addTaskComment,
  ApiRequestError,
  approveTask,
  cancelWorkflowInstance,
  claimTask,
  createWorkflowDefinition,
  delegateTask,
  getMyTasks,
  getRoles,
  getTask,
  getUserGroups,
  getUsers,
  getWorkflowDefinitions,
  getWorkflowHostSession,
  getWorkflowInstanceDetail,
  publishWorkflowDefinition,
  reassignTask,
  rejectTask,
  unclaimTask,
  updateWorkflowDraft,
  validateWorkflowDraft,
  type Role,
  type User,
  type UserGroup,
  type WorkflowDefinition,
  type WorkflowHostSession,
  type WorkflowInstanceDetail,
  type WorkflowTask,
  type WorkflowValidation,
} from './api'

type DefinitionView = 'list' | 'editor'
type WorkspaceView = 'dashboard' | 'tasks' | 'definitions'

const emptyWorkflowTemplate = {
  schemaVersion: 2,
  metadata: {},
  nodes: [
    { id: 'start', type: 'start' },
    { id: 'task', type: 'userTask', name: 'Human Task', assignedToUserId: '' },
    { id: 'end', type: 'end' },
  ],
  edges: [
    { from: 'start', to: 'task' },
    { from: 'task', to: 'end' },
  ],
}

const emptyWorkflowJson = JSON.stringify(emptyWorkflowTemplate, null, 2)

function App() {
  const [session, setSession] = useState<WorkflowHostSession | null>(null)
  const [activeView, setActiveView] = useState<WorkspaceView>('definitions')
  const [users, setUsers] = useState<User[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [userGroups, setUserGroups] = useState<UserGroup[]>([])
  const [definitions, setDefinitions] = useState<WorkflowDefinition[]>([])
  const [tasks, setTasks] = useState<WorkflowTask[]>([])
  const [selectedDefinitionId, setSelectedDefinitionId] = useState('')
  const [definitionSearch, setDefinitionSearch] = useState('')
  const [definitionStatusFilter, setDefinitionStatusFilter] = useState('All')
  const [definitionView, setDefinitionView] = useState<DefinitionView>('list')
  const [draftJson, setDraftJson] = useState(emptyWorkflowJson)
  const [effectiveFromUtc, setEffectiveFromUtc] = useState('')
  const [effectiveToUtc, setEffectiveToUtc] = useState('')
  const [validation, setValidation] = useState<WorkflowValidation | null>(null)
  const [selectedTaskId, setSelectedTaskId] = useState('')
  const [selectedTaskDetail, setSelectedTaskDetail] = useState<WorkflowInstanceDetail | null>(null)
  const [taskSearch, setTaskSearch] = useState('')
  const [taskStatusFilter, setTaskStatusFilter] = useState('All')
  const [taskComment, setTaskComment] = useState('')
  const [delegateTargetUserId, setDelegateTargetUserId] = useState('')
  const [reassignTargetUserId, setReassignTargetUserId] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  useEffect(() => {
    const nextSession = getWorkflowHostSession()
    setSession(nextSession)
    if (nextSession?.definitionId) {
      setSelectedDefinitionId(nextSession.definitionId)
      setActiveView('definitions')
    }
  }, [])

  const appMode = session?.hostMode === 'designer' ? 'designer' : 'library'
  const returnUrl = session?.returnUrl || '/workflow/definitions'

  useEffect(() => {
    if (appMode === 'designer') {
      setDefinitionView('editor')
      return
    }

    setDefinitionView('list')
  }, [appMode])

  useEffect(() => {
    if (!session) {
      return
    }

    void run(async () => {
      const [workflowDefinitions, workflowTasks, directoryUsers, directoryRoles, directoryGroups] = await Promise.all([
        reloadDefinitions(session),
        reloadTasks(session),
        getUsers(session),
        getRoles(session),
        getUserGroups(session),
      ])

      setDefinitions(workflowDefinitions)
      setTasks(workflowTasks)
      setUsers(directoryUsers)
      setRoles(directoryRoles)
      setUserGroups(directoryGroups)
    })
  }, [session])

  useEffect(() => {
    if (!notice) {
      return
    }

    const timeout = window.setTimeout(() => setNotice(''), 4200)
    return () => window.clearTimeout(timeout)
  }, [notice])

  useEffect(() => {
    if (!error) {
      return
    }

    const timeout = window.setTimeout(() => setError(''), 5200)
    return () => window.clearTimeout(timeout)
  }, [error])

  const selectedDefinition = useMemo(
    () => definitions.find((definition) => definition.id === selectedDefinitionId) ?? null,
    [definitions, selectedDefinitionId],
  )

  const filteredDefinitions = useMemo(
    () => definitions.filter((definition) => {
      const search = definitionSearch.trim().toLowerCase()
      const matchesStatus = definitionStatusFilter === 'All' || definition.status === definitionStatusFilter
      const matchesSearch = !search
        || definition.name.toLowerCase().includes(search)
        || (definition.description ?? '').toLowerCase().includes(search)
        || definition.status.toLowerCase().includes(search)

      return matchesStatus && matchesSearch
    }),
    [definitionSearch, definitionStatusFilter, definitions],
  )

  const filteredTasks = useMemo(
    () => tasks.filter((task) => {
      const search = taskSearch.trim().toLowerCase()
      const matchesStatus = taskStatusFilter === 'All' || task.status === taskStatusFilter
      const matchesSearch = !search
        || task.taskName.toLowerCase().includes(search)
        || (task.assignedToDisplayName ?? '').toLowerCase().includes(search)
        || task.assignmentType.toLowerCase().includes(search)
        || task.workflowInstanceId.toLowerCase().includes(search)
        || (task.entityId ?? '').toLowerCase().includes(search)

      return matchesStatus && matchesSearch
    }),
    [taskSearch, taskStatusFilter, tasks],
  )

  useEffect(() => {
    if (!selectedDefinition) {
      setDraftJson(session ? createDraftJson('New Workflow', '', session.user.id) : emptyWorkflowJson)
      setValidation(null)
      return
    }

    setDraftJson(syncDraftDefinitionMetadata(
      selectedDefinition.draftDefinitionJson,
      selectedDefinition.name,
      selectedDefinition.description ?? '',
      session?.user.id ?? '',
    ))
    void refreshValidation(selectedDefinition.id)
  }, [selectedDefinition?.id, session?.user.id])

  async function refreshValidation(definitionId: string) {
    if (!session) {
      return
    }

    try {
      setValidation(await validateWorkflowDraft(definitionId, session))
    } catch {
      setValidation(null)
    }
  }

  async function handleCreateDefinition(name: string, description: string) {
    if (!session) {
      return
    }

    await run(async () => {
      const normalizedName = name.trim() || 'New Workflow'
      const normalizedDescription = description.trim()
      const created = await createWorkflowDefinition(
        session,
        normalizedName,
        normalizedDescription,
        createDraftJson(normalizedName, normalizedDescription, session.user.id),
      )

      const workflowDefinitions = await reloadDefinitions(session)
      setDefinitions(workflowDefinitions)
      setSelectedDefinitionId(created.id)
      setNotice(`Workflow "${created.name}" created successfully.`)
      navigateToDesigner(created.id)
    })
  }

  async function handleSaveDraft() {
    if (!session || !selectedDefinition) {
      return
    }

    await run(async () => {
      let updated
      try {
        updated = await updateWorkflowDraft(
          selectedDefinition.id,
          session,
          selectedDefinition.name,
          selectedDefinition.description ?? '',
          syncDraftDefinitionMetadata(
            draftJson,
            selectedDefinition.name,
            selectedDefinition.description ?? '',
            session.user.id,
          ),
          selectedDefinition.definitionRowVersion,
          selectedDefinition.draftRowVersion,
        )
      } catch (exception) {
        await handleDefinitionConflict(exception, selectedDefinition.id)
        throw exception
      }

      setDefinitions((items) => items.map((item) => item.id === updated.id ? updated : item))
      await refreshValidation(updated.id)
    }, 'Workflow draft saved.')
  }

  async function handlePublish(publishMessage: string) {
    if (!session || !selectedDefinition) {
      return
    }

    await run(async () => {
      try {
        await publishWorkflowDefinition(
          selectedDefinition.id,
          session,
          toUtcIso(effectiveFromUtc),
          toUtcIso(effectiveToUtc),
          publishMessage,
          selectedDefinition.definitionRowVersion,
          selectedDefinition.draftRowVersion,
        )
      } catch (exception) {
        await handleDefinitionConflict(exception, selectedDefinition.id)
        throw exception
      }

      const workflowDefinitions = await reloadDefinitions(session)
      setDefinitions(workflowDefinitions)
      await refreshValidation(selectedDefinition.id)
    }, 'Workflow published successfully.')
  }

  async function handleSelectTask(taskId: string) {
    if (!session) {
      return
    }

    setSelectedTaskId(taskId)
    setTaskComment('')
    setDelegateTargetUserId('')
    setReassignTargetUserId('')

    if (!taskId) {
      setSelectedTaskDetail(null)
      return
    }

    await run(async () => {
      await refreshSelectedTask(taskId, tasks, session)
    })
  }

  async function handleClaimTask() {
    if (!session || !selectedTaskId) {
      return
    }

    await run(async () => {
      await claimTask(selectedTaskId, session)
      await refreshAfterTaskMutation(selectedTaskId, session)
    }, 'Task claimed successfully.')
  }

  async function handleUnclaimTask() {
    if (!session || !selectedTaskId) {
      return
    }

    await run(async () => {
      await unclaimTask(selectedTaskId, session)
      await refreshAfterTaskMutation(selectedTaskId, session)
    }, 'Task returned to queue.')
  }

  async function handleApproveTask() {
    if (!session || !selectedTaskId) {
      return
    }

    await run(async () => {
      await approveTask(selectedTaskId, session, taskComment)
      setTaskComment('')
      await refreshAfterTaskMutation(selectedTaskId, session)
    }, 'Task approved successfully.')
  }

  async function handleRejectTask() {
    if (!session || !selectedTaskId) {
      return
    }

    await run(async () => {
      await rejectTask(selectedTaskId, session, taskComment)
      setTaskComment('')
      await refreshAfterTaskMutation(selectedTaskId, session)
    }, 'Task rejected successfully.')
  }

  async function handleAddTaskComment() {
    if (!session || !selectedTaskId || !taskComment.trim()) {
      return
    }

    await run(async () => {
      await addTaskComment(selectedTaskId, session, taskComment.trim())
      setTaskComment('')
      await refreshAfterTaskMutation(selectedTaskId, session)
    }, 'Comment added successfully.')
  }

  async function handleDelegateTask() {
    if (!session || !selectedTaskId || !delegateTargetUserId) {
      return
    }

    await run(async () => {
      await delegateTask(selectedTaskId, session, delegateTargetUserId, taskComment)
      setDelegateTargetUserId('')
      setTaskComment('')
      await refreshAfterTaskMutation(selectedTaskId, session)
    }, 'Task delegated successfully.')
  }

  async function handleReassignTask() {
    if (!session || !selectedTaskId || !reassignTargetUserId) {
      return
    }

    await run(async () => {
      await reassignTask(selectedTaskId, session, {
        targetUserId: reassignTargetUserId,
        reason: taskComment,
      })
      setReassignTargetUserId('')
      setTaskComment('')
      await refreshAfterTaskMutation(selectedTaskId, session)
    }, 'Task reassigned successfully.')
  }

  async function handleCancelWorkflow() {
    if (!session || !selectedTaskDetail) {
      return
    }

    await run(async () => {
      await cancelWorkflowInstance(selectedTaskDetail.instance.id, session, taskComment)
      setTaskComment('')
      await refreshAfterTaskMutation(selectedTaskId, session)
    }, 'Workflow instance cancelled successfully.')
  }

  async function refreshAfterTaskMutation(taskId: string, activeSession: WorkflowHostSession) {
    const workflowTasks = await reloadTasks(activeSession)
    setTasks(workflowTasks)
    await refreshSelectedTask(taskId, workflowTasks, activeSession)
  }

  async function refreshSelectedTask(taskId: string, taskSource: WorkflowTask[], activeSession: WorkflowHostSession) {
    if (!taskId) {
      setSelectedTaskDetail(null)
      return
    }

    let currentTask = taskSource.find((item) => item.id === taskId) ?? null

    if (!currentTask) {
      try {
        currentTask = await getTask(taskId, activeSession)
      } catch {
        currentTask = null
      }
    }

    if (!currentTask) {
      setSelectedTaskId('')
      setSelectedTaskDetail(null)
      return
    }

    const detail = await getWorkflowInstanceDetail(currentTask.workflowInstanceId, activeSession)
    setSelectedTaskDetail(detail)
  }

  async function reloadDefinitions(activeSession = session) {
    if (!activeSession) {
      return []
    }

    return getWorkflowDefinitions(activeSession)
  }

  async function reloadTasks(activeSession = session) {
    if (!activeSession) {
      return []
    }

    return getMyTasks(activeSession)
  }

  async function handleDefinitionConflict(exception: unknown, definitionId: string) {
    if (!(exception instanceof ApiRequestError) || exception.status !== 409 || !session) {
      return
    }

    const workflowDefinitions = await reloadDefinitions(session)
    setDefinitions(workflowDefinitions)
    await refreshValidation(definitionId)
  }

  async function run(work: () => Promise<void>, successMessage?: string) {
    setBusy(true)
    setError('')
    try {
      await work()
      if (successMessage) {
        setNotice(successMessage)
      }
    } catch (exception) {
      if (exception instanceof ApiRequestError && exception.status === 409) {
        setError('This workflow draft changed in another session. The latest server version has been refreshed. Review the current definition and apply your changes again.')
      } else {
        setError(exception instanceof Error ? exception.message : 'Unexpected error')
      }
    } finally {
      setBusy(false)
    }
  }

  function navigateToDesigner(definitionId: string) {
    window.location.assign(`/workflow/designer/${encodeURIComponent(definitionId)}`)
  }

  function navigateToLibrary() {
    window.location.assign(returnUrl)
  }

  const canCancel = selectedTaskDetail != null
    && selectedTaskDetail.instance.status !== 'Completed'
    && selectedTaskDetail.instance.status !== 'Cancelled'
    && selectedTaskDetail.instance.status !== 'Failed'

  if (!session) {
    return (
      <main className="app-shell">
        <section className="work-area workflow-list-area">
          <header className="page-heading workflow-library-heading">
            <div className="workflow-library-heading-copy">
              <div className="page-eyebrow">Workflow designer</div>
              <h2>Framework sign-in required</h2>
              <p>Open this page from the LogicFlow Enterprise Framework after signing in.</p>
            </div>
          </header>
        </section>
      </main>
    )
  }

  if (appMode === 'designer') {
    return (
      <main className="app-shell" dir="ltr">
        <ToastStack error={error} notice={notice} onDismissError={() => setError('')} onDismissNotice={() => setNotice('')} />

        <WorkflowAdministration
          appMode={appMode}
          busy={busy}
          canEditDraft={!selectedDefinition || selectedDefinition.status === 'Draft' || selectedDefinition.status === 'Published'}
          definitions={definitions}
          definitionSearch={definitionSearch}
          definitionStatusFilter={definitionStatusFilter}
          definitionView={definitionView}
          draftJson={draftJson}
          effectiveFromUtc={effectiveFromUtc}
          effectiveToUtc={effectiveToUtc}
          externalApiEndpoints={[]}
          filteredDefinitions={filteredDefinitions}
          onCreateDefinition={handleCreateDefinition}
          onOpenDesigner={navigateToDesigner}
          onPublish={handlePublish}
          onSaveDraft={handleSaveDraft}
          onSelectDefinition={(definitionId) => {
            setSelectedDefinitionId(definitionId)
          }}
          onShowList={navigateToLibrary}
          roles={roles}
          selectedDefinition={selectedDefinition}
          selectedUser={session.user}
          selectedUserId={session.user.id}
          userGroups={userGroups}
          users={users}
          setDefinitionSearch={setDefinitionSearch}
          setDefinitionStatusFilter={setDefinitionStatusFilter}
          setDraftJson={setDraftJson}
          setEffectiveFromUtc={setEffectiveFromUtc}
          setEffectiveToUtc={setEffectiveToUtc}
          validation={validation}
        />
      </main>
    )
  }

  return (
    <main className="app-shell" dir="ltr">
      <ToastStack error={error} notice={notice} onDismissError={() => setError('')} onDismissNotice={() => setNotice('')} />

      {activeView === 'dashboard' && (
        <Dashboard definitions={definitions} tasks={tasks} userGroups={userGroups} users={users} />
      )}

      {activeView === 'tasks' && (
        <TaskOperations
          busy={busy}
          canCancel={canCancel}
          comment={taskComment}
          delegateTargetUserId={delegateTargetUserId}
          detail={selectedTaskDetail}
          filteredTasks={filteredTasks}
          onAddComment={() => void handleAddTaskComment()}
          onApprove={() => void handleApproveTask()}
          onCancel={() => void handleCancelWorkflow()}
          onClaim={() => void handleClaimTask()}
          onDelegate={() => void handleDelegateTask()}
          onReassign={() => void handleReassignTask()}
          onReject={() => void handleRejectTask()}
          onSelectTask={(taskId) => {
            void handleSelectTask(taskId)
          }}
          onUnclaim={() => void handleUnclaimTask()}
          reassignTargetUserId={reassignTargetUserId}
          selectedTaskId={selectedTaskId}
          setComment={setTaskComment}
          setDelegateTargetUserId={setDelegateTargetUserId}
          setReassignTargetUserId={setReassignTargetUserId}
          setTaskSearch={setTaskSearch}
          setTaskStatusFilter={setTaskStatusFilter}
          taskSearch={taskSearch}
          tasks={tasks}
          taskStatusFilter={taskStatusFilter}
          users={users}
        />
      )}

      {activeView === 'definitions' && (
        <WorkflowAdministration
          appMode={appMode}
          busy={busy}
          canEditDraft={!selectedDefinition || selectedDefinition.status === 'Draft' || selectedDefinition.status === 'Published'}
          definitions={definitions}
          definitionSearch={definitionSearch}
          definitionStatusFilter={definitionStatusFilter}
          definitionView={definitionView}
          draftJson={draftJson}
          effectiveFromUtc={effectiveFromUtc}
          effectiveToUtc={effectiveToUtc}
          externalApiEndpoints={[]}
          filteredDefinitions={filteredDefinitions}
          onCreateDefinition={handleCreateDefinition}
          onOpenDesigner={navigateToDesigner}
          onPublish={handlePublish}
          onSaveDraft={handleSaveDraft}
          onSelectDefinition={(definitionId) => {
            setSelectedDefinitionId(definitionId)
          }}
          onShowList={navigateToLibrary}
          roles={roles}
          selectedDefinition={selectedDefinition}
          selectedUser={session.user}
          selectedUserId={session.user.id}
          userGroups={userGroups}
          users={users}
          setDefinitionSearch={setDefinitionSearch}
          setDefinitionStatusFilter={setDefinitionStatusFilter}
          setDraftJson={setDraftJson}
          setEffectiveFromUtc={setEffectiveFromUtc}
          setEffectiveToUtc={setEffectiveToUtc}
          validation={validation}
        />
      )}
    </main>
  )
}

function toUtcIso(value: string) {
  if (!value.trim()) {
    return undefined
  }

  const parsed = new Date(value)
  return Number.isNaN(parsed.getTime()) ? undefined : parsed.toISOString()
}

function createDraftJson(name: string, description: string, userId: string) {
  return JSON.stringify(
    {
      ...emptyWorkflowTemplate,
      metadata: {
        name,
        ...(description ? { description } : {}),
      },
      nodes: emptyWorkflowTemplate.nodes.map((node) => (
        node.id === 'task'
          ? { ...node, assignedToUserId: userId }
          : node
      )),
    },
    null,
    2,
  )
}

function syncDraftDefinitionMetadata(rawDraftJson: string | null | undefined, name: string, description: string, userId: string) {
  try {
    const parsed = rawDraftJson ? JSON.parse(rawDraftJson) as {
      metadata?: Record<string, unknown>
      nodes?: Array<Record<string, unknown>>
    } : JSON.parse(createDraftJson(name, description, userId)) as {
      metadata?: Record<string, unknown>
      nodes?: Array<Record<string, unknown>>
    }

    parsed.metadata = {
      ...(parsed.metadata ?? {}),
      name,
      ...(description ? { description } : {}),
    }

    if (!description && parsed.metadata.description) {
      delete parsed.metadata.description
    }

    parsed.nodes = (parsed.nodes ?? []).map((node) => {
      if (node.id === 'task' && node.type === 'userTask' && !String(node.assignedToUserId ?? '').trim()) {
        return { ...node, assignedToUserId: userId }
      }

      return node
    })

    return JSON.stringify(parsed, null, 2)
  } catch {
    return createDraftJson(name, description, userId)
  }
}

export default App
