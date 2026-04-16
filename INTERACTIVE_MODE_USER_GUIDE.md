╔══════════════════════════════════════════════════════════════╗
║   📖 INTERACTIVE MODE - QUICK START GUIDE 📖                ║
╚══════════════════════════════════════════════════════════════╝

## 🚀 Getting Started in 3 Minutes

### **Step 1: Launch Interactive Scanner**
```
AutoCAD Command Line:
> ACSE_UI_INTERACTIVE

Result: Interactive compliance window opens
```

### **Step 2: Scan Your Drawing**
```
1. Set template path (or use default):
   C:\ACSE\config\Standards\FAA_002_acad.dwt

2. Click [Scan] button

3. Wait for scan to complete (1-5 seconds)

4. Review violations in grid
```

### **Step 3: Review & Customize**
```
Grid shows violations:
┌──────┬─────────┬────────┬────────┬─────────┬─────────┐
│ Fix? │ Type    │ Entity │ Current│ Fix To  │ Standard│
├──────┼─────────┼────────┼────────┼─────────┼─────────┤
│  ☑   │TextFont │ MText  │ Arial  │romans.shx│romans.shx│
│  ☑   │TextSize │ MText  │ 0.250  │ 0.125   │ 0.125   │
│  ☑   │Layer    │ MText  │ NOTES  │ A-NOTES │ A-NOTES │
└──────┴─────────┴────────┴────────┴─────────┴─────────┘

To customize:
- Uncheck "Fix?" to skip a violation
- Click "Fix To" cell to change value
- Select from dropdown (for styles/layers)
- Type new value (for fonts/sizes)
```

### **Step 4: Apply Fixes**
```
Option A (Selected only):
- Check violations to fix
- Click [Fix Selected]
- Confirm

Option B (All auto-fixable):
- Click [Fix All Auto-Fixable]
- Confirm

Result: Violations fixed and removed from grid
```

---

## 📋 Common Workflows

### **Workflow 1: Fix All with Defaults**
```
1. Launch: ACSE_UI_INTERACTIVE
2. Click: [Scan]
3. Review violations (all checked by default)
4. Click: [Fix All Auto-Fixable]
5. Click: [Yes] to confirm
6. Done! (violations removed)
```
⏱️ **Time:** 30 seconds  
🎯 **Use when:** Standards are correct, no overrides needed

---

### **Workflow 2: Customize Fonts for Specific Entities**
```
Problem: Some MTEXT should use Arial, others romans.shx

Steps:
1. Launch: ACSE_UI_INTERACTIVE
2. Click: [Scan]
3. Grid shows TextFont violations
4. For title entities (layer A-TITLE):
   - Click "Fix To" cell
   - Change to "Arial"
5. For note entities (layer A-NOTES):
   - Keep as "romans.shx"
6. Click: [Fix Selected]
7. Click: [Rescan] to verify

Result: Mixed fonts applied correctly
```
⏱️ **Time:** 2 minutes  
🎯 **Use when:** Different layers need different fonts

---

### **Workflow 3: Preview Before Committing**
```
Problem: Unsure if fixes are correct

Steps:
1. Launch: ACSE_UI_INTERACTIVE
2. Click: [Scan]
3. Select violations (check boxes)
4. Customize "Fix To" values if needed
5. Click: [Preview Selected]
6. Review command line output:
   "[PREVIEW] Would fix MText 4AF"
   "[PREVIEW] Would fix MText 4B3"
7. If OK → Click: [Fix Selected]
   If not → Adjust selections, repeat preview

Result: No surprises, confident fixes
```
⏱️ **Time:** 3 minutes  
🎯 **Use when:** Complex or critical drawings

---

### **Workflow 4: Fix Only Specific Layers**
```
Problem: Only want to fix violations on A-WALL layer

Steps:
1. Launch: ACSE_UI_INTERACTIVE
2. Click: [Scan]
3. Click column header "Layer" to sort by layer
4. Click: [Select None] (uncheck all)
5. Check only violations where Layer = "A-WALL"
6. Click: [Fix Selected]

Result: Only A-WALL layer fixed, others unchanged
```
⏱️ **Time:** 1 minute  
🎯 **Use when:** Fixing layer-by-layer

---

### **Workflow 5: Iterative Fixing**
```
Problem: Want to fix in stages

Steps:
1. Launch: ACSE_UI_INTERACTIVE
2. Click: [Scan] (50 violations found)

Stage 1: Fix Text Styles
3. Uncheck: [DimStyle], [Layer], [Linetype] filters
4. Grid shows only TextStyle violations
5. Click: [Fix All Auto-Fixable]
6. Violations fixed

Stage 2: Fix Layers
7. Check: [Layer] filter, uncheck others
8. Grid shows only Layer violations
9. Customize layer assignments
10. Click: [Fix Selected]

Stage 3: Fix Dimensions
11. Check: [DimStyle] filter, uncheck others
12. Fix dimension violations

13. Click: [Rescan]
14. Verify: 0 violations

Result: Organized, controlled fixing
```
⏱️ **Time:** 5 minutes  
🎯 **Use when:** Many violations, complex drawing

---

## 🎨 UI Controls Reference

### **Top Bar**
```
┌──────────────────────────────────────────────────┐
│ Template: [C:\...\FAA_002.dwt ] [Scan] [O] [Rescan]│
└──────────────────────────────────────────────────┘
       ▲            ▲        ▲   ▲      ▲
       │            │        │   │      │
    Template     Scan     Toggle │  Rescan
     path       button   Interactive
                             │
                        (always on)
```

