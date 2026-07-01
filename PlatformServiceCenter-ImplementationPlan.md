# Platform Service Center Implementation Plan

## Goal

Add a second frontend application, `Platform Service Center`, on top of the existing platform foundation:

- shared API host
- shared identity and JWT authentication
- shared tenant model
- shared role and permission infrastructure
- app-specific modules, configuration, and workflows

The intent is to keep the current solution as a modular monolith and avoid duplicating platform concerns.

## Solution Shape

Add one new frontend project first:

- `LogicFlowEnterpriseFramework.PlatformServiceCenter.Blazor`

Keep these projects shared:

- `LogicFlowEnterpriseFramework.Api`
- `LogicFlowEnterpriseFramework.Application`
- `LogicFlowEnterpriseFramework.Domain`
- `LogicFlowEnterpriseFramework.Infrastructure`
- `LogicFlowEnterpriseFramework.Shared`

If the Service Center domain grows substantially, split it into dedicated modules inside the existing backend projects before creating extra class library projects.

## Recommended Project and Folder Layout

### New frontend project

`LogicFlowEnterpriseFramework.PlatformServiceCenter.Blazor`

Suggested folders:

- `Components/App.razor`
- `Components/Routes.razor`
- `Components/Layout/MainLayout.razor`
- `Components/Layout/LoginLayout.razor`
- `Components/Layout/NavMenu.razor`
- `Components/Layout/SessionRestorer.razor`
- `Components/Pages/Dashboard.razor`
- `Components/Pages/Requests.razor`
- `Components/Pages/RequestWorkspace.razor`
- `Components/Pages/Queues.razor`
- `Components/Pages/AccessManagement.razor`
- `Components/Pages/SlaMonitor.razor`
- `Components/Pages/Reports.razor`
- `Components/Pages/Configuration/*.razor`
- `Services/AuthSession.cs`
- `Services/PlatformServiceCenterApiClient.cs`
- `Models/*.cs`
- `wwwroot/app.css`

### Backend folders inside existing projects

`LogicFlowEnterpriseFramework.Domain`

- `Entities/ServiceCenter/ServiceRequest.cs`
- `Entities/ServiceCenter/ServiceRequestComment.cs`
- `Entities/ServiceCenter/ServiceRequestAttachment.cs`
- `Entities/ServiceCenter/RequestCategory.cs`
- `Entities/ServiceCenter/RequestSubcategory.cs`
- `Entities/ServiceCenter/RequestPriority.cs`
- `Entities/ServiceCenter/RequestStatus.cs`
- `Entities/ServiceCenter/WorkflowTransition.cs`
- `Entities/ServiceCenter/QueueDefinition.cs`
- `Entities/ServiceCenter/QueueAssignmentRule.cs`
- `Entities/ServiceCenter/SlaPolicy.cs`
- `Entities/ServiceCenter/BusinessCalendar.cs`
- `Entities/ServiceCenter/NotificationTemplate.cs`
- `Entities/ServiceCenter/ServiceTeam.cs`
- `Entities/ServiceCenter/ServiceCenterUserAccess.cs`
- `Entities/ServiceCenter/ServiceQueueMembership.cs`
- `Entities/ServiceCenter/EscalationAssignment.cs`
- `Entities/ServiceCenter/FeatureFlag.cs`
- `Enums/ServiceCenter/*.cs`

`LogicFlowEnterpriseFramework.Application`

- `DTOs/ServiceCenter/*.cs`
- `Interfaces/ServiceCenter/*.cs`
- `Validators/ServiceCenter/*.cs`
- `Services/ServiceCenter/*.cs`

`LogicFlowEnterpriseFramework.Infrastructure`

- `Persistence/Configurations/ServiceCenter/*.cs`
- `Repositories/ServiceCenter/*.cs` only if specialized query logic is needed
- `Services/ServiceCenter/*.cs`

`LogicFlowEnterpriseFramework.Api`

- `Controllers/ServiceCenter/RequestsController.cs`
- `Controllers/ServiceCenter/QueuesController.cs`
- `Controllers/ServiceCenter/AccessManagementController.cs`
- `Controllers/ServiceCenter/ReportsController.cs`
- `Controllers/ServiceCenter/Configuration/*.cs`

## Core Domain Modules

### 1. Request Management

Purpose:

- manage service requests from creation to closure

Core entities:

- `ServiceRequest`
- `ServiceRequestComment`
- `ServiceRequestAttachment`
- `ServiceRequestAuditEvent`

Key fields for `ServiceRequest`:

- `Id`
- `RequestNo`
- `TenantId`
- `Title`
- `Description`
- `CategoryId`
- `SubcategoryId`
- `PriorityId`
- `StatusId`
- `Channel`
- `RequesterName`
- `RequesterEmail`
- `AssignedQueueId`
- `AssignedUserId`
- `DueAt`
- `ResolvedAt`
- `ClosedAt`
- `SourceApp`
- `IsDeleted`

