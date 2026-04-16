@echo off
echo ╔══════════════════════════════════════════════════════════════╗
echo ║         ACSE - COMPLETE AUTO REBUILD                        ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.
echo This will:
echo   ✓ Clean solution
echo   ✓ Rebuild from scratch
echo   ✓ Verify DLL timestamp
echo   ✓ List all commands in DLL
echo   ✓ Verify ACSE_UI_INTERACTIVE exists
echo   ✓ Copy path to clipboard
echo.
echo Press any key to start...
pause >nul

PowerShell.exe -ExecutionPolicy Bypass -File "%~dp0AUTO_REBUILD_COMPLETE.ps1"
