import { equal, ok, throws } from 'node:assert/strict'
import type { BranchedStep, Definition } from 'sequential-workflow-designer'
import { designerToRuntime, runtimeToDesigner, type RuntimeDefinition } from './workflowDefinitionMapper.js'

const userId = '11111111-1111-1111-1111-111111111111'

test('maps simple approval runtime JSON to designer and back', () => {
  const runtime: RuntimeDefinition = {
    nodes: [
      { id: 'start', type: 'start' },
      { id: 'approval', type: 'approval', name: 'Manager Approval', assignedToUserId: userId, dueInHours: 24 },
      { id: 'end', type: 'end' },
    ],
    edges: [
      { from: 'start', to: 'approval' },
      { from: 'approval', to: 'end' },
    ],
  }

  const designer = runtimeToDesigner(runtime)

  equal(designer.sequence.length, 1)
  equal(designer.sequence[0].componentType, 'task')
  equal(designer.sequence[0].properties.assignedToUserId, userId)
  assertRuntimeGraph(designerToRuntime(designer), runtime)
})

test('preserves multi-assignment mappings and keeps primary assignee compatibility', () => {
  const runtime: RuntimeDefinition = {
    nodes: [
      { id: 'start', type: 'start' },
      {
        id: 'review',
        type: 'userTask',
        name: 'Joint Review',
        assignmentType: 'User',
        assignedToUserId: userId,
        assignedToUserIds: [userId, '22222222-2222-2222-2222-222222222222'],
      },
      { id: 'end', type: 'end' },
    ],
    edges: [
      { from: 'start', to: 'review' },
      { from: 'review', to: 'end' },
    ],
  }

  const designer = runtimeToDesigner(runtime)
  equal(designer.sequence[0].properties.assignedToUserId, userId)
  ok(Array.isArray(designer.sequence[0].properties.assignedToUserIds))
  equal(designer.sequence[0].properties.assignedToUserIds.length, 2)

  const mapped = designerToRuntime(designer)
  equal(mapped.nodes.find((node) => node.id === 'review')?.assignedToUserId, userId)
  equal(mapped.nodes.find((node) => node.id === 'review')?.assignedToUserIds?.length, 2)
})

test('maps runtime condition branches to switch true and false sequences', () => {
  const runtime: RuntimeDefinition = {
    nodes: [
      { id: 'start', type: 'start' },
      { id: 'amountCheck', type: 'condition', name: 'Amount Check', expression: 'amount > 5000' },
      { id: 'approval', type: 'approval', name: 'Finance Approval', assignedToUserId: userId },
      { id: 'end', type: 'end' },
    ],
    edges: [
      { from: 'start', to: 'amountCheck' },
      { from: 'amountCheck', to: 'approval', outcome: 'true' },
      { from: 'amountCheck', to: 'end', outcome: 'false' },
      { from: 'approval', to: 'end' },
    ],
  }

  const switchStep = runtimeToDesigner(runtime).sequence[0] as BranchedStep

  equal(switchStep.componentType, 'switch')
  equal(switchStep.branches.true.length, 1)
  equal(switchStep.branches.true[0].id, 'approval')
  equal(switchStep.branches.false.length, 0)
})

test('preserves schema version and task-like automation nodes', () => {
  const runtime: RuntimeDefinition = {
    schemaVersion: 2,
    metadata: { name: 'Automation flow' },
    nodes: [
      { id: 'start', type: 'start' },
      { id: 'sync', type: 'serviceTask', name: 'Sync ERP', processKey: 'erp.sync', description: 'Sync with ERP' },
      { id: 'wait', type: 'timer', name: 'Wait', waitType: 'duration', dueInHours: 2 },
      { id: 'end', type: 'end' },
    ],
    edges: [
      { from: 'start', to: 'sync' },
      { from: 'sync', to: 'wait' },
      { from: 'wait', to: 'end' },
    ],
  }

  const designer = runtimeToDesigner(runtime)
  equal(designer.sequence.length, 2)
  equal(designer.sequence[0].type, 'serviceTask')
  equal(designer.sequence[1].type, 'timer')

  const mapped = designerToRuntime(designer)
  equal(mapped.schemaVersion, 2)
  ok(mapped.nodes.some((node) => node.type === 'serviceTask' && node.processKey === 'erp.sync'))
  ok(mapped.nodes.some((node) => node.type === 'timer' && node.dueInHours === 2))
})

