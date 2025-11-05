@echo off
echo ========================================
echo   Project Generator - Build and Run
echo ========================================
echo.

:menu
echo Please select an option:
echo.
echo 1. Build All Projects
echo 2. Run Windows Forms UI
echo 3. Run Console Application
echo 4. Clean Solution
echo 5. Exit
echo.
set /p choice="Enter your choice (1-5): "

if "%choice%"=="1" goto build
if "%choice%"=="2" goto run_ui
if "%choice%"=="3" goto run_console
if "%choice%"=="4" goto clean
if "%choice%"=="5" goto exit
goto menu

:build
echo.
echo Building all projects...
dotnet restore ProjectGenerator.sln
dotnet build ProjectGenerator.sln
echo.
echo Build completed!
pause
goto menu

:run_ui
echo.
echo Running Windows Forms UI...
cd ProjectGenerator.UI
dotnet run
cd ..
pause
goto menu

:run_console
echo.
echo Running Console Application...
cd ProjectGenerator
dotnet run
cd ..
pause
goto menu

:clean
echo.
echo Cleaning solution...
dotnet clean ProjectGenerator.sln
echo.
echo Clean completed!
pause
goto menu

:exit
echo.
echo Goodbye!
exit
