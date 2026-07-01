# Enterprise Workflow Roadmap

## Objective

Turn the current workflow module into an enterprise-ready workflow platform that is:

- safe to publish and operate
- credible for business-critical approval processes
- extensible for connectors and external events
- supportable by engineering and operations teams

This roadmap is based on the active framework stack:

- `LogicFlowEnterpriseFramework.Api`
- `LogicFlowEnterpriseFramework.Infrastructure`
- `LogicFlowEnterpriseFramework.Domain`
- `LogicFlowEnterpriseFramework.Blazor`

The workflow engine source of truth is this framework workspace. Do not maintain a parallel legacy workflow project.

## Current Baseline

The current implementation already provides:

- workflow definitions, drafts, publish/versioning
- optimistic concurrency for definition and draft save/publish
- runtime instance start, cancel, and query
- task claim, unclaim, approve, reject
- audit logs and execution logs
- timer and outbox background processing
- embedded visual designer hosted by Blazor

Primary implementation points:

- Runtime validation: `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowDefinitionDocument.cs`
- Runtime execution: `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowRuntimeService.cs`
- Definition APIs: `LogicFlowEnterpriseFramework.Api/Controllers/Workflow/WorkflowDefinitionsController.cs`
- Runtime/task APIs: `LogicFlowEnterpriseFramework.Api/Controllers/Workflow/WorkflowRuntimeController.cs`, `LogicFlowEnterpriseFramework.Api/Controllers/Workflow/TasksController.cs`
- EF model: `LogicFlowEnterpriseFramework.Infrastructure/Persistence/ApplicationDbContext.cs`
- Designer UI: `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/WorkflowDesigner.tsx`
- Workflow admin UI: `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/components/WorkflowAdministration.tsx`
- Task operations UI: `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/components/TaskOperations.tsx`

## Enterprise Target State

The workflow module should reach these capability bands.

### 1. Trustworthy Runtime

- Designer, validator, and runtime support the same node set.
- Publish blocks unsupported or partially implemented nodes.
- Timers, notifications, and service actions are reliable and replay-safe.
- Workflow state transitions are auditable and concurrency-safe.

### 2. Enterprise Human Tasks

- Delegate, reassign, claim, unclaim, approve, reject, cancel
- SLA due dates, reminders, escalations
- comments, attachments, watchers, supervisor actions
- queue views for users, groups, roles, and admins

### 3. Integration and Orchestration

- outbound connector execution for service tasks
- inbound event correlation and workflow resumption
- notification delivery channels
- retry, backoff, dead-letter, idempotency

### 4. Governance

- maker-checker publish flow
- role-based authoring and release permissions
- release history and publish reason
- environment promotion model
- tenant-safe visibility and execution

### 5. Operations

- metrics and health visibility
- stuck timer/outbox detection
- admin retry and repair actions
- archive/purge strategy
- support runbook and smoke tests

## Architectural Direction

Do not add advanced BPMN-style breadth yet. The current stack needs deeper execution quality before adding:

- parallel split/join
- subflow/call activity
- compensation
- exception routing

The right near-term strategy is:

1. finish the small supported node surface
2. harden task and integration reliability
3. add governance and operations
4. then expand orchestration depth

## 30 / 60 / 90 Day Plan

## Days 1-30: Stabilize the Contract

### Goal

Make the existing workflow surface consistent, testable, and safe to release.

### Deliverables

- one authoritative supported-node matrix
- real runtime/designer parity
- explicit `serviceTask` launch behavior
- real notification processing path
- end-to-end test coverage for core flows

### Backend Work

#### Runtime contract freeze

Files:

- `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowDefinitionDocument.cs`
- `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowRuntimeService.cs`
- `LogicFlowEnterpriseFramework.Api/Workflow/Contracts/WorkflowContracts.cs`

Actions:

- Define the V1 supported node set in one place.
- Remove or reject node types that are not operationally complete.
- If `serviceTask` is not implemented as a real connector step yet, disable publish for it.
- If `notification` is only queueing and not delivering, keep it behind a clear supported/unsupported flag.

#### Reliable outbox processing

Files:

- `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowRuntimeService.cs`
- `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowBackgroundProcessingService.cs`
- `LogicFlowEnterpriseFramework.Domain/Entities/Workflow/WorkflowOutbox.cs`

Actions:

- Replace placeholder outbox completion with actual processing handlers by `EventType`.
- Add bounded retries and transition failed messages to an explicit dead-letter state.
- Capture `LastAttemptAtUtc` and `NextAttemptAtUtc`.
- Persist structured failure details for supportability.

#### Task runtime hardening

Files:

