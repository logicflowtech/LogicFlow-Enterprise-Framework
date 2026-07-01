import { useEffect, useMemo, useRef } from 'react'
import {
  DefaultViewportControllerDesignerExtension,
  Designer,
  LineGridDesignerExtension,
  RectPlaceholderDesignerExtension,
  StartStopRootComponentDesignerExtension,
  StepsDesignerExtension,
  type Definition,
  type DesignerConfiguration,
  type Step,
  type StepDefinition,
  type StepEditorContext,
} from 'sequential-workflow-designer'
import type { ExternalApiEndpoint, Role, User, UserGroup } from './api'
import { designerToRuntime, runtimeToDesigner, type RuntimeDefinition } from './workflowDefinitionMapper'
import 'sequential-workflow-designer/css/designer.css'
import 'sequential-workflow-designer/css/designer-light.css'

type WorkflowDesignerProps = {
  value: string
  onChange: (value: string) => void
  onViewportActionsChange?: (actions: ViewportActions | null) => void
  readonly: boolean
  externalApiEndpoints: ExternalApiEndpoint[]
  roles: Role[]
  selectedUserId: string
  userGroups: UserGroup[]
  users: User[]
}

export type ViewportActions = {
  reset: () => void
  zoomIn: () => void
  zoomOut: () => void
}

const insertableStepTypes = ['userTask', 'condition', 'serviceTask', 'timer', 'notification']

const slaProfileOptions = [
  { label: 'No SLA', value: '' },
  { label: '4 hours', value: '4' },
  { label: '8 hours', value: '8' },
  { label: '1 business day', value: '24' },
  { label: '2 business days', value: '48' },
  { label: '5 business days', value: '120' },
]

const workflowTokenPresets = [
  { label: 'workflow.id', value: 'workflow.id' },
  { label: 'requester.id', value: 'requester.id' },
  { label: 'variables.amount', value: 'variables.amount' },
  { label: 'utcNow()', value: 'utcNow()' },
  { label: 'currentUser.id', value: 'currentUser.id' },
]

type BranchedStepLike = Step & {
  branches: Record<string, Array<Step>>
}

export function WorkflowDesigner({ value, onChange, onViewportActionsChange, readonly, externalApiEndpoints, roles, selectedUserId, userGroups, users }: WorkflowDesignerProps) {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const designerRef = useRef<Designer | null>(null)
  const latestJsonRef = useRef(value)
  const latestRuntimeRef = useRef<RuntimeDefinition | null>(null)
  const isApplyingExternalDefinitionRef = useRef(false)
  const configuredUserIdRef = useRef(selectedUserId)

  useEffect(() => {
    latestJsonRef.current = value
  }, [value])

  const parsed = useMemo(() => {
    try {
      const runtime = JSON.parse(value) as RuntimeDefinition
      return { definition: runtimeToDesigner(runtime), runtime, error: '' }
    } catch (exception) {
      return {
        definition: null,
        runtime: null,
        error: exception instanceof Error ? exception.message : 'Workflow JSON could not be loaded by the designer.',
      }
    }
  }, [value])

  useEffect(() => {
    latestRuntimeRef.current = parsed.runtime
  }, [parsed.runtime])

  useEffect(() => {
    if (!parsed.error) return

    designerRef.current?.destroy()
    designerRef.current = null
  }, [parsed.error])

  useEffect(() => {
    if (!containerRef.current || !parsed.definition) return

    if (designerRef.current && configuredUserIdRef.current === selectedUserId) {
      return
    }

    designerRef.current?.destroy()

    const designer = Designer.create(containerRef.current, parsed.definition, createDesignerConfiguration(readonly, externalApiEndpoints, roles, selectedUserId, userGroups, users))
    designerRef.current = designer
    configuredUserIdRef.current = selectedUserId
    onViewportActionsChange?.({
      reset: () => designer.resetViewport(),
      zoomIn: () => zoomDesigner(designer, true),
      zoomOut: () => zoomDesigner(designer, false),
    })

    designer.onDefinitionChanged.subscribe((event) => {
      if (isApplyingExternalDefinitionRef.current) {
        return
      }

      const nextJson = JSON.stringify(designerToRuntime(event.definition, latestRuntimeRef.current ?? undefined), null, 2)
      latestJsonRef.current = nextJson
      onChange(nextJson)
    })

    designer.onReady.subscribe(() => {
      designer.setIsReadonly(readonly)
    })
  }, [externalApiEndpoints, onChange, onViewportActionsChange, parsed.definition, readonly, roles, selectedUserId, userGroups, users])

  useEffect(() => () => {
    designerRef.current?.destroy()
    designerRef.current = null
    onViewportActionsChange?.(null)
  }, [onViewportActionsChange])

  useEffect(() => {
    const designer = designerRef.current
    if (!designer) return

    designer.setIsReadonly(readonly)
  }, [readonly])

  useEffect(() => {
    const designer = designerRef.current
    if (!designer || !parsed.definition || latestJsonRef.current === value) return

    isApplyingExternalDefinitionRef.current = true
    void designer.replaceDefinition(parsed.definition).finally(() => {
      latestJsonRef.current = value
      isApplyingExternalDefinitionRef.current = false
    })
  }, [parsed.definition, value])

  if (parsed.error) {
    return <div className="designer-error">{parsed.error}</div>
  }

  return <div className="workflow-designer" ref={containerRef} />
}

function zoomDesigner(designer: Designer, zoomIn: boolean) {
  const current = designer.getViewport()
  const scales = [0.5, 0.75, 1, 1.25, 1.5]
  const currentIndex = scales.findIndex((scale) => Math.abs(scale - current.scale) < 0.001)
  const fallbackIndex = scales.findIndex((scale) => scale >= current.scale)
  const index = currentIndex >= 0 ? currentIndex : Math.max(0, fallbackIndex)
  const nextIndex = zoomIn
    ? Math.min(scales.length - 1, index + 1)
    : Math.max(0, index - 1)

  designer.setViewport({
    position: current.position,
    scale: scales[nextIndex],
  })
}

