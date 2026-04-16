╔══════════════════════════════════════════════════════════════╗
║   🎯 ACSE PLUGIN - COMPLETE TESTING & DEPLOYMENT GUIDE     ║
╚══════════════════════════════════════════════════════════════╝

Version: 3.0 (Final)
Date: May 2024
Status: Production Ready (pending final verification)

┌──────────────────────────────────────────────────────────────┐
│  📋 WHERE WE ARE NOW                                         │
└──────────────────────────────────────────────────────────────┘

✅ COMPLETED FIXES:

1. ✅ Duplicate Key Error (eDuplicateKey)
   Fixed: Removed duplicate command registrations
   File: AssemblyInfo.cs, command classes

2. ✅ Delegate GC Crashes (.NET 8 garbage collection)
   Fixed: Instance methods + GCHandle pinning
   Files: All command classes (Commands.cs, ComplianceCommands.cs,
          UiCommands.cs, TemplateCommands.cs, ExtractCommands.cs)

3. ✅ Exit Crash (Memory corruption on AutoCAD close)
   Fixed: GCHandle cleanup in Terminate() + WPF window close
   File: AcsePlugin.cs

4. ✅ All 13 Commands Working
   Commands: ACSE_TEST, ACSE_RUN, ACSE_TEMPLATE, ACSE_FIX,
             ACSE_UI, ACSE_UI_INTERACTIVE, ACSE_PING, etc.

5. ✅ Interactive Mode (WPF UI)
   Files: InteractiveScanWindow.xaml/.xaml.cs,
          InteractiveFixEngine.cs, ViolationViewModel.cs

6. ✅ Advanced Standards (MTEXT, DBTEXT, Dimensions)
   Files: StandardsModel.cs, EntityScanner.cs, FixEngine.cs

┌──────────────────────────────────────────────────────────────┐
│  🎯 WHAT NEEDS TESTING NOW                                   │
└──────────────────────────────────────────────────────────────┘

⚠️ CRITICAL TESTS (Must Pass):

1. Exit Crash Fix Verification
   Status: Fix implemented, needs testing
   Test: TEST_EXIT_CRASH_FIX.bat
   Goal: Confirm AutoCAD exits cleanly without crash

2. DLL Cleanup & Reload
   Status: Need to verify no old DLLs interfere
   Test: CLEANUP_ALL_OLD_DLLS.bat + REBUILD_AND_TEST.bat
   Goal: Ensure only fresh DLL is used

3. Full Command Suite
   Status: Individual commands tested, need full regression
   Test: FINAL_TEST_CHECKLIST.md
   Goal: All 13 commands working in one session

┌──────────────────────────────────────────────────────────────┐
│  📂 TESTING SCRIPTS PROVIDED                                 │
└──────────────────────────────────────────────────────────────┘

1. CLEANUP_ALL_OLD_DLLS.bat
   Purpose: Find and delete old/duplicate DLL builds
   When: Before final testing (optional but recommended)
   Output: Shows all ACSE DLLs with timestamps

2. REBUILD_AND_TEST.bat
   Purpose: Complete rebuild cycle with verification
   When: After code changes or before testing
   Features:
     - Checks if AutoCAD is running (must be closed)
     - Cleans old builds
     - Rebuilds DLL
     - Verifies fresh timestamp
     - Shows NETLOAD path
     - Optionally launches AutoCAD

3. TEST_EXIT_CRASH_FIX.bat
   Purpose: Verify exit crash fix is working
   When: After loading plugin in AutoCAD
   Features:
     - Shows log file before/after
     - Guides through testing steps
     - Checks for cleanup messages
     - Opens Event Viewer for errors
     - Summarizes test results

4. FINAL_TEST_CHECKLIST.md
   Purpose: Comprehensive testing checklist (printable)
   When: Final verification before production
   Covers:
     - Clean load test
     - All 13 commands
     - Compliance scanning
     - Auto-fix engine
     - WPF UI (both modes)
     - Exit crash test
     - Reload test
     - Advanced features

