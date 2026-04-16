Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ACSE DLL VERIFICATION & AUTO-LOADER" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get the script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dllPath = Join-Path $scriptDir "ACSE_AutoCAD2026\bin\Debug\net8.0-windows\ACSE.AutoCAD2026.dll"

Write-Host "Checking DLL..." -ForegroundColor Yellow
Write-Host "Path: $dllPath" -ForegroundColor Gray
Write-Host ""

if (Test-Path $dllPath) {
    $dll = Get-Item $dllPath
    $timestamp = $dll.LastWriteTime
    $size = $dll.Length
    
    Write-Host "✓ DLL Found!" -ForegroundColor Green
    Write-Host "  Timestamp: $timestamp" -ForegroundColor White
    Write-Host "  Size: $([math]::Round($size/1KB, 2)) KB" -ForegroundColor White
    Write-Host ""
    
    # Check if it's recent (within last 10 minutes)
    $age = (Get-Date) - $timestamp
    if ($age.TotalMinutes -lt 10) {
        Write-Host "✓ DLL is FRESH (built $([math]::Round($age.TotalMinutes, 1)) minutes ago)" -ForegroundColor Green
    } else {
        Write-Host "⚠ DLL is OLD (built $([math]::Round($age.TotalHours, 1)) hours ago)" -ForegroundColor Yellow
        Write-Host "  Consider rebuilding in Visual Studio (Ctrl+Shift+B)" -ForegroundColor Yellow
    }
    Write-Host ""
    
    # Try to read assembly info
    try {
        $assembly = [System.Reflection.Assembly]::LoadFile($dllPath)
        $version = $assembly.GetName().Version
        Write-Host "✓ Assembly loads successfully" -ForegroundColor Green
        Write-Host "  Version: $version" -ForegroundColor White
        Write-Host ""
        
        # List command methods
        Write-Host "Commands found in DLL:" -ForegroundColor Cyan
        $commandCount = 0
        foreach ($type in $assembly.GetTypes()) {
            foreach ($method in $type.GetMethods()) {
                $cmdAttr = $method.GetCustomAttributes([Autodesk.AutoCAD.Runtime.CommandMethodAttribute], $false)
                if ($cmdAttr.Count -gt 0) {
                    foreach ($attr in $cmdAttr) {
                        $commandCount++
                        $cmdName = $attr.GlobalName
                        Write-Host "  - $cmdName" -ForegroundColor White
                    }
                }
            }
        }
        
        if ($commandCount -eq 0) {
            Write-Host "  ⚠ NO COMMANDS FOUND!" -ForegroundColor Red
            Write-Host "  This might indicate a build issue." -ForegroundColor Red
        } else {
            Write-Host ""
            Write-Host "✓ Total commands: $commandCount" -ForegroundColor Green
        }
    } catch {
        Write-Host "⚠ Could not load assembly details" -ForegroundColor Yellow
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "AUTOCAD LOADING INSTRUCTIONS:" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. CLOSE AutoCAD completely" -ForegroundColor White
    Write-Host "2. START AutoCAD 2026" -ForegroundColor White
    Write-Host "3. OPEN any drawing or create new" -ForegroundColor White
    Write-Host "4. TYPE: NETLOAD" -ForegroundColor Yellow
    Write-Host "5. PASTE this path:" -ForegroundColor White
    Write-Host ""
    Write-Host "   $dllPath" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "6. Press ENTER" -ForegroundColor White
    Write-Host "7. TYPE: ACSE_UI_INTERACTIVE" -ForegroundColor Yellow
    Write-Host "8. Press ENTER" -ForegroundColor White
    Write-Host ""
    
    # Copy path to clipboard
    try {
        Set-Clipboard -Value $dllPath
        Write-Host "✓ DLL path copied to clipboard!" -ForegroundColor Green
        Write-Host "  You can paste it directly in AutoCAD NETLOAD dialog" -ForegroundColor Gray
    } catch {
        Write-Host "⚠ Could not copy to clipboard" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Press any key to open the DLL folder..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    
    # Open folder
    $folder = Split-Path $dllPath
    Start-Process explorer.exe -ArgumentList $folder
    
} else {
    Write-Host "✗ DLL NOT FOUND!" -ForegroundColor Red
    Write-Host "  Expected path: $dllPath" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Please build the solution first:" -ForegroundColor Yellow
    Write-Host "  1. Open Visual Studio" -ForegroundColor White
    Write-Host "  2. Build → Rebuild Solution" -ForegroundColor White
    Write-Host "  3. Check for errors in Output window" -ForegroundColor White
    Write-Host ""
}

Write-Host ""
Write-Host "Script complete." -ForegroundColor Green
