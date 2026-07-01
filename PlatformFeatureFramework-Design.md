# Platform Feature Framework Design

## Goal

Introduce a shared framework capability model for:

- applications
- modules inside applications
- features inside modules
- groups / access profiles
- tenant-scoped user assignments

The model must support:

- central administration in Platform Service Center
- compile-time-safe consumption from other apps through a generated DLL
- runtime user/group assignment without rebuilding apps
- stable feature codes that do not break when display names change

## Core Principles

### 1. Definitions are source-controlled

Applications, modules, features, and default groups are defined in manifests inside the framework repo.

These definitions are not invented dynamically in production by admin users if the goal is to generate a shared DLL.

### 2. Assignments are data-driven

Service Center manages:

- which users can enter an application
- which groups they belong to inside that application
- which features are granted by those groups
- optional user-specific feature overrides

### 3. Code uses stable codes, not editable names

Every definition has:

- `Code`: stable, unique, used by code
- `Name`: editable display label
- `Description`: editable help text

Code must only reference `Code`.

### 4. UI and API enforcement still happens in app code

Service Center decides access data.

Apps must still implement:

- menu visibility
- page guards
- button enabled / disabled state
- API authorization

against shared feature-access services.

## Target Model

There are two layers.

### Definition layer

This layer is generated from manifests and synced into the database.

- `PlatformApplication`
- `PlatformModule`
- `PlatformFeature`
- `PlatformAccessGroup`
- `PlatformGroupFeature`

### Assignment layer

This layer is managed per tenant and per user at runtime.

- `UserApplicationAccess`
- `UserAccessGroupAssignment`
- `UserFeatureOverride` later if needed

## Recommended Entity Design

### Reuse existing entities

Keep and extend:

- `PlatformApplication`
- `UserApplicationAccess`

### New entities

#### `PlatformModule`

Purpose:

- register functional modules under an application

Suggested fields:

- `Id`
- `TenantId`
- `PlatformApplicationId`
- `Code`
- `Name`
- `Description`
- `DisplayOrder`
- `IsActive`

Rules:

- unique on `{ TenantId, PlatformApplicationId, Code }`
- code is immutable after creation

#### `PlatformFeature`

Purpose:

- stable feature capability under an application module

Suggested fields:

- `Id`
- `TenantId`
- `PlatformApplicationId`
- `PlatformModuleId`
- `Code`
- `Name`
- `Description`
- `Category`
- `DisplayOrder`
- `IsActive`
- `IsDeprecated`

Rules:

- unique on `{ TenantId, PlatformApplicationId, Code }`
- code is immutable after sync
- `Name` and `Description` may be edited
- `IsDeprecated` blocks new assignment but preserves compatibility during migration

#### `PlatformAccessGroup`

Purpose:

- reusable access profile inside an application or module

Suggested fields:

- `Id`
- `TenantId`
- `PlatformApplicationId`
- `PlatformModuleId` nullable
- `Code`
- `Name`
- `Description`
- `IsSystem`
- `IsActive`

Rules:

- unique on `{ TenantId, PlatformApplicationId, Code }`
- `IsSystem` groups come from manifests
- tenant admins may create tenant-specific groups later if desired

#### `PlatformGroupFeature`

Purpose:

- map a group to the features it grants

Suggested fields:

- `Id`
- `TenantId`
- `PlatformAccessGroupId`
- `PlatformFeatureId`
- `IsEnabled`

Rules:

- unique on `{ PlatformAccessGroupId, PlatformFeatureId }`

#### `UserAccessGroupAssignment`

Purpose:

- assign a user to one or more groups within an application

Suggested fields:

- `Id`
- `TenantId`
- `ApplicationUserId`
- `PlatformApplicationId`
- `PlatformAccessGroupId`
- `AssignedAt`
- `AssignedBy`
- `IsEnabled`

Rules:

- unique on `{ ApplicationUserId, PlatformAccessGroupId }`

#### `UserFeatureOverride` later

Purpose:

- exceptional per-user allow or deny

Suggested fields:

- `Id`
- `TenantId`
- `ApplicationUserId`
- `PlatformFeatureId`
- `Mode` (`Allow` or `Deny`)
- `Reason`
- `ExpiresAt`

This should be postponed until the base group model is working.

## Manifest Design

Each application should declare its own access model in source control.

Suggested location:

- `LogicFlowEnterpriseFramework.Shared/FeatureDefinitions/*.json`

Example file:

```json
{
  "applicationCode": "PLATFORM_SERVICE_CENTER",
  "applicationName": "Platform Service Center",
  "description": "Central platform administration and service operations.",
  "entryUrl": "/dashboard",
  "modules": [
    {
      "moduleCode": "ADMINISTRATION",
      "moduleName": "Administration",
      "description": "Administrative setup and access control.",
      "features": [
        {
          "code": "PSC_ADMIN_ACCESS_DASHBOARD",
          "name": "Access Dashboard",
          "description": "View the administration dashboard."
        },
        {
          "code": "PSC_ADMIN_CREATE_APPLICATION",
          "name": "Create Application",
          "description": "Create or register platform applications."
        }
      ],
      "groups": [
        {
          "code": "PSC_ADMIN_ADMIN",
          "name": "Admin",
          "features": [
            "PSC_ADMIN_ACCESS_DASHBOARD",
            "PSC_ADMIN_CREATE_APPLICATION"
          ]
        },
        {
          "code": "PSC_ADMIN_VIEWER",
          "name": "Viewer",
          "features": [
            "PSC_ADMIN_ACCESS_DASHBOARD"
          ]
        }
      ]
    }
  ]
}
```

