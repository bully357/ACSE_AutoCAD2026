@echo off
echo ========================================
echo  TEST: EXIT CRASH FIX VERIFICATION
echo ========================================
echo.

echo This script helps verify the exit crash fix is working.
echo.
echo The fix prevents AutoCAD from crashing on exit by:
echo   1. Closing WPF windows before exit
echo   2. Freeing all GCHandles properly
echo   3. Unsubscribing from events
echo.

pause

echo.
echo ========================================
echo STEP 1: Check Log File (Before Test)
echo ========================================
echo.

set LOG_PATH=C:\ACSE\acse_debug.log

if exist "%LOG_PATH%" (
    echo Log file exists: %LOG_PATH%
    echo.
    echo Last 10 lines of log:
    echo ----------------------------------------
    powershell -Command "Get-Content '%LOG_PATH%' -Tail 10"
    echo ----------------------------------------
) else (
    echo Log file not found yet.
    echo It will be created when AutoCAD loads the plugin.
)

echo.
pause

echo.
echo ========================================
echo STEP 2: Test Instructions
echo ========================================
echo.
echo NOW DO THIS IN AUTOCAD:
echo.
echo 1. Open AutoCAD 2026
echo.
echo 2. NETLOAD the DLL:
echo    C:\Users\jdbul\source\repos\ACSE_AutoCAD2026\
echo    ACSE_AutoCAD2026\bin\Debug\net8.0-windows\
echo    ACSE.AutoCAD2026.dll
echo.
echo 3. Run these commands to test:
echo      ACSE_TEST
echo      ACSE_UI
echo      ACSE_RUN_SIMPLE
echo.
echo 4. Close any ACSE windows (if opened)
echo.
echo 5. File -^> Exit AutoCAD
echo.
echo 6. OBSERVE:
echo      - Does AutoCAD close normally? (YES/NO)
echo      - Any crash dialogs? (YES/NO)
echo      - Process ends cleanly? (YES/NO)
echo.
echo 7. Return to this script window
echo.

pause

echo.
echo ========================================
echo STEP 3: Check Log File (After Test)
echo ========================================
echo.

if exist "%LOG_PATH%" (
    echo Reading log file...
    echo.
    echo Last 20 lines (should show cleanup):
    echo ========================================
    powershell -Command "Get-Content '%LOG_PATH%' -Tail 20"
    echo ========================================
    echo.
    echo.
    echo ========================================
    echo WHAT TO LOOK FOR:
    echo ========================================
    echo.
    echo If the fix worked, you should see:
    echo   [timestamp] Freed GCHandle for ComplianceCommands
    echo   [timestamp] Freed GCHandle for TemplateCommands
    echo   [timestamp] Freed GCHandle for ExtractCommands
    echo   [timestamp] ACSE Plugin terminated successfully
    echo.
    echo If you DON'T see these messages:
    echo   - The plugin didn't unload properly
    echo   - Check if AutoCAD crashed instead of exiting cleanly
    echo   - Check Windows Event Viewer for .NET Runtime errors
    echo.
) else (
    echo.
    echo ERROR: Log file still not found!
    echo.
    echo This means:
    echo   - Plugin never loaded, OR
    echo   - Wrong log path
    echo.
    echo Check if you NETLOAD the plugin in AutoCAD.
)

echo.
echo ========================================
echo STEP 4: Check Windows Event Viewer
echo ========================================
echo.

echo Opening Event Viewer...
echo.
echo Instructions:
echo   1. Event Viewer window will open
echo   2. Go to: Windows Logs -^> Application
echo   3. Look for recent errors (red X icons)
echo   4. Check for:
echo        - Source: .NET Runtime
echo        - Event ID: 1025 (delegate GC crash)
echo        - Event ID: 1000 (application crash)
echo   5. If you find these errors AFTER the fix:
echo        - The fix didn't work
echo        - Copy the error details
echo        - Need to investigate further
echo.

pause

start eventvwr.msc

echo.
echo ========================================
echo STEP 5: Test Results
echo ========================================
echo.

echo Did AutoCAD exit WITHOUT crashing?
set /p EXIT_CLEAN=Enter YES or NO: 

echo.
echo Did you see the GCHandle cleanup messages in the log?
set /p SAW_CLEANUP=Enter YES or NO: 

echo.
echo Any .NET Runtime errors in Event Viewer?
set /p SAW_ERRORS=Enter YES or NO: 

echo.
echo ========================================
echo TEST SUMMARY
echo ========================================
echo.
echo AutoCAD exited cleanly: %EXIT_CLEAN%
echo GCHandle cleanup logged: %SAW_CLEANUP%
echo Event Viewer errors: %SAW_ERRORS%
echo.

if /i "%EXIT_CLEAN%"=="YES" (
    if /i "%SAW_CLEANUP%"=="YES" (
        if /i "%SAW_ERRORS%"=="NO" (
            echo.
            echo ========================================
            echo   ✅ EXIT CRASH FIX: SUCCESS! ✅
            echo ========================================
            echo.
            echo The fix is working correctly!
            echo AutoCAD exits cleanly without crashes.
            echo.
            goto :SUCCESS
        )
    )
)

echo.
echo ========================================
echo   ⚠️ EXIT CRASH FIX: NEEDS ATTENTION
echo ========================================
echo.
echo Results indicate the fix may not be working.
echo.
echo Please check:
echo   1. DLL timestamp (is it fresh after rebuild?)
echo   2. Log file contents (paste last 20 lines)
echo   3. Event Viewer errors (copy error details)
echo.
goto :END

:SUCCESS
echo Next steps:
echo   - Plugin is production ready!
echo   - Exit crash issue resolved
echo   - Safe to use in production AutoCAD
echo.

:END
pause
