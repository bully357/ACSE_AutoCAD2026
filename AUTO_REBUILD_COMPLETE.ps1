Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     ACSE - COMPLETE REBUILD & VERIFICATION SCRIPT           ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Join-Path $scriptDir "ACSE_AutoCAD2026"
$dllPath = Join-Path $scriptDir "ACSE_AutoCAD2026\bin\Debug\net8.0-windows\ACSE.AutoCAD2026.dll"

# Step 1: Clean
Write-Host "═══════════════════════════════════════" -ForegroundColor Yellow
Write-Host "STEP 1: CLEANING SOLUTION" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════" -ForegroundColor Yellow
Write-Host ""

Push-Location $projectDir
try {
    Write-Host "Running: dotnet clean..." -ForegroundColor Gray
    $cleanOutput = dotnet clean --configuration Debug 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Clean succeeded!" -ForegroundColor Green
    } else {
        Write-Host "✗ Clean failed!" -ForegroundColor Red
        Write-Host $cleanOutput
        Pop-Location
        exit 1
    }
} catch {
    Write-Host "✗ Clean failed with exception: $($_.Exception.Message)" -ForegroundColor Red
    Pop-Location
    exit 1
}
Write-Host ""

# Step 2: Rebuild
Write-Host "═══════════════════════════════════════" -ForegroundColor Yellow
Write-Host "STEP 2: REBUILDING SOLUTION" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════" -ForegroundColor Yellow
Write-Host ""

Write-Host "Running: dotnet build --force --no-incremental..." -ForegroundColor Gray
$buildOutput = dotnet build --configuration Debug --force --no-incremental 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Build succeeded!" -ForegroundColor Green
} else {
    Write-Host "✗ Build failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error Details:" -ForegroundColor Yellow
    $buildOutput | Select-String "error" | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    Pop-Location
    exit 1
}
Pop-Location
Write-Host ""

# Step 3: Verify DLL
Write-Host "═══════════════════════════════════════" -ForegroundColor Yellow
Write-Host "STEP 3: VERIFYING DLL" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════" -ForegroundColor Yellow
Write-Host ""

if (Test-Path $dllPath) {
    $dll = Get-Item $dllPath
    $timestamp = $dll.LastWriteTime
    $age = (Get-Date) - $timestamp
    $size = $dll.Length
    
    Write-Host "✓ DLL Found!" -ForegroundColor Green
    Write-Host "  Path: $dllPath" -ForegroundColor White
    Write-Host "  Timestamp: $timestamp" -ForegroundColor White
    Write-Host "  Age: $([math]::Round($age.TotalSeconds, 1)) seconds" -ForegroundColor White
    Write-Host "  Size: $([math]::Round($size/1KB, 2)) KB" -ForegroundColor White
    Write-Host ""
    
    if ($age.TotalSeconds -lt 60) {
        Write-Host "✓ DLL is FRESH! (built $([math]::Round($age.TotalSeconds, 1)) seconds ago)" -ForegroundColor Green
    } else {
        Write-Host "⚠ DLL seems old ($([math]::Round($age.TotalMinutes, 1)) minutes)" -ForegroundColor Yellow
        Write-Host "  This might indicate build didn't actually run." -ForegroundColor Yellow
    }
} else {
    Write-Host "✗ DLL NOT FOUND!" -ForegroundColor Red
    Write-Host "  Expected: $dllPath" -ForegroundColor Gray
    exit 1
}
Write-Host ""

# Step 4: Verify Commands in DLL
Write-Host "═══════════════════════════════════════" -ForegroundColor Yellow
Write-Host "STEP 4: VERIFYING COMMANDS IN DLL" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════" -ForegroundColor Yellow
Write-Host ""

try {
    # Load AutoCAD assemblies from known location
    $acadPath = "C:\Program Files\Autodesk\AutoCAD 2026"
    if (Test-Path $acadPath) {
        Add-Type -Path "$acadPath\acmgd.dll" -ErrorAction SilentlyContinue
        Add-Type -Path "$acadPath\acdbmgd.dll" -ErrorAction SilentlyContinue
        Add-Type -Path "$acadPath\accoremgd.dll" -ErrorAction SilentlyContinue
    }
    
    $assembly = [System.Reflection.Assembly]::LoadFile($dllPath)
    $version = $assembly.GetName().Version
    
    Write-Host "✓ Assembly loads successfully" -ForegroundColor Green
    Write-Host "  Version: $version" -ForegroundColor White
    Write-Host ""
    
    Write-Host "Commands found:" -ForegroundColor Cyan
    $commandCount = 0
    $foundInteractive = $false
    
    foreach ($type in $assembly.GetTypes()) {
        foreach ($method in $type.GetMethods()) {
            $cmdAttrs = $method.GetCustomAttributes($false) | Where-Object { $_.GetType().Name -eq "CommandMethodAttribute" }
            foreach ($attr in $cmdAttrs) {
                $commandCount++
                $cmdName = $attr.GlobalName
                
                if ($cmdName -eq "ACSE_UI_INTERACTIVE") {
                    Write-Host "  ✓ $cmdName" -ForegroundColor Green
                    $foundInteractive = $true
                } else {
                    Write-Host "  - $cmdName" -ForegroundColor White
                }
            }
        }
    }
    
    Write-Host ""
    Write-Host "Total commands: $commandCount" -ForegroundColor White
    Write-Host ""
    
    if ($foundInteractive) {
        Write-Host "✓ ACSE_UI_INTERACTIVE command found in DLL!" -ForegroundColor Green
    } else {
        Write-Host "✗ ACSE_UI_INTERACTIVE NOT FOUND in DLL!" -ForegroundColor Red
        Write-Host "  This indicates a compilation issue." -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠ Could not verify commands: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "  (This is OK if AutoCAD assemblies aren't accessible)" -ForegroundColor Gray
}
Write-Host ""

# Step 5: Copy to Clipboard
Write-Host "═══════════════════════════════════════" -ForegroundColor Yellow
Write-Host "STEP 5: PREPARING FOR AUTOCAD" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════" -ForegroundColor Yellow
Write-Host ""

try {
    Set-Clipboard -Value $dllPath
    Write-Host "✓ DLL path copied to clipboard!" -ForegroundColor Green
    Write-Host "  You can paste it directly in AutoCAD NETLOAD dialog (Ctrl+V)" -ForegroundColor Gray
} catch {
    Write-Host "⚠ Could not copy to clipboard" -ForegroundColor Yellow
}
Write-Host ""

# Summary
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                  REBUILD COMPLETE!                          ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Close AutoCAD (if running)" -ForegroundColor White
Write-Host "  2. Start AutoCAD 2026" -ForegroundColor White
Write-Host "  3. Open any drawing" -ForegroundColor White
Write-Host "  4. Type: NETLOAD" -ForegroundColor Cyan
Write-Host "  5. Press: Ctrl+V (path already in clipboard!)" -ForegroundColor Cyan
Write-Host "  6. Click: Open" -ForegroundColor White
Write-Host "  7. Type: ACSE_UI_INTERACTIVE" -ForegroundColor Cyan
Write-Host "  8. Press: Enter" -ForegroundColor White
Write-Host ""
Write-Host "DLL Path (in clipboard):" -ForegroundColor Yellow
Write-Host "  $dllPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