### 2. Configuration Center

Purpose:

- allow admins to configure the app without redeploying

Core entities:

- `RequestCategory`
- `RequestSubcategory`
- `RequestPriority`
- `RequestStatus`
- `WorkflowTransition`
- `QueueDefinition`
- `QueueAssignmentRule`
- `SlaPolicy`
- `BusinessCalendar`
- `NotificationTemplate`
- `ServiceTeam`
- `FeatureFlag`

### 3. Queue and Assignment

Purpose:

- route work to the right team or officer

Core entities:

- `QueueDefinition`
- `QueueAssignmentRule`
- `ServiceTeam`

### 4. SLA and Escalation

Purpose:

- calculate due dates, warning thresholds, and breaches

Core entities:

- `SlaPolicy`
- `BusinessCalendar`
- `EscalationRule`

### 5. Reporting and Audit

Purpose:

- operational reporting, audit trail, and management dashboards

Core entities:

- `ServiceRequestAuditEvent`
- `SavedReport`
- `DashboardPreference`

### 6. Access Management

Purpose:

- manage Service Center access without duplicating the platform identity system

Scope:

- assign Service Center roles to existing platform users
- map users to teams and queues
- control supervisor and escalation ownership
- activate or deactivate Service Center access
- review app-scoped access and audit history

Out of scope:

- global user registration
- password reset and credential policy
- tenant creation
- global identity security administration

Core entities:

- `ServiceCenterUserAccess`
- `ServiceQueueMembership`
- `EscalationAssignment`

## Configuration That Belongs In The App

These should be data-driven configuration screens inside Platform Service Center:

- request categories and subcategories
- request statuses
- allowed workflow transitions
- queue definitions
- routing and assignment rules
- SLA policies
- business hours and holiday calendar
- notification templates
- service teams
- access-management rules for teams, queues, and escalations
- dashboard widget visibility
- feature flags for app modules

These should stay as environment or deployment configuration:

- connection strings
- JWT secrets
- SMTP/API credentials
- storage credentials
- CORS origins
- API base URLs
- bootstrap admin password

## Frontend Page Plan

### Phase 1 pages

- `/login`
- `/dashboard`
- `/requests`
- `/requests/{id}`
- `/queues`
- `/access-management`
- `/configuration/categories`
- `/configuration/statuses`
- `/configuration/workflows`
- `/configuration/queues`
- `/configuration/sla`
- `/configuration/notifications`
- `/configuration/teams`
- `/configuration/feature-flags`

### Phase 2 pages

- `/sla-monitor`
- `/reports`
- `/audit`
- `/knowledge-base`
- `/integration-monitor`

## API Surface

All endpoints should sit under:

- `/api/service-center/*`

### Request APIs

- `GET /api/service-center/requests`
- `GET /api/service-center/requests/{id}`
- `POST /api/service-center/requests`
- `PUT /api/service-center/requests/{id}`
- `POST /api/service-center/requests/{id}/assign`
- `POST /api/service-center/requests/{id}/transition`
- `POST /api/service-center/requests/{id}/comment`
- `POST /api/service-center/requests/{id}/attachments`
- `POST /api/service-center/requests/{id}/close`
- `POST /api/service-center/requests/bulk/assign`
- `POST /api/service-center/requests/bulk/transition`
- `GET /api/service-center/requests/export`

### Queue APIs

- `GET /api/service-center/queues`
- `GET /api/service-center/queues/{id}/items`
- `POST /api/service-center/queues/rebalance`

### Access Management APIs

- `GET /api/service-center/access/users`
- `GET /api/service-center/access/users/{userId}`
- `PUT /api/service-center/access/users/{userId}`
- `GET /api/service-center/access/teams`
- `GET /api/service-center/access/queues`
- `POST /api/service-center/access/users/{userId}/queues`
- `POST /api/service-center/access/users/{userId}/roles`
- `POST /api/service-center/access/users/{userId}/escalation`
- `GET /api/service-center/access/audit`

### SLA APIs

- `GET /api/service-center/sla/monitor`
- `GET /api/service-center/sla/policies`
- `POST /api/service-center/sla/policies`
- `PUT /api/service-center/sla/policies/{id}`

### Configuration APIs

