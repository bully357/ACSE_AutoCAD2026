╔══════════════════════════════════════════════════════════════╗
║         🔧 WPF UI CRASH FIX APPLIED! 🔧                      ║
╚══════════════════════════════════════════════════════════════╝

┌──────────────────────────────────────────────────────────────┐
│  ❌ PROBLEM IDENTIFIED                                       │
└──────────────────────────────────────────────────────────────┘

ERROR:
  "Cannot create more than one System.Windows.Application 
   instance in the same AppDomain."

WHEN IT OCCURRED:
  - First run of ACSE_UI: ✅ WORKED
  - Second run of ACSE_UI: ❌ CRASHED

ROOT CAUSE:
  WPF requires a System.Windows.Application instance to run.
  AutoCAD plugins run in a single AppDomain, so you can only
  create ONE Application instance per AutoCAD session.
  
  The old code didn't create an Application, assuming AutoCAD
  would provide one - but WPF needs it explicitly.

┌──────────────────────────────────────────────────────────────┐
│  ✅ FIX APPLIED                                              │
└──────────────────────────────────────────────────────────────┘

FILE: Commands\UiCommands.cs

CHANGED:
  OLD (Lines 49-54):
    // Optional: Log for debugging
    // LogToFile("ACSE_UI command started");
    
    // WPF dispatcher should already be initialized by AutoCAD
    // DO NOT create new Application instance - causes crash on second run
    System.Windows.Application.Current?.Dispatcher.Invoke(() => { });

  NEW:
    // Ensure WPF Application instance exists (create only once)
    if (System.Windows.Application.Current == null)
    {
        ed.WriteMessage("\n[DEBUG] Creating WPF Application instance...");
        new System.Windows.Application();
    }

EXPLANATION:
  - Check if Application.Current exists
  - If it doesn't exist (first run), create it
  - If it exists (second+ run), reuse it
  - Now ACSE_UI can be run multiple times without crash

┌──────────────────────────────────────────────────────────────┐
│  🧪 TESTING - EXACTLY LIKE BEFORE BUT...                     │
└──────────────────────────────────────────────────────────────┘

CRITICAL: You MUST reload the NEW DLL!

STEP 1: CLOSE AUTOCAD COMPLETELY
  - Exit AutoCAD to clear the old DLL from memory

STEP 2: RESTART AUTOCAD

STEP 3: NETLOAD THE NEWLY BUILT DLL
  Command: NETLOAD
  
  Path:
  C:\Users\jdbul\source\repos\ACSE_AutoCAD2026\ACSE_AutoCAD2026\bin\Debug\net8.0-windows\ACSE.AutoCAD2026.dll
  
  Expected:
    ✅ Loads successfully
    ✅ No errors

STEP 4: OPEN OR CREATE A DRAWING
  Expected:
    ✅ Welcome message with all commands listed

STEP 5: TEST THE ACSE_UI COMMAND **MULTIPLE TIMES**
  
  First run:
  ─────────
  Command: ACSE_UI
  
  Expected output:
    [DEBUG] ACSE_UI command started
    [DEBUG] Creating WPF Application instance...
    [DEBUG] WPF context initialized
    [DEBUG] Creating new AcseScanWindow...
    [DEBUG] ACSE Compliance Scanner UI shown.
  
  Expected result:
    ✅ WPF window opens
    ✅ No crash
  
  Second run (CLOSE THE WINDOW FIRST):
  ──────────────────────────────────────
  Command: ACSE_UI
  
  Expected output:
    [DEBUG] ACSE_UI command started
    [DEBUG] WPF context initialized
    [DEBUG] Creating new AcseScanWindow...
    [DEBUG] ACSE Compliance Scanner UI shown.
  
  Expected result:
    ✅ WPF window opens AGAIN
    ✅ NO CRASH! (this was failing before)
  
  Third run (WINDOW STILL OPEN):
  ────────────────────────────────
  Command: ACSE_UI
  
  Expected output:
    [DEBUG] ACSE_UI command started
    [DEBUG] WPF context initialized
    [DEBUG] Activating existing window...
    [DEBUG] ACSE Compliance Scanner UI shown.
  
  Expected result:
    ✅ Existing window comes to front
    ✅ No new window created
    ✅ No crash

┌──────────────────────────────────────────────────────────────┐
│  ✅ SUCCESS CRITERIA                                         │
└──────────────────────────────────────────────────────────────┘

ACSE_UI command should work:
  [ ] First time (window closed)
  [ ] Second time (window closed)
  [ ] Third time (window closed)
  [ ] When window is already open (reactivates)
  [ ] After closing and reopening multiple times
  [ ] No "Application instance" error
  [ ] No crashes

┌──────────────────────────────────────────────────────────────┐
│  ⚠️ TROUBLESHOOTING                                          │
└──────────────────────────────────────────────────────────────┘

❌ Still getting "Application instance" error
   → You loaded the OLD DLL
   → Check DLL timestamp matches current build time
   → Delete deploy\ folder to avoid confusion
   → Restart AutoCAD and load from bin\Debug\net8.0-windows\

❌ Different error now
   → Report the new error message
   → This fix was specific to the Application crash

❌ Window doesn't appear
   → Check AutoCAD command line for errors
   → Check C:\ACSE\acse_debug.log for WPF errors

❌ Other commands still working?
   → Test TESTCMD and HELLO to verify base functionality
   → UI issue is isolated to ACSE_UI command

┌──────────────────────────────────────────────────────────────┐
│  📊 WHAT THIS FIX DOES                                       │
└──────────────────────────────────────────────────────────────┘

BEFORE FIX:
  Run 1: WPF tries to use Application.Current (null) → creates one internally → works
  Run 2: WPF tries to create another Application → ERROR!

AFTER FIX:
  Run 1: Code explicitly creates Application.Current → works
  Run 2: Code sees Application.Current exists → reuses it → works
  Run N: Code always reuses existing Application.Current → works

TECHNICAL:
  - System.Windows.Application is a singleton class
  - Only one instance can exist per AppDomain
  - AutoCAD plugins run in one AppDomain
  - Creating it explicitly and checking before creation = safe

┌──────────────────────────────────────────────────────────────┐
│  🎯 NEXT TEST                                                │
└──────────────────────────────────────────────────────────────┘

After confirming ACSE_UI works multiple times:

1. Test the SCAN functionality in the UI
2. Test the FIX ALL button
3. Test the RESET button
4. Verify UI updates correctly
5. Test closing and reopening while AutoCAD has drawings open

╔══════════════════════════════════════════════════════════════╗
║       BUILD SUCCESSFUL - READY TO TEST! 🚀                   ║
╚══════════════════════════════════════════════════════════════╝

Build time: [Check timestamp of ACSE.AutoCAD2026.dll]
Location: bin\Debug\net8.0-windows\ACSE.AutoCAD2026.dll
