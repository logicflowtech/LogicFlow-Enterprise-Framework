import type { BranchedStep, Definition, Sequence, Step } from 'sequential-workflow-designer'

export type RuntimeNode = {
  id: string
  type: string
  name?: string
  label?: string
  description?: string
  nodeCategory?: string
  icon?: string
  expression?: string
  assignmentType?: string
  assignedToUserId?: string
  assignedToUserIds?: string[]
  assignedToGroupId?: string
  assignedToGroupIds?: string[]
  assignedToRoleId?: string
  assignedToRoleIds?: string[]
  dueInHours?: number
  waitType?: string
  waitExpression?: string
  timerType?: string
  timerExpression?: string
  processKey?: string
  serviceKey?: string
  externalApiEndpointId?: string
  notificationKey?: string
  channel?: string
  recipientSource?: string
  templateKey?: string
  formKey?: string
  approvalMode?: string
  inputMapping?: string
  outputMapping?: string
  taskContractInputs?: string
  taskContractOutputs?: string
  taskOutputStorage?: string
  retryPolicy?: string
  timeoutHours?: number
  errorHandlingPath?: string
  targetVariable?: string
  operation?: string
  valueExpression?: string
  childWorkflowKey?: string
  versionMode?: string
  waitForCompletion?: boolean
  triggerCondition?: string
  escalationTarget?: string
  escalationMessage?: string
  errorFilter?: string
  recoveryAction?: string
  fallbackProcessKey?: string
  joinMode?: string
  branchCount?: number
  businessCalendar?: boolean
  metadata?: Record<string, unknown>
}

export type RuntimeEdge = {
  from: string
  to: string
  outcome?: string
}

export type RuntimeDefinition = {
  schemaVersion?: number
  metadata?: {
    name?: string
    description?: string
    owner?: string
    tags?: string[]
  }
  nodes: RuntimeNode[]
  edges: RuntimeEdge[]
}

export function runtimeToDesigner(runtime: RuntimeDefinition): Definition {
  const nodes = new Map(runtime.nodes.map((node) => [node.id, node]))
  const start = runtime.nodes.find((node) => node.type === 'start')

  if (!start) {
    throw new Error('Workflow JSON must contain a start node.')
  }

  return {
    properties: {},
    sequence: buildDesignerSequence(start.id, nodes, runtime.edges, new Set()),
  }
}

export function designerToRuntime(definition: Definition, source?: RuntimeDefinition): RuntimeDefinition {
  const nodes: RuntimeNode[] = [
    { id: 'start', type: 'start' },
    { id: 'end', type: 'end' },
  ]
  const edges: RuntimeEdge[] = []

  appendSequence(definition.sequence, 'start', 'end', nodes, edges)

  return {
    schemaVersion: source?.schemaVersion ?? 2,
    metadata: source?.metadata ?? {},
    nodes,
    edges,
  }
}

