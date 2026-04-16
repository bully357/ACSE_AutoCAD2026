@echo off
echo ========================================
echo DLL TIMESTAMP CHECKER
echo ========================================
echo.
echo Checking DLL file timestamp...
echo.
dir "C:\Users\jdbul\source\repos\ACSE_AutoCAD2026\ACSE_AutoCAD2026\bin\Debug\net8.0-windows\ACSE.AutoCAD2026.dll" | findstr "ACSE.AutoCAD2026.dll"
echo.
echo If the timestamp shows TODAY and recent time (within last 5 minutes), the DLL is fresh.
echo If it shows an OLD date/time, the rebuild didn't work or AutoCAD has it locked.
echo.
echo Current time is:
time /t
date /t
echo.
pause
