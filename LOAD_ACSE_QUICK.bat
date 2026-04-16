REM ═══════════════════════════════════════════════════════════════════
REM   🚀 ACSE QUICK LOAD & TEST SCRIPT 🚀
REM ═══════════════════════════════════════════════════════════════════

@echo off
echo.
echo ═══════════════════════════════════════════════════════════════════
echo   ACSE AutoCAD 2026 - Quick Load Script
echo ═══════════════════════════════════════════════════════════════════
echo.

REM Check if AutoCAD is running
tasklist /FI "IMAGENAME eq acad.exe" 2>NUL | find /I /N "acad.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo ⚠️  WARNING: AutoCAD is currently running!
    echo    Please close AutoCAD first to avoid DLL lock issues.
    echo.
    echo    1. Type QUIT in AutoCAD
    echo    2. Wait 10 seconds
    echo    3. Run this script again
    echo.
    pause
    exit /b
)

echo ✅ AutoCAD is not running - Good!
echo.

REM Set paths
set "SOLUTION_DIR=C:\Users\jdbul\source\repos\ACSE_AutoCAD2026\ACSE_AutoCAD2026"
set "DLL_PATH=%SOLUTION_DIR%\bin\Release\net8.0-windows\ACSE.AutoCAD2026.dll"

echo Checking DLL location...
if not exist "%DLL_PATH%" (
    echo ❌ ERROR: DLL not found!
    echo    Expected: %DLL_PATH%
    echo.
    echo    Please rebuild:
    echo    cd %SOLUTION_DIR%
    echo    dotnet build -c Release
    echo.
    pause
    exit /b
)

echo ✅ DLL found: %DLL_PATH%
echo.

REM Show DLL info
for %%F in ("%DLL_PATH%") do (
    echo    Size: %%~zF bytes
    echo    Modified: %%~tF
)
echo.

echo ═══════════════════════════════════════════════════════════════════
echo   📋 COPY THIS PATH TO NETLOAD IN AUTOCAD:
echo ═══════════════════════════════════════════════════════════════════
echo.
echo %DLL_PATH%
echo.
echo ═══════════════════════════════════════════════════════════════════
echo.

echo Instructions:
echo 1. Open AutoCAD 2026
echo 2. Open or create a drawing
echo 3. Type: NETLOAD
echo 4. Paste the path above
echo 5. Press ENTER
echo 6. Type: ACSE_STANDARDS
echo 7. Press ENTER
echo.
echo Expected Result: Standards Control Panel window opens!
echo.

echo ═══════════════════════════════════════════════════════════════════
echo   📝 COMMANDS TO TEST:
echo ═══════════════════════════════════════════════════════════════════
echo.
echo Primary Commands:
echo   • ACSE_STANDARDS    ← Main command (NEW!)
echo   • ACSE_CONTROL      ← Alias for ACSE_STANDARDS
echo.
echo Other Commands:
echo   • ACSE_UI           ← Original scan window
echo   • ACSE_GLOBAL_TEXT  ← Text modifier
echo   • ACSE_UI_INTERACTIVE ← Interactive scanner
echo.
echo ═══════════════════════════════════════════════════════════════════
echo.

REM Optionally copy path to clipboard (requires clip.exe)
echo %DLL_PATH% | clip
echo ✅ DLL path copied to clipboard!
echo    Just paste (Ctrl+V) in NETLOAD dialog
echo.

pause