5. CHECK_DLL_TIMESTAMP_NOW.bat
   Purpose: Quick timestamp check
   When: Verify DLL is fresh after rebuild

┌──────────────────────────────────────────────────────────────┐
│  🚀 STEP-BY-STEP TESTING PROCEDURE                          │
└──────────────────────────────────────────────────────────────┘

PHASE 1: CLEANUP & REBUILD (10 minutes)
────────────────────────────────────────────────

1. Close AutoCAD completely
   - Exit all instances
   - Check Task Manager (no acad.exe)

2. Run: CLEANUP_ALL_OLD_DLLS.bat
   - Review all DLL locations
   - Type YES to clean (recommended)
   - Deletes bin/ and obj/ folders

3. Run: REBUILD_AND_TEST.bat
   - Verifies AutoCAD is closed
   - Cleans project
   - Rebuilds DLL
   - Shows fresh timestamp
   - Optionally launches AutoCAD

4. Verify fresh DLL exists:
   Path: C:\Users\jdbul\source\repos\ACSE_AutoCAD2026\
         ACSE_AutoCAD2026\bin\Debug\net8.0-windows\
         ACSE.AutoCAD2026.dll
   
   Timestamp: Should be NOW (within last few minutes)
   Size: ~83 KB

PHASE 2: BASIC FUNCTIONALITY TEST (15 minutes)
────────────────────────────────────────────────

5. Open AutoCAD 2026

6. NETLOAD the DLL (use exact path from REBUILD_AND_TEST.bat)

7. Expected: NO eDuplicateKey error, NO crash

8. Open or create a drawing

9. Test basic commands:
   - ACSE_TEST → Should show popup + info
   - ACSE_PING → Should show "ACSE_PING ok."
   - ACSE_RUN_SIMPLE → Should show popup

10. All working? Proceed to Phase 3

PHASE 3: EXIT CRASH TEST (10 minutes)
────────────────────────────────────────────────

11. With plugin loaded, run: TEST_EXIT_CRASH_FIX.bat

12. Follow the script instructions:
    - Run several commands (ACSE_TEST, ACSE_UI, etc.)
    - Close any ACSE windows
    - File → Exit AutoCAD
    - Observe: Does it close cleanly?

13. Script will check log file:
    Look for:
      "Freed GCHandle for ComplianceCommands"
      "Freed GCHandle for TemplateCommands"
      "Freed GCHandle for ExtractCommands"
      "ACSE Plugin terminated successfully"

14. Script opens Event Viewer:
    - Check for .NET Runtime errors
    - Event ID 1025 or 1000 = BAD (crash occurred)
    - No errors = GOOD (fix worked)

15. Result:
    ✅ Exit clean + cleanup messages = FIX WORKS!
    ❌ Crash or errors = Need more investigation

PHASE 4: FULL REGRESSION TEST (30 minutes)
────────────────────────────────────────────────

16. Print or open: FINAL_TEST_CHECKLIST.md

17. Restart AutoCAD (fresh session)

18. NETLOAD plugin again

19. Work through the checklist systematically:
    - Test 1: Clean load
    - Test 2: Basic commands
    - Test 3: Compliance scanning
    - Test 4: Auto-fix engine
    - Test 5: WPF UI
    - Test 6: Exit crash (repeat Phase 3)
    - Test 7: Reload test
    - Test 8: Advanced features

20. Mark each test: PASS / FAIL / NOTES

21. Complete final summary section

PHASE 5: PRODUCTION VERIFICATION (Optional)
────────────────────────────────────────────────

22. Test with real drawings (customer data if available)

23. Test template-based scanning:
    - Load FAA_002_acad.dwt or similar
    - Run ACSE_TEMPLATE
    - Verify violations detected correctly

24. Test auto-fix on real violations:
    - ACSE_FIX
    - Verify entities are corrected
    - Check compliance score improves

25. Test interactive mode:
    - ACSE_UI_INTERACTIVE
    - Select violations
    - Apply selective fixes
    - Verify custom overrides work

