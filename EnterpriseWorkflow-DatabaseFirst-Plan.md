# Enterprise Workflow Database-First Plan

## Purpose

Define the database changes required to support:

- a canonical workflow node model centered on `userTask`
- a configurable task box
- durable assignment history, comments, watchers, and SLA state
- reliable outbox processing and future event correlation

This plan is intentionally database-first. API and UI work should follow these schema changes.

## Baseline

The active schema direction is the richer `wf.*` model reflected in:

- `LogicFlowEnterpriseFramework.Api/Workflow/Sql/001_rebuild_wf_schema.sql`
- `LogicFlowEnterpriseFramework.Infrastructure/Persistence/ApplicationDbContext.cs`

Key current runtime tables:

- `wf.WorkflowDefinitions`
- `wf.WorkflowDrafts`
- `wf.WorkflowVersions`
- `wf.WorkflowInstances`
- `wf.WorkflowInstanceNodes`
- `wf.WorkflowInstanceVariables`
- `wf.WorkflowTasks`
- `wf.WorkflowTimers`
- `wf.WorkflowEventSubscriptions`
- `wf.WorkflowAuditLogs`
- `wf.WorkflowExecutionLogs`
- `wf.WorkflowOutbox`

## Design Rules

### 1. Preserve runtime simplicity

Do not create a database table per node type.

The node contract stays in workflow definition JSON. The database stores:

- resolved runtime state
- task rendering state
- audit and operations state

### 2. Snapshot task rendering data

The task box should not re-interpret workflow definition JSON on every screen load.

When a task is created, persist:

- resolved actions
- display metadata
- view/form keys
- SLA state

### 3. Keep history append-only where possible

Comments, assignments, and watcher changes should go into dedicated history tables instead of overwriting one row repeatedly.

### 4. Add schema only where state must be durable

Do not add columns for logic that can remain in runtime code only.

## Schema Scope

This plan covers three schema slices.

### Slice A: Human task customization

- enrich `wf.WorkflowTasks`
- add comments
- add assignment history
- add watchers

### Slice B: Background reliability

- enrich `wf.WorkflowOutbox`
- prepare for dead-letter and scheduled retry

### Slice C: Event and operations readiness

- tighten `wf.WorkflowEventSubscriptions`
- enrich `wf.WorkflowInstanceNodes`

## Phase 1: Extend `wf.WorkflowTasks`

Current table definition is in:

- `LogicFlowEnterpriseFramework.Api/Workflow/Sql/001_rebuild_wf_schema.sql`
- `LogicFlowEnterpriseFramework.Domain/Entities/Workflow/WorkflowTask.cs`

## Why this comes first

The task box customization depends on durable task state. Without this, the API will be forced to assemble UI behavior dynamically and inconsistently.

## Add columns

### Task identity and rendering

- `TaskMode NVARCHAR(40) NOT NULL DEFAULT N'approval'`
- `Priority NVARCHAR(20) NULL`
- `EntityType NVARCHAR(80) NULL`
- `EntityId NVARCHAR(120) NULL`
- `FormKey NVARCHAR(120) NULL`
- `ListViewKey NVARCHAR(120) NULL`
- `DetailViewKey NVARCHAR(120) NULL`

### UI/action snapshot

- `AvailableActionsJson NVARCHAR(MAX) NULL`
- `DisplayMetadataJson NVARCHAR(MAX) NULL`
- `TaskTagsJson NVARCHAR(MAX) NULL`

### SLA and escalation

- `ReminderAtUtc DATETIME2(7) NULL`
- `EscalationAtUtc DATETIME2(7) NULL`
- `EscalationPolicyKey NVARCHAR(120) NULL`
- `SlaStatus NVARCHAR(30) NULL`
- `EscalatedAtUtc DATETIME2(7) NULL`

### Optional ownership and queue semantics

- `ClaimRequired BIT NOT NULL DEFAULT 0`
- `QueueKey NVARCHAR(120) NULL`

## Recommended constraints

