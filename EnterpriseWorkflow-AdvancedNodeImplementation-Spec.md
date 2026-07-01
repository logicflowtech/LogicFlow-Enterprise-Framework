# Enterprise Workflow Advanced Node Implementation Spec

## Purpose

Define the enterprise workflow node catalog that the framework should grow into, while staying aligned with the current implementation in:

- `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/WorkflowDesigner.tsx`
- `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/workflowDefinitionMapper.ts`
- `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowDefinitionDocument.cs`
- `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowRuntimeService.cs`

This spec answers one practical question:

- how to support `Decision`, `Human Task`, `Process Task`, `Wait`, `Escalation`, `Notification`, `Parallel Split`, `Parallel Join`, `Subflow`, and `Exception Handler` without turning the runtime into unstructured JSON and special-case logic

## Current Implementation Baseline

### Currently validator-supported runtime types

`WorkflowDefinitionDocument` currently validates:

- `start`
- `approval`
- `userTask`
- `condition`
- `timer`
- `delay`
- `serviceTask`
- `notification`
- `end`

### Currently mapper-aware types

`workflowDefinitionMapper.ts` already understands a broader catalog:

- `approval`
- `userTask`
- `condition`
- `timer`
- `delay`
- `serviceTask`
- `notification`
- `subflow`
- `escalation`
- `parallelSplit`
- `parallelJoin`
- `exceptionHandler`
- `dataUpdate`

### Currently exposed toolbox types

`WorkflowDesigner.tsx` currently exposes only:

- `condition`
- `userTask`

That means the designer, mapper, and runtime are not yet aligned. This spec closes that gap in a controlled way.

## Target Enterprise Node Catalog

## Node Groups

### Flow Control

- `start`
- `end`
- `condition`
- `timer`
- `parallelSplit`
- `parallelJoin`

### Human Work

- `userTask`

Compatibility alias:

- `approval` remains supported as legacy input but should map to `userTask` with `taskMode = approval`

### System Work

- `serviceTask`
- `notification`
- `subflow`

### Governance And Recovery

- `escalation`
- `exceptionHandler`

## Canonical Enterprise Meaning

### `condition`

Use for branch decisions based on evaluated workflow state.

Recommended capabilities:

- expression-based routing
- decision-table routing later
- default/fallback path later

### `userTask`

Use for all human work routed into the task box.

Recommended task modes:

- `approval`
- `review`
- `dataEntry`
- `acknowledgement`
- `manualAction`
- `exception`

### `serviceTask`

Use for synchronous or asynchronous system execution.

Recommended process modes:

- `internal`
- `externalApi`
- `database`
- `dataUpdate`

### `timer`

Use for pause, deadline, and resumable wait behavior.

Recommended wait modes:

- `duration`
- `absoluteDate`
- `event`

### `notification`

Use for outbound communication that does not require a human task.

Recommended channels:

- `inApp`
- `email`
- `webhook`

### `escalation`

Use for SLA or policy-based escalation behavior.

Recommended outcomes:

- notify
- reassign
- increase priority
- branch to supervisor flow

### `parallelSplit`

Use to fan out execution into multiple concurrent branches.

### `parallelJoin`

Use to merge fan-out execution back into one flow.

Recommended join modes:

- `all`
- `any`
- `quorum`

### `subflow`

Use to invoke another workflow definition as a child workflow.

### `exceptionHandler`

Use to catch and recover from process errors, timeout errors, or integration failures.

Recommended recovery actions:

- retry
- fallback
- compensate
- terminate

## Canonical Node Envelope

All nodes should share a common base envelope.

```json
{
  "id": "financeReview",
  "type": "userTask",
  "name": "Finance Review",
  "description": "Validate budget and cost center.",
  "nodeCategory": "humanWork",
  "formKey": "finance-review",
  "permissionKey": "WF_FINANCE_REVIEW",
  "slaProfile": {
    "dueInHours": 8,
    "reminderInHours": 6,
    "escalationInHours": 12,
    "businessCalendar": true
  },
  "metadata": {}
}
```

Recommended common properties:

- `id`
- `type`
- `name`
- `description`
- `nodeCategory`
- `formKey`
- `permissionKey`
- `slaProfile`
- `metadata`

## Node-Specific Contracts

## Decision

