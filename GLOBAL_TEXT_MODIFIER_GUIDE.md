# 🎯 ACSE GLOBAL TEXT PROPERTY MODIFIER - USER GUIDE

## ✅ **NEW FEATURE ADDED!**

### **Command:** `ACSE_GLOBAL_TEXT`

---

## 📋 **WHAT IT DOES:**

The Global Text Property Modifier allows you to **scan and modify all Text/MText entities** in your drawing in one operation!

### **Key Features:**
- ✅ Scan entire drawing for Text/MText
- ✅ Filter by current properties (style, layer, font, height, etc.)
- ✅ Bulk modify properties:
  - Text Style
  - Text Height
  - Annotative (Yes/No)
  - Layer
  - Justification (DBText)
  - Attachment Point (MText)
  - Rotation
  - Width Factor
- ✅ Preview changes before applying
- ✅ Select specific entities to modify

---

## 🚀 **HOW TO USE:**

### **Step 1: Launch the Tool**
```
Command: ACSE_GLOBAL_TEXT
```

A WPF window will open with 3 tabs:
1. **Filter & Scan** - Find text entities
2. **Modify Properties** - Set new values
3. **Help** - Quick reference

---

### **Step 2: Filter & Scan (Tab 1)**

#### **Option A: Scan ALL Text (No Filter)**
1. Leave all filter fields blank
2. Click **"🔍 Scan Drawing"**
3. All text/mtext in the drawing will be found

#### **Option B: Scan with Filters (Find Specific Text)**
1. Set filter criteria:
   - **Text Style:** e.g., "Arial", "Romans"
   - **Layer:** e.g., "Text", "Dimensions"
   - **Font File:** e.g., "arial", "romans.shx"
   - **Height Range:** Min and/or Max height
   - **Annotative Only:** Check if you only want annotative text
2. Click **"🔍 Scan Drawing"**
3. Only text matching your filters will be found

#### **Example Filter Scenarios:**

**Find all text with "Arial" style on "Text" layer:**
- Text Style: `Arial`
- Layer: `Text`
- Click Scan

**Find all non-annotative text with height between 0.1 and 0.2:**
- Annotative Only: ☐ (unchecked)
- Min Height: `0.1`
- Max Height: `0.2`
- Click Scan

**Find all text using "romans.shx" font:**
- Font File: `romans`
- Click Scan

---

### **Step 3: Review Results**

After scanning, the grid shows:
- **Type:** DBText or MText
- **Text Style:** Current style name
- **Layer:** Current layer
- **Height:** Current height
- **Rotation:** Current rotation (degrees)
- **Width Factor:** Current width factor
- **Annot:** Is it annotative? (✓ or blank)
- **Text Preview:** First 50 characters

#### **Select Entities to Modify:**
- **Check boxes** in the "Select" column
- Or use buttons:
  - **Select All** - Check all entities
  - **Select None** - Uncheck all entities
  - **Invert Selection** - Toggle selection

---

### **Step 4: Modify Properties (Tab 2)**

1. **Check the properties you want to change**
   - Only checked properties will be modified
   - Unchecked properties remain unchanged

2. **Set new values:**

   **Text Properties:**
   - ☑ **Text Style:** Select from dropdown (available styles in drawing)
   - ☑ **Height:** Enter new height value (e.g., `0.125`)
   - ☑ **Annotative:** Yes or No
   - ☑ **Layer:** Select from dropdown (available layers in drawing)
   - ☑ **Rotation:** Enter angle in degrees (e.g., `90`, `-45`)
   - ☑ **Width Factor:** Enter factor (e.g., `0.8`, `1.0`, `1.2`)

   **DBText-Specific:**
   - ☑ **Justification:**
     - Horizontal: Left, Center, Right, Aligned, Middle, Fit
     - Vertical: Baseline, Bottom, Middle, Top

   **MText-Specific:**
   - ☑ **Attachment:** Top Left, Top Center, Top Right, etc.

3. **Preview changes** (optional but recommended):
   - Click **"👁️ Preview Changes"**
   - See summary of what will be modified
   - NO actual changes made yet

4. **Apply changes:**
   - Click **"✅ Apply Changes"**
   - Confirm the dialog
   - Changes are applied!

---

## 💡 **USAGE EXAMPLES:**

### **Example 1: Change All "Arial" Text to "Romans"**

**Scenario:** You want to change all text using "Arial" style to "Romans" style.

**Steps:**
1. Launch: `ACSE_GLOBAL_TEXT`
2. **Tab 1 (Filter & Scan):**
   - Text Style: `Arial`
   - Click **Scan**
3. **Select All** entities found
4. **Tab 2 (Modify):**
   - ☑ Text Style
   - Select: `Romans`
   - Click **Preview** (optional)
   - Click **Apply**
5. Done! All Arial text is now Romans.

---

### **Example 2: Make All Text on "Dimensions" Layer Annotative**

**Scenario:** You want to make all text on the "Dimensions" layer annotative with height 0.125.

**Steps:**
1. Launch: `ACSE_GLOBAL_TEXT`
2. **Tab 1 (Filter & Scan):**
   - Layer: `Dimensions`
   - Click **Scan**
3. **Select All** entities found
4. **Tab 2 (Modify):**
   - ☑ Annotative: `Yes`
   - ☑ Height: `0.125`
   - Click **Preview** (optional)
   - Click **Apply**
