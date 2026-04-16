@echo off
cls
echo.
echo ╔══════════════════════════════════════════════════════════════╗
echo ║         🎯 ACSE PLUGIN - QUICK START TESTING 🎯             ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.
echo This script will guide you through the complete testing process.
echo.
echo ========================================
echo  CHOOSE YOUR OPTION:
echo ========================================
echo.
echo  1. FIRST TIME SETUP (Cleanup + Rebuild + Test)
echo     - Recommended for fresh start
echo     - Deletes old builds
echo     - Rebuilds DLL
echo     - Launches testing
echo.
echo  2. QUICK REBUILD (Just rebuild and verify)
echo     - For code changes
echo     - Skips cleanup
echo     - Faster
echo.
echo  3. TEST EXIT CRASH FIX (Plugin already loaded)
echo     - AutoCAD is running with plugin loaded
echo     - Tests if exit crash fix works
echo.
echo  4. CHECK DLL TIMESTAMP (Verify fresh build)
echo     - Quick check if DLL is up-to-date
echo.
echo  5. VIEW MASTER GUIDE (Read full documentation)
echo     - Opens MASTER_TESTING_GUIDE.md
echo.
echo  6. OPEN TEST CHECKLIST (Print/fill out)
echo     - Opens FINAL_TEST_CHECKLIST.md
echo.
echo  0. EXIT
echo.

set /p CHOICE=Enter your choice (0-6): 

if "%CHOICE%"=="1" goto FIRST_TIME
if "%CHOICE%"=="2" goto QUICK_REBUILD
if "%CHOICE%"=="3" goto TEST_EXIT
if "%CHOICE%"=="4" goto CHECK_TIMESTAMP
if "%CHOICE%"=="5" goto VIEW_GUIDE
if "%CHOICE%"=="6" goto VIEW_CHECKLIST
if "%CHOICE%"=="0" goto END

echo.
echo Invalid choice. Please try again.
pause
goto START

:FIRST_TIME
cls
echo.
echo ════════════════════════════════════════
echo  OPTION 1: FIRST TIME SETUP
echo ════════════════════════════════════════
echo.
echo This will:
echo   1. Find and show all ACSE DLLs
echo   2. Delete old builds (if you approve)
echo   3. Rebuild the plugin
echo   4. Verify fresh DLL
echo   5. Show you how to test
echo.
echo Press any key to start cleanup...
pause >nul

call CLEANUP_ALL_OLD_DLLS.bat

echo.
echo ════════════════════════════════════════
echo Now running rebuild...
echo ════════════════════════════════════════
pause

call REBUILD_AND_TEST.bat

echo.
echo ════════════════════════════════════════
echo FIRST TIME SETUP COMPLETE!
echo ════════════════════════════════════════
echo.
echo Next steps:
echo   1. AutoCAD should be open (or launch it)
echo   2. NETLOAD the DLL (path shown above)
echo   3. Run option 3 to test exit crash fix
echo.
pause
goto START

:QUICK_REBUILD
cls
echo.
echo ════════════════════════════════════════
echo  OPTION 2: QUICK REBUILD
echo ════════════════════════════════════════
echo.
echo Rebuilding plugin...
echo.

call REBUILD_AND_TEST.bat

echo.
echo ════════════════════════════════════════
echo REBUILD COMPLETE!
echo ════════════════════════════════════════
echo.
pause
goto START

:TEST_EXIT
cls
echo.
echo ════════════════════════════════════════
echo  OPTION 3: TEST EXIT CRASH FIX
echo ════════════════════════════════════════
echo.
echo Prerequisites:
echo   - AutoCAD 2026 is running
echo   - Plugin is loaded (NETLOAD completed)
echo.
echo This will guide you through testing the exit crash fix.
echo.
pause

call TEST_EXIT_CRASH_FIX.bat

echo.
echo ════════════════════════════════════════
echo EXIT CRASH TEST COMPLETE!
echo ════════════════════════════════════════
echo.
pause
goto START

:CHECK_TIMESTAMP
cls
echo.
echo ════════════════════════════════════════
echo  OPTION 4: CHECK DLL TIMESTAMP
echo ════════════════════════════════════════
echo.

call CHECK_DLL_TIMESTAMP_NOW.bat

pause
goto START

:VIEW_GUIDE
cls
echo.
echo ════════════════════════════════════════
echo  OPTION 5: MASTER TESTING GUIDE
echo ════════════════════════════════════════
echo.
echo Opening MASTER_TESTING_GUIDE.md...
echo.

if exist "MASTER_TESTING_GUIDE.md" (
    start "" "MASTER_TESTING_GUIDE.md"
    echo Guide opened in your default markdown viewer.
) else (
    echo ERROR: MASTER_TESTING_GUIDE.md not found!
)

echo.
pause
goto START

:VIEW_CHECKLIST
cls
echo.
echo ════════════════════════════════════════
echo  OPTION 6: TEST CHECKLIST
echo ════════════════════════════════════════
echo.
echo Opening FINAL_TEST_CHECKLIST.md...
echo.

if exist "FINAL_TEST_CHECKLIST.md" (
    start "" "FINAL_TEST_CHECKLIST.md"
    echo Checklist opened. Print it for manual testing.
) else (
    echo ERROR: FINAL_TEST_CHECKLIST.md not found!
)

echo.
pause
goto START

:END
cls
echo.
echo ════════════════════════════════════════
echo  ACSE PLUGIN TESTING - QUICK REFERENCE
echo ════════════════════════════════════════
echo.
echo Testing Workflow:
echo.
echo   1. First Time: Run option 1 (cleanup + rebuild)
echo   2. Code Changes: Run option 2 (quick rebuild)
echo   3. After NETLOAD: Run option 3 (test exit crash)
echo   4. Verify Fresh: Run option 4 (check timestamp)
echo   5. Full Tests: Use option 6 (checklist)
echo.
echo DLL Location:
echo   C:\Users\jdbul\source\repos\ACSE_AutoCAD2026\
echo   ACSE_AutoCAD2026\bin\Debug\net8.0-windows\
echo   ACSE.AutoCAD2026.dll
echo.
echo Log File:
echo   C:\ACSE\acse_debug.log
echo.
echo Good luck with testing! 🚀
echo.
pause
exit /b 0

:START
cls
goto :EOF