function buildDesignerSequence(fromNodeId: string, nodes: Map<string, RuntimeNode>, edges: RuntimeEdge[], visited: Set<string>): Sequence {
  const sequence: Sequence = []
  let nextEdge = findDefaultEdge(fromNodeId, edges)

  while (nextEdge) {
    const node = nodes.get(nextEdge.to)
    if (!node || node.type === 'end' || visited.has(node.id)) break

    visited.add(node.id)

    if (isTaskLikeType(node.type)) {
      const normalizedType = normalizeTaskType(node.type)
      const taskMode = node.type === 'approval' || node.approvalMode || node.formKey ? 'approval' : 'task'
      const processMode = node.type === 'dataUpdate'
        || node.targetVariable
        || node.valueExpression
        ? 'dataUpdate'
        : node.externalApiEndpointId
          ? 'externalApi'
          : 'service'
      sequence.push({
        id: node.id,
        componentType: 'task',
        type: normalizedType,
        name: node.name || defaultNodeName(normalizedType),
        properties: {
          taskMode,
          assignedToUserId: node.assignedToUserId ?? '',
          assignedToUserIds: normalizeAssignedIds(node.assignedToUserIds, node.assignedToUserId),
          assignedToGroupId: node.assignedToGroupId ?? '',
          assignedToGroupIds: normalizeAssignedIds(node.assignedToGroupIds, node.assignedToGroupId),
          assignedToRoleId: node.assignedToRoleId ?? '',
          assignedToRoleIds: normalizeAssignedIds(node.assignedToRoleIds, node.assignedToRoleId),
          dueInHours: node.dueInHours ?? null,
          description: node.description ?? '',
          formKey: node.formKey ?? '',
          approvalMode: node.approvalMode ?? 'single',
          taskContractInputs: getTaskContractSegment(node.metadata, 'inputs'),
          taskContractOutputs: getTaskContractSegment(node.metadata, 'outputs'),
          taskOutputStorage: getTaskOutputStorage(node.metadata),
          ...(isDelayType(node.type)
            ? {
                waitType: node.waitType ?? node.timerType ?? 'duration',
                waitExpression: node.waitExpression ?? node.timerExpression ?? '',
                businessCalendar: node.businessCalendar ?? false,
              }
            : {}),
          ...(node.type === 'serviceTask'
            ? {
                processMode,
                processKey: node.processKey ?? node.serviceKey ?? '',
                externalApiEndpointId: node.externalApiEndpointId ?? '',
                inputMapping: node.inputMapping ?? '',
                outputMapping: node.outputMapping ?? '',
                retryPolicy: node.retryPolicy ?? 'none',
                timeoutHours: node.timeoutHours ?? null,
                errorHandlingPath: node.errorHandlingPath ?? '',
                targetVariable: node.targetVariable ?? '',
                operation: node.operation ?? 'set',
                valueExpression: node.valueExpression ?? '',
              }
            : {}),
          ...(node.type === 'notification'
            ? {
                notificationKey: node.notificationKey ?? '',
                channel: node.channel ?? 'inApp',
                recipientSource: node.recipientSource ?? 'workflowInitiator',
                templateKey: node.templateKey ?? '',
              }
            : {}),
          ...(node.type === 'dataUpdate'
            ? {
                processMode: 'dataUpdate',
                targetVariable: node.targetVariable ?? '',
                operation: node.operation ?? 'set',
                valueExpression: node.valueExpression ?? '',
              }
            : {}),
          ...(node.type === 'subflow'
            ? {
                childWorkflowKey: node.childWorkflowKey ?? '',
                versionMode: node.versionMode ?? 'latest',
                waitForCompletion: node.waitForCompletion ?? true,
                inputMapping: node.inputMapping ?? '',
                outputMapping: node.outputMapping ?? '',
              }
            : {}),
          ...(node.type === 'escalation'
            ? {
                triggerCondition: node.triggerCondition ?? '',
                escalationTarget: node.escalationTarget ?? '',
                escalationMessage: node.escalationMessage ?? '',
              }
            : {}),
          ...(node.type === 'exceptionHandler'
            ? {
                errorFilter: node.errorFilter ?? '',
                recoveryAction: node.recoveryAction ?? '',
                fallbackProcessKey: node.fallbackProcessKey ?? '',
              }
            : {}),
          ...(node.type === 'parallelSplit'
            ? {
                branchCount: node.branchCount ?? 2,
              }
            : {}),
          ...(node.type === 'parallelJoin'
            ? {
                joinMode: node.joinMode ?? 'all',
              }
            : {}),
        },
      })
      nextEdge = findDefaultEdge(node.id, edges)
      continue
    }

    if (isBranchedType(node.type)) {
      const branches = buildBranches(node, nodes, edges, visited)
      sequence.push({
        id: node.id,
        componentType: 'switch',
        type: node.type,
        name: node.name || defaultNodeName(node.type),
        properties: {
          expression: node.expression ?? '',
          branchCount: node.branchCount ?? Object.keys(branches).length,
          description: node.description ?? '',
        },
        branches,
      } as BranchedStep)
      break
    }

    break
  }

  return sequence
}