- `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowRuntimeService.cs`
- `LogicFlowEnterpriseFramework.Domain/Entities/Workflow/WorkflowTask.cs`

Actions:

- Add row-version checks for claim/unclaim/complete actions to prevent double-action races.
- Ensure task ownership rules are consistently enforced on all complete actions.
- Normalize task outcomes and comments for audit quality.

### Data Model Additions

Add to `wf.WorkflowOutbox`:

- `LastAttemptAtUtc`
- `NextAttemptAtUtc`
- `DeadLetteredAtUtc`
- `ProcessorName`

Add to `wf.WorkflowTasks`:

- `Priority`
- `ReminderAtUtc`
- `EscalationAtUtc`

### UI Work

Files:

- `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/WorkflowDesigner.tsx`
- `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/components/WorkflowAdministration.tsx`
- `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/api.ts`

Actions:

- Reduce the designer toolbox to nodes that the runtime truly supports.
- Show definition/draft version conflicts clearly in the editor.
- Add publish-time validation summary that maps backend errors to node-level issues where possible.

### Test Work

Add or extend:

- API integration tests for create, save draft, validate, publish, start, claim, approve, reject, cancel
- runtime tests for timer and outbox processing
- designer mapper tests for runtime/designer parity

Success criteria:

- no node appears in the toolbox unless it can be published and executed
- outbox processing is no longer a placeholder
- save/publish/start/approve flows pass end to end

## Days 31-60: Human Task Depth

### Goal

Turn the workflow engine from a simple approval runner into a real task operations module.

### Deliverables

- delegation and reassignment
- SLA and escalation support
- richer task inbox and admin views
- audit-grade comments/history

### Backend Work

#### Task actions

Files:

- `LogicFlowEnterpriseFramework.Api/Controllers/Workflow/TasksController.cs`
- `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowRuntimeService.cs`
- `LogicFlowEnterpriseFramework.Api/Workflow/Contracts/WorkflowContracts.cs`

New endpoints:

- `POST /api/tasks/{id}/delegate`
- `POST /api/tasks/{id}/reassign`
- `POST /api/tasks/{id}/comment`
- `POST /api/tasks/{id}/remind`

Actions:

- Separate user action permissions from admin/supervisor permissions.
- Add explicit action audit logs for delegate, reassign, reminder, escalation, reopen if supported.

#### SLA and escalation engine

Files:

- `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowBackgroundProcessingService.cs`
- `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowRuntimeService.cs`
- `LogicFlowEnterpriseFramework.Domain/Entities/Workflow/WorkflowTask.cs`

Actions:

- Background job scans open tasks by `ReminderAtUtc` and `EscalationAtUtc`.
- Queue reminder and escalation notifications.
- Add escalation policy resolution by user, role, or group.

### Data Model Additions

Create:

- `WorkflowTaskComments`
- `WorkflowTaskAssignments`
- `WorkflowTaskWatchers`

Recommended fields:

- `WorkflowTaskComments`: task id, comment text, actor user id, created at
- `WorkflowTaskAssignments`: task id, action type, from assignee, to assignee, actor, reason, created at
- `WorkflowTaskWatchers`: task id, user id, notification preferences

### UI Work

Files:

- `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/components/TaskOperations.tsx`
- `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/api.ts`

Actions:

- Add delegate/reassign actions to task detail modal.
- Add comment timeline and assignment history.
- Surface overdue and due-soon tasks in the inbox.
- Add admin filter views: by role queue, by group queue, overdue, escalated.

Success criteria:

- a team lead can move work safely without database intervention
- overdue tasks are visible and actionable
- comment and assignment history is preserved in audit trails

## Days 61-90: Integration, Events, and Operations

### Goal

Make the engine integration-ready and supportable in production.

### Deliverables

- connector-backed service tasks
- inbound event resume support
- dead-letter and retry operations
- operational dashboards and runbooks

### Backend Work

#### Service task execution model

Files:

- `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowRuntimeService.cs`
- new folder: `LogicFlowEnterpriseFramework.Api/Workflow/Execution`

Create abstractions:

- `IWorkflowServiceTaskExecutor`
- `IWorkflowNotificationDispatcher`
- `IWorkflowEventCorrelator`

Recommended service task contract:

- step declares `serviceKey`
- runtime resolves executor by `serviceKey`
- executor returns `Completed`, `Retry`, or `Failed`
- runtime records attempt count and failure details on `WorkflowInstanceNode`

#### Inbound event resumption

Files:

- `LogicFlowEnterpriseFramework.Api/Controllers/Workflow`
- `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowRuntimeService.cs`
- `LogicFlowEnterpriseFramework.Domain/Entities/Workflow/WorkflowEventSubscription.cs`