function createDesignerConfiguration(readonly: boolean, externalApiEndpoints: ExternalApiEndpoint[], roles: Role[], selectedUserId: string, userGroups: UserGroup[], users: User[]): DesignerConfiguration {
  return {
    theme: 'light',
    isReadonly: readonly,
    undoStackSize: 20,
    controlBar: false,
    contextMenu: true,
    extensions: [
      StartStopRootComponentDesignerExtension.create({}),
      RectPlaceholderDesignerExtension.create(),
      DefaultViewportControllerDesignerExtension.create({
        scales: [0.5, 0.75, 1, 1.25, 1.5],
        smoothDeltaYLimit: 16,
        padding: 40,
      }),
      LineGridDesignerExtension.create(),
      StepsDesignerExtension.create({
        task: {},
        switch: {
          branchNameLabelResolver: (branchName, step) => {
            if (step.type === 'condition') {
              return branchName === 'true' ? 'True' : 'False'
            }

            if (step.type === 'parallelSplit') {
              return formatParallelBranchName(branchName)
            }

            return branchName
          },
        },
      }),
    ],
    toolbox: {
      isCollapsed: true,
      groups: [
        {
          name: 'Flow',
          steps: [
            {
              componentType: 'switch',
              type: 'condition',
              name: 'Decision',
              properties: {
                expression: 'amount > 5000',
              },
              branches: {
                true: [],
                false: [],
              },
            } as unknown as StepDefinition,
            {
              componentType: 'task',
              type: 'timer',
              name: 'Wait',
              properties: {
                description: '',
                waitType: 'duration',
                dueInHours: 24,
                waitExpression: '',
                businessCalendar: false,
              },
            },
          ],
        },
        {
          name: 'Human Work',
          steps: [
            {
              componentType: 'task',
              type: 'userTask',
              name: 'Approval Task',
              properties: {
                taskMode: 'approval',
                assignedToUserId: selectedUserId,
                assignedToRoleId: '',
                assignedToGroupId: '',
                dueInHours: 24,
                description: '',
                approvalMode: 'single',
                formKey: '',
              },
            },
            {
              componentType: 'task',
              type: 'userTask',
              name: 'Action Task',
              properties: {
                taskMode: 'task',
                assignedToUserId: selectedUserId,
                assignedToRoleId: '',
                assignedToGroupId: '',
                dueInHours: null,
                description: '',
                approvalMode: '',
                formKey: '',
              },
            },
          ],
        },
        {
          name: 'System Work',
          steps: [
            {
              componentType: 'task',
              type: 'serviceTask',
              name: 'Process Task',
              properties: {
                description: '',
                processMode: 'service',
                processKey: '',
                externalApiEndpointId: '',
                inputMapping: '',
                outputMapping: '',
                retryPolicy: 'none',
                timeoutHours: null,
                errorHandlingPath: '',
                targetVariable: '',
                operation: 'set',
                valueExpression: '',
              },
            },
            {
              componentType: 'task',
              type: 'notification',
              name: 'Notification',
              properties: {
                description: '',
                notificationKey: '',
                channel: 'inApp',
                recipientSource: 'workflowInitiator',
                templateKey: '',
              },
            },
          ],
        },
      ],
    },
    steps: {
      isDeletable: (step) => step.type !== 'start' && step.type !== 'end',
      canInsertStep: (step) => insertableStepTypes.includes(step.type),
      canMoveStep: (_sourceSequence, step) => insertableStepTypes.includes(step.type),
      iconUrlProvider: (_componentType, type) => getStepIconUrl(type),
    },
    editors: {
      isCollapsed: false,
      stepEditorProvider: (step, context, definition) => createStepEditor(step, context, definition, readonly, externalApiEndpoints, roles, userGroups, users),
            
      rootEditorProvider: () => {
        const root = document.createElement('div')
        root.className = 'designer-editor-form'
        root.append(createEditorTitle('Workflow', 'Configure the root workflow container.'))
        return root
      },
    },
    validator: {
      step: (step) => {
        if (step.type === 'userTask') {
          const assignmentCounts = [
            getNormalizedSelectionValues(step.properties.assignedToUserIds, step.properties.assignedToUserId).length,
            getNormalizedSelectionValues(step.properties.assignedToGroupIds, step.properties.assignedToGroupId).length,
            getNormalizedSelectionValues(step.properties.assignedToRoleIds, step.properties.assignedToRoleId).length,
          ].filter((count) => count > 0).length

          return assignmentCounts === 1
        }

        if (step.type === 'condition') {
          return Boolean(String(step.properties.expression ?? '').trim())
        }

        if (step.type === 'timer' || step.type === 'delay') {
          const waitType = String(step.properties.waitType ?? step.properties.timerType ?? 'duration').trim()
          const waitExpression = String(step.properties.waitExpression ?? step.properties.timerExpression ?? '').trim()
          const dueInHours = typeof step.properties.dueInHours === 'number' ? step.properties.dueInHours : null

          if (waitType === 'expression') {
            return Boolean(waitExpression)
          }

          return (dueInHours != null && dueInHours > 0) || Boolean(waitExpression)
        }

        if (step.type === 'serviceTask') {
          const processMode = String(
            step.properties.processMode
              ?? (String(step.properties.externalApiEndpointId ?? '').trim()
                ? 'externalApi'
                : String(step.properties.targetVariable ?? '').trim() || String(step.properties.valueExpression ?? '').trim()
                  ? 'dataUpdate'
                  : 'service'),
          )

          if (processMode === 'externalApi') {
            return Boolean(String(step.properties.externalApiEndpointId ?? '').trim())
          }

          if (processMode === 'dataUpdate') {
            return Boolean(String(step.properties.targetVariable ?? '').trim() && String(step.properties.valueExpression ?? '').trim())
          }

          return Boolean(String(step.properties.processKey ?? step.properties.serviceKey ?? '').trim())
        }

        if (step.type === 'notification') {
          return Boolean(
            String(step.properties.notificationKey ?? '').trim()
            || String(step.properties.templateKey ?? '').trim(),
          )
        }

        return true
      },
      root: (definition) => definition.sequence.length > 0,
    },
  }
}

function getStepIconUrl(type: string): string | null {
  switch (type) {
    case 'userTask':
      return createStepIconSvg('#4c93bc', '<path d="M8 8.1a2.1 2.1 0 1 0 0-4.2 2.1 2.1 0 0 0 0 4.2Z"/><path d="M4.4 12.1c.5-1.7 1.8-2.6 3.6-2.6s3.1.9 3.6 2.6"/>')
    case 'condition':
      return createStepIconSvg('#8e5bb8', '<path d="M8 3.3 12.7 8 8 12.7 3.3 8 8 3.3Z"/><path d="M6.5 8h3"/><path d="M8 6.5v3"/>')
    case 'serviceTask':
      return createStepIconSvg('#3f6fb5', '<path d="M5 8h6"/><path d="M8 5v6"/><rect x="3.6" y="3.6" width="8.8" height="8.8" rx="1.6"/>')
    case 'timer':
    case 'delay':
      return createStepIconSvg('#c27b28', '<circle cx="8" cy="8" r="4.2"/><path d="M8 5.6v2.7l1.7 1"/>')
    case 'notification':
      return createStepIconSvg('#3f9b74', '<path d="M3.8 5.1h8.4v5.8H3.8z"/><path d="m4.3 5.6 3.7 3 3.7-3"/>')
    default:
      return null
  }
}

function createStepIconSvg(background: string, content: string): string {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16" fill="none"><rect x="1" y="1" width="14" height="14" rx="3" fill="${background}"/><g stroke="#ffffff" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round">${content}</g></svg>`
  return `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`
}

