@echo off
echo ╔══════════════════════════════════════════════════════════════╗
echo ║        ACSE - AUTOMATIC CLEAN REBUILD SCRIPT                ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.
echo This will:
echo   1. Clean the solution (delete old DLLs)
echo   2. Rebuild everything from scratch
echo   3. Verify the new DLL timestamp
echo   4. Copy DLL path to clipboard
echo.
echo Press any key to start automatic rebuild...
pause >nul
echo.
echo ========================================
echo STEP 1: CLEANING SOLUTION...
echo ========================================
echo.

cd /d "%~dp0ACSE_AutoCAD2026"

dotnet clean --configuration Debug
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ❌ CLEAN FAILED!
    echo Check if solution file exists.
    echo.
    pause
    exit /b 1
)

echo.
echo ✅ Clean succeeded!
echo.
echo ========================================
echo STEP 2: REBUILDING SOLUTION...
echo ========================================
echo.

dotnet build --configuration Debug --force --no-incremental
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ❌ BUILD FAILED!
    echo Check the error messages above.
    echo Possible issues:
    echo   - Syntax errors in code
    echo   - Missing references
    echo   - Invalid project configuration
    echo.
    pause
    exit /b 1
)

echo.
echo ✅ Build succeeded!
echo.
echo ========================================
echo STEP 3: VERIFYING DLL...
echo ========================================
echo.

set DLL_PATH=%~dp0ACSE_AutoCAD2026\bin\Debug\net8.0-windows\ACSE.AutoCAD2026.dll

if exist "%DLL_PATH%" (
    echo ✅ DLL Found!
    echo.
    echo Location:
    echo %DLL_PATH%
    echo.
    echo File Details:
    dir "%DLL_PATH%" | findstr "ACSE"
    echo.
    echo Current Time:
    echo %DATE% %TIME%
    echo.
    echo ✅ DLL is FRESH!
    echo.
    
    REM Copy path to clipboard using PowerShell
    echo|set /p="Copying DLL path to clipboard..."
    echo Set-Clipboard -Value '%DLL_PATH%' | PowerShell -Command -
    echo  Done!
    echo.
) else (
    echo ❌ DLL NOT FOUND!
    echo Expected: %DLL_PATH%
    echo.
    echo This shouldn't happen after successful build.
    echo Check if output directory is correct.
    echo.
    pause
    exit /b 1
)

echo ========================================
echo ✅ REBUILD COMPLETE!
echo ========================================
echo.
echo Next Steps:
echo   1. Close AutoCAD (if open)
echo   2. Start AutoCAD 2026
echo   3. Type: NETLOAD
echo   4. Paste DLL path (Ctrl+V - already in clipboard!)
echo   5. Type: ACSE_UI_INTERACTIVE
echo   6. Press Enter
echo.
echo DLL path is in your clipboard, ready to paste!
echo.
pause
