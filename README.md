# LogicFlowEnterpriseFramework

Enterprise-grade .NET 8 modular monolith using Clean Architecture, ASP.NET Core Web API, EF Core, SQL Server, Identity, JWT authentication, role/permission authorization, multi-tenancy, audit fields, repository, and unit of work patterns.

## Codex Working Rule

Before making changes, Codex must:

- list the intended task(s) first
- get user confirmation before executing

Execution should only start after the user confirms the listed task.

## Projects

- `LogicFlowEnterpriseFramework.Api`
- `LogicFlowEnterpriseFramework.Application`
- `LogicFlowEnterpriseFramework.Domain`
- `LogicFlowEnterpriseFramework.Infrastructure`
- `LogicFlowEnterpriseFramework.Shared`
- `LogicFlowEnterpriseFramework.Blazor`

## Database

The default connection string targets a local SQL Server default instance:

```json
"Server=localhost;Database=LogicFlowEnterpriseFrameworkDb;Trusted_Connection=True;TrustServerCertificate=True"
```

Apply migrations:

```powershell
dotnet ef database update --project .\LogicFlowEnterpriseFramework.Infrastructure\LogicFlowEnterpriseFramework.Infrastructure.csproj --startup-project .\LogicFlowEnterpriseFramework.Api\LogicFlowEnterpriseFramework.Api.csproj
```

The API also runs migrations and seed data on startup.

## Company Profile Sync

The application keeps a local cache table `dbo.CompanyProfiles` and syncs it incrementally from the external company source.

The local tables are now part of the EF Core model and are created by normal application migrations on startup.

Two source modes are supported:

- `CompanyProfileSync:UseLocalSynonym=true`: read through `dbo.syn_Company` in the local application database.
- `CompanyProfileSync:UseLocalSynonym=false`: read directly from an external source connection defined in `ConnectionStrings:CompanyProfileSource` or `CompanyProfileSync:SourceConnectionString`.

If you want the SQL-only stored-procedure path as a separate utility, run `scripts/company-profile-sync.sql` against the local application database to create the helper procedure.

Then execute:

```sql
EXEC dbo.SyncCompanyProfilesFromSynonym;
```

That script creates:

- `dbo.SyncCompanyProfilesFromSynonym`

For the application-driven sync endpoint and scheduler, configure:

```json
"ConnectionStrings": {
  "CompanyProfileSource": ""
},
"CompanyProfileSync": {
  "UseLocalSynonym": true,
  "LocalSynonymName": "[dbo].[syn_Company]",
  "SourceConnectionStringName": "CompanyProfileSource",
  "SourceConnectionString": "",
  "SourceObjectName": "[dbo].[OSUSR_1sw_Company]",
  "ScheduleEnabled": false,
  "ScheduleMinutes": 60,
  "BatchSize": 1000
}
```

## Run

```powershell
dotnet run --project .\LogicFlowEnterpriseFramework.Api\LogicFlowEnterpriseFramework.Api.csproj
```

Swagger is enabled in development.

Run the full local stack:

```powershell
.\LogicFlowEnterpriseFramework.bat
```

The launcher starts:

- API: `http://localhost:5077`
- Swagger: `http://localhost:5077/swagger`
- Blazor frontend: `http://localhost:5088`

## Bootstrap Admin

- Email defaults to `admin@logicflow.local`
- Tenant: `Default Tenant`
- Role: `Admin`
- Initial password must be configured through `BootstrapAdmin:InitialPassword` before first installation

The bootstrap admin is created only if it does not already exist. Redeployments do not reset the password.

## Auth Endpoints

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh-token`
- `GET /api/auth/me`

## Permissions and Features

This project uses two related but different concepts:

- `FeatureCode`: controls UI/module availability
- `Permission`: controls API and action authorization
- `AspNetRoles`: coarse identity/user-type classification
- `PlatformRoleFeatures`: database-driven authorization mapping for application access

At runtime, permission checks behave like static code constants. For example, API endpoints use permission constants such as:

- `Permissions.ServiceCenterAccessRead`
- `Permissions.ServiceCenterAccessManage`

These are enforced through attributes such as:

```csharp
[HasPermission(Permissions.ServiceCenterAccessManage)]
```

When a user signs in, the authentication service collects the user's permission claims from role claims, then places them into the JWT using the `permission` claim type. API authorization then checks those JWT claims against the required permission policy.

The important implementation detail is that many permission constants are not handwritten directly in source. They are generated at build time from database-driven feature records:

- script: `scripts/GeneratePermissions.ps1`
- generated output: `obj/Debug/net8.0/Generated/FeatureCodeConstants.g.cs`

The intended workflow is feature-first:

- create or update the row in `PlatformFeatures`
- link the feature to `PlatformAccessRoles` through `PlatformRoleFeatures`
- build the solution so the generator emits the constant into `FeatureCodeConstants.g.cs`
- consume the generated `Permissions.*` or `PlatformFeatureCodes.*` constant from application code

Current direction:

- continue using `AspNetRoles` for sign-in identity buckets such as `Admin`, `Applicant`, or `Screening Officer`
- continue moving actual permission grants to `PlatformRoleFeatures`
- keep `AspNetRoleClaims` only as fallback/transition support while older flows are being migrated

Role alignment rule:

- every business role should exist in both `AspNetRoles` and `PlatformAccessRoles`
- the role name should match exactly in both places
- `AspNetRoles` answers who the user is
- `PlatformRoleFeatures` answers what the user can access
- users should be assigned into the platform path through `UserAccessGroupAssignments -> GroupAccessRoleAssignments -> PlatformRoleFeatures`
- legacy roles such as `Admin` may remain temporarily for bootstrap compatibility during migration

That means:

- runtime usage is constant/code-based
- the source of many permission codes is database-driven at build time

Relevant files:

- `LogicFlowEnterpriseFramework.Shared/Constants/Permissions.cs`
- `LogicFlowEnterpriseFramework.Api/Security/HasPermissionAttribute.cs`
- `LogicFlowEnterpriseFramework.Infrastructure/Identity/AuthService.cs`
- `scripts/GeneratePermissions.ps1`

## Security Defaults

- JWT issuer, audience, lifetime, signature, and minimum 32-byte secret are validated at startup.
- Refresh tokens are stored as SHA-256 hashes, not plaintext.
- Login uses Identity lockout after repeated failed attempts.
- Unknown endpoints require authentication by default through a fallback authorization policy.
- Auth endpoints and all API requests are rate limited.
- CORS is restricted through `Cors:AllowedOrigins`.
- Security headers are applied globally.
- Production error responses hide internal exception details.

For production, replace `Jwt:Secret` with a strong secret from a secure store such as Azure Key Vault, configure explicit CORS origins, and run behind HTTPS.