```json
{
  "id": "amountDecision",
  "type": "condition",
  "name": "Amount Decision",
  "expression": "variables.amount > 5000",
  "metadata": {
    "expressionDialect": "simple"
  }
}
```

Required:

- exactly two branches in current runtime: `true`, `false`

Future extension:

- branch table model
- named outcomes
- default branch

## Human Task

```json
{
  "id": "managerApproval",
  "type": "userTask",
  "name": "Manager Approval",
  "assignment": {
    "mode": "role",
    "roleId": "00000000-0000-0000-0000-000000000001",
    "claimRequired": true,
    "allowDelegate": true,
    "allowReassign": true
  },
  "taskProfile": {
    "taskMode": "approval",
    "priority": "High",
    "formKey": "leave-approval",
    "listViewKey": "leave-approval-card",
    "detailViewKey": "leave-approval-detail",
    "actionSetKey": "approval-standard"
  },
  "dataContract": {
    "inputs": ["request.amount", "request.requester"],
    "outputs": ["decision.outcome", "decision.comment"]
  },
  "slaProfile": {
    "dueInHours": 24,
    "reminderInHours": 20,
    "escalationInHours": 30,
    "businessCalendar": true
  },
  "metadata": {
    "allowComments": true,
    "allowAttachments": true
  }
}
```

Initial runtime rule:

- exactly one assignment target required: user, group, or role

## Process Task

```json
{
  "id": "createErpRecord",
  "type": "serviceTask",
  "name": "Create ERP Record",
  "service": {
    "processMode": "externalApi",
    "serviceKey": "erp.createPurchaseRequest",
    "externalApiEndpointId": "endpoint-id",
    "inputMapping": "requestPayload",
    "outputMapping": "erpResponse",
    "retryPolicy": "standard",
    "timeoutHours": 1
  },
  "metadata": {
    "idempotencyKey": "workflow.id"
  }
}
```

Initial runtime rule:

- execution must go through a defined executor contract, not arbitrary script text

## Wait

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

Initial runtime modes:

- `duration`
- `absoluteDate`

Deferred:

- `event`

