@echo off
echo ========================================
echo  DELETE OLD/DUPLICATE ACSE DLLS
echo ========================================
echo.

set PROJECT_ROOT=C:\Users\jdbul\source\repos\ACSE_AutoCAD2026

echo This will DELETE:
echo   - All Release builds (bin\Release, obj\Release)
echo   - All intermediate builds (obj\Debug)
echo   - Old deploy folders
echo.
echo This will KEEP:
echo   - Current Debug DLL: bin\Debug\net8.0-windows\ACSE.AutoCAD2026.dll
echo.

pause

echo.
echo Deleting Release builds...
if exist "%PROJECT_ROOT%\ACSE_AutoCAD2026\bin\Release" (
    rmdir /s /q "%PROJECT_ROOT%\ACSE_AutoCAD2026\bin\Release"
    echo   Deleted: bin\Release
) else (
    echo   Not found: bin\Release
)

if exist "%PROJECT_ROOT%\ACSE_AutoCAD2026\obj\Release" (
    rmdir /s /q "%PROJECT_ROOT%\ACSE_AutoCAD2026\obj\Release"
    echo   Deleted: obj\Release
) else (
    echo   Not found: obj\Release
)

echo.
echo Deleting intermediate Debug builds...
if exist "%PROJECT_ROOT%\ACSE_AutoCAD2026\obj\Debug\net8.0-windows\ACSE.AutoCAD2026.dll" (
    del /q "%PROJECT_ROOT%\ACSE_AutoCAD2026\obj\Debug\net8.0-windows\ACSE.AutoCAD2026.dll"
    echo   Deleted: obj\Debug DLL
) else (
    echo   Not found: obj\Debug DLL
)

echo.
echo Deleting old deploy folders...
if exist "C:\ACSE\deploy" (
    rmdir /s /q "C:\ACSE\deploy"
    echo   Deleted: C:\ACSE\deploy
) else (
    echo   Not found: C:\ACSE\deploy
)

if exist "%PROJECT_ROOT%\deploy" (
    rmdir /s /q "%PROJECT_ROOT%\deploy"
    echo   Deleted: Project deploy folder
) else (
    echo   Not found: Project deploy folder
)

echo.
echo ========================================
echo CLEANUP COMPLETE!
echo ========================================
echo.
echo KEPT (current working DLL):
echo   %PROJECT_ROOT%\ACSE_AutoCAD2026\bin\Debug\net8.0-windows\ACSE.AutoCAD2026.dll
echo.

pause
