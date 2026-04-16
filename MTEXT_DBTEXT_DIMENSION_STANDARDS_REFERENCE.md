╔══════════════════════════════════════════════════════════════╗
║   📋 MTEXT, DBTEXT & DIMENSION STANDARDS REFERENCE 📋       ║
╚══════════════════════════════════════════════════════════════╝

This document lists ALL standards checked for text and dimension entities.

┌──────────────────────────────────────────────────────────────┐
│  📝 MTEXT STANDARDS (Multi-line Text)                        │
└──────────────────────────────────────────────────────────────┘

**BASIC CHECKS:**

1. TS-002: Text Style Compliance
   What it checks: MText must use approved text style from template
   Standard property: RequiredTextStyle OR ApprovedTextStyles
   Example: "Standard", "ROMANS", "ARIAL"
   Fix: Changes TextStyleId to required style
   
2. TS-004: Font File Type (SHX Fonts)
   What it checks: MText text style must use .shx font
   Standard property: ApprovedTextFontFiles
   Example: ["romans.shx", "txt.shx"]
   Fix: Not auto-fixable (manual style edit required)

**ADVANCED CHECKS (NEW):**

3. TF-001: Font Name Match
   What it checks: MText font must match required font exactly
   Standard property: RequiredFont
   Example: "romans.shx" or "Arial"
   Fix: Updates TextStyleTableRecord.FileName or .Font
   
4. TH-001: Text Height Match
   What it checks: MText height must match required size
   Standard property: RequiredFontSize
   Example: 0.125 (drawing units)
   Fix: Updates MText.TextHeight
   Optional: Multiply by CANNOSCALE if MatchDrawingScale=true
   
5. AN-001: Annotative Property
   What it checks: MText must be annotative (if required)
   Standard property: RequireAnnotative
   Example: true
   Fix: Sets MText.Annotative = AnnotativeStates.True

┌──────────────────────────────────────────────────────────────┐
│  🔤 DBTEXT STANDARDS (Single-line Text)                      │
└──────────────────────────────────────────────────────────────┘

**BASIC CHECKS:**

1. TS-001: Text Style Compliance
   What it checks: DBText must use approved text style from template
   Standard property: RequiredTextStyle OR ApprovedTextStyles
   Example: "Standard", "ROMANS", "ARIAL"
   Fix: Changes TextStyleId to required style
   
2. TS-003: Font File Type (SHX Fonts)
   What it checks: DBText text style must use .shx font
   Standard property: ApprovedTextFontFiles
   Example: ["romans.shx", "txt.shx"]
   Fix: Not auto-fixable (manual style edit required)

**ADVANCED CHECKS (NEW):**

3. TF-001: Font Name Match
   What it checks: DBText font must match required font exactly
   Standard property: RequiredFont
   Example: "romans.shx" or "Arial"
   Fix: Updates TextStyleTableRecord.FileName or .Font
   
4. TH-001: Text Height Match
   What it checks: DBText height must match required size
   Standard property: RequiredFontSize
   Example: 0.125 (drawing units)
   Fix: Updates DBText.Height
   Optional: Multiply by CANNOSCALE if MatchDrawingScale=true