function createStepEditor(step: Step, context: StepEditorContext, _definition: Definition, isReadonly: boolean, externalApiEndpoints: ExternalApiEndpoint[], roles: Role[], userGroups: UserGroup[], users: User[]) {
  const root = document.createElement('div')
  root.className = 'designer-editor-form'
  root.append(createEditorTitle(getEditorTitle(step), getEditorCaption(step)))
  root.append(createSummaryBadges(step, externalApiEndpoints, roles, userGroups, users))
  root.append(createReadOnlyHint('Runtime-supported nodes in this release: Decision, Human Task, Process Task, Wait, and Notification.'))

  const generalSection = createEditorSection('General')
  generalSection.append(createTextInput('Name', step.name, isReadonly, (value) => {
    step.name = value
    context.notifyNameChanged()
  }))

  if (step.type !== 'condition') {
    generalSection.append(createTextArea('Description', String(step.properties.description ?? ''), isReadonly, (value) => {
      step.properties.description = value
      context.notifyPropertiesChanged()
    }))
  }

  root.append(generalSection)

  if (step.type === 'userTask') {
    const assignmentType = getUserTaskAssignmentType(step)
    const roleOptions = [
      ...roles
        .filter((role) => role.status === 'Active')
        .sort((left, right) => left.name.localeCompare(right.name))
        .map((role) => ({ label: `${role.name} (${role.code})`, value: role.id })),
    ]
    const groupOptions = [
      ...userGroups
        .filter((group) => group.status === 'Active')
        .sort((left, right) => left.name.localeCompare(right.name))
        .map((group) => ({ label: `${group.name} (${group.code})`, value: group.id, description: group.assignmentMode })),
    ]
    const userOptions = users
      .filter((user) => user.status === 'Active')
      .sort((left, right) => (left.displayName || left.userName).localeCompare(right.displayName || right.userName))
      .map((user) => ({ label: user.displayName || user.userName, value: user.id, description: user.email || user.userName }))
    const enrichedRoleOptions = roleOptions.map((role) => ({ ...role, description: 'Workflow role' }))
    const assignmentSelectionCount = assignmentType === 'User'
      ? getNormalizedSelectionValues(step.properties.assignedToUserIds, step.properties.assignedToUserId).length
      : assignmentType === 'Group'
        ? getNormalizedSelectionValues(step.properties.assignedToGroupIds, step.properties.assignedToGroupId).length
        : getNormalizedSelectionValues(step.properties.assignedToRoleIds, step.properties.assignedToRoleId).length
    const assignmentIntro = document.createElement('div')
    assignmentIntro.className = 'designer-assignment-card'
    assignmentIntro.append(
      createAssignmentStat('Directory source', assignmentType === 'User' ? 'Local users' : assignmentType === 'Group' ? 'Local groups' : 'Local roles'),
      createAssignmentStat('Selections', assignmentSelectionCount === 0 ? 'None selected' : `${assignmentSelectionCount} mapped`),
      createAssignmentStat('Runtime primary', assignmentSelectionCount === 0 ? 'Pending' : 'First selected entry'),
    )
    const assignmentSection = createEditorSection('Assignment')
    assignmentSection.append(assignmentIntro)
    assignmentSection.append(createReadOnlyHint('Selections map directly to the local workflow user, group, and role tables. You can select multiple entries; the first selected item remains the primary runtime assignee.'))
    assignmentSection.append(createSelectInput(
      'Task mode',
      String(step.properties.taskMode ?? (String(step.properties.approvalMode ?? '').trim() || String(step.properties.formKey ?? '').trim() ? 'approval' : 'task')),
      isReadonly,
      [
        { label: 'Action task', value: 'task' },
        { label: 'Approval task', value: 'approval' },
      ],
      (value) => {
        step.properties.taskMode = value
        if (value !== 'approval') {
          step.properties.approvalMode = ''
        } else if (!String(step.properties.approvalMode ?? '').trim()) {
          step.properties.approvalMode = 'single'
        }
        context.notifyPropertiesChanged()
      },
    ))
    assignmentSection.append(createSelectInput(
      'Assignment type',
      assignmentType,
      isReadonly,
      [
        { label: 'User', value: 'User' },
        { label: 'User Group', value: 'Group' },
        { label: 'Role', value: 'Role' },
      ],
      (value) => {
        setUserTaskSelectionValues(step, value as 'User' | 'Group' | 'Role', [])
        context.notifyPropertiesChanged()
      },
    ))
    if (assignmentType === 'User') {
      assignmentSection.append(createMultiSelectPicker(
        'Mapped users',
        'Select one or more workflow users from the local user directory.',
        userOptions,
        getNormalizedSelectionValues(step.properties.assignedToUserIds, step.properties.assignedToUserId),
        isReadonly,
        (values) => {
          setUserTaskSelectionValues(step, 'User', values)
          context.notifyPropertiesChanged()
        },
      ))
    } else if (assignmentType === 'Group') {
      assignmentSection.append(createMultiSelectPicker(
        'Mapped groups',
        'Select one or more local groups for queue-based or claim-based routing.',
        groupOptions,
        getNormalizedSelectionValues(step.properties.assignedToGroupIds, step.properties.assignedToGroupId),
        isReadonly,
        (values) => {
          setUserTaskSelectionValues(step, 'Group', values)
          context.notifyPropertiesChanged()
        },
      ))
    } else {
      assignmentSection.append(createMultiSelectPicker(
        'Mapped roles',
        'Select one or more local workflow roles for role-based routing.',
        enrichedRoleOptions,
        getNormalizedSelectionValues(step.properties.assignedToRoleIds, step.properties.assignedToRoleId),
        isReadonly,
        (values) => {
          setUserTaskSelectionValues(step, 'Role', values)
          context.notifyPropertiesChanged()
        },
      ))
    }
    assignmentSection.append(createNumberInput('Due in hours', step.properties.dueInHours == null ? '' : String(step.properties.dueInHours), isReadonly, (value) => {
      step.properties.dueInHours = value == null ? null : value
      context.notifyPropertiesChanged()
    }))
    assignmentSection.append(createSelectInput(
      'SLA profile',
      step.properties.dueInHours == null ? '' : String(step.properties.dueInHours),
      isReadonly,
      slaProfileOptions,
      (value) => {
        step.properties.dueInHours = value ? Number(value) : null
        context.notifyPropertiesChanged()
      },
    ))
    if (String(step.properties.taskMode ?? '').trim() === 'approval' || String(step.properties.approvalMode ?? '').trim()) {
      assignmentSection.append(createSelectInput(
        'Approval mode',
        String(step.properties.approvalMode ?? 'single'),
        isReadonly,
        [
          { label: 'Single', value: 'single' },
          { label: 'Any', value: 'any' },
          { label: 'All', value: 'all' },
          { label: 'Sequential', value: 'sequential' },
        ],
        (value) => {
          step.properties.approvalMode = value
          context.notifyPropertiesChanged()
        },
      ))
      assignmentSection.append(createTextInput('Approval form key', String(step.properties.formKey ?? ''), isReadonly, (value) => {
        step.properties.formKey = value
        context.notifyPropertiesChanged()
      }))
    }
    root.append(assignmentSection)

    const dataSection = createEditorSection('Task Data')
    dataSection.append(createTextArea('Input mapping', String(step.properties.inputMapping ?? ''), isReadonly, (value) => {
      step.properties.inputMapping = value
      context.notifyPropertiesChanged()
    }))
    dataSection.append(createTokenButtons('Input tokens', workflowTokenPresets, isReadonly, (value) => {
      step.properties.inputMapping = appendToken(step.properties.inputMapping, value)
      context.notifyPropertiesChanged()
    }))
    dataSection.append(createTextArea('Output mapping', String(step.properties.outputMapping ?? ''), isReadonly, (value) => {
      step.properties.outputMapping = value
      context.notifyPropertiesChanged()
    }))
    dataSection.append(createReadOnlyHint('Keep mappings simple in v1. Use workflow variables and explicit approve/reject outcomes for downstream logic.'))
    root.append(dataSection)
  }

  if (step.type === 'condition') {
    const ruleSection = createEditorSection('Rule')
    ruleSection.append(createTextArea('Expression', String(step.properties.expression ?? ''), isReadonly, (value) => {
      step.properties.expression = value
      context.notifyPropertiesChanged()
    }))
    ruleSection.append(createPresetButtons('Quick templates', [
      { label: 'Amount threshold', value: 'amount > 5000' },
      { label: 'Requester department', value: "requester.department == 'Finance'" },
      { label: 'Priority check', value: "request.priority == 'High'" },
    ], isReadonly, (value) => {
      step.properties.expression = value
      context.notifyPropertiesChanged()
    }))
    ruleSection.append(createTokenButtons('Expression tokens', workflowTokenPresets, isReadonly, (value) => {
      step.properties.expression = appendToken(step.properties.expression, value)
      context.notifyPropertiesChanged()
    }))
    root.append(ruleSection)
  }

  if (step.type === 'timer' || step.type === 'delay') {
    const timingSection = createEditorSection('Timing')
    timingSection.append(createSelectInput(
      'Wait type',
      String(step.properties.waitType ?? step.properties.timerType ?? 'duration'),
      isReadonly,
      [
        { label: 'Duration', value: 'duration' },
        { label: 'Expression', value: 'expression' },
      ],
      (value) => {
        step.properties.waitType = value
        context.notifyPropertiesChanged()
      },
    ))
    timingSection.append(createNumberInput('Duration hours', step.properties.dueInHours == null ? '' : String(step.properties.dueInHours), isReadonly, (value) => {
      step.properties.dueInHours = value == null ? null : value
      context.notifyPropertiesChanged()
    }))
    timingSection.append(createTextArea('Wait expression', String(step.properties.waitExpression ?? step.properties.timerExpression ?? ''), isReadonly, (value) => {
      step.properties.waitExpression = value
      context.notifyPropertiesChanged()
    }))
    timingSection.append(createPresetButtons('Wait templates', [
      { label: 'Next business day', value: 'nextBusinessDay()' },
      { label: 'Month end', value: 'endOfMonth()' },
      { label: 'After approval SLA', value: 'task.completedAt + 4h' },
    ], isReadonly, (value) => {
      step.properties.waitExpression = value
      context.notifyPropertiesChanged()
    }))
    timingSection.append(createTokenButtons('Wait tokens', workflowTokenPresets, isReadonly, (value) => {
      step.properties.waitExpression = appendToken(step.properties.waitExpression, value)
      context.notifyPropertiesChanged()
    }))
    timingSection.append(createCheckboxInput('Use business calendar', Boolean(step.properties.businessCalendar ?? false), isReadonly, (value) => {
      step.properties.businessCalendar = value
      context.notifyPropertiesChanged()
    }))
    root.append(timingSection)
  }

  if (step.type === 'serviceTask') {
    const integrationSection = createEditorSection('Integration')
    const processMode = String(
      step.properties.processMode
        ?? (String(step.properties.externalApiEndpointId ?? '').trim() ? 'externalApi' : String(step.properties.targetVariable ?? '').trim() || String(step.properties.valueExpression ?? '').trim() ? 'dataUpdate' : 'service'),
    )
    integrationSection.append(createSelectInput(
      'Process mode',
      processMode,
      isReadonly,
      [
        { label: 'System process', value: 'service' },
        { label: 'External API', value: 'externalApi' },
        { label: 'Data update', value: 'dataUpdate' },
      ],
      (value) => {
        step.properties.processMode = value
        context.notifyPropertiesChanged()
      },
    ))
    if (processMode === 'externalApi') {
      integrationSection.append(createSelectInput(
        'API setup',
        String(step.properties.externalApiEndpointId ?? ''),
        isReadonly,
        [
          { label: 'Select API setup', value: '' },
          ...externalApiEndpoints
            .filter((item) => item.status === 'Active')
            .sort((left, right) => left.name.localeCompare(right.name))
            .map((item) => ({ label: `${item.name} (${item.httpMethod})`, value: item.id })),
        ],
        (value) => {
          step.properties.externalApiEndpointId = value
          context.notifyPropertiesChanged()
        },
      ))
    } else {
      integrationSection.append(createTextInput('Process key', String(step.properties.processKey ?? step.properties.serviceKey ?? ''), isReadonly, (value) => {
        step.properties.processKey = value
        context.notifyPropertiesChanged()
      }))
    }
    integrationSection.append(createSelectInput(
      'Retry policy',
      String(step.properties.retryPolicy ?? 'none'),
      isReadonly,
      [
        { label: 'None', value: 'none' },
        { label: 'Immediate', value: 'immediate' },
        { label: 'Exponential', value: 'exponential' },
      ],
      (value) => {
        step.properties.retryPolicy = value
        context.notifyPropertiesChanged()
      },
    ))
    integrationSection.append(createNumberInput('Timeout hours', step.properties.timeoutHours == null ? '' : String(step.properties.timeoutHours), isReadonly, (value) => {
      step.properties.timeoutHours = value == null ? null : value
      context.notifyPropertiesChanged()
    }))
    integrationSection.append(createTextArea(processMode === 'externalApi' ? 'Request mapping' : 'Input mapping', String(step.properties.inputMapping ?? ''), isReadonly, (value) => {
      step.properties.inputMapping = value
      context.notifyPropertiesChanged()
    }))
    integrationSection.append(createPresetButtons(processMode === 'externalApi' ? 'Request templates' : 'Input mapping templates', [
      { label: 'Payload object', value: '{ requestId: workflow.id, amount: variables.amount }' },
      { label: 'Task context', value: '{ taskId: task.id, actor: currentUser.id }' },
    ], isReadonly, (value) => {
      step.properties.inputMapping = value
      context.notifyPropertiesChanged()
    }))
    integrationSection.append(createTokenButtons(processMode === 'externalApi' ? 'Request tokens' : 'Mapping tokens', workflowTokenPresets, isReadonly, (value) => {
      step.properties.inputMapping = appendToken(step.properties.inputMapping, value)
      context.notifyPropertiesChanged()
    }))
    integrationSection.append(createTextArea(processMode === 'externalApi' ? 'Response mapping' : 'Output mapping', String(step.properties.outputMapping ?? ''), isReadonly, (value) => {
      step.properties.outputMapping = value
      context.notifyPropertiesChanged()
    }))
    integrationSection.append(createTextInput('Error handling path', String(step.properties.errorHandlingPath ?? ''), isReadonly, (value) => {
      step.properties.errorHandlingPath = value
      context.notifyPropertiesChanged()
    }))
    if (processMode === 'dataUpdate') {
      integrationSection.append(createTextInput('Target variable', String(step.properties.targetVariable ?? ''), isReadonly, (value) => {
        step.properties.targetVariable = value
        context.notifyPropertiesChanged()
      }))
      integrationSection.append(createSelectInput(
        'Operation',
        String(step.properties.operation ?? 'set'),
        isReadonly,
        [
          { label: 'Set', value: 'set' },
          { label: 'Add', value: 'add' },
          { label: 'Remove', value: 'remove' },
          { label: 'Map', value: 'map' },
        ],
        (value) => {
          step.properties.operation = value
          context.notifyPropertiesChanged()
        },
      ))
      integrationSection.append(createTextArea('Value expression', String(step.properties.valueExpression ?? ''), isReadonly, (value) => {
        step.properties.valueExpression = value
        context.notifyPropertiesChanged()
      }))
      integrationSection.append(createPresetButtons('Update templates', [
        { label: 'Set approved', value: "'Approved'" },
        { label: 'Stamp now', value: 'utcNow()' },
        { label: 'Copy requester', value: 'requester.id' },
      ], isReadonly, (value) => {
        step.properties.valueExpression = value
        context.notifyPropertiesChanged()
      }))
      integrationSection.append(createTokenButtons('Value tokens', workflowTokenPresets, isReadonly, (value) => {
        step.properties.valueExpression = appendToken(step.properties.valueExpression, value)
        context.notifyPropertiesChanged()
      }))
    }
    root.append(integrationSection)
  }

  if (step.type === 'subflow') {
    const subflowSection = createEditorSection('Subflow')
    subflowSection.append(createTextInput('Child workflow key', String(step.properties.childWorkflowKey ?? ''), isReadonly, (value) => {
      step.properties.childWorkflowKey = value
      context.notifyPropertiesChanged()
    }))
    subflowSection.append(createSelectInput(
      'Version mode',
      String(step.properties.versionMode ?? 'latest'),
      isReadonly,
      [
        { label: 'Latest', value: 'latest' },
        { label: 'Pinned', value: 'pinned' },
      ],
      (value) => {
        step.properties.versionMode = value
        context.notifyPropertiesChanged()
      },
    ))
    subflowSection.append(createCheckboxInput('Wait for completion', Boolean(step.properties.waitForCompletion ?? true), isReadonly, (value) => {
      step.properties.waitForCompletion = value
      context.notifyPropertiesChanged()
    }))
    subflowSection.append(createTextArea('Input mapping', String(step.properties.inputMapping ?? ''), isReadonly, (value) => {
      step.properties.inputMapping = value
      context.notifyPropertiesChanged()
    }))
    subflowSection.append(createTokenButtons('Subflow tokens', workflowTokenPresets, isReadonly, (value) => {
      step.properties.inputMapping = appendToken(step.properties.inputMapping, value)
      context.notifyPropertiesChanged()
    }))
    subflowSection.append(createTextArea('Output mapping', String(step.properties.outputMapping ?? ''), isReadonly, (value) => {
      step.properties.outputMapping = value
      context.notifyPropertiesChanged()
    }))
    root.append(subflowSection)
  }

  if (step.type === 'escalation') {
    const escalationSection = createEditorSection('Escalation')
    escalationSection.append(createTextInput('Trigger condition', String(step.properties.triggerCondition ?? ''), isReadonly, (value) => {
      step.properties.triggerCondition = value
      context.notifyPropertiesChanged()
    }))
    escalationSection.append(createTextInput('Escalation target', String(step.properties.escalationTarget ?? ''), isReadonly, (value) => {
      step.properties.escalationTarget = value
      context.notifyPropertiesChanged()
    }))
    escalationSection.append(createTextArea('Escalation message', String(step.properties.escalationMessage ?? ''), isReadonly, (value) => {
      step.properties.escalationMessage = value
      context.notifyPropertiesChanged()
    }))
    root.append(escalationSection)
  }

  if (step.type === 'notification') {
    const notificationSection = createEditorSection('Delivery')
    notificationSection.append(createTextInput('Notification key', String(step.properties.notificationKey ?? ''), isReadonly, (value) => {
      step.properties.notificationKey = value
      context.notifyPropertiesChanged()
    }))
    notificationSection.append(createSelectInput(
      'Channel',
      String(step.properties.channel ?? 'inApp'),
      isReadonly,
      [
        { label: 'In-app', value: 'inApp' },
        { label: 'Email', value: 'email' },
        { label: 'Teams', value: 'teams' },
      ],
      (value) => {
        step.properties.channel = value
        context.notifyPropertiesChanged()
      },
    ))
    notificationSection.append(createTextInput('Recipient source', String(step.properties.recipientSource ?? 'workflowInitiator'), isReadonly, (value) => {
      step.properties.recipientSource = value
      context.notifyPropertiesChanged()
    }))
    notificationSection.append(createTextInput('Template key', String(step.properties.templateKey ?? ''), isReadonly, (value) => {
      step.properties.templateKey = value
      context.notifyPropertiesChanged()
    }))
    root.append(notificationSection)
  }

  if (step.type === 'parallelSplit') {
    const parallelSection = createEditorSection('Parallel split')
    parallelSection.append(createNumberInput('Branch count', step.properties.branchCount == null ? '2' : String(step.properties.branchCount), isReadonly, (value) => {
      const nextCount = Math.max(2, value == null ? 2 : value)
      step.properties.branchCount = nextCount
      syncParallelBranches(step as BranchedStepLike, nextCount)
      context.notifyPropertiesChanged()
      context.notifyChildrenChanged()
    }))
    parallelSection.append(createReadOnlyHint(`Active branches: ${getParallelBranchNamesFromStep(step as BranchedStepLike).map(formatParallelBranchName).join(', ')}`))
    root.append(parallelSection)
  }

  if (step.type === 'parallelJoin') {
    const joinSection = createEditorSection('Parallel join')
    joinSection.append(createSelectInput(
      'Join mode',
      String(step.properties.joinMode ?? 'all'),
      isReadonly,
      [
        { label: 'Wait all', value: 'all' },
        { label: 'Wait any', value: 'any' },
        { label: 'Threshold', value: 'threshold' },
      ],
      (value) => {
        step.properties.joinMode = value
        context.notifyPropertiesChanged()
      },
    ))
    joinSection.append(createReadOnlyHint('Place this node after parallel branches converge back into a single flow.'))
    root.append(joinSection)
  }

  if (step.type === 'exceptionHandler') {
    const exceptionSection = createEditorSection('Recovery')
    exceptionSection.append(createTextInput('Error filter', String(step.properties.errorFilter ?? ''), isReadonly, (value) => {
      step.properties.errorFilter = value
      context.notifyPropertiesChanged()
    }))
    exceptionSection.append(createSelectInput(
      'Recovery action',
      String(step.properties.recoveryAction ?? 'notify'),
      isReadonly,
      [
        { label: 'Notify', value: 'notify' },
        { label: 'Retry', value: 'retry' },
        { label: 'Fallback process', value: 'fallback' },
      ],
      (value) => {
        step.properties.recoveryAction = value
        context.notifyPropertiesChanged()
      },
    ))
    exceptionSection.append(createTextInput('Fallback process key', String(step.properties.fallbackProcessKey ?? ''), isReadonly, (value) => {
      step.properties.fallbackProcessKey = value
      context.notifyPropertiesChanged()
    }))
    root.append(exceptionSection)
  }

  return root
}