- `CK_wf_WorkflowTasks_TaskMode`
  Allowed initial values:
  - `approval`
  - `review`
  - `dataEntry`
  - `acknowledgement`
  - `manualAction`
  - `exception`

- `CK_wf_WorkflowTasks_Priority`
  Allowed values:
  - `Low`
  - `Medium`
  - `High`
  - `Critical`

- `CK_wf_WorkflowTasks_SlaStatus`
  Allowed values:
  - `OnTrack`
  - `DueSoon`
  - `Overdue`
  - `Escalated`

## Recommended indexes

- `IX_wf_WorkflowTasks_StatusPriorityDue`
  `(Status, Priority, DueAtUtc, CreatedAtUtc)`

- `IX_wf_WorkflowTasks_TaskModeStatus`
  `(TaskMode, Status, DueAtUtc)`

- `IX_wf_WorkflowTasks_Entity`
  `(EntityType, EntityId)`
  Filter where `EntityType IS NOT NULL AND EntityId IS NOT NULL`

- `IX_wf_WorkflowTasks_Escalation`
  `(Status, EscalationAtUtc)`
  Filter where `EscalationAtUtc IS NOT NULL`

- `IX_wf_WorkflowTasks_Reminder`
  `(Status, ReminderAtUtc)`
  Filter where `ReminderAtUtc IS NOT NULL`

## Entity update plan

Update:

- `LogicFlowEnterpriseFramework.Domain/Entities/Workflow/WorkflowTask.cs`
- `LogicFlowEnterpriseFramework.Infrastructure/Persistence/ApplicationDbContext.cs`

Add matching properties and max lengths.

## Phase 2: Add `wf.WorkflowTaskComments`

## Why

The current `WorkflowTask.Comment` field is not enough for:

- threaded or sequential comments
- action commentary
- review history
- requester/assignee discussion

## Table definition

`wf.WorkflowTaskComments`

Columns:

- `Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID()`
- `WorkflowTaskId UNIQUEIDENTIFIER NOT NULL`
- `CommentType NVARCHAR(30) NOT NULL`
- `Body NVARCHAR(4000) NOT NULL`
- `Visibility NVARCHAR(30) NOT NULL DEFAULT N'Internal'`
- `CreatedBy NVARCHAR(200) NOT NULL`
- `CreatedByUserId UNIQUEIDENTIFIER NULL`
- `CreatedAtUtc DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()`

## Recommended comment types

- `Comment`
- `Decision`
- `SystemNote`
- `Escalation`
- `AssignmentReason`

## Recommended visibility values

- `Internal`
- `Participant`
- `Watcher`

## Constraints

- FK to `wf.WorkflowTasks`
- FK to `dbo.AspNetUsers` on `CreatedByUserId`
- `CK_wf_WorkflowTaskComments_CommentType`
- `CK_wf_WorkflowTaskComments_Visibility`

## Indexes

- `IX_wf_WorkflowTaskComments_TaskCreated`
  `(WorkflowTaskId, CreatedAtUtc DESC)`

## Phase 3: Add `wf.WorkflowTaskAssignments`

## Why

Assignment history should be durable and queryable. This is required for:

- delegation
- reassignment
- escalation routing
- audit investigation

## Table definition

`wf.WorkflowTaskAssignments`

Columns:

- `Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID()`
- `WorkflowTaskId UNIQUEIDENTIFIER NOT NULL`
- `ActionType NVARCHAR(30) NOT NULL`
- `FromUserId UNIQUEIDENTIFIER NULL`
- `FromGroupId UNIQUEIDENTIFIER NULL`
- `FromRoleId UNIQUEIDENTIFIER NULL`
- `ToUserId UNIQUEIDENTIFIER NULL`
- `ToGroupId UNIQUEIDENTIFIER NULL`
- `ToRoleId UNIQUEIDENTIFIER NULL`
- `Reason NVARCHAR(1000) NULL`
- `PerformedBy NVARCHAR(200) NOT NULL`
- `PerformedByUserId UNIQUEIDENTIFIER NULL`
- `CreatedAtUtc DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()`