Note: DBText does NOT check annotative (DBText doesn't support annotative property)

┌──────────────────────────────────────────────────────────────┐
│  📏 DIMENSION STANDARDS                                      │
└──────────────────────────────────────────────────────────────┘

**BASIC CHECKS:**

1. DS-001: Dimension Style Compliance
   What it checks: Dimension must use approved dim style from template
   Standard property: RequiredDimStyle OR ApprovedDimStyles
   Example: "ASBUILT", "002-above-dim-Monospace"
   Fix: Changes DimensionStyle to required style
   
2. DS-002: Dimension Text Style Check
   What it checks: Dimension style must reference correct text style
   Standard property: RequiredTextStyle
   Example: "Standard"
   Fix: Not auto-fixable (manual dim style edit required)

**ADVANCED CHECKS (NEW):**

3. DF-001: Dimension Font Match
   What it checks: Dimension's text style font must match required
   Standard property: RequiredDimFont (fallback: RequiredFont)
   Example: "romans.shx" or "Arial"
   Fix: Updates DimStyleTableRecord → Dimtxsty → Font
   
4. DH-001: Dimension Text Height Match
   What it checks: Dimension text height must match required size
   Standard property: RequiredDimFontSize (fallback: RequiredFontSize)
   Example: 0.125 (drawing units)
   Fix: Updates DimStyleTableRecord.Dimtxt
   Optional: Multiply by CANNOSCALE if MatchDrawingScale=true

┌──────────────────────────────────────────────────────────────┐
│  ⚙️ CONFIGURATION - Standards.json                          │
└──────────────────────────────────────────────────────────────┘

Here's how to configure these standards in your Standards.json file:

{
  // ===== BASIC STYLE STANDARDS =====
  "RequiredTextStyle": "Standard",
  "RequiredDimStyle": "ASBUILT",
  "ApprovedTextStyles": ["Standard", "ROMANS"],
  "ApprovedDimStyles": ["ASBUILT", "002-above-dim-Monospace"],
  "ApprovedTextFontFiles": ["romans.shx", "txt.shx"],
  
  // ===== ADVANCED FONT/SIZE STANDARDS =====
  "RequiredFont": "romans.shx",
  "RequiredFontSize": 0.125,
  "RequireAnnotative": false,
  "RequiredAnnotativeScale": 0.0,
  
  "RequiredDimFont": null,              // null = use RequiredFont
  "RequiredDimFontSize": 0.125,
  
  "MatchDrawingScale": false
}

┌──────────────────────────────────────────────────────────────┐
│  📊 STANDARDS PROPERTY REFERENCE                             │
└──────────────────────────────────────────────────────────────┘

**Text Style Properties:**
├─ RequiredTextStyle (string)
│  └─ Exact text style name required for all text entities
├─ ApprovedTextStyles (array)
│  └─ List of acceptable text style names (template mode)
└─ ApprovedTextFontFiles (array)
   └─ List of acceptable .shx font files

**Dimension Style Properties:**
├─ RequiredDimStyle (string)
│  └─ Exact dimension style name required for all dimensions
└─ ApprovedDimStyles (array)
   └─ List of acceptable dim style names (template mode)

**Advanced Font Properties:**
├─ RequiredFont (string)
│  └─ Font name for MTEXT/DBTEXT (e.g., "romans.shx" or "Arial")
├─ RequiredDimFont (string, nullable)
│  └─ Font name for DIMENSIONS (if null, uses RequiredFont)
├─ RequiredMLeaderFont (string, nullable)
│  └─ Font name for MLEADERS (future use)

**Advanced Size Properties:**
├─ RequiredFontSize (double)
│  └─ Text height for MTEXT/DBTEXT in drawing units
├─ RequiredDimFontSize (double)
│  └─ Dimension text height in drawing units
├─ RequiredMLeaderFontSize (double)
│  └─ MLeader text height in drawing units (future use)

**Annotative Properties:**
├─ RequireAnnotative (bool)
│  └─ If true, MTEXT must be annotative
├─ RequiredAnnotativeScale (double)
│  └─ Annotative scale (0 = match CANNOSCALE, >0 = specific value)
└─ MatchDrawingScale (bool)
   └─ If true, multiply text sizes by current CANNOSCALE

┌──────────────────────────────────────────────────────────────┐
│  🎯 EXAMPLE CONFIGURATIONS                                   │
└──────────────────────────────────────────────────────────────┘

**Example 1: Basic Style Checking Only**
{
  "RequiredTextStyle": "Standard",
  "RequiredDimStyle": "ASBUILT"
}
Result: Only checks if entities use these styles, no font/size checking

**Example 2: Font Enforcement (SHX only)**
{
  "RequiredTextStyle": "Standard",
  "RequiredFont": "romans.shx",
  "RequiredDimFont": "romans.shx"
}
Result: Checks style AND enforces romans.shx font for all text

**Example 3: Size Standardization**
{
  "RequiredTextStyle": "Standard",
  "RequiredFontSize": 0.125,
  "RequiredDimFontSize": 0.125
}
Result: Checks style AND enforces 0.125 height for all text/dims

**Example 4: Complete Standards with Scale Matching**
{
  "RequiredTextStyle": "Standard",
  "RequiredDimStyle": "ASBUILT",
  "RequiredFont": "romans.shx",
  "RequiredFontSize": 0.125,
  "RequiredDimFontSize": 0.125,
  "MatchDrawingScale": true
}
Result: Full enforcement with automatic scale adjustment
(e.g., at 1/4" scale (48), text will be 0.125 × 48 = 6.0 units)

**Example 5: Annotative Workflow**
{
  "RequiredTextStyle": "Standard",
  "RequiredFont": "romans.shx",
  "RequiredFontSize": 0.125,
  "RequireAnnotative": true,
  "RequiredAnnotativeScale": 0.0
}
Result: All MTEXT must be annotative and match drawing scale

**Example 6: Template-Based Standards (Extracted)**
{
  "TemplatePath": "C:\\ACSE\\config\\Standards\\FAA_002_acad.dwt",
  "ApprovedTextStyles": ["Standard", "ROMANS"],
  "ApprovedDimStyles": ["ASBUILT", "002-above-dim-Monospace"],
  "ApprovedLayers": ["A-ANNO", "A-DIMS", "A-WALL"],
  "RequiredFont": "romans.shx",
  "RequiredFontSize": 0.125
}
Result: Uses template-extracted approved lists + font/size enforcement

┌──────────────────────────────────────────────────────────────┐
│  🔍 VIOLATION DETECTION LOGIC                                │
└──────────────────────────────────────────────────────────────┘

**For each MTEXT entity:**
1. Check if TextStyleId matches RequiredTextStyle ✅
2. Check if TextStyle uses .shx font (if ApprovedTextFontFiles set) ✅
3. Check if font name contains RequiredFont ✅ (NEW)
4. Check if TextHeight equals RequiredFontSize ✅ (NEW)
5. Check if Annotative property is True (if required) ✅ (NEW)

**For each DBTEXT entity:**
1. Check if TextStyleId matches RequiredTextStyle ✅
2. Check if TextStyle uses .shx font (if ApprovedTextFontFiles set) ✅
3. Check if font name contains RequiredFont ✅ (NEW)
4. Check if Height equals RequiredFontSize ✅ (NEW)

**For each DIMENSION entity:**
1. Check if DimensionStyle matches RequiredDimStyle ✅
2. Check if DimStyle's TextStyle matches RequiredTextStyle ✅
3. Check if DimStyle's font matches RequiredDimFont ✅ (NEW)
4. Check if DimStyle's Dimtxt equals RequiredDimFontSize ✅ (NEW)

┌──────────────────────────────────────────────────────────────┐
│  🛠️ FIX ENGINE BEHAVIOR                                      │
└──────────────────────────────────────────────────────────────┘

**Text Style Fixes (TS-001, TS-002):**
- Changes entity's TextStyleId to required style
- If style doesn't exist, can import from template
- Affects: MTEXT, DBTEXT

**Font Fixes (TF-001, DF-001):**
- Updates TextStyleTableRecord.FileName (for .shx fonts)
- Updates TextStyleTableRecord.Font.TypeFace (for TrueType fonts)
- Changes affect ALL entities using that text style
- Affects: MTEXT, DBTEXT, DIMENSIONS

**Size Fixes (TH-001, DH-001):**
- MTEXT: Updates MText.TextHeight directly
- DBTEXT: Updates DBText.Height directly
- DIMENSIONS: Updates DimStyleTableRecord.Dimtxt
- Optional: Multiplies by CANNOSCALE if MatchDrawingScale=true

**Annotative Fixes (AN-001):**
- Sets MText.Annotative = AnnotativeStates.True
- Optionally sets annotative scale
- Only applies to MTEXT

**Dimension Style Fixes (DS-001):**
- Changes Dimension.DimensionStyle to required style
- May need to import style from template

┌──────────────────────────────────────────────────────────────┐
│  📈 TOLERANCE & PRECISION                                    │
└──────────────────────────────────────────────────────────────┘

**Font Name Matching:**
- Uses case-insensitive partial match
- "romans.shx" matches "ROMANS.SHX"
- "Arial" matches "arial" or "Arial Regular"

**Size Matching:**
- Tolerance: 0.001 drawing units
- 0.125 matches 0.125 ✅
- 0.125 does NOT match 0.124 ❌
- 0.125 does NOT match 0.126 ❌

**Annotative Scale:**
- 0.0 = Use current CANNOSCALE from drawing
- >0.0 = Use specific scale value

┌──────────────────────────────────────────────────────────────┐
│  💡 BEST PRACTICES                                           │
└──────────────────────────────────────────────────────────────┘

**1. Use Template Extraction**
   Command: ACSE_EXTRACT
   Automatically populates all standards from your DWT template
   This is the recommended approach for consistency

**2. Start with Basic Checks**
   Configure only RequiredTextStyle and RequiredDimStyle first
   Verify violations are detected correctly
   Then add advanced font/size requirements

**3. Test on Sample Drawing**
   Create test drawing with intentional violations
   Run ACSE_TEMPLATE or ACSE_UI
   Verify violations detected correctly
   Test Fix All button

**4. Font Consistency**
   Use same font for all text (RequiredFont)
   Or specify different fonts per entity type:
   - RequiredFont for MTEXT/DBTEXT
   - RequiredDimFont for DIMENSIONS
   - RequiredMLeaderFont for MLEADERS

**5. Size Standardization**
   Choose standard height (e.g., 0.125)
   Use MatchDrawingScale for plotted drawings
   Keep annotative entities at nominal size

**6. Gradual Rollout**
   Phase 1: Style checking only
   Phase 2: Add font checking
   Phase 3: Add size checking
   Phase 4: Add annotative requirements

┌──────────────────────────────────────────────────────────────┐
│  🧪 TESTING CHECKLIST                                        │
└──────────────────────────────────────────────────────────────┘

Test each standard type individually:

**MTEXT Style:**
  [ ] Create MTEXT with wrong style → Violation TS-002 detected
  [ ] Run Fix All → Style changed to RequiredTextStyle
  [ ] Rescan → Violation cleared

**MTEXT Font:**
  [ ] Create MTEXT with Arial (when romans.shx required) → TF-001
  [ ] Run Fix All → Font changed to romans.shx
  [ ] Rescan → Violation cleared

**MTEXT Size:**
  [ ] Create MTEXT with height 0.25 (when 0.125 required) → TH-001
  [ ] Run Fix All → Height changed to 0.125
  [ ] Rescan → Violation cleared

**MTEXT Annotative:**
  [ ] Set RequireAnnotative=true in standards
  [ ] Create non-annotative MTEXT → AN-001
  [ ] Run Fix All → Annotative property set
  [ ] Rescan → Violation cleared

**DBTEXT Style:**
  [ ] Create DBTEXT with wrong style → TS-001
  [ ] Run Fix All → Style changed
  [ ] Rescan → Violation cleared

**DBTEXT Font:**
  [ ] Create DBTEXT with Arial → TF-001
  [ ] Run Fix All → Font changed
  [ ] Rescan → Violation cleared

**DBTEXT Size:**
  [ ] Create DBTEXT with height 0.08 → TH-001
  [ ] Run Fix All → Height changed to 0.125
  [ ] Rescan → Violation cleared

**DIMENSION Style:**
  [ ] Create dimension with wrong dim style → DS-001
  [ ] Run Fix All → Dim style changed
  [ ] Rescan → Violation cleared

**DIMENSION Font:**
  [ ] Create dimension with Arial dim text → DF-001
  [ ] Run Fix All → Dim style font changed
  [ ] Rescan → Violation cleared

**DIMENSION Size:**
  [ ] Create dimension with Dimtxt=0.08 → DH-001
  [ ] Run Fix All → Dimtxt changed to 0.125
  [ ] Rescan → Violation cleared

┌──────────────────────────────────────────────────────────────┐
│  📚 RULE ID QUICK REFERENCE                                  │
└──────────────────────────────────────────────────────────────┘

**Text Entities:**
TS-001: DBText style compliance
TS-002: MText style compliance
TS-003: DBText SHX font check
TS-004: MText SHX font check
TF-001: Text font name match (MTEXT/DBTEXT)
TH-001: Text height match (MTEXT/DBTEXT)
AN-001: MText annotative property

**Dimension Entities:**
DS-001: Dimension style compliance
DS-002: Dimension text style check
DF-001: Dimension font match
DH-001: Dimension text height match

**Other Entities:**
TS-005: AttributeDefinition style
TS-006: AttributeReference style
TS-007: MLeader text style
TS-008: Leader annotation text style

╔══════════════════════════════════════════════════════════════╗
║   📋 SUMMARY: ALL STANDARDS FOR TEXT & DIMENSIONS 📋        ║
╚══════════════════════════════════════════════════════════════╝

**MTEXT Requirements:**
✅ Text Style (RequiredTextStyle or ApprovedTextStyles)
✅ Font Type (.shx preferred)
✅ Font Name (RequiredFont) ← NEW
✅ Text Height (RequiredFontSize) ← NEW
✅ Annotative Property (if RequireAnnotative=true) ← NEW

**DBTEXT Requirements:**
✅ Text Style (RequiredTextStyle or ApprovedTextStyles)
✅ Font Type (.shx preferred)
✅ Font Name (RequiredFont) ← NEW
✅ Text Height (RequiredFontSize) ← NEW

**DIMENSION Requirements:**
✅ Dimension Style (RequiredDimStyle or ApprovedDimStyles)
✅ Dim Style's Text Style (RequiredTextStyle)
✅ Dim Font Name (RequiredDimFont or RequiredFont) ← NEW
✅ Dim Text Height (RequiredDimFontSize or RequiredFontSize) ← NEW

All standards are configurable via Standards.json.
All violations are auto-fixable with the Fix All button.