## Notification

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
  }
}
```

## Escalation

```json
{
  "id": "escalateManagerApproval",
  "type": "escalation",
  "name": "Escalate Manager Approval",
  "escalation": {
    "triggerCondition": "sla.overdue",
    "escalationTarget": "role:supervisor",
    "escalationMessage": "Approval SLA breached",
    "action": "reassign"
  }
}
```

Initial runtime recommendation:

- treat escalation as policy-based task mutation plus audit event
- do not make escalation a hidden special case inside the UI

## Parallel Split

```json
{
  "id": "parallelReview",
  "type": "parallelSplit",
  "name": "Parallel Review",
  "branchCount": 3,
  "metadata": {}
}
```

Runtime requirement:

- persist branch token state in `WorkflowInstanceNodes`

## Parallel Join

```json
{
  "id": "mergeParallelReview",
  "type": "parallelJoin",
  "name": "Merge Parallel Review",
  "joinMode": "all",
  "metadata": {}
}
```

Runtime requirement:

- wait for configured branch completion strategy before proceeding

## Subflow

```json
{
  "id": "financeSubflow",
  "type": "subflow",
  "name": "Finance Subflow",
  "subflow": {
    "childWorkflowKey": "finance-review",
    "versionMode": "latestPublished",
    "waitForCompletion": true,
    "inputMapping": "subflowInput",
    "outputMapping": "subflowOutput"
  }
}
```

Runtime requirement:

- parent-child workflow instance link
- input/output variable mapping

## Exception Handler

```json
{
  "id": "integrationFailureHandler",
  "type": "exceptionHandler",
  "name": "Integration Failure Handler",
  "exception": {
    "errorFilter": "ExternalApiTimeout",
    "recoveryAction": "retry",
    "fallbackProcessKey": "ops.manualRepair"
  }
}
```

Runtime requirement:

- catch scope by node or workflow branch
- explicit recovery contract

## Enterprise Task Box Recommendations

`userTask` is where the biggest business value sits. Build it as a configurable task box contract, not a hardcoded approve/reject modal.

### Core task-box features

- action-driven button bar
- comments
- assignment history
- delegate
- reassign
- claim / unclaim
- priority
- SLA badge
- reminder / escalation state
- form renderer by `formKey`
- list card renderer by `listViewKey`
- detail renderer by `detailViewKey`

### Suggested customizable task types

- approval card
- work item card
- exception card
- review card
- acknowledgment card

## Palette Recommendation

The workflow designer toolbox should be grouped like this:

### Flow Control

- `Decision`
- `Wait`
- `Parallel Split`
- `Parallel Join`

### Human Work

- `Human Task`

### System Work

- `Process Task`
- `Notification`
- `Subflow`

### Governance

- `Escalation`
- `Exception Handler`

## Runtime Support Matrix

### Phase 1: Production runtime now

- `condition`
- `userTask`
- `serviceTask`
- `timer`
- `notification`

### Phase 2: Designer-visible but runtime-gated

- `escalation`
- `subflow`

These can be added to the designer with validation warnings until runtime execution is completed.

### Phase 3: Full runtime enhancement

- `parallelSplit`
- `parallelJoin`
- `exceptionHandler`

These require execution-token and merge-state handling.

## Database And Runtime Changes Needed

## Human Task

Already mostly underway in current implementation:

- `WorkflowTasks`
- `WorkflowTaskComments`
- `WorkflowTaskAssignments`

Recommended additional persistence:

- `WorkflowTaskWatchers`
- `WorkflowTaskAttachments`

## Wait

Needs reliable timer persistence:

- `WorkflowTimers`
- support for event-based wait later

## Parallel

Needs branch execution state in:

- `WorkflowInstanceNodes`

Recommended additional fields:

- `ParentNodeId`
- `BranchKey`
- `JoinGroupId`
- `ExpectedBranchCount`
- `CompletedBranchCount`

## Subflow

Recommended additional persistence:

- child workflow instance reference on parent execution node
- subflow correlation fields

Suggested fields:

- `ChildWorkflowInstanceId`
- `ParentWorkflowInstanceId`
- `ParentNodeId`

## Exception Handling

Recommended additional persistence:

- execution error code
- retry count
- last exception payload

Potential location:

- `WorkflowInstanceNodes`
- `WorkflowOutbox`

## API Contract Changes Needed

## Designer / definition validation

Expand validation rules in `WorkflowDefinitionDocument` to support:

- `subflow`
- `escalation`
- `parallelSplit`
- `parallelJoin`
- `exceptionHandler`

### Task APIs

Keep building on current `/api/tasks/*` surface with:

- metadata-driven actions
- custom output validation by task action
- watcher endpoints
- attachment endpoints

### Runtime administration APIs

Recommended future endpoints:

- `GET /api/workflow-instances/{id}/nodes`
- `POST /api/workflow-instances/{id}/resume`
- `POST /api/workflow-instances/{id}/retry-node`
- `POST /api/workflow-instances/{id}/force-join`

## Suggested Delivery Order

Deliver in this sequence:

1. `Decision`
2. `Human Task`
3. `Process Task`
4. `Wait`
5. `Notification`
6. `Escalation`
7. `Subflow`
8. `Parallel Split`
9. `Parallel Join`
10. `Exception Handler`

Why this order:

- first five deliver immediate business workflow value
- escalation depends on mature SLA/task behavior
- subflow depends on stable child-instance orchestration
- parallel and exception handling require the biggest runtime-state expansion

## Immediate Next Implementation Slice

If the framework should move now, the next concrete slice should be:

1. Expand the designer toolbox to expose:
   - `Decision`
   - `Human Task`
   - `Process Task`
   - `Wait`
   - `Notification`
2. Expand `WorkflowDefinitionDocument` validation for:
   - `serviceTask`
   - `notification`
   - richer `timer` validation
3. Canonicalize `approval` -> `userTask`
4. Add structured task metadata sections:
   - assignment
   - taskProfile
   - dataContract
   - slaProfile
5. Keep:
   - `Escalation`
   - `Subflow`
   - `Parallel`
   - `Exception Handler`
   in mapper-compatible but runtime-gated mode until executor support lands

## Decision Summary

The right enterprise direction is:

- yes to `Decision`, `Human Task`, `Process Task`, `Wait`, `Escalation`, `Notification`, `Parallel Split`, `Parallel Join`, `Subflow`, and `Exception Handler`
- but not as ten unrelated special cases

The framework should evolve around:

- one canonical node envelope
- one rich `userTask` contract
- one explicit system executor contract
- persisted timer / branch / subflow / exception state

That gives you an enterprise workflow engine, not just a visual flow editor.
