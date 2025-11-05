@echo off
echo ========================================
echo   Project Generator - Windows Forms
echo ========================================
echo.
echo Building...
cd ProjectGenerator.UI
dotnet build
if %errorlevel% neq 0 (
    echo.
    echo Build FAILED!
    pause
    exit /b 1
)

echo.
echo Build successful! Starting application...
echo.
dotnet run

pause
