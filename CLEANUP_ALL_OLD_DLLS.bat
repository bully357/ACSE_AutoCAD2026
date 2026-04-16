@echo off
echo ========================================
echo  CLEANUP ALL OLD/DUPLICATE DLL FILES
echo ========================================
echo.

echo This script will:
echo  1. Find all ACSE DLL files in the project
echo  2. Show their timestamps and sizes
echo  3. Delete old Debug/Release builds
echo  4. Keep ONLY the fresh Debug build
echo.

pause

echo.
echo ========================================
echo STEP 1: Searching for ALL ACSE DLLs...
echo ========================================
echo.

set PROJECT_ROOT=C:\Users\jdbul\source\repos\ACSE_AutoCAD2026

echo Checking Debug folder...
if exist "%PROJECT_ROOT%\ACSE_AutoCAD2026\bin\Debug\net8.0-windows\ACSE.AutoCAD2026.dll" (
    echo [DEBUG] Found: 
    dir "%PROJECT_ROOT%\ACSE_AutoCAD2026\bin\Debug\net8.0-windows\ACSE.AutoCAD2026.dll" | findstr "ACSE"
) else (
    echo [DEBUG] Not found
)

echo.
echo Checking Release folder...
if exist "%PROJECT_ROOT%\ACSE_AutoCAD2026\bin\Release\net8.0-windows\ACSE.AutoCAD2026.dll" (
    echo [RELEASE] Found:
    dir "%PROJECT_ROOT%\ACSE_AutoCAD2026\bin\Release\net8.0-windows\ACSE.AutoCAD2026.dll" | findstr "ACSE"
) else (
    echo [RELEASE] Not found
)

echo.
echo Checking deploy folder...
if exist "C:\ACSE\deploy\ACSE.AutoCAD2026.dll" (
    echo [DEPLOY] Found:
    dir "C:\ACSE\deploy\ACSE.AutoCAD2026.dll" | findstr "ACSE"
) else (
    echo [DEPLOY] Not found
)

echo.
echo Checking obj folders (intermediate builds)...
if exist "%PROJECT_ROOT%\ACSE_AutoCAD2026\obj\Debug\net8.0-windows\ACSE.AutoCAD2026.dll" (
    echo [OBJ-DEBUG] Found:
    dir "%PROJECT_ROOT%\ACSE_AutoCAD2026\obj\Debug\net8.0-windows\ACSE.AutoCAD2026.dll" | findstr "ACSE"
) else (
    echo [OBJ-DEBUG] Not found
)

echo.
echo ========================================
echo STEP 2: Current Time Reference
echo ========================================
echo.
echo Current Time: %DATE% %TIME%
echo.
echo ** Fresh DLL should have timestamp close to NOW **
echo ** Old DLLs should have older timestamps **
echo.

pause

echo.
echo ========================================
echo STEP 3: CLEANUP (Optional)
echo ========================================
echo.
echo Do you want to DELETE old build folders and force rebuild?
echo This will:
echo   - Delete bin\Debug folder
echo   - Delete bin\Release folder  
echo   - Delete obj folder
echo   - Keep source code files only
echo.
echo Type YES to clean, or press Ctrl+C to cancel
echo.

set /p CONFIRM=Type YES to clean: 

if /i not "%CONFIRM%"=="YES" (
    echo.
    echo Cleanup cancelled. No files deleted.
    goto :END
)

echo.
echo Deleting old build folders...

if exist "%PROJECT_ROOT%\ACSE_AutoCAD2026\bin" (
    echo Deleting bin folder...
    rmdir /s /q "%PROJECT_ROOT%\ACSE_AutoCAD2026\bin"
    echo   Done: bin folder deleted
)

if exist "%PROJECT_ROOT%\ACSE_AutoCAD2026\obj" (
    echo Deleting obj folder...
    rmdir /s /q "%PROJECT_ROOT%\ACSE_AutoCAD2026\obj"
    echo   Done: obj folder deleted
)

echo.
echo ========================================
echo CLEANUP COMPLETE!
echo ========================================
echo.
echo Next steps:
echo   1. Close this window
echo   2. Run REBUILD_AND_TEST.bat to build fresh DLL
echo   3. Test in AutoCAD
echo.

:END
pause
