using System.Collections.Generic;

namespace ACSE.AutoCAD2026.Standards
{
    public class StandardsModel
    {
        // ---- Template Source ----
        /// <summary>
        /// Path to the DWT template file used to extract standards.
        /// </summary>
        public string TemplatePath { get; set; } = "";

        // ---- Text Styles ----
        public string? RequiredTextStyle { get; set; }
        public HashSet<string> ApprovedTextStyles { get; set; } = [];
        /// <summary>
        /// Maps text style name to font file (e.g., "STANDARD" -> "romans.shx")
        /// </summary>
        public Dictionary<string, string> TextStyleFonts { get; set; } = [];

        // ---- Dimension Styles ----
        public string? RequiredDimStyle { get; set; }
        public HashSet<string> ApprovedDimStyles { get; set; } = [];

        // ---- Layers ----
        public string[]? AllowedLayers { get; set; }
        public HashSet<string> ApprovedLayers { get; set; } = [];
        public string LayerNamePrefix { get; set; } = "";
        public Dictionary<string, string>? LayerMap { get; set; }

        // ---- Linetypes ----
        public string? RequiredLinetype { get; set; }
        public string[]? AllowedLinetypes { get; set; }
        public HashSet<string> ApprovedLinetypes { get; set; } = [];

        // ---- Options ----
        /// <summary>
        /// Optional: List of approved SHX font files (e.g., ["romans.shx"]).
        /// If empty, only style name is enforced.
        /// </summary>
        public string[] ApprovedTextFontFiles { get; set; } = [];

        /// <summary>
        /// If true, auto-fix will create missing text styles.
        /// If false, missing styles will be marked as manual-fix.
        /// </summary>
        public bool CreateMissingTextStyle { get; set; } = false;

        // ---- Advanced Entity Corrections ----

        /// <summary>
        /// Required font name for MTEXT/TEXT entities (e.g., "Arial", "romans.shx")
        /// If null, font checking is skipped.
        /// </summary>
        public string? RequiredFont { get; set; } = null;

        /// <summary>
        /// Required text height for MTEXT/TEXT in drawing units.
        /// If 0 or negative, size checking is skipped.
        /// </summary>
        public double RequiredFontSize { get; set; } = 0.125;

        /// <summary>
        /// If true, MTEXT/TEXT entities should be set to annotative.
        /// </summary>
        public bool RequireAnnotative { get; set; } = false;

        /// <summary>
        /// Required annotative scale for text entities.
        /// If 0, match current drawing scale (CANNOSCALE).
        /// If > 0, use this specific scale.
        /// </summary>
        public double RequiredAnnotativeScale { get; set; } = 0.0;

        /// <summary>
        /// Required font name for DIMENSION entities.
        /// If null, uses RequiredFont or skips checking.
        /// </summary>
        public string? RequiredDimFont { get; set; } = null;

        /// <summary>
        /// Required text height for DIMENSION entities in drawing units.
        /// If 0 or negative, uses RequiredFontSize or skips checking.
        /// </summary>
        public double RequiredDimFontSize { get; set; } = 0.125;

        /// <summary>
        /// Required font name for MLEADER entities.
        /// If null, uses RequiredFont or skips checking.
        /// </summary>
        public string? RequiredMLeaderFont { get; set; } = null;

        /// <summary>
        /// Required text height for MLEADER entities in drawing units.
        /// If 0 or negative, uses RequiredFontSize or skips checking.
        /// </summary>
        public double RequiredMLeaderFontSize { get; set; } = 0.125;

        /// <summary>
        /// If true, auto-fix will scale text heights by drawing scale.
        /// Uses CANNOSCALE or RequiredAnnotativeScale.
        /// </summary>
        public bool MatchDrawingScale { get; set; } = false;

        // ---- Detailed Dimension Style Properties ----

        /// <summary>
        /// Required dimension style name (e.g., "ASBUILT").
        /// Used by DimensionStyleManager to create/update styles.
        /// </summary>
        public string RequiredDimStyleName { get; set; } = "ASBUILT";

        /// <summary>
        /// Dimension text height (DIMTXT).
        /// </summary>
        public double DimTextHeight { get; set; } = 0.125;

        /// <summary>
        /// Dimension arrow size (DIMASZ).
        /// </summary>
        public double DimArrowSize { get; set; } = 0.125;

        /// <summary>
        /// Extension line extend beyond dimension line (DIMEXE).
        /// </summary>
        public double DimExtensionLineExtend { get; set; } = 0.125;

        /// <summary>
        /// Extension line offset from origin (DIMEXO).
        /// </summary>
        public double DimExtensionLineOffset { get; set; } = 0.0625;

        /// <summary>
        /// Arrow block name (DIMBLK).
        /// Common values: "" (Closed filled), "ArchTick", "DotSmall", etc.
        /// </summary>
        public string DimArrowBlock { get; set; } = "";  // Empty string = Closed filled

