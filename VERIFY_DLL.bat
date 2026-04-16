@echo off
echo ==========================================
echo ACSE DLL VERIFICATION
echo ==========================================
echo.
echo This will check if the DLL is built correctly
echo and prepare it for loading in AutoCAD.
echo.
echo Press any key to start verification...
pause >nul

PowerShell.exe -ExecutionPolicy Bypass -File "%~dp0VERIFY_AND_LOAD_DLL.ps1"

echo.
echo ==========================================
echo.
echo If you saw commands listed above, the DLL is good!
echo If not, rebuild the solution in Visual Studio.
echo.
pause
