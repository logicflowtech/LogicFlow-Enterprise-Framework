@echo off
setlocal

cd /d "%~dp0"

set ASPNETCORE_ENVIRONMENT=Development

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