function buildBranchSequence(nodeId: string, nodes: Map<string, RuntimeNode>, edges: RuntimeEdge[], visited: Set<string>) {
  const syntheticEdges = [{ from: `branch-${nodeId}`, to: nodeId } as RuntimeEdge, ...edges]
  return buildDesignerSequence(`branch-${nodeId}`, nodes, syntheticEdges, new Set(visited))
}

function appendSequence(sequence: Sequence, previousNodeId: string, finalNodeId: string, nodes: RuntimeNode[], edges: RuntimeEdge[]) {
  if (sequence.length === 0) {
    edges.push({ from: previousNodeId, to: finalNodeId })
    return
  }

  sequence.forEach((step, index) => {
    const nextStep = sequence[index + 1]
    const nextNodeId = nextStep?.id ?? finalNodeId

    if (isTaskLikeType(step.type)) {
      nodes.push(toTaskLikeNode(step))
      if (shouldAddDefaultEdge(previousNodeId, nodes)) {
        edges.push({ from: previousNodeId, to: step.id })
      }
      previousNodeId = step.id
      return
    }

    if (isBranchedType(step.type)) {
      const branchedStep = step as BranchedStep
      nodes.push(toBranchedNode(step))
      if (shouldAddDefaultEdge(previousNodeId, nodes)) {
        edges.push({ from: previousNodeId, to: step.id })
      }
      appendBranches(branchedStep, nextNodeId, nodes, edges)
      previousNodeId = step.id
    }
  })

  const lastStep = sequence.at(-1)
  if (lastStep && isTaskLikeType(lastStep.type)) {
    edges.push({ from: lastStep.id, to: finalNodeId })
  }
}

function appendBranch(
  sequence: Sequence,
  conditionNodeId: string,
  finalNodeId: string,
  outcome: string,
  nodes: RuntimeNode[],
  edges: RuntimeEdge[],
) {
  if (sequence.length === 0) {
    edges.push({ from: conditionNodeId, to: finalNodeId, outcome })
    return
  }

  edges.push({ from: conditionNodeId, to: sequence[0].id, outcome })
  appendSequenceAfterFirst(sequence, finalNodeId, nodes, edges)
}

function appendSequenceAfterFirst(sequence: Sequence, finalNodeId: string, nodes: RuntimeNode[], edges: RuntimeEdge[]) {
  sequence.forEach((step, index) => {
    const nextNodeId = sequence[index + 1]?.id ?? finalNodeId

    if (isTaskLikeType(step.type)) {
      nodes.push(toTaskLikeNode(step))
      edges.push({ from: step.id, to: nextNodeId })
      return
    }

    if (isBranchedType(step.type)) {
      const branchedStep = step as BranchedStep
      nodes.push(toBranchedNode(step))
      appendBranches(branchedStep, nextNodeId, nodes, edges)
    }
  })
}