function getEditorTitle(step: Step) {
  switch (step.type) {
    case 'condition':
      return 'Decision'
    case 'userTask':
      return isApprovalTask(step) ? 'Approval Task' : 'Action Task'
    default:
      return 'Workflow Step'
  }
}

function getEditorCaption(step: Step) {
  switch (step.type) {
    case 'condition':
      return 'Configure branching logic and outcomes.'
    case 'userTask':
      return isApprovalTask(step)
        ? 'Route approval to one user, group, or role and capture the decision.'
        : 'Route work to one user, group, or role for action.'
    default:
      return 'Configure workflow step properties.'
  }
}

function createEditorTitle(value: string, caption: string) {
  const header = document.createElement('div')
  header.className = 'designer-editor-header'

  const title = document.createElement('strong')
  title.textContent = value

  const subtitle = document.createElement('small')
  subtitle.textContent = caption

  header.append(title, subtitle)
  return header
}

function createEditorSection(title: string) {
  const section = document.createElement('section')
  section.className = 'designer-editor-section'

  const heading = document.createElement('h4')
  heading.textContent = title

  section.append(heading)
  return section
}

function createFieldShell(label: string) {
  const wrapper = document.createElement('label')
  wrapper.className = 'designer-editor-field'

  const caption = document.createElement('span')
  caption.textContent = label

  wrapper.append(caption)
  return wrapper
}