        /// <summary>
        /// Whether dimension style is annotative (DIMANNO).
        /// </summary>
        public bool DimAnnotative { get; set; } = true;

        /// <summary>
        /// Dimension units format (DIMLUNIT).
        /// 2 = Decimal, 4 = Architectural, 5 = Fractional, etc.
        /// </summary>
        public short DimUnits { get; set; } = 2;  // Decimal

        /// <summary>
        /// Number of decimal places for dimension values (DIMDEC).
        /// </summary>
        public short DimDecimals { get; set; } = 2;

        /// <summary>
        /// Linear scale factor (DIMLFAC).
        /// </summary>
        public double DimScaleFactor { get; set; } = 1.0;

        /// <summary>
        /// Dimension line color (DIMCLRD).
        /// 0 = ByBlock, 256 = ByLayer, 7 = White, etc.
        /// </summary>
        public short DimLineColor { get; set; } = 256;  // ByLayer

        /// <summary>
        /// Dimension line lineweight (DIMLWD).
        /// -1 = ByLayer, -2 = ByBlock, 0-211 = specific weight
        /// </summary>
        public short DimLineWeight { get; set; } = -1;  // ByLayer

        /// <summary>
        /// Extension line color (DIMCLRE).
        /// </summary>
        public short DimExtLineColor { get; set; } = 256;  // ByLayer

        /// <summary>
        /// Extension line lineweight (DIMLWE).
        /// </summary>
        public short DimExtLineWeight { get; set; } = -1;  // ByLayer

        /// <summary>
        /// Dimension text color (DIMCLRT).
        /// </summary>
        public short DimTextColor { get; set; } = 256;  // ByLayer

        /// <summary>
        /// Text gap - distance around text (DIMGAP).
        /// </summary>
        public double DimTextGap { get; set; } = 0.09;

        /// <summary>
        /// If true, dimension style manager will set this style as current.
        /// </summary>
        public bool SetDimStyleAsCurrent { get; set; } = true;

        /// <summary>
        /// If true, dimension style manager will purge unused dimension styles.
        /// </summary>
        public bool PurgeUnusedDimStyles { get; set; } = false;

        // ---- Smart Auto-Fix Custom Scoring Factors ----

        /// <summary>
        /// Custom scoring factors for Smart Auto-Fix confidence calculation.
        /// Allows fine-tuning which violation types get higher confidence scores.
        /// </summary>
        public List<ScoringFactor> CustomScoringFactors { get; set; } = new();

        /// <summary>
        /// Initializes default scoring factors if none are defined in JSON.
        /// Called automatically by SmartFixEngine.
        /// </summary>
        public void InitializeDefaultScoringFactors()
        {
            if (CustomScoringFactors.Count == 0)
            {
                CustomScoringFactors = new List<ScoringFactor>
                {
                    new ScoringFactor 
                    { 
                        Name = "LayerCriticality", 
                        Weight = 0.30, 
                        BaseValue = 0.95,
                        Description = "Layer violations are high priority and safe to fix"
                    },
                    new ScoringFactor 
                    { 
                        Name = "DimStyleReliability", 
                        Weight = 0.25, 
                        BaseValue = 0.92,
                        Description = "Dimension style changes are very reliable"
                    },
                    new ScoringFactor 
                    { 
                        Name = "TextConsistency", 
                        Weight = 0.20, 
                        BaseValue = 0.85,
                        Description = "Text style fixes are generally safe"
                    },
                    new ScoringFactor 
                    { 
                        Name = "FrequencyBonus", 
                        Weight = 0.15, 
                        BaseValue = 0.80,
                        Condition = "HighFrequency",
                        Description = "Common violations get higher confidence"
                    },
                    new ScoringFactor 
                    { 
                        Name = "SideEffectRisk", 
                        Weight = 0.10, 
                        BaseValue = 0.70,
                        Description = "Penalty for high-risk operations (blocks, xrefs)"
                    }
                };
            }
        }
    }

    /// <summary>
    /// Defines a custom scoring factor for Smart Auto-Fix confidence calculation
    /// </summary>
    public class ScoringFactor
    {
        /// <summary>
        /// Factor name (e.g., "LayerCriticality", "FrequencyBonus")
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Weight in overall score calculation (should sum to ~1.0 across all factors)
        /// </summary>
        public double Weight { get; set; } = 0.2;

        /// <summary>
        /// Base confidence value (0.0 - 1.0) when this factor applies
        /// </summary>
        public double BaseValue { get; set; } = 0.8;

        /// <summary>
        /// Optional condition for when this factor applies
        /// (e.g., "HighFrequency", "ModelSpace", "Layer", "DimStyle")
        /// </summary>
        public string Condition { get; set; } = "";

        /// <summary>
        /// Human-readable description of what this factor does
        /// </summary>
        public string Description { get; set; } = "";
    }
}
