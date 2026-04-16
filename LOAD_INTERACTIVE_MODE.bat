@echo off
echo ==========================================
echo ACSE INTERACTIVE MODE - QUICK START
echo ==========================================
echo.
echo DLL Location:
echo %~dp0ACSE_AutoCAD2026\bin\Debug\net8.0-windows\ACSE.AutoCAD2026.dll
echo.
echo.
echo INSTRUCTIONS:
echo ==========================================
echo 1. Close AutoCAD completely (if open)
echo 2. Start AutoCAD 2026
echo 3. Open any drawing or create new
echo 4. Type: NETLOAD
echo 5. Browse to the DLL location shown above
echo 6. Click Open
echo 7. Type: ACSE_UI_INTERACTIVE
echo 8. Press Enter
echo ==========================================
echo.
echo Press any key to open the DLL folder...
pause >nul
explorer "%~dp0ACSE_AutoCAD2026\bin\Debug\net8.0-windows\"