function createTextInput(label: string, value: string, readonly: boolean, onInput: (value: string) => void) {
  const wrapper = createFieldShell(label)
  const input = document.createElement('input')
  input.type = 'text'
  input.value = value
  input.disabled = readonly
  input.addEventListener('input', () => onInput(input.value))

  wrapper.append(input)
  return wrapper
}

function createTextArea(label: string, value: string, readonly: boolean, onInput: (value: string) => void) {
  const wrapper = createFieldShell(label)
  const input = document.createElement('textarea')
  input.value = value
  input.disabled = readonly
  input.rows = 3
  input.addEventListener('input', () => onInput(input.value))

  wrapper.append(input)
  return wrapper
}

function createNumberInput(label: string, value: string, readonly: boolean, onInput: (value: number | null) => void) {
  const wrapper = createFieldShell(label)
  const input = document.createElement('input')
  input.type = 'number'
  input.min = '0'
  input.step = '1'
  input.value = value
  input.disabled = readonly
  input.addEventListener('input', () => {
    const nextValue = input.value.trim()
    const parsed = Number(nextValue)
    onInput(nextValue && Number.isFinite(parsed) ? parsed : null)
  })

  wrapper.append(input)
  return wrapper
}

function createSelectInput(
  label: string,
  value: string,
  readonly: boolean,
  options: Array<{ label: string; value: string }>,
  onInput: (value: string) => void,
) {
  const wrapper = createFieldShell(label)
  const input = document.createElement('select')
  input.disabled = readonly

  options.forEach((option) => {
    const optionElement = document.createElement('option')
    optionElement.value = option.value
    optionElement.textContent = option.label
    input.append(optionElement)
  })

  input.value = value
  input.addEventListener('input', () => onInput(input.value))

  wrapper.append(input)
  return wrapper
}