- `GET /api/service-center/config/categories`
- `POST /api/service-center/config/categories`
- `PUT /api/service-center/config/categories/{id}`
- `GET /api/service-center/config/subcategories`
- `GET /api/service-center/config/statuses`
- `POST /api/service-center/config/statuses`
- `GET /api/service-center/config/workflows`
- `POST /api/service-center/config/workflows`
- `GET /api/service-center/config/queues`
- `POST /api/service-center/config/queues`
- `GET /api/service-center/config/assignment-rules`
- `POST /api/service-center/config/assignment-rules`
- `GET /api/service-center/config/notification-templates`
- `POST /api/service-center/config/notification-templates`
- `GET /api/service-center/config/teams`
- `POST /api/service-center/config/teams`
- `GET /api/service-center/config/feature-flags`
- `PUT /api/service-center/config/feature-flags/{key}`

### Reporting APIs

- `GET /api/service-center/reports/dashboard`
- `GET /api/service-center/reports/aging`
- `GET /api/service-center/reports/sla-breach`
- `GET /api/service-center/reports/throughput`
- `GET /api/service-center/reports/export`

## Permissions

Add a dedicated Service Center permission group in `Shared.Constants.Permissions`, for example:

- `ServiceCenterRequestsRead`
- `ServiceCenterRequestsCreate`
- `ServiceCenterRequestsEdit`
- `ServiceCenterRequestsAssign`
- `ServiceCenterRequestsTransition`
- `ServiceCenterRequestsClose`
- `ServiceCenterQueuesRead`
- `ServiceCenterAccessRead`
- `ServiceCenterAccessManage`
- `ServiceCenterSlaRead`
- `ServiceCenterSlaManage`
- `ServiceCenterConfigRead`
- `ServiceCenterConfigManage`
- `ServiceCenterReportsRead`
- `ServiceCenterAuditRead`

Recommended roles:

- `PlatformServiceCenterAdmin`
- `PlatformServiceCenterSupervisor`
- `PlatformServiceCenterOfficer`
- `PlatformServiceCenterViewer`

Recommended access rules:

- only `PlatformServiceCenterAdmin` can manage app-wide access
- supervisors can view team membership and queue ownership
- officers cannot assign roles to other users
- central platform admins remain the owner of global identity lifecycle

## Database Plan

Add EF Core entities and configurations into the existing `ApplicationDbContext`.

Recommended rules:

- every Service Center entity inherits `BaseEntity`
- all tenant-owned entities must enforce tenant filtering
- all list entities should support `IsActive`
- workflow configuration should use stable keys, not display text
- avoid storing derived SLA states that can be calculated unless needed for reporting

New tables expected:

- `ServiceRequests`
- `ServiceRequestComments`
- `ServiceRequestAttachments`
- `ServiceRequestAuditEvents`
- `RequestCategories`
- `RequestSubcategories`
- `RequestPriorities`
- `RequestStatuses`
- `WorkflowTransitions`
- `QueueDefinitions`
- `QueueAssignmentRules`
- `SlaPolicies`
- `BusinessCalendars`
- `NotificationTemplates`
- `ServiceTeams`
- `ServiceCenterUserAccesses`
- `ServiceQueueMemberships`
- `EscalationAssignments`
- `FeatureFlags`

## Service Layer Plan

Add service interfaces in Application:

- `IServiceRequestService`
- `IQueueService`
- `IAccessManagementService`
- `ISlaPolicyService`
- `IServiceCenterConfigurationService`
- `IServiceCenterReportService`

Recommended responsibilities:

- validation and business rules in Application
- persistence and query execution in Infrastructure
- auth, permission, and HTTP contract in API

## Recommended Delivery Order

### Milestone 1: App Shell

- create `LogicFlowEnterpriseFramework.PlatformServiceCenter.Blazor`
- reuse auth session pattern
- add layouts, top nav, login, dashboard shell
- add permission constants and roles

### Milestone 2: Configuration Backbone

- categories
- statuses
- priorities
- teams
- queues
- workflow transitions
- access-management model for Service Center roles, queue membership, and escalation ownership

### Milestone 3: Request Operations

- request list
- request workspace
- assignment
- status transitions
- comments
- attachments

### Milestone 4: SLA

- SLA policies
- due date calculation
- warning and breach views
- escalation hooks

### Milestone 5: Reporting

- dashboard KPIs
- aging report
- SLA breach report
- throughput report
- export

## Suggested First Build Scope

If we want the smallest useful first release, build these first:

1. new Blazor app shell
2. request categories
3. request statuses
4. workflow transitions
5. queue definitions
6. request list and request detail workspace
7. assignment and transition actions
8. SLA policy configuration

That gives a usable operational Service Center without overbuilding reporting or integrations first.

## Access Management Recommendation

Include `Access Management` inside Platform Service Center, but keep it app-scoped.

It should manage:

- Service Center role assignment
- team assignment
- queue assignment
- escalation ownership
- app-level activation and deactivation

It should not manage:

- password lifecycle
- tenant provisioning
- central user registration
- JWT or security policy