26. Stress test:
    - Large drawing (1000+ entities)
    - Multiple scan/fix cycles
    - Check performance and stability

┌──────────────────────────────────────────────────────────────┐
│  📊 EXPECTED RESULTS                                         │
└──────────────────────────────────────────────────────────────┘

If all fixes are working correctly:

✅ NETLOAD succeeds without eDuplicateKey error
✅ All 13 commands execute without crashes
✅ Compliance scanning works (ACSE_RUN, ACSE_TEMPLATE)
✅ Auto-fix applies corrections (ACSE_FIX)
✅ WPF UI opens and functions (ACSE_UI)
✅ Interactive mode allows selective fixes (ACSE_UI_INTERACTIVE)
✅ AutoCAD exits cleanly WITHOUT crash
✅ Log file shows GCHandle cleanup messages
✅ Event Viewer shows NO .NET Runtime errors
✅ DLL can be unloaded and reloaded
✅ Rebuild works without file locking issues

┌──────────────────────────────────────────────────────────────┐
│  🚨 TROUBLESHOOTING                                          │
└──────────────────────────────────────────────────────────────┘

PROBLEM: eDuplicateKey error on NETLOAD
Solution:
  1. Close AutoCAD
  2. Run CLEANUP_ALL_OLD_DLLS.bat
  3. Run REBUILD_AND_TEST.bat
  4. NETLOAD the fresh DLL

PROBLEM: AutoCAD crashes on command execution
Solution:
  1. Check DLL timestamp (must be fresh)
  2. Verify all command classes use instance methods
  3. Check log file for errors
  4. Review Windows Event Viewer

PROBLEM: AutoCAD crashes on exit
Solution:
  1. Verify Terminate() method exists in AcsePlugin.cs
  2. Verify FreeAllGCHandles() is called
  3. Verify CloseUi() is called before GCHandle cleanup
  4. Run TEST_EXIT_CRASH_FIX.bat for detailed diagnosis

PROBLEM: "Command not found" error
Solution:
  1. Verify AssemblyInfo.cs registers all command classes
  2. Check command class names match exactly
  3. Rebuild DLL
  4. NETLOAD fresh DLL

PROBLEM: WPF window doesn't open (ACSE_UI)
Solution:
  1. Check for null reference errors in log
  2. Verify WPF resources are included in build
  3. Check InteractiveScanWindow.xaml is embedded resource
  4. Verify System.Windows.Presentation reference exists

PROBLEM: Build fails with "file in use" error
Solution:
  1. Close AutoCAD completely
  2. Wait 5 seconds
  3. Run: dotnet clean
  4. Rebuild

PROBLEM: Old DLL keeps loading (wrong timestamp)
Solution:
  1. Check AutoCAD startup scripts for old NETLOAD paths
  2. Run CLEANUP_ALL_OLD_DLLS.bat to find all copies
  3. Delete old DLL files
  4. Rebuild and NETLOAD fresh DLL

┌──────────────────────────────────────────────────────────────┐
│  📁 FILE REFERENCE                                           │
└──────────────────────────────────────────────────────────────┘

Core Plugin Files:
  ACSE_AutoCAD2026/
  ├── AcsePlugin.cs           (Main entry, Terminate() with cleanup)
  ├── AssemblyInfo.cs          (Command registration)
  ├── Commands/
  │   ├── Commands.cs          (ACSE_TEST, ACSE_RUN_SIMPLE)
  │   ├── ComplianceCommands.cs (ACSE_RUN, ACSE_PING, ACSE_FIX, etc.)
  │   ├── UiCommands.cs        (ACSE_UI, ACSE_UI_INTERACTIVE)
  │   ├── TemplateCommands.cs  (ACSE_LOAD_TEMPLATE, etc.)
  │   └── ExtractCommands.cs   (ACSE_EXTRACT)
  ├── Compliance/
  │   ├── EntityScanner.cs     (Scan entities for violations)
  │   ├── FixEngine.cs         (Apply auto-fixes)
  │   ├── InteractiveFixEngine.cs (Selective/interactive fixes)
  │   └── ViolationType.cs     (Violation types enum)
  ├── Standards/
  │   └── StandardsModel.cs    (Standards data model)
  └── UI/
      ├── AcseScanWindow.xaml  (Simple WPF UI)
      ├── InteractiveScanWindow.xaml (Interactive WPF UI)
      └── ViolationViewModel.cs (UI data binding)

