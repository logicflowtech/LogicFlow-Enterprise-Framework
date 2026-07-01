# Enterprise Workflow Node And Task Box Spec

## Purpose

Define a workflow node model and task box architecture that can support multiple business modules without turning the workflow runtime into a collection of one-off features.

This spec is for the active framework implementation:

- `LogicFlowEnterpriseFramework.Api`
- `LogicFlowEnterpriseFramework.Domain`
- `LogicFlowEnterpriseFramework.Infrastructure`
- `LogicFlowEnterpriseFramework.Blazor`

It builds on the current runtime, mapper, and task operations implementation:

- `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowDefinitionDocument.cs`
- `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowRuntimeService.cs`
- `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/workflowDefinitionMapper.ts`
- `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/components/TaskOperations.tsx`

For the expanded enterprise node catalog and phased runtime roadmap, also see:

- `EnterpriseWorkflow-AdvancedNodeImplementation-Spec.md`

## Design Principles

### 1. Keep runtime node types small

Do not create a separate runtime branch for every business variation.

Use a small stable node set:

- `start`
- `end`
- `userTask`
- `condition`
- `timer`
- `notification`
- `serviceTask`

Optional compatibility mapping:

- keep `approval` as a supported alias for `userTask` during transition

### 2. Put business variation in task profiles and metadata

Business modules usually differ more in:

- what a user sees
- what actions are allowed
- what data must be captured
- how SLA and escalation behave

They usually do not require a new workflow engine primitive.

### 3. Make the task box a renderer

The task box should not be hardcoded around one use case such as `Approve` and `Reject`.

The API should return enough metadata for the UI to render:

- list cards
- detail sections
- available actions
- form binding
- SLA state

### 4. Database first, API second, UI third

Task customization depends on durable workflow state. It should not be implemented only in frontend code.

## Current State Assessment

The current mapper already exposes a wider model than the runtime fully owns. See:

- `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/workflowDefinitionMapper.ts`

That file already carries properties for:

- assignment
- form binding
- approval mode
- notification settings
- service execution settings
- subflow and parallel concepts
- escalation and exception concepts

The runtime and validator are narrower than that. This spec resolves that mismatch by defining:

- what is truly first-class now
- what remains metadata only
- what is deferred entirely

## Canonical Node Catalog

## Node Groups

### Group A: Runtime Core

- `start`
- `end`
- `condition`

### Group B: Human Work

- `userTask`

Compatibility alias:

- `approval` maps internally to `userTask` with `taskMode = approval`

### Group C: System Work

- `timer`
- `notification`
- `serviceTask`

### Deferred Groups

Do not expose these in the production toolbox until runtime support exists:

- `parallelSplit`
- `parallelJoin`
- `subflow`
- `eventWait`
- `escalation`
- `exceptionHandler`
- `compensation`

## Canonical Node Shape

Every node should support the same base contract.

```json
{
  "id": "managerApproval",
  "type": "userTask",
  "name": "Manager Approval",
  "description": "Approver reviews the submitted request.",
  "tags": ["approval", "manager"],
  "formKey": "leave-request-approval",
  "permissionKey": "WF_APPROVE_LEAVE",
  "slaProfile": {
    "dueInHours": 24,
    "reminderInHours": 20,
    "escalationInHours": 30,
    "businessCalendar": true
  },
  "metadata": {}
}
```

### Common properties

- `id`
- `type`
- `name`
- `description`
- `tags`
- `formKey`
- `permissionKey`
- `slaProfile`
- `metadata`

Recommended validator rule:

- unknown top-level fields are rejected unless they are under `metadata`

That keeps the contract evolvable without turning it into an uncontrolled JSON bag.

## Human Task Node Model

## Why `userTask` should be the main customizable node

Most business workflow variation belongs here:

- approval
- review
- acknowledgement
- exception handling
- manual update
- request for information

Instead of adding many new node types, use one node type with a task profile.

## `userTask` contract