function toTaskLikeNode(step: Step): RuntimeNode {
  const taskMode = String(step.properties.taskMode ?? '')
  const processMode = String(step.properties.processMode ?? '')
  const node: RuntimeNode = {
    id: step.id,
    type: step.type === 'userTask'
      ? (taskMode === 'approval' || emptyToUndefined(step.properties.approvalMode) || emptyToUndefined(step.properties.formKey) ? 'approval' : 'userTask')
      : step.type === 'serviceTask' && (processMode === 'dataUpdate' || emptyToUndefined(step.properties.targetVariable) || emptyToUndefined(step.properties.valueExpression))
        ? 'dataUpdate'
        : step.type,
    name: step.name,
    description: emptyToUndefined(step.properties.description),
  }

  const assignedToUserId = emptyToUndefined(step.properties.assignedToUserId)
  const assignedToUserIds = normalizeAssignedIds(step.properties.assignedToUserIds, assignedToUserId)
  const assignedToGroupId = emptyToUndefined(step.properties.assignedToGroupId)
  const assignedToGroupIds = normalizeAssignedIds(step.properties.assignedToGroupIds, assignedToGroupId)
  const assignedToRoleId = emptyToUndefined(step.properties.assignedToRoleId)
  const assignedToRoleIds = normalizeAssignedIds(step.properties.assignedToRoleIds, assignedToRoleId)

  if (assignedToUserIds.length > 0) {
    node.assignedToUserId = assignedToUserIds[0]
    if (assignedToUserIds.length > 1) {
      node.assignedToUserIds = assignedToUserIds
    }
  }

  if (assignedToGroupIds.length > 0) {
    node.assignedToGroupId = assignedToGroupIds[0]
    if (assignedToGroupIds.length > 1) {
      node.assignedToGroupIds = assignedToGroupIds
    }
  }

  if (assignedToRoleIds.length > 0) {
    node.assignedToRoleId = assignedToRoleIds[0]
    if (assignedToRoleIds.length > 1) {
      node.assignedToRoleIds = assignedToRoleIds
    }
  }

  const assignmentType = emptyToUndefined(step.properties.assignmentType)
  if (assignmentType) {
    node.assignmentType = assignmentType
  }

  const formKey = emptyToUndefined(step.properties.formKey)
  if (formKey) {
    node.formKey = formKey
  }

  const approvalMode = emptyToUndefined(step.properties.approvalMode)
  if (approvalMode && approvalMode !== 'single') {
    node.approvalMode = approvalMode
  }

  if (step.type === 'userTask') {
    const metadata = buildUserTaskMetadata(step)
    if (metadata) {
      node.metadata = metadata
    }

    assignStringProperty(node, 'inputMapping', step.properties.inputMapping)
    assignStringProperty(node, 'outputMapping', step.properties.outputMapping)
  }

  if (typeof step.properties.dueInHours === 'number') {
    node.dueInHours = step.properties.dueInHours
  }

  if (isDelayType(step.type)) {
    const waitType = emptyToUndefined(step.properties.waitType ?? step.properties.timerType)
    const waitExpression = emptyToUndefined(step.properties.waitExpression ?? step.properties.timerExpression)

    if (waitType) {
      node.waitType = waitType
    }

    if (waitExpression) {
      node.waitExpression = waitExpression
    }

    if (typeof step.properties.businessCalendar === 'boolean') {
      node.businessCalendar = step.properties.businessCalendar
    }
  }

  if (step.type === 'serviceTask') {
    const processKey = emptyToUndefined(step.properties.processKey ?? step.properties.serviceKey)
    if (processKey) {
      node.processKey = processKey
    }

    assignStringProperty(node, 'externalApiEndpointId', step.properties.externalApiEndpointId)
    assignStringProperty(node, 'inputMapping', step.properties.inputMapping)
    assignStringProperty(node, 'outputMapping', step.properties.outputMapping)
    assignStringProperty(node, 'retryPolicy', step.properties.retryPolicy)
    assignStringProperty(node, 'errorHandlingPath', step.properties.errorHandlingPath)

    if (typeof step.properties.timeoutHours === 'number') {
      node.timeoutHours = step.properties.timeoutHours
    }

    const processMode = emptyToUndefined(step.properties.processMode)
    if (processMode === 'dataUpdate' || emptyToUndefined(step.properties.targetVariable) || emptyToUndefined(step.properties.valueExpression)) {
      assignStringProperty(node, 'targetVariable', step.properties.targetVariable)
      assignStringProperty(node, 'operation', step.properties.operation)
      assignStringProperty(node, 'valueExpression', step.properties.valueExpression)
    }
  }

  if (step.type === 'notification') {
    const notificationKey = emptyToUndefined(step.properties.notificationKey)
    if (notificationKey) {
      node.notificationKey = notificationKey
    }

    assignStringProperty(node, 'channel', step.properties.channel)
    assignStringProperty(node, 'recipientSource', step.properties.recipientSource)
    assignStringProperty(node, 'templateKey', step.properties.templateKey)
  }

  if (step.type === 'subflow') {
    assignStringProperty(node, 'childWorkflowKey', step.properties.childWorkflowKey)
    assignStringProperty(node, 'versionMode', step.properties.versionMode)
    assignStringProperty(node, 'inputMapping', step.properties.inputMapping)
    assignStringProperty(node, 'outputMapping', step.properties.outputMapping)

    if (typeof step.properties.waitForCompletion === 'boolean') {
      node.waitForCompletion = step.properties.waitForCompletion
    }
  }

  if (step.type === 'escalation') {
    assignStringProperty(node, 'triggerCondition', step.properties.triggerCondition)
    assignStringProperty(node, 'escalationTarget', step.properties.escalationTarget)
    assignStringProperty(node, 'escalationMessage', step.properties.escalationMessage)
  }

  if (step.type === 'exceptionHandler') {
    assignStringProperty(node, 'errorFilter', step.properties.errorFilter)
    assignStringProperty(node, 'recoveryAction', step.properties.recoveryAction)
    assignStringProperty(node, 'fallbackProcessKey', step.properties.fallbackProcessKey)
  }

  if (step.type === 'parallelSplit' && typeof step.properties.branchCount === 'number') {
    node.branchCount = step.properties.branchCount
  }

  if (step.type === 'parallelJoin') {
    assignStringProperty(node, 'joinMode', step.properties.joinMode)
  }

  return node
}

