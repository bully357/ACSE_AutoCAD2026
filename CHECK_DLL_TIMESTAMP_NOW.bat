@echo off
echo ========================================
echo DLL TIMESTAMP CHECK
echo ========================================
echo.

set DLL_PATH=C:\Users\jdbul\source\repos\ACSE_AutoCAD2026\ACSE_AutoCAD2026\bin\Debug\net8.0-windows\ACSE.AutoCAD2026.dll

if exist "%DLL_PATH%" (
    echo DLL Found: %DLL_PATH%
    echo.
    echo File Details:
    dir "%DLL_PATH%" | findstr "ACSE"
    echo.
    echo Current Time:
    echo %DATE% %TIME%
    echo.
    echo ========================================
    echo.
    echo If the DLL timestamp matches the current time
    echo (within last few minutes), it's FRESH!
    echo.
    echo Otherwise, rebuild in Visual Studio:
    echo   1. Build -^> Clean Solution
    echo   2. Build -^> Rebuild Solution
    echo.
) else (
    echo ERROR: DLL not found!
    echo Expected: %DLL_PATH%
    echo.
    echo Please build the solution first.
)

echo.
pause