```json
{
  "id": "financeReview",
  "type": "userTask",
  "name": "Finance Review",
  "description": "Finance validates cost center and budget.",
  "assignment": {
    "mode": "role",
    "roleId": "00000000-0000-0000-0000-000000000001",
    "claimRequired": true,
    "allowDelegate": true,
    "allowReassign": true
  },
  "taskProfile": {
    "taskMode": "review",
    "priority": "High",
    "formKey": "finance-review-form",
    "listViewKey": "finance-review-card",
    "detailViewKey": "finance-review-detail",
    "actionSetKey": "review-actions"
  },
  "slaProfile": {
    "dueInHours": 8,
    "reminderInHours": 6,
    "escalationInHours": 12,
    "businessCalendar": true
  },
  "dataContract": {
    "inputs": ["request.amount", "request.department", "request.requester"],
    "outputs": ["decision.outcome", "decision.comment"]
  },
  "metadata": {
    "allowComments": true,
    "allowAttachments": true,
    "watcherMode": "manual"
  }
}
```

## `userTask` sections

### Assignment

Supported modes:

- `user`
- `group`
- `role`
- `expression` later

Assignment fields:

- `mode`
- `userId`
- `groupId`
- `roleId`
- `claimRequired`
- `allowDelegate`
- `allowReassign`
- `allowAdminOverride`

### Task profile

Fields:

- `taskMode`
- `priority`
- `formKey`
- `listViewKey`
- `detailViewKey`
- `actionSetKey`

Recommended initial `taskMode` values:

- `approval`
- `review`
- `dataEntry`
- `acknowledgement`
- `manualAction`
- `exception`

### SLA profile

Fields:

- `dueInHours`
- `reminderInHours`
- `escalationInHours`
- `businessCalendar`
- `escalationPolicyKey`

### Data contract

Fields:

- `inputs`
- `outputs`
- `outputStorageMode`

Recommended `outputStorageMode` values:

- `variables`
- `taskOutput`
- `both`

## Human Task Actions

Actions should not be hardcoded in UI. They should be defined per node or action set.

## Canonical action contract

```json
{
  "code": "approve",
  "label": "Approve",
  "style": "primary",
  "requiresComment": false,
  "requiresConfirmation": false,
  "permissionKey": "WF_APPROVE_LEAVE",
  "result": {
    "outcome": "Approved",
    "transition": "default"
  }
}
```

## Initial action codes

- `approve`
- `reject`
- `sendBack`
- `requestInfo`
- `complete`
- `claim`
- `unclaim`
- `delegate`
- `reassign`
- `cancel`

## Action set examples

### Approval actions

- `approve`
- `reject`
- `requestInfo`

### Review actions

- `complete`
- `sendBack`

### Exception actions

- `assign`
- `escalate`
- `resolve`

## Condition Node Model

Keep the current simple expression model initially, but formalize the contract.

```json
{
  "id": "amountCheck",
  "type": "condition",
  "name": "Amount Check",
  "expression": "amount > 5000",
  "metadata": {
    "expressionDialect": "simple"
  }
}
```

Recommendation:

- keep current simple evaluator for V1
- do not introduce arbitrary script execution
- add a clear expression dialect field for future extensibility

## Timer Node Model

```json
{
  "id": "waitForResponse",
  "type": "timer",
  "name": "Wait For Response",
  "timer": {
    "mode": "duration",
    "expression": "PT24H",
    "businessCalendar": true
  },
  "metadata": {}
}
```

Supported initial timer modes:

- `duration`
- `absoluteDate`

Recommended future mode:

- `businessDueDate`

## Notification Node Model

```json
{
  "id": "notifyRequester",
  "type": "notification",
  "name": "Notify Requester",
  "notification": {
    "channel": "email",
    "templateKey": "leave-approved",
    "recipientSource": "workflowInitiator",
    "notificationKey": "leave-approved-email"
  },
  "metadata": {}
}
```

Supported initial channels:

- `inApp`
- `email`

Deferred:

- `sms`
- `teams`
- `webhook`

## Service Task Node Model

```json
{
  "id": "createErpRecord",
  "type": "serviceTask",
  "name": "Create ERP Record",
  "service": {
    "serviceKey": "erp.createPurchaseRequest",
    "inputMapping": "requestPayload",
    "outputMapping": "erpResponse",
    "retryPolicyKey": "standard",
    "timeoutHours": 1
  },
  "metadata": {}
}
```

Service task should be backed by a real executor contract, not a pass-through runtime branch.

## Database-First Task Box Model