function toBranchedNode(step: Step): RuntimeNode {
  return {
    id: step.id,
    type: step.type,
    name: step.name,
    expression: String(step.properties.expression ?? ''),
    description: emptyToUndefined(step.properties.description),
    ...(step.type === 'parallelSplit'
      ? {
          branchCount: Object.keys((step as BranchedStep).branches ?? {}).length,
        }
      : {}),
  }
}

function findDefaultEdge(from: string, edges: RuntimeEdge[]) {
  return edges.find((edge) => edge.from === from && !edge.outcome)
}

function findOutcomeEdge(from: string, outcome: string, edges: RuntimeEdge[]) {
  return edges.find((edge) => edge.from === from && edge.outcome === outcome)
}

function buildBranches(node: RuntimeNode, nodes: Map<string, RuntimeNode>, edges: RuntimeEdge[], visited: Set<string>) {
  if (node.type === 'condition') {
    const trueTarget = findOutcomeEdge(node.id, 'true', edges)
    const falseTarget = findOutcomeEdge(node.id, 'false', edges)
    return {
      true: trueTarget ? buildBranchSequence(trueTarget.to, nodes, edges, visited) : [],
      false: falseTarget ? buildBranchSequence(falseTarget.to, nodes, edges, visited) : [],
    }
  }

  const branchNames = getParallelBranchNames(node, edges)
  return Object.fromEntries(
    branchNames.map((branchName) => {
      const target = findOutcomeEdge(node.id, branchName, edges)
      return [branchName, target ? buildBranchSequence(target.to, nodes, edges, visited) : []]
    }),
  )
}

function getParallelBranchNames(node: RuntimeNode, edges: RuntimeEdge[]) {
  const configured = edges
    .filter((edge) => edge.from === node.id && edge.outcome)
    .map((edge) => String(edge.outcome))

  if (configured.length > 0) {
    return Array.from(new Set(configured))
  }

  const branchCount = Math.max(2, node.branchCount ?? 2)
  return Array.from({ length: branchCount }, (_, index) => `branch${index + 1}`)
}

function appendBranches(step: BranchedStep, nextNodeId: string, nodes: RuntimeNode[], edges: RuntimeEdge[]) {
  Object.entries(step.branches).forEach(([branchName, sequence]) => {
    appendBranch(sequence, step.id, nextNodeId, branchName, nodes, edges)
  })
}