test('supports legacy service and timer keys when loading runtime JSON', () => {
  const runtime: RuntimeDefinition = {
    nodes: [
      { id: 'start', type: 'start' },
      { id: 'sync', type: 'serviceTask', name: 'Sync ERP', serviceKey: 'erp.sync' },
      { id: 'wait', type: 'delay', name: 'Wait', timerType: 'expression', timerExpression: 'nextBusinessDay()' },
      { id: 'end', type: 'end' },
    ],
    edges: [
      { from: 'start', to: 'sync' },
      { from: 'sync', to: 'wait' },
      { from: 'wait', to: 'end' },
    ],
  }

  const designer = runtimeToDesigner(runtime)
  equal(designer.sequence[0].properties.processKey, 'erp.sync')
  equal(designer.sequence[1].properties.waitType, 'expression')
  equal(designer.sequence[1].properties.waitExpression, 'nextBusinessDay()')
})

test('preserves enterprise node properties through designer mapping', () => {
  const runtime: RuntimeDefinition = {
    nodes: [
      { id: 'start', type: 'start' },
      { id: 'subflow', type: 'subflow', name: 'Run onboarding', childWorkflowKey: 'hr.onboarding', versionMode: 'pinned', waitForCompletion: true },
      { id: 'update', type: 'dataUpdate', name: 'Set status', targetVariable: 'request.status', operation: 'set', valueExpression: 'Approved' },
      { id: 'end', type: 'end' },
    ],
    edges: [
      { from: 'start', to: 'subflow' },
      { from: 'subflow', to: 'update' },
      { from: 'update', to: 'end' },
    ],
  }

  const designer = runtimeToDesigner(runtime)
  equal(designer.sequence[0].properties.childWorkflowKey, 'hr.onboarding')
  equal(designer.sequence[1].properties.targetVariable, 'request.status')

  const mapped = designerToRuntime(designer)
  ok(mapped.nodes.some((node) => node.type === 'subflow' && node.childWorkflowKey === 'hr.onboarding'))
  ok(mapped.nodes.some((node) => node.type === 'dataUpdate' && node.operation === 'set'))
})

test('maps parallel split as a branched node with named branches', () => {
  const runtime: RuntimeDefinition = {
    nodes: [
      { id: 'start', type: 'start' },
      { id: 'split', type: 'parallelSplit', name: 'Run in parallel', branchCount: 2 },
      { id: 'taskA', type: 'userTask', name: 'Finance review', assignedToUserId: userId },
      { id: 'taskB', type: 'serviceTask', name: 'Sync downstream', processKey: 'erp.sync' },
      { id: 'join', type: 'parallelJoin', name: 'Join', joinMode: 'all' },
      { id: 'end', type: 'end' },
    ],
    edges: [
      { from: 'start', to: 'split' },
      { from: 'split', to: 'taskA', outcome: 'branch1' },
      { from: 'split', to: 'taskB', outcome: 'branch2' },
      { from: 'taskA', to: 'join' },
      { from: 'taskB', to: 'join' },
      { from: 'join', to: 'end' },
    ],
  }

  const designer = runtimeToDesigner(runtime)
  const split = designer.sequence[0] as BranchedStep
  equal(split.type, 'parallelSplit')
  equal(Object.keys(split.branches).length, 2)
  equal(split.branches.branch1[0].id, 'taskA')
  equal(split.branches.branch2[0].id, 'taskB')

  const mapped = designerToRuntime(designer)
  ok(mapped.edges.some((edge) => edge.from === 'split' && edge.to === 'taskA' && edge.outcome === 'branch1'))
  ok(mapped.edges.some((edge) => edge.from === 'split' && edge.to === 'taskB' && edge.outcome === 'branch2'))
})