The task box should be powered by persisted task state, not only workflow definition state.

## Extend `WorkflowTasks`

Current entity:

- `LogicFlowEnterpriseFramework.Domain/Entities/Workflow/WorkflowTask.cs`

Recommended new fields:

- `TaskMode`
- `Priority`
- `EntityType`
- `EntityId`
- `FormKey`
- `ListViewKey`
- `DetailViewKey`
- `AvailableActionsJson`
- `DisplayMetadataJson`
- `ReminderAtUtc`
- `EscalationAtUtc`
- `EscalationPolicyKey`
- `IsOverdue`

### Purpose of the new fields

- `TaskMode`: allows UI and API to treat approval, review, and exception work differently
- `EntityType` and `EntityId`: connect the task to the business module record
- `FormKey`: tells UI what form to render
- `ListViewKey` and `DetailViewKey`: support module-specific card/detail renderers
- `AvailableActionsJson`: stores the resolved actions snapshot for stable rendering and auditing
- `DisplayMetadataJson`: stores list/detail display payload for fast inbox rendering

## New table: `WorkflowTaskComments`

Purpose:

- preserve threaded or sequential user comments
- separate operational commentary from the main `WorkflowTask.Comment` field

Recommended columns:

- `Id`
- `WorkflowTaskId`
- `CommentType`
- `Body`
- `CreatedBy`
- `CreatedByUserId`
- `CreatedAtUtc`

## New table: `WorkflowTaskAssignments`

Purpose:

- keep a durable history of assignment changes
- support audit, reporting, and supervisor operations

Recommended columns:

- `Id`
- `WorkflowTaskId`
- `ActionType`
- `FromUserId`
- `FromGroupId`
- `FromRoleId`
- `ToUserId`
- `ToGroupId`
- `ToRoleId`
- `Reason`
- `PerformedBy`
- `PerformedByUserId`
- `CreatedAtUtc`

## New table: `WorkflowTaskWatchers`

Purpose:

- support notification and visibility use cases
- allow stakeholders to follow task progress without ownership

Recommended columns:

- `Id`
- `WorkflowTaskId`
- `WatcherUserId`
- `NotificationMode`
- `CreatedAtUtc`

## Optional table: `WorkflowTaskAttachments`

Add only if attachment support is needed in the task box.

Recommended columns:

- `Id`
- `WorkflowTaskId`
- `StorageKey`
- `FileName`
- `ContentType`
- `SizeInBytes`
- `UploadedByUserId`
- `UploadedAtUtc`

## API Design

The API should support both workflow correctness and inbox rendering.

## Recommended task response shape

```json
{
  "id": "task-id",
  "workflowInstanceId": "instance-id",
  "nodeId": "financeReview",
  "taskMode": "review",
  "title": "Finance Review",
  "summary": "Validate budget and cost center",
  "status": "Pending",
  "priority": "High",
  "assignedToDisplayName": "Finance Approvers",
  "dueAtUtc": "2026-06-29T12:00:00Z",
  "isOverdue": false,
  "entityType": "PurchaseRequest",
  "entityId": "PR-2026-0001",
  "formKey": "finance-review-form",
  "listViewKey": "finance-review-card",
  "detailViewKey": "finance-review-detail",
  "availableActions": [
    { "code": "claim", "label": "Claim", "style": "secondary" },
    { "code": "complete", "label": "Complete Review", "style": "primary" }
  ],
  "displayMetadata": {
    "requester": "Alicia Tan",
    "department": "Finance",
    "amount": 12500,
    "currency": "MYR"
  }
}
```

## Recommended task endpoints

### Inbox and queue

- `GET /api/tasks/my`
- `GET /api/tasks/queue`
- `GET /api/tasks/overdue`
- `GET /api/tasks/escalated`
- `GET /api/tasks/{id}`
- `GET /api/tasks/{id}/detail`

### Actions

- `POST /api/tasks/{id}/claim`
- `POST /api/tasks/{id}/unclaim`
- `POST /api/tasks/{id}/complete`
- `POST /api/tasks/{id}/delegate`
- `POST /api/tasks/{id}/reassign`
- `POST /api/tasks/{id}/comment`
- `POST /api/tasks/{id}/watch`
- `POST /api/tasks/{id}/unwatch`

