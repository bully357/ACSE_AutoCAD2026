@echo off
echo ========================================
echo  FIND ALL ACSE DLLS IN PROJECT
echo ========================================
echo.

set PROJECT_ROOT=C:\Users\jdbul\source\repos\ACSE_AutoCAD2026

echo Searching for all ACSE.AutoCAD2026.dll files...
echo.

powershell -Command "Get-ChildItem -Path '%PROJECT_ROOT%' -Recurse -Filter 'ACSE.AutoCAD2026.dll' -ErrorAction SilentlyContinue | ForEach-Object { Write-Host ''; Write-Host 'FOUND:' -ForegroundColor Yellow; Write-Host ('  Path: ' + $_.FullName) -ForegroundColor White; Write-Host ('  Size: ' + [math]::Round($_.Length/1KB, 2) + ' KB') -ForegroundColor Cyan; Write-Host ('  Modified: ' + $_.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss')) -ForegroundColor Magenta; $age = (Get-Date) - $_.LastWriteTime; if ($age.TotalMinutes -lt 10) { Write-Host '  Status: FRESH BUILD' -ForegroundColor Green } elseif ($age.TotalDays -lt 1) { Write-Host ('  Status: Today (' + [math]::Round($age.TotalHours, 1) + ' hours old)') -ForegroundColor Yellow } else { Write-Host ('  Status: OLD (' + [math]::Round($age.TotalDays, 1) + ' days old)') -ForegroundColor Red } }"

echo.
echo ========================================
echo RECOMMENDATION:
echo ========================================
echo.
echo KEEP only the FRESHEST DLL (usually in bin\Debug\net8.0-windows)
echo DELETE all others
echo.
echo Run: DELETE_OLD_DLLS_NOW.bat to clean up
echo.

pause