function createCheckboxInput(label: string, value: boolean, readonly: boolean, onInput: (value: boolean) => void) {
  const wrapper = createFieldShell(label)
  const input = document.createElement('input')
  input.type = 'checkbox'
  input.checked = value
  input.disabled = readonly
  input.className = 'designer-editor-checkbox'
  input.addEventListener('change', () => onInput(input.checked))

  wrapper.append(input)
  return wrapper
}

function createReadOnlyHint(value: string) {
  const hint = document.createElement('div')
  hint.className = 'designer-editor-hint'
  hint.textContent = value
  return hint
}

function createSummaryBadges(step: Step, externalApiEndpoints: ExternalApiEndpoint[], roles: Role[], userGroups: UserGroup[], users: User[]) {
  const items = getStepSummaryItems(step, externalApiEndpoints, roles, userGroups, users)
  const wrapper = document.createElement('div')
  wrapper.className = 'designer-editor-summary'

  items.forEach((item) => {
    const badge = document.createElement('span')
    badge.className = 'designer-editor-summary-badge'
    badge.textContent = item
    wrapper.append(badge)
  })

  return wrapper
}

function createAssignmentStat(label: string, value: string) {
  const item = document.createElement('div')
  item.className = 'designer-assignment-stat'

  const caption = document.createElement('span')
  caption.textContent = label

  const strong = document.createElement('strong')
  strong.textContent = value

  item.append(caption, strong)
  return item
}

function createMultiSelectPicker(
  label: string,
  description: string,
  options: Array<{ label: string; value: string; description?: string }>,
  selectedValues: string[],
  readonly: boolean,
  onChange: (values: string[]) => void,
) {
  const wrapper = document.createElement('div')
  wrapper.className = 'designer-multi-select'

  const heading = document.createElement('div')
  heading.className = 'designer-multi-select-header'

  const title = document.createElement('strong')
  title.textContent = label

  const caption = document.createElement('span')
  caption.textContent = `${description} ${selectedValues.length > 0 ? `${selectedValues.length} selected.` : 'No selections yet.'}`

  heading.append(title, caption)
  wrapper.append(heading)

  const list = document.createElement('div')
  list.className = 'designer-multi-select-list'

  options.forEach((option) => {
    const card = document.createElement('label')
    card.className = 'designer-multi-select-option'

    const checkbox = document.createElement('input')
    checkbox.type = 'checkbox'
    checkbox.checked = selectedValues.includes(option.value)
    checkbox.disabled = readonly
    checkbox.addEventListener('change', () => {
      const next = checkbox.checked
        ? [...selectedValues, option.value]
        : selectedValues.filter((value) => value !== option.value)
      onChange(Array.from(new Set(next)))
    })

    const body = document.createElement('div')
    body.className = 'designer-multi-select-option-copy'

    const optionTitle = document.createElement('strong')
    optionTitle.textContent = option.label

    const optionCaption = document.createElement('span')
    optionCaption.textContent = option.description ?? 'Mapped from local workflow directory'

    body.append(optionTitle, optionCaption)
    card.append(checkbox, body)
    list.append(card)
  })

  wrapper.append(list)
  return wrapper
}

function createPresetButtons(
  title: string,
  items: Array<{ label: string; value: string }>,
  readonly: boolean,
  onSelect: (value: string) => void,
) {
  const wrapper = document.createElement('div')
  wrapper.className = 'designer-editor-presets'

  const caption = document.createElement('span')
  caption.className = 'designer-editor-presets-label'
  caption.textContent = title
  wrapper.append(caption)

  const list = document.createElement('div')
  list.className = 'designer-editor-presets-list'

  items.forEach((item) => {
    const button = document.createElement('button')
    button.type = 'button'
    button.className = 'designer-editor-preset-button'
    button.textContent = item.label
    button.disabled = readonly
    button.addEventListener('click', () => onSelect(item.value))
    list.append(button)
  })

  wrapper.append(list)
  return wrapper
}

function createCollapsibleAdvancedSection(title: string, children: HTMLElement[]) {
  const wrapper = document.createElement('details')
  wrapper.className = 'designer-editor-advanced'

  const summary = document.createElement('summary')
  summary.textContent = title
  wrapper.append(summary)

  const content = document.createElement('div')
  content.className = 'designer-editor-advanced-content'
  children.forEach((child) => content.append(child))
  wrapper.append(content)

  return wrapper
}

