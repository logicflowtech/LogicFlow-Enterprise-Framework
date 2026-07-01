# LogicFlowEnterpriseFramework

Enterprise-grade .NET 8 modular monolith using Clean Architecture, ASP.NET Core Web API, EF Core, SQL Server, Identity, JWT authentication, role/permission authorization, multi-tenancy, audit fields, repository, and unit of work patterns.

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
