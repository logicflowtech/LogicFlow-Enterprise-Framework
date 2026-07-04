@echo off
setlocal

cd /d "%~dp0"

set API_URL=http://localhost:5077
set BLAZOR_URL=http://localhost:5088
set SWAGGER_URL=%API_URL%/swagger
set ASPNETCORE_ENVIRONMENT=Development

echo Stopping stale LogicFlowEnterpriseFramework processes...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$targets = Get-CimInstance Win32_Process | Where-Object { (($_.Name -eq 'dotnet.exe') -and $_.CommandLine -and ( $_.CommandLine -like '*LogicFlowEnterpriseFramework.Api*' -or $_.CommandLine -like '*LogicFlowEnterpriseFramework.Blazor*' )) -or ($_.Name -eq 'LogicFlowEnterpriseFramework.Api.exe') -or ($_.Name -eq 'LogicFlowEnterpriseFramework.Blazor.exe') };" ^
  "foreach ($target in $targets) { Write-Host ('Stopping process ' + $target.ProcessId + ': ' + $target.CommandLine); Stop-Process -Id $target.ProcessId -Force -ErrorAction SilentlyContinue }"

echo Checking whether app ports are already in use...
for /f "tokens=5" %%p in ('netstat -ano ^| findstr ":5077" ^| findstr "LISTENING"') do (
    echo Stopping existing API process on port 5077: %%p
    taskkill /PID %%p /F >nul 2>&1
)
for /f "tokens=5" %%p in ('netstat -ano ^| findstr ":5088" ^| findstr "LISTENING"') do (
    echo Stopping existing Blazor process on port 5088: %%p
    taskkill /PID %%p /F >nul 2>&1
)
echo Checking .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo .NET SDK was not found. Install .NET 8 SDK or newer, then run this file again.
    pause
    exit /b 1
)

echo.
echo Restoring packages...
dotnet restore ".\LogicFlowEnterpriseFramework.sln"
if errorlevel 1 (
    echo.
    echo Package restore failed.
    pause
    exit /b 1
)

echo.
echo Building solution...
dotnet build ".\LogicFlowEnterpriseFramework.sln" --no-restore
if errorlevel 1 (
    echo.
    echo Build failed.
    pause
    exit /b 1
)

echo Applying database migrations...
dotnet ef database update --project ".\LogicFlowEnterpriseFramework.Infrastructure\LogicFlowEnterpriseFramework.Infrastructure.csproj" --startup-project ".\LogicFlowEnterpriseFramework.Api\LogicFlowEnterpriseFramework.Api.csproj" --no-build
if errorlevel 1 (
    echo.
    echo Database migration failed. Fix the error above, then run this file again.
    pause
    exit /b 1
)

echo.
echo Starting LogicFlowEnterpriseFramework API at %API_URL% ...
start "LogicFlowEnterpriseFramework API" cmd /k "set ASPNETCORE_ENVIRONMENT=%ASPNETCORE_ENVIRONMENT% && dotnet run --project .\LogicFlowEnterpriseFramework.Api\LogicFlowEnterpriseFramework.Api.csproj --no-build --no-launch-profile --urls %API_URL%"

echo Starting LogicFlowEnterpriseFramework Blazor at %BLAZOR_URL% ...
start "LogicFlowEnterpriseFramework Blazor" cmd /k "set ASPNETCORE_ENVIRONMENT=%ASPNETCORE_ENVIRONMENT% && dotnet run --project .\LogicFlowEnterpriseFramework.Blazor\LogicFlowEnterpriseFramework.Blazor.csproj --no-build --no-launch-profile --urls %BLAZOR_URL%"

echo Waiting for app startup...
timeout /t 10 /nobreak >nul

echo Opening Blazor frontend: %BLAZOR_URL%
start "" "%BLAZOR_URL%"

echo.
echo Bootstrap admin login:
echo Email: admin@logicflow.local
echo Password: configured via BootstrapAdmin:InitialPassword
echo.
pause
