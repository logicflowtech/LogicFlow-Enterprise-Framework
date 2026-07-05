# Role-Based Dashboard Plan

## Objective

Introduce role-specific dashboards without regressing the current applicant dashboard or changing the current visual theme.

Current understanding:

- The existing [Dashboard.razor](C:/LogicFlow-Enterprise-Framework/LogicFlowEnterpriseFramework.Blazor/Components/Pages/Dashboard.razor) is effectively an applicant-facing dashboard.
- The current workspace feature registers only one dashboard route:
  - `/dashboard`
- The current theme and layout language should be preserved:
  - left selector rail
  - white `data-panel` sections
  - compact metric chips
  - current buttons
  - current `dashboard-table` styling
  - current green active company selection state

## Roles In Scope

Initial role-based dashboard split:

- `Applicant`
  - outsider / submitter
  - company-centric
  - focused on own applications and own company records
- `Screening Officer`
  - reviewer / validator
  - queue-centric
  - focused on assigned workload, document validation, and SLA

Future roles can be added later:

- `Approver`
- `Administrator`

## Non-Regression Rules

These rules must be kept during implementation:

- Do not redesign the current applicant dashboard while splitting it.
- Do not change the current shell, header, or navigation style.
- Do not introduce a new design system for officer pages.
- Reuse existing dashboard CSS primitives where possible:
  - `data-panel`
  - `dashboard-company-frame`
  - `dashboard-table`
  - `dashboard-metrics-row`
  - `dashboard-metric-chip`
  - existing button classes
- Keep current applicant behavior intact until role routing is proven stable.

## Target Routes

Recommended route structure:

- `/dashboard`
  - role resolver only
- `/dashboard/applicant`
  - current dashboard behavior
- `/dashboard/screening-officer`
  - new operational dashboard

Recommended menu behavior:

- Keep the menu item label as `Dashboard`
- Keep the menu target at `/dashboard`
- Let `/dashboard` redirect based on role

This avoids menu duplication and keeps the user entry point stable.

## Current Page Mapping

Current workspace registration is in:

- [WorkspaceFeature.cs](C:/LogicFlow-Enterprise-Framework/LogicFlowEnterpriseFramework.Blazor/Features/Workspace/WorkspaceFeature.cs)

Current page registration:

- `typeof(Dashboard), "/dashboard"`

Recommended future registration:

- `typeof(Dashboard), "/dashboard"` as role resolver
- `typeof(ApplicantDashboard), "/dashboard/applicant"`
- `typeof(ScreeningOfficerDashboard), "/dashboard/screening-officer"`

## Applicant Dashboard Definition

Use the current dashboard as the applicant baseline.

### Purpose

- Continue applications
- Review own company records
- Review own application records
- Review contacts and authorised persons linked to assigned companies

### Information Architecture

Top:

- `My Tasks`

Main section:

- left: `Company Selection`
- right: selected company details

Current applicant sections to keep:

- company selector
- company header
- applications table
- authorised persons table
- company login contacts table

Optional future applicant widgets:

- `Required Documents`
- `Returned for Amendment`
- `Recent Activity`
- `Continue Draft`

### What Should Not Exist On Applicant Dashboard

- officer workload queue
- unassigned work queue
- SLA review management
- team validation load
- cross-company screening workload

## Screening Officer Dashboard Definition

Create this as a new page. Do not derive its layout from the applicant selector pattern.

### Purpose

- Validate submissions
- Review documents
- Process assigned workload
- Manage queue and SLA

### Core Layout

Top:

- queue summary chips

Main:

- operational queue layout
- no company selector as the primary left rail

Suggested structure:

- top metrics strip
- main left column: primary queues
- main right column: supporting panels

### Recommended Sections

Primary:

- `Assigned Cases`
- `Unassigned Queue`
- `Documents Pending Validation`

Supporting:

- `Overdue / SLA Risk`
- `Recent Decisions`
- `Quick Filters`

### Recommended Summary Chips

- `Assigned`
- `Pending Validation`
- `Due Today`
- `Overdue`

### Recommended Assigned Cases Columns

- `Application No.`
- `Company`
- `Application Type`
- `Submitted Date`
- `Current Step`
- `Due / SLA`
- `Status`
- `Action`

## Shared Components To Extract

The current dashboard should be refactored into reusable components before major routing changes.

Recommended applicant/shared components:

- `DashboardTaskStrip.razor`
- `CompanySelectorPanel.razor`
- `CompanyWorkspaceHeader.razor`
- `CompanyApplicationsTable.razor`
- `CompanyAuthorisedPersonsTable.razor`
- `CompanyLoginContactsTable.razor`

Recommended screening components:

