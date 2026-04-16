╔══════════════════════════════════════════════════════════════╗
║   🎯 ACSE PLUGIN - FINAL TEST CHECKLIST                     ║
╚══════════════════════════════════════════════════════════════╝

Date: ____________
Tester: ____________
AutoCAD Version: 2026

┌──────────────────────────────────────────────────────────────┐
│  ⚙️ PRE-TEST SETUP                                           │
└──────────────────────────────────────────────────────────────┘

□ AutoCAD is completely closed
□ Ran CLEANUP_ALL_OLD_DLLS.bat (optional but recommended)
□ Ran REBUILD_AND_TEST.bat 
□ Build succeeded with no errors
□ DLL timestamp is FRESH (within last 5 minutes)

DLL Path:
C:\Users\jdbul\source\repos\ACSE_AutoCAD2026\ACSE_AutoCAD2026\
bin\Debug\net8.0-windows\ACSE.AutoCAD2026.dll

DLL Timestamp: ________________
DLL Size: ______ KB

┌──────────────────────────────────────────────────────────────┐
│  🔧 TEST 1: CLEAN LOAD (No Duplicate Key Error)             │
└──────────────────────────────────────────────────────────────┘

Steps:
1. Open AutoCAD 2026
2. Type: NETLOAD
3. Select the DLL (path above)

Expected Results:
□ DLL loads successfully
□ NO "eDuplicateKey" error appears
□ NO crash occurs
□ Welcome message appears in command line OR popup

Actual Result: ________________

Notes:
_______________________________________________________
_______________________________________________________

┌──────────────────────────────────────────────────────────────┐
│  🎯 TEST 2: BASIC COMMANDS (No GC Crash)                    │
└──────────────────────────────────────────────────────────────┘

Test each command below:

□ ACSE_TEST
  Expected: Popup + detailed info
  Result: ________________

□ ACSE_PING
  Expected: "ACSE_PING ok." message
  Result: ________________

□ ACSE_RUN_SIMPLE
  Expected: Popup "ACSE_RUN_SIMPLE executed"
  Result: ________________

□ TESTCMD (if MinimalTest included)
  Expected: Popup "MINIMAL TEST WORKS"
  Result: ________________

All basic commands working? □ YES  □ NO

Notes:
_______________________________________________________
_______________________________________________________

┌──────────────────────────────────────────────────────────────┐
│  📊 TEST 3: COMPLIANCE SCANNING                             │
└──────────────────────────────────────────────────────────────┘

Prerequisites:
□ Drawing with entities loaded (lines, circles, text, etc.)
□ Standards.json exists at C:\ACSE\Config\Standards.json

Test ACSE_RUN:

□ Command executed
  Result: ________________

□ Entities scanned
  Count: ______

□ Violations found
  Count: ______
  Types: ________________________________

□ Compliance score displayed
  Score: ______%

Test ACSE_TEMPLATE:

□ Template file exists
  Path: ________________________________

□ Command executed
  Result: ________________

□ Template loaded successfully
  Layers: ______ Styles: ______ Linetypes: ______

□ Violations found
  Count: ______

Scanning working? □ YES  □ NO

Notes:
_______________________________________________________
_______________________________________________________

┌──────────────────────────────────────────────────────────────┐
│  🔨 TEST 4: AUTO-FIX ENGINE                                  │
└──────────────────────────────────────────────────────────────┘

Prerequisites:
□ Drawing has violations (from previous test)
□ Template loaded (ACSE_TEMPLATE ran successfully)

Test ACSE_FIX:

□ Command executed
  Result: ________________

□ Fixes applied
  Count: ______
  Types fixed: ________________________________

□ Re-scan shows fewer violations
  Before: ______ After: ______

□ Drawing visually changed (entities updated)
  Visual confirmation: □ YES  □ NO

Auto-fix working? □ YES  □ NO

Notes:
_______________________________________________________
_______________________________________________________

┌──────────────────────────────────────────────────────────────┐
│  🎨 TEST 5: WPF UI (Interactive Mode)                       │
└──────────────────────────────────────────────────────────────┘

Test ACSE_UI:

□ Command executed
  Result: ________________

□ WPF window opens
  □ YES  □ NO  □ Error: ________________

□ Window shows scan results
  Violations listed: ______

□ Buttons functional
  □ Scan button works
  □ Fix Selected works
  □ Close button works

□ Can close window without crash
  □ YES  □ NO

Test ACSE_UI_INTERACTIVE (if implemented):