## Action types

- `Assigned`
- `Claimed`
- `Unclaimed`
- `Delegated`
- `Reassigned`
- `Escalated`
- `AutoRouted`

## Constraints

- FK to `wf.WorkflowTasks`
- FK to `dbo.AspNetUsers` and role/group tables where applicable
- `CK_wf_WorkflowTaskAssignments_ActionType`

## Indexes

- `IX_wf_WorkflowTaskAssignments_TaskCreated`
  `(WorkflowTaskId, CreatedAtUtc DESC)`

- `IX_wf_WorkflowTaskAssignments_PerformedBy`
  `(PerformedByUserId, CreatedAtUtc DESC)`
  Filter where `PerformedByUserId IS NOT NULL`

## Phase 4: Add `wf.WorkflowTaskWatchers`

## Why

Watchers are useful for:

- requester visibility
- supervisor monitoring
- silent stakeholders who need updates but not task ownership

## Table definition

`wf.WorkflowTaskWatchers`

Columns:

- `Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID()`
- `WorkflowTaskId UNIQUEIDENTIFIER NOT NULL`
- `WatcherUserId UNIQUEIDENTIFIER NOT NULL`
- `NotificationMode NVARCHAR(30) NOT NULL DEFAULT N'All'`
- `CreatedAtUtc DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()`

## Notification modes

- `All`
- `StatusOnly`
- `DecisionOnly`
- `None`

## Constraints

- FK to `wf.WorkflowTasks`
- FK to `dbo.AspNetUsers`
- unique constraint on `(WorkflowTaskId, WatcherUserId)`

## Indexes

- `IX_wf_WorkflowTaskWatchers_User`
  `(WatcherUserId, CreatedAtUtc DESC)`

## Phase 5: Enrich `wf.WorkflowOutbox`

Current table supports:

- `RetryCount`
- `Status`
- `ErrorMessage`
- `LockId`
- `LockedAtUtc`

That is not yet enough for production-grade retry and dead-letter handling.

## Add columns

- `LastAttemptAtUtc DATETIME2(7) NULL`
- `NextAttemptAtUtc DATETIME2(7) NULL`
- `DeadLetteredAtUtc DATETIME2(7) NULL`
- `ProcessorName NVARCHAR(120) NULL`
- `ErrorCode NVARCHAR(100) NULL`
- `HeadersJson NVARCHAR(MAX) NULL`

## Status model update

Current status values:

- `Pending`
- `Processing`
- `Processed`
- `Failed`

Recommended status values:

- `Pending`
- `Processing`
- `Processed`
- `Failed`
- `DeadLettered`

## Constraints

- update `CK_wf_WorkflowOutbox_Status`

## Indexes

Replace or complement the current outbox index with:

- `IX_wf_WorkflowOutbox_StatusNextAttempt`
  `(Status, NextAttemptAtUtc, OccurredAtUtc, RetryCount)`

- `IX_wf_WorkflowOutbox_LockedAt`
  `(Status, LockedAtUtc)`
  Filter where `LockedAtUtc IS NOT NULL`

## Why these fields matter

They support:

- exponential backoff
- delayed retry scheduling
- stuck processor recovery
- support dashboards

## Phase 6: Enrich `wf.WorkflowEventSubscriptions`

Current schema is adequate for simple waiting, but not ideal for durable inbound event correlation operations.

## Add columns

- `EventSource NVARCHAR(120) NULL`
- `CorrelationType NVARCHAR(40) NULL`
- `MatchedPayloadJson NVARCHAR(MAX) NULL`
- `SatisfiedByMessageId NVARCHAR(200) NULL`
- `CancelledAtUtc DATETIME2(7) NULL`

## Constraints

- `CK_wf_WorkflowEventSubscriptions_CorrelationType`
  Suggested values:
  - `BusinessKey`
  - `CorrelationId`
  - `Custom`

## Indexes

- `IX_wf_WorkflowEventSubscriptions_OpenCorrelation`
  `(Status, EventName, CorrelationKey, EventSource, ExpiresAtUtc)`