Testing Scripts:
  CLEANUP_ALL_OLD_DLLS.bat      (Find/delete old DLLs)
  REBUILD_AND_TEST.bat          (Complete rebuild cycle)
  TEST_EXIT_CRASH_FIX.bat       (Verify exit crash fix)
  CHECK_DLL_TIMESTAMP_NOW.bat   (Quick timestamp check)

Documentation:
  FINAL_TEST_CHECKLIST.md       (Printable test checklist)
  THIS FILE                     (Master testing guide)
  EXIT_CRASH_FIX.md             (Exit crash fix details)
  FINAL_TEST_GUIDE.md           (Testing instructions)
  VERSION_3_0_SUMMARY.md        (Feature summary)

Output Files:
  C:\ACSE\acse_debug.log        (Plugin debug log)
  C:\ACSE\Config\Standards.json (Standards configuration)

DLL Location:
  C:\Users\jdbul\source\repos\ACSE_AutoCAD2026\
  ACSE_AutoCAD2026\bin\Debug\net8.0-windows\
  ACSE.AutoCAD2026.dll

┌──────────────────────────────────────────────────────────────┐
│  ✅ FINAL SIGN-OFF CRITERIA                                  │
└──────────────────────────────────────────────────────────────┘

Plugin is PRODUCTION READY when:

□ CLEANUP_ALL_OLD_DLLS.bat run (optional)
□ REBUILD_AND_TEST.bat run successfully
□ DLL timestamp is fresh (within last hour)
□ DLL size is correct (~83 KB)
□ NETLOAD succeeds without eDuplicateKey error
□ All basic commands tested (ACSE_TEST, ACSE_PING, etc.)
□ TEST_EXIT_CRASH_FIX.bat shows PASS
  - AutoCAD exits cleanly
  - Cleanup messages in log
  - No Event Viewer errors
□ FINAL_TEST_CHECKLIST.md completed
  - All critical tests passed
  - Known issues documented
  - Sign-off completed

□ PRODUCTION DEPLOYMENT APPROVED: _____ (initials/date)

┌──────────────────────────────────────────────────────────────┐
│  🚀 NEXT STEPS AFTER TESTING                                 │
└──────────────────────────────────────────────────────────────┘

If all tests pass:
  1. Archive test results (save completed checklist)
  2. Tag code repository (v3.0-production)
  3. Create deployment package:
     - Copy DLL to deployment folder
     - Include Standards.json template
     - Include user documentation
  4. Deploy to production AutoCAD installations
  5. Monitor for issues in production use

If issues found:
  1. Document in FINAL_TEST_CHECKLIST.md
  2. Prioritize: Critical / High / Medium / Low
  3. Fix critical issues first
  4. Re-run full test cycle
  5. Repeat until all critical tests pass

┌──────────────────────────────────────────────────────────────┐
│  📞 SUPPORT INFORMATION                                      │
└──────────────────────────────────────────────────────────────┘

Debug Log: C:\ACSE\acse_debug.log
Event Viewer: eventvwr.msc → Windows Logs → Application
DLL Path: [See File Reference section above]

Common Log Messages:
  - "ACSE Plugin initialized successfully" = Good
  - "Freed GCHandle for..." = Exit cleanup working
  - "ACSE Plugin terminated successfully" = Clean shutdown
  - "ERROR:" = Problem occurred (read full message)

Common Event Viewer Errors:
  - Event ID 1025 (.NET Runtime) = Delegate GC crash
  - Event ID 1000 (Application Error) = Unhandled exception

═══════════════════════════════════════════════════════════════

               🎉 ACSE PLUGIN v3.0 - READY TO TEST! 🎉

═══════════════════════════════════════════════════════════════