- `QueueMetricsStrip.razor`
- `AssignedCasesTable.razor`
- `ValidationQueuePanel.razor`
- `SlaRiskPanel.razor`
- `RecentDecisionsPanel.razor`

## Role Resolution Strategy

There is no dedicated dashboard resolver yet. One should be introduced at `/dashboard`.

### Recommended Resolver Rule

Initial rule:

1. If the user has a screening officer role, redirect to `/dashboard/screening-officer`
2. Otherwise, redirect to `/dashboard/applicant`

### Important Note

Current code shows generic role usage through:

- `Session.User?.Roles`

But this repository does not currently show a confirmed built-in screening officer role name.

That means the actual role mapping must be confirmed first, for example:

- `Screening Officer`
- `ScreeningOfficer`
- `Officer`

This should be centralized in a small dashboard role resolver helper instead of repeated string checks inside pages.

## Implementation Phases

### Phase 1: Freeze Current Applicant Dashboard

Goal:

- protect current dashboard behavior before role split

Tasks:

- keep [Dashboard.razor](C:/LogicFlow-Enterprise-Framework/LogicFlowEnterpriseFramework.Blazor/Components/Pages/Dashboard.razor) unchanged visually
- treat it as applicant-only behavior
- avoid mixing officer features into it

### Phase 2: Extract Reusable Applicant Components

Goal:

- split the current dashboard into components without changing UI

Tasks:

- move current `My Tasks` block into a component
- move company selector into a component
- move applications table into a component
- move authorised persons and login contacts into components
- keep current CSS classes and markup style as much as possible

### Phase 3: Introduce `ApplicantDashboard`

Goal:

- create `/dashboard/applicant`

Tasks:

- compose extracted applicant components into a dedicated applicant page
- verify the rendered UI matches the current dashboard

### Phase 4: Turn `/dashboard` Into Resolver

Goal:

- keep user entry stable while supporting multiple dashboard types

Tasks:

- convert `Dashboard.razor` into a redirect/resolver page
- detect current role from `Session.User?.Roles`
- redirect to the appropriate dashboard route

### Phase 5: Create `ScreeningOfficerDashboard`

Goal:

- add a queue-first officer dashboard using the current theme

Tasks:

- create the page with the same shell and visual primitives
- use compact chips, white panels, and existing table styling
- scaffold sections with real data where already available
- avoid company-selector-first layout

### Phase 6: Validate With Business Users

Goal:

- confirm page composition before deeper buildout

Validation targets:

- one applicant walkthrough
- one screening officer walkthrough

Questions to validate:

- are the top 3 tasks on the dashboard the right ones
- should officer queue show only assigned items or also unassigned pool
- is company context needed as a filter or only as a column

## Recommended Technical Changes

### Pages

Add:

- `ApplicantDashboard.razor`
- `ScreeningOfficerDashboard.razor`

Refactor:

- `Dashboard.razor` becomes resolver after applicant page is stable

### Feature Registration

Update [WorkspaceFeature.cs](C:/LogicFlow-Enterprise-Framework/LogicFlowEnterpriseFramework.Blazor/Features/Workspace/WorkspaceFeature.cs) to register:

- `/dashboard`
- `/dashboard/applicant`
- `/dashboard/screening-officer`

Keep menu entry:

- `Dashboard -> /dashboard`

### Role Resolver Helper

Add a small helper/service, for example:

- `DashboardRoleResolver`

Responsibilities:

- map role names to dashboard routes
- keep role naming rules in one place

## Data Strategy

### Applicant Dashboard

Already supported by current data:

- company profiles
- IRPM company profile
- company financial statements as applications
- workflow tasks strip

### Screening Officer Dashboard

Likely first reusable source:

- `WorkflowOperationsClient.GetMyTasksAsync()`

Additional queue sources may be needed later if officers need:

- unassigned queue
- case status counts
- document validation counts
- team queue visibility

If those APIs do not exist yet, start with:

- assigned task view only
- compact queue-first dashboard

Then expand after layout approval.

## Recommended Build Order

1. Extract current applicant dashboard into components with no visual change
2. Create `ApplicantDashboard.razor`
3. Register `/dashboard/applicant`
4. Create `ScreeningOfficerDashboard.razor` scaffold with current theme
5. Register `/dashboard/screening-officer`
6. Convert `/dashboard` into role resolver
7. Validate role mapping names with actual user roles
8. Add deeper officer data sources only after layout is approved

## Immediate Next Step

Best next implementation step:

- extract the current dashboard into applicant components and introduce `/dashboard/applicant` first

That preserves the current page, avoids theme regression, and creates the right base for officer-specific dashboards.