□ Command executed
  Result: ________________

□ Interactive window opens
  □ YES  □ NO

□ Can select individual violations
  □ YES  □ NO

□ Can apply selective fixes
  □ YES  □ NO

□ Custom overrides work
  □ YES  □ NO  □ Not tested

UI working? □ YES  □ NO  □ Partially

Notes:
_______________________________________________________
_______________________________________________________

┌──────────────────────────────────────────────────────────────┐
│  🚨 TEST 6: EXIT CRASH FIX (CRITICAL!)                      │
└──────────────────────────────────────────────────────────────┘

This tests the GCHandle cleanup fix in AcsePlugin.Terminate()

Steps:
1. Use several commands (ACSE_TEST, ACSE_UI, ACSE_RUN, etc.)
2. Close any open ACSE windows
3. File → Exit AutoCAD
4. Wait for AutoCAD to close

Expected Results:
□ AutoCAD closes normally (no crash dialog)
□ Process terminates cleanly (check Task Manager)
□ NO "Application Error" or ".NET Runtime" errors
□ NO Windows Error Reporting dialog

Actual Result: ________________

□ Check log file: C:\ACSE\acse_debug.log
  Last few lines should contain:
  □ "Freed GCHandle for ComplianceCommands"
  □ "Freed GCHandle for TemplateCommands"
  □ "Freed GCHandle for ExtractCommands"
  □ "ACSE Plugin terminated successfully"

Exit clean? □ YES  □ NO

If crash occurred:
  - Check Windows Event Viewer
  - Application log → .NET Runtime errors
  - Event ID: ______
  - Error message: ________________________________

Notes:
_______________________________________________________
_______________________________________________________

┌──────────────────────────────────────────────────────────────┐
│  🔄 TEST 7: RELOAD TEST (Verify No Lock Issues)             │
└──────────────────────────────────────────────────────────────┘

Tests that DLL doesn't remain locked after unload

Steps:
1. Open AutoCAD
2. NETLOAD the DLL
3. Run ACSE_TEST
4. Type: NETUNLOAD
5. Select ACSE.AutoCAD2026.dll
6. Close AutoCAD
7. Rebuild DLL (run REBUILD_AND_TEST.bat)

Expected Results:
□ NETUNLOAD succeeds
□ Rebuild succeeds (DLL not locked)
□ New timestamp on DLL after rebuild

Actual Result: ________________

Reload working? □ YES  □ NO

Notes:
_______________________________________________________
_______________________________________________________

┌──────────────────────────────────────────────────────────────┐
│  📝 TEST 8: ADVANCED FEATURES                               │
└──────────────────────────────────────────────────────────────┘

Test ACSE_EXTRACT:

□ Command executed
  Result: ________________

□ Template file selected
  Path: ________________________________

□ Standards.json created/updated
  □ YES  □ NO

Extract working? □ YES  □ NO  □ Not tested

Test ACSE_LOAD_TEMPLATE:

□ Command executed
  Result: ________________

□ Template loaded
  □ YES  □ NO

Load template working? □ YES  □ NO  □ Not tested

Test ACSE_LIST_TEMPLATE_STYLES:

□ Command executed
  Result: ________________

□ Styles listed
  Count: ______

List styles working? □ YES  □ NO  □ Not tested

Notes:
_______________________________________________________
_______________________________________________________

┌──────────────────────────────────────────────────────────────┐
│  ✅ FINAL SUMMARY                                            │
└──────────────────────────────────────────────────────────────┘

Overall Test Results:

□ ALL TESTS PASSED - PRODUCTION READY
□ PARTIAL PASS - Minor issues (list below)
□ FAILED - Critical issues (list below)

Working Features:
_______________________________________________________
_______________________________________________________

Known Issues:
_______________________________________________________
_______________________________________________________

Critical Issues (must fix):
_______________________________________________________
_______________________________________________________

┌──────────────────────────────────────────────────────────────┐
│  📋 ADDITIONAL NOTES                                         │
└──────────────────────────────────────────────────────────────┘

Performance:
_______________________________________________________

Stability:
_______________________________________________________

User Experience:
_______________________________________________________

Recommendations:
_______________________________________________________
_______________________________________________________

┌──────────────────────────────────────────────────────────────┐
│  ✍️ SIGN-OFF                                                 │
└──────────────────────────────────────────────────────────────┘

Tested by: ________________
Date: ________________
Status: □ APPROVED  □ NEEDS WORK

Next Steps:
_______________________________________________________
_______________________________________________________