New endpoints:

- `POST /api/workflow-events/{eventName}`
- optional admin replay endpoint for support

Actions:

- Create waiting event subscriptions from workflow nodes once event-wait nodes are introduced, or from service-task callbacks if that is the first event pattern.
- Correlate by event name plus business key/correlation key.
- Resume the waiting workflow instance deterministically.

#### Operations and recovery

Files:

- `LogicFlowEnterpriseFramework.Api/Controllers/Workflow`
- `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowBackgroundProcessingService.cs`

New admin endpoints:

- `GET /api/workflow-admin/health`
- `GET /api/workflow-admin/outbox`
- `POST /api/workflow-admin/outbox/{id}/retry`
- `GET /api/workflow-admin/timers`
- `POST /api/workflow-admin/timers/{id}/retry`
- `GET /api/workflow-admin/instances/stuck`

Actions:

- detect stale `Processing` outbox items
- detect overdue pending timers
- detect instances with no heartbeat progression
- support safe retry and dead-letter requeue

### Data Model Additions

Add to `wf.WorkflowInstanceNodes`:

- `LastAttemptAtUtc`
- `NextAttemptAtUtc`
- `ProcessorKey`

Add to `wf.WorkflowInstances` if not already fully used:

- stronger use of `CorrelationId`
- optional `SuspendedAtUtc`
- optional `ArchivedAtUtc`

### UI Work

Files:

- `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/components/Dashboard.tsx`
- `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/components/TaskOperations.tsx`
- new admin components under `src/components`

Actions:

- add workflow operations dashboard
- show queue depth, overdue tasks, failed outbox items, pending timers
- add admin retry controls for failed processing items

Success criteria:

- service tasks use a formal execution contract
- external events can resume workflows safely
- operations team can diagnose and retry failures without direct database edits

## Prioritized Backlog

### Priority 0

- Freeze designer/runtime/validator node parity
- Decide `serviceTask` status for launch
- Implement real outbox handlers instead of placeholder completion
- Add end-to-end API tests for the full happy path

### Priority 1

- Add task delegation and reassignment
- Add SLA and escalation timestamps to tasks
- Add task comments and assignment history
- Add overdue task and escalated task filters in UI

### Priority 2

- Add connector abstraction for service tasks
- Add notification dispatcher abstraction
- Add dead-letter workflow for outbox failures
- Add admin operational endpoints and UI

### Priority 3

- Add inbound event correlation and resume
- Add maker-checker publish approval
- Add promotion model across environments

## Recommended API Expansion

### Definition and governance

- `POST /api/workflow-definitions/{id}/submit-for-approval`
- `POST /api/workflow-definitions/{id}/approve-publication`
- `POST /api/workflow-definitions/{id}/archive`

### Task operations

- `POST /api/tasks/{id}/delegate`
- `POST /api/tasks/{id}/reassign`
- `POST /api/tasks/{id}/comment`
- `GET /api/tasks/overdue`
- `GET /api/tasks/queue`

### Operations

- `GET /api/workflow-admin/outbox`
- `POST /api/workflow-admin/outbox/{id}/retry`
- `GET /api/workflow-admin/timers`
- `POST /api/workflow-admin/timers/{id}/retry`
- `GET /api/workflow-admin/instances/stuck`

### Events

- `POST /api/workflow-events/{eventName}`
- `POST /api/workflow-events/{eventName}/replay`

## Recommended Database Expansion

Short-term additions:

- extend `WorkflowOutbox`
- extend `WorkflowTasks`
- add `WorkflowTaskComments`
- add `WorkflowTaskAssignments`
- add `WorkflowTaskWatchers`

Medium-term additions:

- `WorkflowDeadLetters`
- `WorkflowConnectorExecutions`
- `WorkflowPublishApprovals`

## Definition of Done for Enterprise V1

The module should be considered enterprise-ready when:

- runtime-supported nodes are fully aligned across UI, validation, and execution
- task actions are concurrency-safe and audit-complete
- notification and service execution are reliable and retryable
- delegated and reassigned work is first-class
- overdue/escalated work is visible and operable
- dead-letter and retry paths exist for background failures
- support staff can inspect and recover workflow failures without manual SQL

## Suggested First Execution Sprint

If starting immediately, the first sprint should contain:

1. Remove unsupported nodes from the designer toolbox or reject them at publish.
2. Implement real outbox event handlers and dead-letter status.
3. Add API integration tests for publish-to-complete flows.
4. Add delegate and reassign task actions.
5. Extend task UI with overdue status and assignment history.

That sequence improves product trust faster than adding new diagram node types.
