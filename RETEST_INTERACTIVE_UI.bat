@echo off
echo ╔══════════════════════════════════════════════════════════════╗
echo ║                                                              ║
echo ║       RETEST ACSE_UI_INTERACTIVE (AFTER FIX)                ║
echo ║                                                              ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.

echo ✅ Interactive UI fix has been applied and rebuilt!
echo.
echo What was fixed:
echo   - Added null checks in ApplyFilters() method
echo   - Added null checks in UpdateSummary() method
echo   - Prevents NullReferenceException during XAML init
echo.
echo ═══════════════════════════════════════════════════════════════
echo.

set DLL_PATH=C:\Users\jdbul\source\repos\ACSE_AutoCAD2026\ACSE_AutoCAD2026\bin\Debug\net8.0-windows\ACSE.AutoCAD2026.dll

echo 📍 Fresh DLL Location:
echo    %DLL_PATH%
echo.

if not exist "%DLL_PATH%" (
    echo ❌ ERROR: DLL not found!
    echo    Run REBUILD_NOW.bat first
    pause
    exit /b 1
)

echo ✅ DLL exists!
echo.

for %%F in ("%DLL_PATH%") do (
    echo Size: %%~zF bytes
    echo Modified: %%~tF
)

echo.
echo ═══════════════════════════════════════════════════════════════
echo.
echo 🎯 RETEST INSTRUCTIONS:
echo.
echo 1. Close AutoCAD (if open)
echo    - This releases the old DLL
echo.
echo 2. Open AutoCAD 2026
echo.
echo 3. Type: NETLOAD
echo    - Paste this path:
echo    %DLL_PATH%
echo.
echo 4. Type: ACSE_UI_INTERACTIVE
echo.
echo 5. Expected Result:
echo    ✅ WPF window opens successfully
echo    ✅ No NullReferenceException
echo    ✅ Shows all UI elements:
echo       - Template path textbox
echo       - Filter checkboxes (Text, Dim, Layer, Linetype)
echo       - Scan/Fix/Preview buttons
echo       - Violations grid (empty until scan)
echo.
echo 6. Test the UI:
echo    a. Browse to a .dwt template file
echo    b. Click "Scan" button
echo    c. Check violations appear
echo    d. Try filter checkboxes
echo    e. Try "Fix Selected" button
echo.
echo 7. Close the window
echo.
echo 8. Exit AutoCAD
echo    - Verify it closes cleanly (no crash)
echo.
echo ═══════════════════════════════════════════════════════════════
echo.
echo 💡 TIP: If ACSE_UI_INTERACTIVE still fails, check:
echo    - AutoCAD command line for error message
echo    - Event Viewer for .NET errors
echo.
echo ═══════════════════════════════════════════════════════════════
echo.

pause

echo.
echo 📋 Want to see what changed?
echo    Open: INTERACTIVE_UI_FIX_APPLIED.md
echo.
echo 🔍 Want to verify DLL timestamp?
echo    Run: CHECK_DLL_TIMESTAMP_NOW.bat
echo.

pause