## Generated DLL Design

The generator should produce strongly typed constants and metadata wrappers.

Suggested output project:

- `LogicFlowEnterpriseFramework.Shared`

Suggested generated file:

- `Generated/PlatformFeatureCatalog.g.cs`

Example output:

```csharp
namespace LogicFlowEnterpriseFramework.Shared.Generated;

public static class PlatformApps
{
    public static class PlatformServiceCenter
    {
        public const string Code = "PLATFORM_SERVICE_CENTER";

        public static class Modules
        {
            public static class Administration
            {
                public const string Code = "ADMINISTRATION";

                public static class Features
                {
                    public const string AccessDashboard = "PSC_ADMIN_ACCESS_DASHBOARD";
                    public const string CreateApplication = "PSC_ADMIN_CREATE_APPLICATION";
                }

                public static class Groups
                {
                    public const string Admin = "PSC_ADMIN_ADMIN";
                    public const string Viewer = "PSC_ADMIN_VIEWER";
                }
            }
        }
    }
}
```

This DLL should expose:

- application codes
- module codes
- feature codes
- group codes

It should not generate business logic.

## Consumption Pattern in Apps

Apps should call one generic framework service with generated constants.

Example:

```csharp
var canCreate = await featureAccessService.HasFeatureAsync(
    userId,
    PlatformApps.PlatformServiceCenter.Code,
    PlatformApps.PlatformServiceCenter.Modules.Administration.Features.CreateApplication,
    cancellationToken);
```

Examples in UI:

```razor
@if (_canAccessDashboard)
{
    <NavLink href="/dashboard">Dashboard</NavLink>
}

<button disabled="@(!_canCreateApplication)">Create Application</button>
```

Examples in API:

```csharp
[HasFeature(
    PlatformApps.PlatformServiceCenter.Code,
    PlatformApps.PlatformServiceCenter.Modules.Administration.Features.CreateApplication)]
```

## Shared Services

### `IPlatformDefinitionCatalogService`

Purpose:

- read synced application/module/feature/group definitions

Suggested methods:

- `GetApplicationsAsync()`
- `GetModulesAsync(applicationCode)`
- `GetFeaturesAsync(applicationCode, moduleCode)`
- `GetGroupsAsync(applicationCode, moduleCode)`

### `IPlatformFeatureAccessService`

Purpose:

- resolve runtime access for a user

Suggested methods:

- `HasApplicationAccessAsync(Guid userId, string applicationCode, CancellationToken cancellationToken = default)`
- `HasFeatureAsync(Guid userId, string applicationCode, string featureCode, CancellationToken cancellationToken = default)`
- `GetUserFeaturesAsync(Guid userId, string applicationCode, CancellationToken cancellationToken = default)`
- `GetUserGroupsAsync(Guid userId, string applicationCode, CancellationToken cancellationToken = default)`

### `IPlatformDefinitionSyncService`

Purpose:

- sync manifest definitions into database records

Suggested methods:

- `SyncDefinitionsAsync(CancellationToken cancellationToken = default)`

### `IPlatformAccessAdministrationService`

Purpose:

- central admin operations used by Service Center

Suggested methods:

- `AssignGroupsAsync(Guid userId, string applicationCode, IReadOnlyCollection<string> groupCodes, CancellationToken cancellationToken = default)`
- `GetUserApplicationAccessDetailAsync(Guid userId, string applicationCode, CancellationToken cancellationToken = default)`
- `CreateGroupAsync(...)` later if tenant custom groups are allowed

## API Surface

### Shared definition endpoints

Suggested route prefix:

- `/api/platform/catalog/*`

Endpoints:

- `GET /api/platform/catalog/applications`
- `GET /api/platform/catalog/applications/{applicationCode}/modules`
- `GET /api/platform/catalog/applications/{applicationCode}/features`
- `GET /api/platform/catalog/applications/{applicationCode}/groups`

### Shared access endpoints

Suggested route prefix:

- `/api/platform/access/*`

Endpoints:

- `GET /api/platform/access/users`
- `GET /api/platform/access/users/{userId}`
- `PUT /api/platform/access/users/{userId}/applications`
- `PUT /api/platform/access/users/{userId}/applications/{applicationCode}/groups`
- `GET /api/platform/access/users/{userId}/applications/{applicationCode}/features`

### Authorization endpoints later if needed

- `GET /api/platform/authorization/me/applications/{applicationCode}/features`

## Service Center UI Plan

The current access page should remain focused on user provisioning and assignment.

Add new pages:

### 1. Application Registry

Route suggestion:

- `/platform/catalog/applications`

Functions:

