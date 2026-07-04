@echo off
setlocal

cd /d "%~dp0"

set ASPNETCORE_ENVIRONMENT=Development

echo Stopping stale LogicFlowEnterpriseFramework API processes...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$targets = Get-CimInstance Win32_Process | Where-Object { (($_.Name -eq 'dotnet.exe') -and $_.CommandLine -and $_.CommandLine -like '*LogicFlowEnterpriseFramework.Api*') -or ($_.Name -eq 'LogicFlowEnterpriseFramework.Api.exe') };" ^
  "foreach ($target in $targets) { Write-Host ('Stopping process ' + $target.ProcessId + ': ' + $target.CommandLine); Stop-Process -Id $target.ProcessId -Force -ErrorAction SilentlyContinue }"

echo Checking whether port 5077 is already in use...
for /f "tokens=5" %%p in ('netstat -ano ^| findstr ":5077" ^| findstr "LISTENING"') do (
    echo Stopping existing process on port 5077: %%p
    taskkill /PID %%p /F >nul 2>&1
)

echo Applying database migrations...
dotnet ef database update --project ".\LogicFlowEnterpriseFramework.Infrastructure\LogicFlowEnterpriseFramework.Infrastructure.csproj" --startup-project ".\LogicFlowEnterpriseFramework.Api\LogicFlowEnterpriseFramework.Api.csproj"
if errorlevel 1 (
    echo.
    echo Database migration failed.
    pause
    exit /b 1
)

echo.
echo Starting LogicFlowEnterpriseFramework API...
echo Swagger will be available at the URL printed by ASP.NET Core, followed by /swagger.
echo.
dotnet run --project ".\LogicFlowEnterpriseFramework.Api\LogicFlowEnterpriseFramework.Api.csproj"

echo.
echo API process exited.
pause
