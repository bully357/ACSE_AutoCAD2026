╔══════════════════════════════════════════════════════════════╗
║           AUTO REBUILD SCRIPTS - QUICK REFERENCE            ║
╚══════════════════════════════════════════════════════════════╝

## 🚀 QUICK START

**To get a fresh DLL with ACSE_UI_INTERACTIVE command:**

```
Double-click: REBUILD_NOW.bat
```

That's it! The script will:
✅ Clean old files
✅ Rebuild everything fresh
✅ Verify DLL is current
✅ List all commands (including ACSE_UI_INTERACTIVE)
✅ Copy DLL path to clipboard
✅ Show you next steps

---

## 📂 SCRIPTS AVAILABLE

### **1. REBUILD_NOW.bat** ⭐ **RECOMMENDED**
   **What it does:**
   - Complete clean rebuild
   - Full verification
   - Command list
   - Clipboard ready
   
   **When to use:**
   - After making code changes
   - When command is missing
   - When DLL seems old
   - Before loading in AutoCAD

### **2. AUTO_REBUILD.bat** (Simple version)
   **What it does:**
   - Clean + rebuild
   - Basic verification
   - Clipboard copy
   
   **When to use:**
   - Quick rebuild needed
   - Don't need command list
   - Just want fresh DLL

### **3. CHECK_DLL_TIMESTAMP_NOW.bat**
   **What it does:**
   - Shows DLL timestamp
   - Compares to current time
   - Doesn't rebuild
   
   **When to use:**
   - Check if DLL is fresh
   - Verify last build worked
   - Before loading in AutoCAD

### **4. VERIFY_DLL.bat**
   **What it does:**
   - Full DLL analysis
   - Lists all commands
   - Timestamp check
   - Clipboard copy
   
   **When to use:**
   - Verify commands exist
   - Debug missing command
   - Check DLL contents

---

## 🎯 WORKFLOW EXAMPLES

### **Scenario 1: Made Code Changes**
```
1. Double-click: REBUILD_NOW.bat
2. Wait for "REBUILD COMPLETE!"
3. Check for ✓ ACSE_UI_INTERACTIVE in command list
4. Close AutoCAD
5. Restart AutoCAD
6. NETLOAD → Ctrl+V (paste DLL path)
7. Type: ACSE_UI_INTERACTIVE
8. Done! ✅
```

### **Scenario 2: Command Not Working**
```
1. Double-click: REBUILD_NOW.bat
2. Look for ACSE_UI_INTERACTIVE in output
3. If listed → DLL is good, reload in AutoCAD
4. If NOT listed → Code error, check build output
```

### **Scenario 3: Quick DLL Check**
```
1. Double-click: CHECK_DLL_TIMESTAMP_NOW.bat
2. Check timestamp vs current time
3. If fresh (< 10 min) → Good to load
4. If old → Run REBUILD_NOW.bat
```

### **Scenario 4: Verify Commands**
```
1. Double-click: VERIFY_DLL.bat
2. Check command list for ACSE_UI_INTERACTIVE
3. If found → Load in AutoCAD
4. If not found → Run REBUILD_NOW.bat
```

---

## ✅ SUCCESS INDICATORS

**After running REBUILD_NOW.bat, look for:**

```
✓ Clean succeeded!
✓ Build succeeded!
✓ DLL Found!
✓ DLL is FRESH! (built X seconds ago)
✓ Assembly loads successfully
Commands found:
  - ACSE_RUN
  - ACSE_TEMPLATE
  - ACSE_UI
  ✓ ACSE_UI_INTERACTIVE    ← MUST BE HERE!
  ...
✓ ACSE_UI_INTERACTIVE command found in DLL!
✓ DLL path copied to clipboard!
```

**If you see all these ✓ marks, DLL is ready to load!**

---

## 🐛 TROUBLESHOOTING

### **Problem: "Build failed!"**
**Cause:** Syntax errors in code

