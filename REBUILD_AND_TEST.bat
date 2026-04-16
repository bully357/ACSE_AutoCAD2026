@echo off
echo ========================================
echo  REBUILD ACSE PLUGIN - COMPLETE CYCLE
echo ========================================
echo.

set PROJECT_ROOT=C:\Users\jdbul\source\repos\ACSE_AutoCAD2026
set DLL_PATH=%PROJECT_ROOT%\ACSE_AutoCAD2026\bin\Debug\net8.0-windows\ACSE.AutoCAD2026.dll

echo This script will:
echo   1. Check if AutoCAD is running (must be closed)
echo   2. Clean old builds
echo   3. Rebuild the DLL
echo   4. Verify the new DLL timestamp
echo   5. Show you the exact NETLOAD path
echo.

pause

echo.
echo ========================================
echo STEP 1: Checking AutoCAD process...
echo ========================================
echo.

tasklist /FI "IMAGENAME eq acad.exe" 2>NUL | find /I /N "acad.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo.
    echo *** WARNING: AutoCAD is RUNNING! ***
    echo.
    echo AutoCAD locks the DLL file and prevents rebuild.
    echo.
    echo Please:
    echo   1. Close AutoCAD completely
    echo   2. Wait 5 seconds
    echo   3. Run this script again
    echo.
    pause
    exit /b 1
) else (
    echo   OK: AutoCAD is not running
)

echo.
echo ========================================
echo STEP 2: Cleaning old builds...
echo ========================================
echo.

cd /d "%PROJECT_ROOT%\ACSE_AutoCAD2026"

echo Running: dotnet clean
dotnet clean -c Debug

if errorlevel 1 (
    echo.
    echo ERROR: Clean failed!
    pause
    exit /b 1
)

echo   Done: Old builds cleaned

echo.
echo ========================================
echo STEP 3: Building DLL (Debug mode)...
echo ========================================
echo.

echo Running: dotnet build -c Debug
dotnet build -c Debug

if errorlevel 1 (
    echo.
    echo *** BUILD FAILED! ***
    echo.
    echo Check the error messages above.
    echo Common issues:
    echo   - Missing AutoCAD references
    echo   - Syntax errors in code
    echo   - Missing NuGet packages
    echo.
    pause
    exit /b 1
)

echo.
echo ========================================
echo BUILD SUCCESS!
echo ========================================
echo.

echo.
echo ========================================
echo STEP 4: Verifying DLL...
echo ========================================
echo.

if exist "%DLL_PATH%" (
    echo DLL Found: 
    echo   %DLL_PATH%
    echo.
    echo File Details:
    dir "%DLL_PATH%" | findstr "ACSE"
    echo.
    echo Current Time:
    echo   %DATE% %TIME%
    echo.
    echo ** If DLL timestamp matches current time, it's FRESH! **
    echo.
) else (
    echo.
    echo ERROR: DLL not found after build!
    echo Expected: %DLL_PATH%
    echo.
    pause
    exit /b 1
)

echo.
echo ========================================
echo STEP 5: Ready to test in AutoCAD
echo ========================================
echo.
echo Next steps:
echo.
echo 1. Open AutoCAD 2026
echo.
echo 2. Type: NETLOAD
echo.
echo 3. Browse to and select this EXACT file:
echo    %DLL_PATH%
echo.
echo 4. Expected results:
echo      - NO "eDuplicateKey" error
echo      - NO crash
echo      - Welcome message appears
echo.
echo 5. Test basic commands:
echo      ACSE_TEST
echo      ACSE_PING  
echo      ACSE_UI
echo.
echo 6. Test EXIT CRASH FIX:
echo      - Use plugin commands
echo      - File -^> Exit AutoCAD
echo      - Should close WITHOUT crash
echo.
echo 7. Verify in log file:
echo      C:\ACSE\acse_debug.log
echo      Should contain:
echo        "Freed GCHandle for ComplianceCommands"
echo        "Freed GCHandle for TemplateCommands"
echo        "Freed GCHandle for ExtractCommands"
echo        "ACSE Plugin terminated successfully"
echo.
echo ========================================

pause

echo.
echo Opening AutoCAD is optional. Press Ctrl+C to skip.
echo Press any key to launch AutoCAD now...
pause >nul

echo.
echo Launching AutoCAD 2026...
start "" "C:\Program Files\Autodesk\AutoCAD 2026\acad.exe"

echo.
echo ========================================
echo AutoCAD launched!
echo ========================================
echo.
echo Remember to NETLOAD this file:
echo %DLL_PATH%
echo.

pause
