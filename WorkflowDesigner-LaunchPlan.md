# Workflow Designer Launch Plan

## Objective

Launch the embedded workflow designer inside `LogicFlowEnterpriseFramework` with:

- Blazor as the shell and navigation surface
- `LogicFlowEnterpriseFramework.Api` as the workflow API
- MSSQL as the system of record
- `sequential-workflow-designer` as the visual designer engine

## Current State

The framework already contains an embedded workflow designer implementation:

- Blazor hosts the React/Vite app from `LogicFlowEnterpriseFramework.Blazor/wwwroot/workflow-designer`
- The React app already uses `sequential-workflow-designer`
- The API already supports workflow definitions, drafts, versions, instances, tasks, timers, audit logs, and outbox processing
- Background processing for timers and outbox is already registered in the API

This means the remaining work is consolidation and hardening, not initial integration.

## Key Decisions

### 1. Single Source of Truth

Treat these projects as the production workflow stack:

- `LogicFlowEnterpriseFramework.Blazor`
- `LogicFlowEnterpriseFramework.Api`
- `LogicFlowEnterpriseFramework.Infrastructure`
- `LogicFlowEnterpriseFramework.Domain`

Use this framework repository as the single workflow source of truth. Do not continue parallel workflow feature development elsewhere.

### 2. Runtime-Owned Contract

The workflow JSON contract must be owned by the framework runtime, not by the standalone spike.

Authoritative contract layers:

- Designer mapping: `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/workflowDefinitionMapper.ts`
- Runtime validation: `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowDefinitionDocument.cs`
- Runtime execution: `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowRuntimeService.cs`

Any new node type must be added to all three before it is considered supported.

### 3. Strict V1 Node Scope

Supported for launch:

- `start`
- `end`
- `userTask`
- `condition`
- `timer` or `delay` only after final UI and runtime verification
- `notification` only after background delivery integration is complete
- `serviceTask` only after concrete execution behavior is finalized

Not for launch:

- `subflow`
- `parallelSplit`
- `parallelJoin`
- `escalation`
- `exceptionHandler`
- any node that validates or maps but does not execute cleanly in the runtime

## Delivery Phases

### Phase 1. Consolidate the Embedded Stack

Goal:
Make the framework implementation the only active workflow product.

Tasks:

- Keep repository docs aligned to `LogicFlow-Enterprise-Framework` as the only active workflow codebase
- Point all workflow development guidance to `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp`
- Remove references that imply the standalone app is still the active product

Exit criteria:

- Developers can identify one production workflow frontend and one production API without ambiguity

### Phase 2. Harden the Host Boundary

Goal:
Make the embedded React app behave like a hosted module, not a separate web app.

Tasks:

- Keep workflow host session in memory instead of persisting it in `localStorage`
- Continue using Blazor authentication/session as the source of truth
- Keep asset versioning on hosted workflow files
- Ensure the React mount/unmount lifecycle is deterministic from Blazor pages

Exit criteria:

- The workflow host token is not stored in browser `localStorage`
- Refresh behavior is intentional and documented

### Phase 3. Freeze the V1 Contract

Goal:
Prevent designer/runtime drift.

Tasks:

- Reduce the exposed toolbox to only node types supported in runtime
- Align validation rules with actual runtime behavior
- Add a supported-node matrix to workflow documentation
- Reject unsupported nodes at publish time, not after instance start

Exit criteria:

- Every node available in the toolbox can be published and executed
- No hidden runtime-only or designer-only node types remain in active use

### Phase 4. Add Definition Concurrency Controls

Goal:
Prevent silent overwrites on draft editing and publish.

Tasks:

- Use `RowVersion` on `WorkflowDefinition` and `WorkflowDraft` in update and publish flows
- Return conflict responses when a stale editor saves over newer work
- Surface conflict handling in the React designer UX

Exit criteria:

- Two editors cannot silently overwrite each other

### Phase 5. Complete Launch-Safe Runtime Behavior

Goal:
Guarantee that published workflows execute consistently in MSSQL-backed runtime state.

Tasks:

- Verify task creation, task completion, timer processing, notification queueing, and audit logging end to end
- Decide whether `serviceTask` is synchronous no-op, external call, or disabled for launch
- Verify background processor settings in non-development environments
- Confirm tenant and access filtering behavior for workflow instances and tasks

Exit criteria:

- All launch-supported node types pass end-to-end execution tests

### Phase 6. Operational Readiness

Goal:
Make the workflow feature supportable after release.

Tasks:

- Add launch runbook for workflow API, background processing, and seed data
- Document required app settings for JWT, CORS, database, and workflow background processing
- Add smoke-test checklist for create, save draft, validate, publish, start, approve, complete, cancel

Exit criteria:

- Another developer can deploy and validate the workflow module without reverse engineering the repo

## Recommended Immediate Backlog

1. Remove workflow host session persistence from `localStorage`
2. Freeze the toolbox to launch-supported nodes only
3. Add concurrency tokens to draft save and publish APIs
4. Add end-to-end tests for create, publish, start, complete, timer, and cancel flows
5. Update repository docs so the standalone workflow app is clearly non-authoritative

## Files To Touch Next

- `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/WorkflowDesigner.tsx`
- `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/workflowDefinitionMapper.ts`
- `LogicFlowEnterpriseFramework.Blazor/WorkflowDesignerApp/src/api.ts`
- `LogicFlowEnterpriseFramework.Api/Controllers/Workflow/WorkflowDefinitionsController.cs`
- `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowDefinitionDocument.cs`
- `LogicFlowEnterpriseFramework.Api/Workflow/Runtime/WorkflowRuntimeService.cs`
- `LogicFlowEnterpriseFramework.Domain/Entities/Workflow/WorkflowDefinition.cs`
- `LogicFlowEnterpriseFramework.Domain/Entities/Workflow/WorkflowDraft.cs`

## Definition of Done for Launch

The workflow designer is launch-ready when:

- the embedded Blazor host is the only active workflow frontend
- the published toolbox matches the runtime-supported nodes
- draft save/publish is concurrency-safe
- workflow execution passes end-to-end against MSSQL
- authentication and task access follow the framework security model
- workflow host credentials are not persisted in browser storage