**Solution:**
1. Read error messages in output
2. Open Visual Studio
3. Check Error List (View → Error List)
4. Fix errors
5. Run REBUILD_NOW.bat again

### **Problem: "DLL NOT FOUND!"**
**Cause:** Build didn't create DLL

**Solution:**
1. Check if project file exists
2. Make sure you're in correct folder
3. Open Visual Studio
4. Build → Rebuild Solution manually
5. Check Output window for errors

### **Problem: "ACSE_UI_INTERACTIVE NOT FOUND in DLL!"**
**Cause:** Command wasn't compiled

**Solution:**
1. Open Visual Studio
2. Open: UiCommands.cs
3. Search for: ACSE_UI_INTERACTIVE
4. Verify [CommandMethod("ACSE_UI_INTERACTIVE")] exists
5. If missing → Add it back
6. Run REBUILD_NOW.bat again

### **Problem: "DLL seems old (X minutes)"**
**Cause:** Build used cached version

**Solution:**
1. Open Visual Studio
2. Build → Clean Solution
3. Delete bin\ and obj\ folders manually
4. Close Visual Studio
5. Run REBUILD_NOW.bat again

---

## 📊 COMPARISON: Scripts Overview

| Script                      | Clean? | Build? | Verify? | Commands? | Clipboard? |
|-----------------------------|--------|--------|---------|-----------|------------|
| REBUILD_NOW.bat             | ✅     | ✅     | ✅      | ✅        | ✅         |
| AUTO_REBUILD.bat            | ✅     | ✅     | ✅      | ❌        | ✅         |
| CHECK_DLL_TIMESTAMP_NOW.bat | ❌     | ❌     | ✅      | ❌        | ❌         |
| VERIFY_DLL.bat              | ❌     | ❌     | ✅      | ✅        | ✅         |

**Recommended:** Use **REBUILD_NOW.bat** for most tasks.

---

## 💡 PRO TIPS

### **Tip 1: Always Rebuild After Code Changes**
```
Changed any .cs file?
→ Run REBUILD_NOW.bat before testing
```

### **Tip 2: Close AutoCAD Before Rebuilding**
```
AutoCAD locks the DLL!
→ Close AutoCAD first
→ Then run REBUILD_NOW.bat
→ Then restart AutoCAD
```

### **Tip 3: Verify After Every Build**
```
Don't assume build worked!
→ Check for ACSE_UI_INTERACTIVE in output
→ Verify timestamp is fresh
→ Then load in AutoCAD
```

### **Tip 4: Use Clipboard Feature**
```
REBUILD_NOW.bat copies DLL path!
→ In AutoCAD: NETLOAD
→ Press Ctrl+V
→ Click Open
→ Done!
```

---

## 🎯 RECOMMENDED WORKFLOW

**Every time you modify code:**

```
1. Save all files in Visual Studio
2. Close AutoCAD (if running)
3. Double-click: REBUILD_NOW.bat
4. Wait for completion
5. Verify ACSE_UI_INTERACTIVE is listed
6. Start AutoCAD
7. NETLOAD → Ctrl+V
8. Type: ACSE_UI_INTERACTIVE
9. Test your changes
```

**This ensures:**
- Fresh DLL every time
- All commands compiled
- No old cached code
- Clean testing

---

╔══════════════════════════════════════════════════════════════╗
║              QUICK REFERENCE SUMMARY                        ║
╚══════════════════════════════════════════════════════════════╝

**Need fresh DLL?**
→ REBUILD_NOW.bat

**Made code changes?**
→ REBUILD_NOW.bat

**Command not working?**
→ REBUILD_NOW.bat

**Want to verify DLL?**
→ VERIFY_DLL.bat

**Just check timestamp?**
→ CHECK_DLL_TIMESTAMP_NOW.bat

**When in doubt:**
→ REBUILD_NOW.bat

═══════════════════════════════════════════════════════════════

**The DLL path is always copied to your clipboard,**
**ready to paste in AutoCAD's NETLOAD dialog!**

Just double-click REBUILD_NOW.bat and you're good to go! 🚀