- view synced applications
- view entry URLs
- view module counts

### 2. Module Registry

Route suggestion:

- `/platform/catalog/modules`

Functions:

- view modules under each application

### 3. Feature Catalog

Route suggestion:

- `/platform/catalog/features`

Functions:

- view features by app/module
- edit display name and description
- activate / deactivate
- deprecate

### 4. Group Setup

Route suggestion:

- `/platform/catalog/groups`

Functions:

- view groups
- view feature membership
- later allow tenant custom groups if required

### 5. User Feature Assignment

Route suggestion:

- `/platform/access/users`

Functions:

- assign app access
- assign groups within app
- view effective features

## Database Sync Flow

### Build-time

1. Read manifests from source-controlled folder.
2. Generate `PlatformFeatureCatalog.g.cs`.
3. Compile shared DLL.

### Startup or deployment-time

1. Read the same manifest definitions.
2. Upsert `PlatformApplication`.
3. Upsert `PlatformModule`.
4. Upsert `PlatformFeature`.
5. Upsert system `PlatformAccessGroup`.
6. Upsert `PlatformGroupFeature`.
7. Mark removed definitions as deprecated or inactive, not immediately deleted.

## Feature Lifecycle Rules

### Rename

- `Name` may change safely
- `Code` must not change casually

### Deprecate

- prevent new assignment
- preserve existing runtime checks during transition

### Retire

- only after app code no longer uses it
- only after group/user assignments are removed

### Delete

- should be rare
- prefer soft delete / archive over hard delete

## Project Structure Recommendation

### `LogicFlowEnterpriseFramework.Domain`

Add entities:

- `PlatformModule.cs`
- `PlatformFeature.cs`
- `PlatformAccessGroup.cs`
- `PlatformGroupFeature.cs`
- `UserAccessGroupAssignment.cs`
- `UserFeatureOverride.cs` later

### `LogicFlowEnterpriseFramework.Application`

Add DTOs:

- `PlatformDefinitionDtos.cs`
- `PlatformFeatureAccessDtos.cs`
- `PlatformGroupAssignmentDtos.cs`

Add interfaces:

- `IPlatformDefinitionCatalogService.cs`
- `IPlatformFeatureAccessService.cs`
- `IPlatformDefinitionSyncService.cs`
- `IPlatformAccessAdministrationService.cs`

### `LogicFlowEnterpriseFramework.Infrastructure`

Add services:

- `PlatformDefinitionCatalogService.cs`
- `PlatformFeatureAccessService.cs`
- `PlatformDefinitionSyncService.cs`
- `PlatformAccessAdministrationService.cs`

Add generator or sync helpers:

- `FeatureDefinition/ManifestModels/*.cs`
- `FeatureDefinition/ManifestLoader.cs`
- `FeatureDefinition/DefinitionSyncEngine.cs`

### `LogicFlowEnterpriseFramework.Shared`

Add:

- `FeatureDefinitions/*.json`
- `Generated/PlatformFeatureCatalog.g.cs`

### `LogicFlowEnterpriseFramework.Api`

Add controllers:

- `Controllers/Platform/PlatformCatalogController.cs`
- `Controllers/Platform/PlatformAccessController.cs`
- `Security/HasFeatureAttribute.cs`
- `Security/FeatureAuthorizationHandler.cs`

### `LogicFlowEnterpriseFramework.PlatformServiceCenter.Blazor`

Add pages:

- `Components/Pages/PlatformCatalogApplications.razor`
- `Components/Pages/PlatformCatalogFeatures.razor`
- `Components/Pages/PlatformCatalogGroups.razor`
- extend `AccessManagement.razor` to assign groups per app

## Migration from Current Model

Current state:

- app access exists
- static permissions exist in code
- Service Center access has app-specific role/team/queue logic

Suggested migration:

### Phase 1

- keep `PlatformApplication` and `UserApplicationAccess`
- add modules, features, groups, group-feature assignments
- introduce generated catalog and sync pipeline

### Phase 2

- move Service Center feature checks from static `Permissions` constants toward generated feature codes
- keep current permission attributes working during transition

### Phase 3

- add group assignment UI
- expose effective feature resolution

### Phase 4

- optionally retire most hardcoded permission constants after all apps are migrated

## First Implementation Slice

Build the smallest useful vertical slice:

1. Add entities:
   - `PlatformModule`
   - `PlatformFeature`
   - `PlatformAccessGroup`
   - `PlatformGroupFeature`
   - `UserAccessGroupAssignment`
2. Add manifest files for Platform Service Center
3. Generate shared constants into `LogicFlowEnterpriseFramework.Shared`
4. Sync definitions into database on startup
5. Add `IPlatformFeatureAccessService`
6. Add read-only catalog page in Service Center for applications, modules, features, and groups

This gives the framework:

- a single source of truth for app/module/feature definitions
- compile-time-safe codes for all apps
- the base runtime service needed for visibility and enable/disable decisions

## Recommendation

Do not start with fully dynamic runtime-created features if a generated DLL is a hard requirement.

Use a controlled manifest-driven definition model, then let Service Center manage assignments and editable labels around those definitions.
