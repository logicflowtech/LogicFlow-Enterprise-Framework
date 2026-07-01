# Workflow Designer App

Embedded React/Vite workflow designer and workflow operations UI for `LogicFlowEnterpriseFramework.Blazor`.

## Current Scope

This app is the active workflow frontend used by the Blazor host.

Use `LogicFlow-Enterprise-Framework` as the only active workflow source for development.

Current scope:

- Select acting user.
- View pending and claimed task inbox.
- Inspect workflow instance detail.
- Claim and unclaim tasks.
- Approve and reject tasks.
- Cancel running workflow instances.
- Review tasks, variables, and audit timeline.

## Local Development

Start the API first:

```powershell
dotnet run --project ..\..\LogicFlowEnterpriseFramework.Api\LogicFlowEnterpriseFramework.Api.csproj --urls http://localhost:5116
```

Start the web app:

```powershell
npm run dev -- --host 127.0.0.1 --port 5173
```

Open:

```text
http://127.0.0.1:5173
```

Vite proxies `/api` to:

```text
http://localhost:5116
```

For a different backend URL, set:

```text
VITE_API_BASE_URL=http://localhost:5116
```