function createTaskContractCollectionEditor(
  title: string,
  rawValue: string,
  readonly: boolean,
  mode: 'input' | 'output',
  onChange: (value: string) => void,
) {
  const wrapper = document.createElement('div')
  wrapper.className = 'designer-editor-collection'

  const header = document.createElement('div')
  header.className = 'designer-editor-collection-header'

  const heading = document.createElement('strong')
  heading.textContent = title

  const caption = document.createElement('span')
  caption.textContent = mode === 'input'
    ? 'Define what this task receives from workflow state.'
    : 'Define what an external client can submit when closing this task.'

  const addButton = document.createElement('button')
  addButton.type = 'button'
  addButton.className = 'designer-editor-preset-button'
  addButton.textContent = mode === 'input' ? 'Add input' : 'Add output'
  addButton.disabled = readonly

  header.append(heading, caption, addButton)
  wrapper.append(header)

  const rowsHost = document.createElement('div')
  rowsHost.className = 'designer-editor-collection-rows'
  wrapper.append(rowsHost)

  const rows = parseContractRows(rawValue, mode)

  function sync() {
    const serialized = rows.length > 0 ? JSON.stringify(rows, null, 2) : ''
    onChange(serialized)
  }

  function render() {
    rowsHost.replaceChildren()

    if (rows.length === 0) {
      rowsHost.append(createReadOnlyHint(mode === 'input'
        ? 'No input parameters defined yet.'
        : 'No output parameters defined yet.'))
      return
    }

    rows.forEach((row, index) => {
      const rowElement = document.createElement('div')
      rowElement.className = 'designer-editor-contract-row'

      rowElement.append(createInlineTextField('Key', row.key ?? '', readonly, (value) => {
        row.key = value
        sync()
      }))
      rowElement.append(createInlineTextField('Label', row.label ?? '', readonly, (value) => {
        row.label = value
        sync()
      }))
      rowElement.append(createInlineSelectField('Type', row.type ?? 'string', readonly, [
        { label: 'String', value: 'string' },
        { label: 'Number', value: 'number' },
        { label: 'Boolean', value: 'boolean' },
        { label: 'Date', value: 'date' },
        { label: 'JSON', value: 'json' },
      ], (value) => {
        row.type = value
        sync()
      }))
      rowElement.append(createInlineCheckboxField('Required', Boolean(row.required), readonly, (value) => {
        row.required = value
        sync()
      }))

      if (mode === 'input') {
        const inputRow = row as TaskContractInputRow
        rowElement.append(createInlineTextField('Source', inputRow.source ?? '', readonly, (value) => {
          inputRow.source = value
          sync()
        }))
      } else {
        const outputRow = row as TaskContractOutputRow
        rowElement.append(createInlineTextField('Required on actions', Array.isArray(outputRow.requiredOnActions) ? outputRow.requiredOnActions.join(', ') : '', readonly, (value) => {
          outputRow.requiredOnActions = value
            .split(',')
            .map((item) => item.trim())
            .filter(Boolean)
          sync()
        }))
      }

      const removeButton = document.createElement('button')
      removeButton.type = 'button'
      removeButton.className = 'designer-editor-row-remove'
      removeButton.textContent = 'Remove'
      removeButton.disabled = readonly
      removeButton.addEventListener('click', () => {
        rows.splice(index, 1)
        sync()
        render()
      })
      rowElement.append(removeButton)

      rowsHost.append(rowElement)
    })
  }

  addButton.addEventListener('click', () => {
    rows.push(mode === 'input'
      ? { key: '', label: '', type: 'string', required: false, source: '' }
      : { key: '', label: '', type: 'string', required: false, requiredOnActions: [] })
    sync()
    render()
  })

  render()
  return wrapper
}

function createTaskOutputStorageEditor(
  title: string,
  rawValue: string,
  readonly: boolean,
  onChange: (value: string) => void,
) {
  const wrapper = document.createElement('div')
  wrapper.className = 'designer-editor-collection'

  const header = document.createElement('div')
  header.className = 'designer-editor-collection-header'

  const heading = document.createElement('strong')
  heading.textContent = title

  const caption = document.createElement('span')
  caption.textContent = 'Map each output key to a workflow variable path.'

  const addButton = document.createElement('button')
  addButton.type = 'button'
  addButton.className = 'designer-editor-preset-button'
  addButton.textContent = 'Add storage rule'
  addButton.disabled = readonly

  header.append(heading, caption, addButton)
  wrapper.append(header)

  const rowsHost = document.createElement('div')
  rowsHost.className = 'designer-editor-collection-rows'
  wrapper.append(rowsHost)

  const rows = parseStorageRows(rawValue)

  function sync() {
    const output = Object.fromEntries(
      rows
        .filter((row) => row.key.trim() && row.target.trim())
        .map((row) => [row.key.trim(), row.target.trim()]),
    )
    onChange(Object.keys(output).length > 0 ? JSON.stringify(output, null, 2) : '')
  }

  function render() {
    rowsHost.replaceChildren()

    if (rows.length === 0) {
      rowsHost.append(createReadOnlyHint('No output storage rules defined. The runtime will fall back to task.{nodeId}.outputs.{key}.'))
      return
    }

    rows.forEach((row, index) => {
      const rowElement = document.createElement('div')
      rowElement.className = 'designer-editor-contract-row designer-editor-contract-row--storage'

      rowElement.append(createInlineTextField('Output key', row.key, readonly, (value) => {
        row.key = value
        sync()
      }))
      rowElement.append(createInlineTextField('Store to variable', row.target, readonly, (value) => {
        row.target = value
        sync()
      }))

      const removeButton = document.createElement('button')
      removeButton.type = 'button'
      removeButton.className = 'designer-editor-row-remove'
      removeButton.textContent = 'Remove'
      removeButton.disabled = readonly
      removeButton.addEventListener('click', () => {
        rows.splice(index, 1)
        sync()
        render()
      })
      rowElement.append(removeButton)

      rowsHost.append(rowElement)
    })
  }

  addButton.addEventListener('click', () => {
    rows.push({ key: '', target: '' })
    sync()
    render()
  })

  render()
  return wrapper
}

function createInlineTextField(label: string, value: string, readonly: boolean, onInput: (value: string) => void) {
  const wrapper = document.createElement('label')
  wrapper.className = 'designer-editor-inline-field'

  const caption = document.createElement('span')
  caption.textContent = label

  const input = document.createElement('input')
  input.type = 'text'
  input.value = value
  input.disabled = readonly
  input.addEventListener('input', () => onInput(input.value))

  wrapper.append(caption, input)
  return wrapper
}

function createInlineSelectField(
  label: string,
  value: string,
  readonly: boolean,
  options: Array<{ label: string; value: string }>,
  onInput: (value: string) => void,
) {
  const wrapper = document.createElement('label')
  wrapper.className = 'designer-editor-inline-field'

  const caption = document.createElement('span')
  caption.textContent = label

  const input = document.createElement('select')
  input.disabled = readonly
  options.forEach((option) => {
    const optionElement = document.createElement('option')
    optionElement.value = option.value
    optionElement.textContent = option.label
    input.append(optionElement)
  })
  input.value = value
  input.addEventListener('input', () => onInput(input.value))

  wrapper.append(caption, input)
  return wrapper
}

function createInlineCheckboxField(label: string, value: boolean, readonly: boolean, onInput: (value: boolean) => void) {
  const wrapper = document.createElement('label')
  wrapper.className = 'designer-editor-inline-field designer-editor-inline-field--checkbox'

  const caption = document.createElement('span')
  caption.textContent = label

  const input = document.createElement('input')
  input.type = 'checkbox'
  input.checked = value
  input.disabled = readonly
  input.addEventListener('change', () => onInput(input.checked))

  wrapper.append(caption, input)
  return wrapper
}

function createTokenButtons(
  title: string,
  items: Array<{ label: string; value: string }>,
  readonly: boolean,
  onSelect: (value: string) => void,
) {
  return createPresetButtons(title, items, readonly, onSelect)
}

function appendToken(current: unknown, token: string) {
  const text = String(current ?? '').trim()
  return text ? `${text} ${token}` : token
}

function parseContractRows(rawValue: string, mode: 'input' | 'output'): TaskContractRow[] {
  try {
    const parsed = JSON.parse(rawValue || '[]')
    if (!Array.isArray(parsed)) {
      return []
    }

    return parsed.map((item) => ({
      key: String(item?.key ?? ''),
      label: String(item?.label ?? ''),
      type: String(item?.type ?? 'string'),
      required: Boolean(item?.required ?? false),
      ...(mode === 'input'
        ? { source: String(item?.source ?? '') }
        : {
            requiredOnActions: Array.isArray(item?.requiredOnActions)
              ? item.requiredOnActions.map((entry: unknown) => String(entry))
              : [],
          }),
    }))
  } catch {
    return []
  }
}

type TaskContractBaseRow = {
  key: string
  label: string
  type: string
  required: boolean
}

type TaskContractInputRow = TaskContractBaseRow & {
  source: string
}