## Why this matters

These fields allow:

- safer external callback handling
- better diagnostics
- message replay protection

## Phase 7: Enrich `wf.WorkflowInstanceNodes`

This table should hold more operational state for service tasks and retryable runtime nodes.

## Add columns

- `LastAttemptAtUtc DATETIME2(7) NULL`
- `NextAttemptAtUtc DATETIME2(7) NULL`
- `ProcessorKey NVARCHAR(120) NULL`
- `ExecutionMetadataJson NVARCHAR(MAX) NULL`

## Why

This supports:

- service task retries
- timer retry analysis
- processor-specific diagnostics

## Migration Sequence

Recommended EF migration sequence:

1. `AddWorkflowTaskBoxState`
2. `AddWorkflowTaskComments`
3. `AddWorkflowTaskAssignments`
4. `AddWorkflowTaskWatchers`
5. `EnhanceWorkflowOutboxReliability`
6. `EnhanceWorkflowEventSubscriptions`
7. `EnhanceWorkflowInstanceNodeOperations`

If you prefer fewer migrations, combine phases 1-4 into one migration:

- `AddWorkflowTaskCustomizationModel`

and phases 5-7 into one migration:

- `AddWorkflowOperationsReliabilityModel`

## Backward Compatibility Strategy

### Existing rows

For existing `wf.WorkflowTasks` rows:

- `TaskMode` backfill to `approval`
- `Priority` leave `NULL` or backfill to `Medium`
- `SlaStatus` backfill based on `DueAtUtc`

For existing `wf.WorkflowOutbox` rows:

- `NextAttemptAtUtc` backfill from `OccurredAtUtc`
- `LastAttemptAtUtc` leave `NULL`

### Application rollout

Rollout order:

1. apply migration
2. deploy API that writes new columns/tables
3. deploy UI that reads new payloads

Do not deploy UI expectations before API begins populating the new fields.

## SQL Script Update Scope

If the team continues maintaining:

- `LogicFlowEnterpriseFramework.Api/Workflow/Sql/001_rebuild_wf_schema.sql`

then update it alongside EF migrations so the rebuild script does not diverge from the runtime schema.

## Files To Update After Schema Approval

### Domain

- `LogicFlowEnterpriseFramework.Domain/Entities/Workflow/WorkflowTask.cs`
- new:
  - `WorkflowTaskComment.cs`
  - `WorkflowTaskAssignment.cs`
  - `WorkflowTaskWatcher.cs`

### EF configuration

- `LogicFlowEnterpriseFramework.Infrastructure/Persistence/ApplicationDbContext.cs`

### Migration

- new EF migration files under:
  `LogicFlowEnterpriseFramework.Infrastructure/Persistence/Migrations`

### SQL rebuild script

- `LogicFlowEnterpriseFramework.Api/Workflow/Sql/001_rebuild_wf_schema.sql`

## Recommended First Migration Slice

If you want the smallest high-value schema slice, start with:

### In `wf.WorkflowTasks`

- `TaskMode`
- `Priority`
- `FormKey`
- `AvailableActionsJson`
- `DisplayMetadataJson`
- `ReminderAtUtc`
- `EscalationAtUtc`
- `SlaStatus`

### New tables

- `wf.WorkflowTaskComments`
- `wf.WorkflowTaskAssignments`

### In `wf.WorkflowOutbox`

- `LastAttemptAtUtc`
- `NextAttemptAtUtc`
- `DeadLetteredAtUtc`
- add `DeadLettered` status

That is the best first database slice because it unlocks:

- customizable task box rendering
- delegate/reassign/comment APIs
- reliable outbox retry behavior

without forcing advanced orchestration changes yet.

## Decision Summary

The database-first strategy should focus on:

- richer task persistence
- append-only collaboration history
- reliable background processing state

It should not focus first on:

- parallel orchestration tables
- BPMN breadth
- speculative node-specific tables

That path keeps the workflow engine small while making the workflow product much more enterprise-capable.