5. Done! All text on Dimensions layer is now annotative at 0.125 height.

---

### **Example 3: Change Small Text to Larger Size**

**Scenario:** You want to increase all text smaller than 0.08 to 0.125.

**Steps:**
1. Launch: `ACSE_GLOBAL_TEXT`
2. **Tab 1 (Filter & Scan):**
   - Max Height: `0.08`
   - Click **Scan**
3. **Select All** entities found
4. **Tab 2 (Modify):**
   - ☑ Height: `0.125`
   - Click **Preview** (optional)
   - Click **Apply**
5. Done! All small text is now 0.125.

---

### **Example 4: Move All Text from "Layer1" to "Text" Layer**

**Scenario:** You want to move all text from "Layer1" to "Text" layer.

**Steps:**
1. Launch: `ACSE_GLOBAL_TEXT`
2. **Tab 1 (Filter & Scan):**
   - Layer: `Layer1`
   - Click **Scan**
3. **Select All** entities found
4. **Tab 2 (Modify):**
   - ☑ Layer: `Text`
   - Click **Preview** (optional)
   - Click **Apply**
5. Done! All text moved to Text layer.

---

### **Example 5: Rotate All Vertical Text to Horizontal**

**Scenario:** You want to rotate all vertical text (90°) to horizontal (0°).

**Steps:**
1. Launch: `ACSE_GLOBAL_TEXT`
2. **Tab 1 (Filter & Scan):**
   - (No filter - scan all text)
   - Click **Scan**
3. Manually **check only text** with Rotation = 90° (or near 90°)
4. **Tab 2 (Modify):**
   - ☑ Rotation: `0`
   - Click **Preview** (optional)
   - Click **Apply**
5. Done! Selected text is now horizontal.

---

## ⚠️ **IMPORTANT NOTES:**

### **Before Applying Changes:**
- ✅ **Always Preview first** to verify changes
- ✅ **Select only what you want to modify** (use filters!)
- ✅ **Make sure target style/layer exists** in the drawing
- ✅ **Save your drawing first** (in case you need to revert)

### **After Applying Changes:**
- ✅ Changes are immediate (cannot undo in the tool)
- ✅ Use **AutoCAD's UNDO command** to revert if needed
- ✅ Rescan to verify changes were applied correctly

### **Limitations:**
- ❌ Annotative scale assignment requires additional work (just toggles on/off)
- ❌ Width Factor only affects DBText (not MText)
- ❌ Justification only affects DBText (MText uses Attachment)

---

## 🎯 **TIPS & BEST PRACTICES:**

### **Tip 1: Use Filters to Target Specific Text**
Instead of scanning all text and manually selecting, use filters to find exactly what you need.

**Example:** Find all non-annotative text:
- Annotative Only: ☐ (unchecked but set to False state)
- This finds only non-annotative text

### **Tip 2: Combine Multiple Changes**
You can modify multiple properties at once!

**Example:** Change style AND height AND layer:
- ☑ Text Style: `Romans`
- ☑ Height: `0.125`
- ☑ Layer: `Text`
- Apply once - all three properties change!

### **Tip 3: Preview Before Applying**
Always click **Preview** first to see:
- How many entities will be modified
- What properties will change
- Summary of changes

### **Tip 4: Save Your Drawing First**
Before making bulk changes:
```
Command: QSAVE
```
This allows you to revert if something goes wrong.

### **Tip 5: Use UNDO if Needed**
If you apply changes and don't like the result:
```
Command: UNDO
```
This reverts the last change.

---

## 🔧 **TROUBLESHOOTING:**

### **Problem: "No entities selected"**
**Solution:** Make sure you checked the boxes in the "Select" column after scanning.

### **Problem: "No properties selected for modification"**
**Solution:** Check at least one property checkbox in the Modify Properties tab.

### **Problem: "Text style not found"**
**Solution:** Make sure the target text style exists in the drawing. Use STYLE command to create it first.

### **Problem: "Layer not found"**
**Solution:** Make sure the target layer exists in the drawing. Use LAYER command to create it first.

### **Problem: Text didn't change**
**Solution:** 
1. Check if you had the entity selected (checkbox in grid)
2. Check if you had the property checkbox checked
3. Try rescanning to verify changes

---

## 📁 **RELATED COMMANDS:**

- **ACSE_UI** - Simple compliance scanner
- **ACSE_UI_INTERACTIVE** - Interactive compliance scanner with custom fixes
- **ACSE_TEMPLATE** - Template-based standards scanning
- **ACSE_FIX** - Auto-fix compliance violations

---

## 🎊 **SUMMARY:**

The Global Text Property Modifier is a **powerful tool for bulk text modifications**!

**Use it when you need to:**
- Change text styles across entire drawing
- Standardize text heights
- Convert text to annotative
- Move text to correct layers
- Fix text justification/attachment
- Update rotations or width factors

**Key Benefits:**
- ⚡ **Fast:** Modify hundreds of text entities in seconds
- 🎯 **Precise:** Filter to target specific text
- 👁️ **Safe:** Preview before applying
- 🔄 **Flexible:** Modify any combination of properties

---

## 🚀 **GET STARTED NOW:**

```
Command: ACSE_GLOBAL_TEXT
```

Happy text modifying! 🎉

---

**Questions or Issues?**
Check the Help tab in the tool for quick reference!