type TaskContractOutputRow = TaskContractBaseRow & {
  requiredOnActions: string[]
}

type TaskContractRow = TaskContractInputRow | TaskContractOutputRow

function parseStorageRows(rawValue: string) {
  try {
    const parsed = JSON.parse(rawValue || '{}')
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
      return []
    }

    return Object.entries(parsed).map(([key, target]) => ({
      key,
      target: String(target ?? ''),
    }))
  } catch {
    return []
  }
}

void [createCollapsibleAdvancedSection, createTaskContractCollectionEditor, createTaskOutputStorageEditor]

function syncParallelBranches(step: BranchedStepLike, branchCount: number) {
  const nextBranches: Record<string, Array<Step>> = {}
  const currentBranches = step.branches ?? {}

  for (let index = 1; index <= branchCount; index += 1) {
    const branchName = `branch${index}`
    nextBranches[branchName] = currentBranches[branchName] ?? []
  }

  step.branches = nextBranches
}

function getParallelBranchNamesFromStep(step: BranchedStepLike) {
  return Object.keys(step.branches ?? {})
}

function formatParallelBranchName(branchName: string) {
  const match = /^branch(\d+)$/i.exec(branchName)
  return match ? `Branch ${match[1]}` : branchName
}

function getNormalizedSelectionValues(values: unknown, fallback?: unknown) {
  const list = Array.isArray(values)
    ? values
    : typeof values === 'string'
      ? values.split(',')
      : []

  const normalized = list
    .map((item) => String(item ?? '').trim())
    .filter(Boolean)

  const fallbackValue = String(fallback ?? '').trim()
  if (fallbackValue && !normalized.includes(fallbackValue)) {
    normalized.unshift(fallbackValue)
  }

  return Array.from(new Set(normalized))
}

function getUserTaskAssignmentType(step: Step) {
  if (getNormalizedSelectionValues(step.properties.assignedToUserIds, step.properties.assignedToUserId).length > 0) {
    return 'User'
  }

  if (getNormalizedSelectionValues(step.properties.assignedToGroupIds, step.properties.assignedToGroupId).length > 0) {
    return 'Group'
  }

  if (getNormalizedSelectionValues(step.properties.assignedToRoleIds, step.properties.assignedToRoleId).length > 0) {
    return 'Role'
  }

  return String(step.properties.assignmentType ?? 'User') === 'Group'
    ? 'Group'
    : String(step.properties.assignmentType ?? 'User') === 'Role'
      ? 'Role'
      : 'User'
}

function setUserTaskSelectionValues(step: Step, assignmentType: 'User' | 'Group' | 'Role', values: string[]) {
  const normalized = getNormalizedSelectionValues(values)

  step.properties.assignmentType = assignmentType
  step.properties.assignedToUserIds = assignmentType === 'User' ? normalized : []
  step.properties.assignedToGroupIds = assignmentType === 'Group' ? normalized : []
  step.properties.assignedToRoleIds = assignmentType === 'Role' ? normalized : []
  step.properties.assignedToUserId = assignmentType === 'User' ? (normalized[0] ?? '') : ''
  step.properties.assignedToGroupId = assignmentType === 'Group' ? (normalized[0] ?? '') : ''
  step.properties.assignedToRoleId = assignmentType === 'Role' ? (normalized[0] ?? '') : ''
}

function summarizeMappedNames(
  prefix: string,
  ids: string[],
  resolveName: (id: string) => string,
) {
  const names = ids
    .map((id) => resolveName(id))
    .filter(Boolean)

  if (names.length === 0) {
    return ''
  }

  if (names.length === 1) {
    return `${prefix}: ${names[0]}`
  }

  const remaining = names.length - 2
  const lead = names.slice(0, 2).join(', ')
  return remaining > 0 ? `${prefix}: ${lead} +${remaining}` : `${prefix}: ${lead}`
}

function getStepSummaryItems(step: Step, externalApiEndpoints: ExternalApiEndpoint[], roles: Role[], userGroups: UserGroup[], users: User[]) {
  const items = [getEditorTitle(step)]

  if (step.type === 'userTask') {
    const userSummary = summarizeMappedNames(
      'Users',
      getNormalizedSelectionValues(step.properties.assignedToUserIds, step.properties.assignedToUserId),
      (id) => {
        const user = users.find((item) => item.id === id)
        return user ? user.displayName || user.userName : id
      },
    )
    const groupSummary = summarizeMappedNames(
      'Groups',
      getNormalizedSelectionValues(step.properties.assignedToGroupIds, step.properties.assignedToGroupId),
      (id) => userGroups.find((item) => item.id === id)?.name || id,
    )
    const roleSummary = summarizeMappedNames(
      'Roles',
      getNormalizedSelectionValues(step.properties.assignedToRoleIds, step.properties.assignedToRoleId),
      (id) => {
        const role = roles.find((item) => item.id === id)
        return role ? role.name : id
      },
    )

    const assignmentSummary = userSummary || groupSummary || roleSummary
    if (assignmentSummary) {
      items.push(assignmentSummary)
    }

    if (typeof step.properties.dueInHours === 'number') {
      items.push(`SLA: ${step.properties.dueInHours}h`)
    }

    if ((String(step.properties.taskMode ?? '').trim() === 'approval' || String(step.properties.approvalMode ?? '').trim()) && String(step.properties.approvalMode ?? '').trim()) {
      items.push(`Mode: ${String(step.properties.approvalMode)}`)
    }
  }

  if (step.type === 'serviceTask') {
    const processMode = String(
      step.properties.processMode
        ?? (String(step.properties.externalApiEndpointId ?? '').trim() ? 'externalApi' : String(step.properties.targetVariable ?? '').trim() || String(step.properties.valueExpression ?? '').trim() ? 'dataUpdate' : 'service'),
    )
    if (processMode === 'externalApi' && String(step.properties.externalApiEndpointId ?? '').trim()) {
      const endpointId = String(step.properties.externalApiEndpointId)
      const endpoint = externalApiEndpoints.find((item) => item.id === endpointId)
      items.push(`API: ${endpoint?.name || endpointId}`)
    } else if (processMode === 'dataUpdate' && String(step.properties.targetVariable ?? '').trim()) {
      items.push(`Update: ${String(step.properties.targetVariable)}`)
    } else if (String(step.properties.processKey ?? '').trim()) {
      items.push(`Process: ${String(step.properties.processKey)}`)
    }
  }

  if ((step.type === 'timer' || step.type === 'delay') && String(step.properties.waitType ?? '').trim()) {
    items.push(`Wait: ${String(step.properties.waitType)}`)
  }

  if (step.type === 'notification' && String(step.properties.channel ?? '').trim()) {
    items.push(`Channel: ${String(step.properties.channel)}`)
  }

  if (step.type === 'subflow' && String(step.properties.childWorkflowKey ?? '').trim()) {
    items.push(`Subflow: ${String(step.properties.childWorkflowKey)}`)
  }

  if (step.type === 'parallelSplit' && typeof step.properties.branchCount === 'number') {
    items.push(`Branches: ${step.properties.branchCount}`)
  }

  if (step.type === 'parallelJoin' && String(step.properties.joinMode ?? '').trim()) {
    items.push('Merge')
    items.push(`Join: ${String(step.properties.joinMode)}`)
  }

  return items.slice(0, 4)
}

function isApprovalTask(step: Step) {
  return step.type === 'userTask'
    && (String(step.properties.taskMode ?? '').trim() === 'approval'
      || String(step.properties.approvalMode ?? '').trim().length > 0)
}
