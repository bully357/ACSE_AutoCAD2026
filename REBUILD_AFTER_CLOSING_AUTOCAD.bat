@echo off
echo ╔══════════════════════════════════════════════════════════════╗
echo ║                                                              ║
echo ║    🔴 CLOSE AUTOCAD FIRST, THEN RUN THIS! 🔴               ║
echo ║                                                              ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.

echo Checking if AutoCAD is running...
echo.

tasklist /FI "IMAGENAME eq acad.exe" 2>NUL | find /I /N "acad.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo ❌ AutoCAD is STILL RUNNING!
    echo.
    echo Please close AutoCAD first:
    echo   1. In AutoCAD, type: QUIT
    echo   2. Wait for it to close
    echo   3. Then run this script again
    echo.
    pause
    exit /b 1
)

echo ✅ AutoCAD is NOT running (good!)
echo.

echo Rebuilding DLL with Interactive UI fix...
echo.

cd C:\Users\jdbul\source\repos\ACSE_AutoCAD2026\ACSE_AutoCAD2026

echo Cleaning old build...
dotnet clean -c Debug

echo.
echo Building fresh DLL...
dotnet build -c Debug

echo.
if %ERRORLEVEL% EQU 0 (
    echo ╔══════════════════════════════════════════════════════════════╗
    echo ║  ✅ BUILD SUCCESSFUL!                                        ║
    echo ╚══════════════════════════════════════════════════════════════╝
    echo.
    echo Fresh DLL created at:
    echo   C:\Users\jdbul\source\repos\ACSE_AutoCAD2026\
    echo   ACSE_AutoCAD2026\bin\Debug\net8.0-windows\
    echo   ACSE.AutoCAD2026.dll
    echo.
    echo 🎯 NEXT STEPS:
    echo   1. Open AutoCAD 2026
    echo   2. Type: NETLOAD
    echo   3. Browse to the path above
    echo   4. Type: ACSE_UI_INTERACTIVE
    echo   5. Window should open WITHOUT error!
    echo.
) else (
    echo ╔══════════════════════════════════════════════════════════════╗
    echo ║  ❌ BUILD FAILED!                                            ║
    echo ╚══════════════════════════════════════════════════════════════╝
    echo.
    echo Check the errors above.
    echo.
)

pause