function emptyToUndefined(value: unknown) {
  const text = String(value ?? '').trim()
  return text ? text : undefined
}

function normalizeAssignedIds(values: unknown, fallback?: string) {
  const list = Array.isArray(values)
    ? values
    : typeof values === 'string'
      ? values.split(',')
      : []

  const normalized = list
    .map((item) => String(item ?? '').trim())
    .filter(Boolean)

  if (fallback && !normalized.includes(fallback)) {
    normalized.unshift(fallback)
  }

  return Array.from(new Set(normalized))
}

function assignStringProperty<T extends keyof RuntimeNode>(node: RuntimeNode, key: T, value: unknown) {
  const text = emptyToUndefined(value)
  if (text) {
    node[key] = text as RuntimeNode[T]
  }
}

function getTaskContractSegment(metadata: Record<string, unknown> | undefined, segment: 'inputs' | 'outputs') {
  const taskContract = metadata?.taskContract
  if (!taskContract || typeof taskContract !== 'object' || !Array.isArray((taskContract as Record<string, unknown>)[segment])) {
    return ''
  }

  return JSON.stringify((taskContract as Record<string, unknown>)[segment], null, 2)
}

function getTaskOutputStorage(metadata: Record<string, unknown> | undefined) {
  const outputMapping = metadata?.outputMapping
  if (!outputMapping || typeof outputMapping !== 'object' || Array.isArray(outputMapping)) {
    return ''
  }

  return JSON.stringify(outputMapping, null, 2)
}

function buildUserTaskMetadata(step: Step) {
  const metadata: Record<string, unknown> = {}
  const inputs = parseJsonText(step.properties.taskContractInputs)
  const outputs = parseJsonText(step.properties.taskContractOutputs)
  const outputStorage = parseJsonText(step.properties.taskOutputStorage)

  if (Array.isArray(inputs) || Array.isArray(outputs)) {
    metadata.taskContract = {
      ...(Array.isArray(inputs) ? { inputs } : {}),
      ...(Array.isArray(outputs) ? { outputs } : {}),
    }
  }

  if (outputStorage && typeof outputStorage === 'object' && !Array.isArray(outputStorage)) {
    metadata.outputMapping = outputStorage
  }

  return Object.keys(metadata).length > 0 ? metadata : undefined
}

function parseJsonText(value: unknown) {
  const text = String(value ?? '').trim()
  if (!text) {
    return undefined
  }

  try {
    return JSON.parse(text)
  } catch {
    return undefined
  }
}

function shouldAddDefaultEdge(previousNodeId: string, nodes: RuntimeNode[]) {
  return nodes.find((node) => node.id === previousNodeId)?.type !== 'condition'
}

function defaultNodeName(type: string) {
  switch (type) {
    case 'userTask':
      return 'Human Task'
    case 'approval':
      return 'Approval Task'
    case 'timer':
    case 'delay':
      return 'Wait'
    case 'serviceTask':
      return 'Process Task'
    case 'subflow':
      return 'Subflow'
    case 'escalation':
      return 'Escalation'
    case 'parallelSplit':
      return 'Parallel Split'
    case 'parallelJoin':
      return 'Parallel Join'
    case 'notification':
      return 'Notification'
    case 'exceptionHandler':
      return 'Exception Handler'
    default:
      return 'Task'
  }
}

function normalizeTaskType(type: string) {
  if (type === 'approval') {
    return 'userTask'
  }

  if (type === 'dataUpdate') {
    return 'serviceTask'
  }

  return type
}

function isBranchedType(type: string) {
  return type === 'condition' || type === 'parallelSplit'
}

function isTaskLikeType(type: string) {
  return [
    'approval',
    'userTask',
    'timer',
    'delay',
    'serviceTask',
    'dataUpdate',
    'notification',
    'subflow',
    'escalation',
    'parallelJoin',
    'exceptionHandler',
  ].includes(type)
}

function isDelayType(type: string) {
  return type === 'timer' || type === 'delay'
}