### **Summary Bar**
```
┌──────────────────────────────────────────────────┐
│ Drawing: Floor1.dwg  │ Violations: 23 total     │
│ Selected: 15 selected │ Score: 76.3%            │
└──────────────────────────────────────────────────┘
```

### **Filter Bar**
```
┌──────────────────────────────────────────────────┐
│ Filter: [✓]TextStyle [✓]DimStyle [✓]Layer [✓]Linetype │
│ [Select All] [Select None] [Invert]             │
└──────────────────────────────────────────────────┘
```

### **Bottom Bar**
```
┌──────────────────────────────────────────────────┐
│      [Preview Selected] [Fix Selected] [Fix All] [Close] │
└──────────────────────────────────────────────────┘
        Purple          Green        Orange
       (preview)        (apply)      (batch)
```

---

## 💡 Pro Tips

### **Tip 1: Use Filters to Focus**
```
✅ DO: Uncheck filters to hide unwanted violations
   Example: Fixing only text? Uncheck DimStyle/Layer/Linetype
   
❌ DON'T: Manually uncheck hundreds of violations
   Use filters instead!
```

### **Tip 2: Sort Grid for Efficiency**
```
✅ DO: Click column headers to sort
   - Sort by Layer → Fix layer-by-layer
   - Sort by Type → Fix all fonts together
   - Sort by Entity → Fix all MTEXT together
   
❌ DON'T: Randomly select violations
   Organize first!
```

### **Tip 3: Select All, Then Uncheck Exceptions**
```
✅ DO: [Select All] → Uncheck specific rows
   Faster than checking many rows individually
   
❌ DON'T: Check 50 rows one-by-one
   Use Select All + exceptions!
```

### **Tip 4: Preview When Uncertain**
```
✅ DO: [Preview Selected] before [Fix Selected]
   Verify in command line first
   No harm in previewing multiple times
   
❌ DON'T: [Fix All] without reviewing
   Can't undo easily!
```

### **Tip 5: Rescan After Each Stage**
```
✅ DO: Click [Rescan] after fixing
   Verifies fixes were applied
   Shows new violations (if any)
   
❌ DON'T: Assume all fixed
   Always verify!
```

---

## ❓ FAQ

### **Q: How do I change the template path?**
A: Click in the "Template:" textbox at top, type new path, click Scan.

### **Q: Can I edit "Fix To" values for multiple rows?**
A: Currently no. Must edit each row individually. (Future enhancement)

### **Q: What if I check the wrong violations?**
A: Click [Select None] to uncheck all, then re-select.

### **Q: How do I fix only one violation?**
A: 1) Click [Select None], 2) Check that one violation, 3) Click [Fix Selected].

### **Q: Can I save my custom "Fix To" values?**
A: Not yet. Feature planned for future release.

### **Q: What if [Fix Selected] fails?**
A: Check command line for error message. Common causes:
   - Entity locked
   - Layer frozen
   - Invalid font name
   - Circular reference

### **Q: How to undo fixes?**
A: Use AutoCAD's U (undo) command immediately after fixing.

### **Q: Difference between [Fix Selected] and [Fix All]?**
A: 
- **Fix Selected** → Only checked violations
- **Fix All** → All auto-fixable (ignores checkboxes)

### **Q: Can I run both ACSE_UI and ACSE_UI_INTERACTIVE?**
A: Yes, but use one at a time. Close previous window first.

### **Q: Does it work with xrefs?**
A: Currently scans main drawing only. Xref support planned.

---

## 📊 Before/After Example

### **Before Scan:**
```
Drawing: Floor1.dwg
Violations: UNKNOWN
Score: UNKNOWN
```

### **After Scan:**
```
Drawing: Floor1.dwg
Violations: 23 total
Score: 76.3%

Breakdown:
- 8 TextFont violations
- 5 TextSize violations
- 6 Layer violations
- 4 Linetype violations
```

### **After Customization:**
```
Selected for fix:
- 8 TextFont (change to "Arial")
- 3 TextSize (leave 0.250 for titles)
- 6 Layer (auto-fix to approved)
- 4 Linetype (auto-fix to ByLayer)

Not selected:
- 2 TextSize (keep as-is for titles)
```

### **After Fix:**
```
Drawing: Floor1.dwg
Violations: 2 total
Score: 96.5%

Remaining:
- 2 TextSize (intentionally skipped)
```

---

## ⚡ Quick Reference Card

```
┌────────────────────────────────────────┐
│ ACSE INTERACTIVE MODE QUICK REFERENCE │
├────────────────────────────────────────┤
│ Launch:  ACSE_UI_INTERACTIVE          │
│ Scan:    Click [Scan] button          │
│ Select:  Check "Fix?" boxes           │
│ Custom:  Edit "Fix To" cells          │
│ Preview: Click [Preview Selected]     │
│ Fix:     Click [Fix Selected]         │
│ Batch:   Click [Fix All]              │
│ Verify:  Click [Rescan]               │
├────────────────────────────────────────┤
│ Filters: Check/uncheck violation types│
│ Sort:    Click column headers         │
│ Select:  [All] [None] [Invert]        │
├────────────────────────────────────────┤
│ Undo:    AutoCAD U command            │
│ Close:   Click [Close] or X           │
└────────────────────────────────────────┘
```

Print this page for quick reference! 📄

╔══════════════════════════════════════════════════════════════╗
║   Ready to use Interactive Mode! 🚀                         ║
║   Run: ACSE_UI_INTERACTIVE                                  ║
╚══════════════════════════════════════════════════════════════╝