test('preserves runtime metadata when designer updates workflow graph', () => {
  const runtime: RuntimeDefinition = {
    schemaVersion: 7,
    metadata: {
      name: 'Expense approval',
      description: 'Preserve runtime metadata',
      owner: 'workflow-admin',
      tags: ['finance', 'approval'],
    },
    nodes: [
      { id: 'start', type: 'start' },
      { id: 'approval', type: 'approval', name: 'Manager Approval', assignedToUserId: userId },
      { id: 'end', type: 'end' },
    ],
    edges: [
      { from: 'start', to: 'approval' },
      { from: 'approval', to: 'end' },
    ],
  }

  const designer = runtimeToDesigner(runtime)
  designer.sequence.push({
    id: 'notify',
    componentType: 'task',
    type: 'notification',
    name: 'Notify requester',
    properties: { notificationKey: 'notify.requester', description: '' },
  })

  const mapped = designerToRuntime(designer, runtime)
  equal(mapped.schemaVersion, 7)
  equal(mapped.metadata?.name, 'Expense approval')
  equal(mapped.metadata?.owner, 'workflow-admin')
  ok(mapped.metadata?.tags?.includes('finance'))
  ok(mapped.nodes.some((node) => node.id === 'notify' && node.type === 'notification'))
})

test('maps designer branch joins without adding invalid default condition edges', () => {
  const designer: Definition = {
    properties: {},
    sequence: [
      {
        id: 'amountCheck',
        componentType: 'switch',
        type: 'condition',
        name: 'Amount Check',
        properties: { expression: 'amount > 5000' },
        branches: {
          true: [
            {
              id: 'financeApproval',
              componentType: 'task',
              type: 'approval',
              name: 'Finance Approval',
              properties: { assignedToUserId: userId, assignedToRoleId: '', dueInHours: null },
            },
          ],
          false: [],
        },
      } as BranchedStep,
      {
        id: 'finalApproval',
        componentType: 'task',
        type: 'approval',
        name: 'Final Approval',
        properties: { assignedToUserId: userId, assignedToRoleId: '', dueInHours: null },
      },
    ],
  }

  const runtime = designerToRuntime(designer)

  ok(runtime.edges.some((edge) => edge.from === 'amountCheck' && edge.to === 'financeApproval' && edge.outcome === 'true'))
  ok(runtime.edges.some((edge) => edge.from === 'amountCheck' && edge.to === 'finalApproval' && edge.outcome === 'false'))
  ok(runtime.edges.some((edge) => edge.from === 'financeApproval' && edge.to === 'finalApproval'))
  ok(!runtime.edges.some((edge) => edge.from === 'amountCheck' && edge.to === 'finalApproval' && !edge.outcome))
})

test('rejects runtime JSON without a start node', () => {
  throws(() => runtimeToDesigner({ nodes: [{ id: 'end', type: 'end' }], edges: [] }), /start node/)
})

function test(name: string, run: () => void) {
  run()
  console.log(`ok - ${name}`)
}

function assertRuntimeGraph(actual: RuntimeDefinition, expected: RuntimeDefinition) {
  equal(actual.nodes.length, expected.nodes.length)
  equal(actual.edges.length, expected.edges.length)

  for (const node of expected.nodes) {
    ok(actual.nodes.some((candidate) => sameShape(candidate, node)), `missing node ${node.id}`)
  }

  for (const edge of expected.edges) {
    ok(actual.edges.some((candidate) => sameShape(candidate, edge)), `missing edge ${edge.from}->${edge.to}`)
  }
}

function sameShape(actual: unknown, expected: unknown) {
  return JSON.stringify(sortKeys(actual)) === JSON.stringify(sortKeys(expected))
}

function sortKeys(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value.map(sortKeys)
  }

  if (value && typeof value === 'object') {
    return Object.fromEntries(
      Object.entries(value as Record<string, unknown>)
        .sort(([left], [right]) => left.localeCompare(right))
        .map(([key, nested]) => [key, sortKeys(nested)]),
    )
  }

  return value
}