### Admin operations

- `POST /api/tasks/{id}/escalate`
- `POST /api/tasks/{id}/reopen`

## API behavior recommendations

### Snapshot task display data at task creation time

When a task is created, resolve and store:

- available actions
- assignment display label
- list/detail display payload

This avoids requiring the UI to re-evaluate workflow definition rules every time the task is opened.

### Still keep workflow definition as source of truth

The definition controls the behavior.

The task record stores a resolved snapshot used for:

- rendering
- auditing
- resilient history

## UI Architecture

The task box should not be one static screen. It should have a shared shell and pluggable renderers.

## Recommended UI composition

### 1. Inbox shell

Responsibilities:

- tabs
- filters
- saved views
- sort
- queue selection

### 2. Task card renderer

Responsibilities:

- display list card by `listViewKey` or `taskMode`
- fallback to default renderer when no custom renderer exists

### 3. Task detail renderer

Responsibilities:

- display detail sections by `detailViewKey`
- render form by `formKey`
- render comments, watchers, attachments, assignment history

### 4. Generic action bar

Responsibilities:

- render API-provided `availableActions`
- enforce comment-required and confirmation-required behavior

## Renderer strategy

Recommended order:

1. try `listViewKey` / `detailViewKey`
2. fallback to `taskMode`
3. fallback to default workflow task renderer

This keeps customization controlled without breaking the shared task box.

## Suggested React structure

Under `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/components`:

- `TaskInboxShell.tsx`
- `TaskListRenderer.tsx`
- `TaskDetailRenderer.tsx`
- `TaskActionBar.tsx`
- `task-renderers/DefaultTaskCard.tsx`
- `task-renderers/DefaultTaskDetail.tsx`
- `task-renderers/by-mode/*`
- `task-renderers/by-view-key/*`

## Validator Rules

Validator updates should enforce:

- `userTask` must define exactly one assignment mode unless expression mode is used
- `taskMode` must be from a known set
- `actionSetKey` or inline actions must resolve to a supported set
- `formKey`, `listViewKey`, and `detailViewKey` must be valid identifiers
- timer and service task config must be structurally valid

## Mapper Rules

The current mapper should be simplified around the canonical model.

### Recommended mapper decisions

- map legacy `approval` nodes to canonical `userTask`
- stop emitting deferred node types into the production toolbox
- move non-core fields under structured sections such as `assignment`, `taskProfile`, `slaProfile`, `service`, and `notification`

## Runtime Rules

At runtime:

- `userTask` creates a task with resolved action snapshot
- `condition` evaluates expression and branches
- `timer` creates a waiting timer
- `notification` queues a delivery request
- `serviceTask` invokes an executor contract

The runtime should not depend on UI interpretation to know what a task does.

## Suggested Implementation Sequence

## Phase 1: Canonicalize task nodes

- keep `userTask` as the main human-task node
- treat `approval` as compatibility only
- add structured task metadata sections to definition contract

## Phase 2: Expand task persistence

- extend `WorkflowTasks`
- add `WorkflowTaskComments`
- add `WorkflowTaskAssignments`
- add `WorkflowTaskWatchers`

## Phase 3: Expand task APIs

- add `complete`, `delegate`, `reassign`, `comment`
- return `availableActions` and `displayMetadata`

## Phase 4: Rebuild the task box UI

- replace hardcoded decision modal behavior with metadata-driven rendering
- add queue views and saved filters
- add comments, history, and watchers

## Recommended First Slice

If you want the smallest high-value implementation slice:

1. Canonicalize `userTask` and `approval` mapping.
2. Extend `WorkflowTasks` with:
   - `TaskMode`
   - `Priority`
   - `FormKey`
   - `AvailableActionsJson`
   - `DisplayMetadataJson`
3. Add `WorkflowTaskComments` and `WorkflowTaskAssignments`.
4. Add task APIs for:
   - `complete`
   - `delegate`
   - `reassign`
   - `comment`
5. Update task box UI to render actions from API response rather than hardcoded approve/reject only.

## Decision Summary

The key recommendation is:

- do not grow the engine by adding many new node types first
- grow the product by making `userTask` configurable and making the task box metadata-driven

That approach will support more business workflows with less runtime complexity.
