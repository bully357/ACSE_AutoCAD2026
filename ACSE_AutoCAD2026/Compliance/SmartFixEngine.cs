using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using ACSE.AutoCAD2026.Standards;

namespace ACSE.AutoCAD2026.Compliance
{
    /// <summary>
    /// Intelligent fix engine with confidence scoring and safety recommendations
    /// </summary>
    public static class SmartFixEngine
    {
        public static SmartFixResult ApplySmartFixes(
            List<Violation> violations, 
            StandardsModel standards, 
            SmartFixMode mode = SmartFixMode.SafeOnly)
        {
            var result = new SmartFixResult();
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return result;

            var db = doc.Database;

            // Initialize default scoring factors if not defined
            standards.InitializeDefaultScoringFactors();

            // Pre-compute frequency for better scoring
            var frequencyMap = CalculateViolationFrequency(violations);

            using var docLock = doc.LockDocument();
            using var tr = db.TransactionManager.StartTransaction();

            foreach (var violation in violations)
            {
                // Enhanced scoring with custom factors
                AnalyzeAndScoreWithCustomFactors(violation, standards, frequencyMap);

                // Determine if we should apply this fix based on mode
                bool shouldApply = mode switch
                {
                    SmartFixMode.SafeOnly => violation.Recommendation == FixRecommendation.Safe,
                    SmartFixMode.SafeAndRecommended => violation.Recommendation != FixRecommendation.ManualReview,
                    SmartFixMode.All => violation.AutoFixable,
                    _ => false
                };

                if (!shouldApply || !violation.AutoFixable)
                {
                    result.SkippedCount++;
                    result.SkippedViolations.Add(violation);
                    continue;
                }

                try
                {
                    if (TryApplyFix(violation, standards, tr, db))
                    {
                        result.AppliedCount++;
                        result.AppliedViolations.Add(violation);
                    }
                    else
                    {
                        result.FailedCount++;
                        result.FailedViolations.Add(violation);
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.FailedViolations.Add(violation);
                    result.ErrorMessages.Add($"{violation.RuleId}: {ex.Message}");
                }
            }

            tr.Commit();
            doc.Editor.Regen();

            return result;
        }

        /// <summary>
        /// Enhanced scoring with custom factors from standards
        /// </summary>
        private static void AnalyzeAndScoreWithCustomFactors(
            Violation v, 
            StandardsModel standards,
            Dictionary<string, int> frequencyMap)
        {
            double totalScore = 0.0;
            double totalWeight = 0.0;

            foreach (var factor in standards.CustomScoringFactors)
            {
                double factorScore = CalculateFactorScore(v, factor, frequencyMap, standards);
                totalScore += factorScore * factor.Weight;
                totalWeight += factor.Weight;
            }

            // Normalize if weights don't sum to 1.0
            v.Confidence = totalWeight > 0 ? Math.Clamp(totalScore / totalWeight, 0.0, 1.0) : 0.75;

            // Determine recommendation based on final confidence
            if (v.Confidence >= 0.90)
                v.Recommendation = FixRecommendation.Safe;
            else if (v.Confidence >= 0.70)
                v.Recommendation = FixRecommendation.Recommended;
            else
                v.Recommendation = FixRecommendation.ManualReview;

            v.SuggestedAction = GenerateSuggestedAction(v, standards);
        }

        /// <summary>
        /// Calculates score contribution for a single factor
        /// </summary>
        private static double CalculateFactorScore(
            Violation v, 
            ScoringFactor factor,
            Dictionary<string, int> frequencyMap, 
            StandardsModel standards)
        {
            switch (factor.Name)
            {
                case "LayerCriticality":
                    // Boost confidence for Layer violations - they're very safe to fix
                    if (v.Type == ViolationType.Layer)
                        return 0.95; // Very high confidence for layer fixes

                    return factor.BaseValue * 0.6;

                case "DimStyleReliability":
                    return v.Type == ViolationType.DimStyle 
                        ? factor.BaseValue 
                        : factor.BaseValue * 0.7;

                case "TextConsistency":
                    // Boost confidence for text-related violations - they're very safe
                    if (v.Type == ViolationType.Annotative)
                        return 0.95; // Very high confidence for annotative fixes

                    if (v.Type == ViolationType.TextSize)
                        return 0.95; // Very high confidence for text size fixes

                    if (v.Type == ViolationType.TextFont)
                        return 0.95; // Very high confidence for text font fixes

                    return v.Type == ViolationType.TextStyle
                        ? factor.BaseValue 
                        : factor.BaseValue * 0.65;

                case "FrequencyBonus":
                    if (frequencyMap.TryGetValue(v.RuleId, out int freq))
                    {
                        // More occurrences = higher confidence (cap at 10+)
                        double freqMultiplier = Math.Min(1.0, freq / 8.0);
                        return freqMultiplier * factor.BaseValue;
                    }
                    return factor.BaseValue * 0.5;

                case "SideEffectRisk":
                    // Penalty for high-risk operations
                    double risk = (v.EntityName.Contains("Block") || v.EntityName.Contains("Xref")) 
                        ? 0.8 
                        : 0.3;
                    return (1.0 - risk) * factor.BaseValue;

                case "EntityReliability":
                    return GetEntityReliabilityScore(v.EntityName) * factor.BaseValue;

                case "StandardsMatch":
                    return GetStandardsMatchScore(v, standards) * factor.BaseValue;

                default:
                    // Custom user-defined factor via Condition
                    if (!string.IsNullOrEmpty(factor.Condition))
                    {
                        if (factor.Condition == "HighFrequency" && frequencyMap.GetValueOrDefault(v.RuleId, 0) >= 5)
                            return factor.BaseValue;

                        if (factor.Condition == "ModelSpace" && v.Layer?.StartsWith("A-") == true)
                            return factor.BaseValue;

                        if (factor.Condition == "Layer" && v.Type == ViolationType.Layer)
                            return factor.BaseValue;

                        if (factor.Condition == "DimStyle" && v.Type == ViolationType.DimStyle)
                            return factor.BaseValue;
                    }
                    return factor.BaseValue * 0.75;
            }
        }

        private static double GetEntityReliabilityScore(string entityName)
        {
            return entityName switch
            {
                "Dimension" or "RotatedDimension" or "AlignedDimension" => 0.95,
                "MText" or "DBText" => 0.90,
                "AttributeReference" => 0.88,
                "MLeader" => 0.85,
                _ => 0.70
            };
        }

        private static double GetStandardsMatchScore(Violation v, StandardsModel std)
        {
            if (string.IsNullOrEmpty(v.Expected)) 
                return 0.5;

            // Higher confidence if expected value matches known standards
            if (v.Expected.Contains("ASBUILT", StringComparison.OrdinalIgnoreCase) || 
                v.Expected.Contains("STANDARD", StringComparison.OrdinalIgnoreCase) ||
                v.Expected.Contains("ANNO-", StringComparison.OrdinalIgnoreCase))
            {
                return 0.95;
            }

            return 0.75;
        }

        private static string GenerateSuggestedAction(Violation v, StandardsModel std)
        {
            return v.Type switch
            {
                ViolationType.Layer => $"Move to layer '{v.Expected ?? "0"}'",
                ViolationType.DimStyle => $"Change to dim style '{std.RequiredDimStyleName ?? v.Expected ?? "ASBUILT"}'",
                ViolationType.TextStyle => $"Apply text style '{std.RequiredTextStyle ?? v.Expected ?? "Standard"}' + height {std.RequiredFontSize}",
                ViolationType.Linetype => "Set linetype to ByLayer",
                _ => "Review and apply manually"
            };
        }

        /// <summary>
        /// Counts how many times each rule appears (for frequency scoring)
        /// </summary>
        private static Dictionary<string, int> CalculateViolationFrequency(List<Violation> violations)
        {
            var dict = new Dictionary<string, int>();
            foreach (var v in violations)
            {
                if (dict.ContainsKey(v.RuleId))
                    dict[v.RuleId]++;
                else
                    dict[v.RuleId] = 1;
            }
            return dict;
        }

        /// <summary>
        /// Attempts to apply the fix to the entity
        /// </summary>
        private static bool TryApplyFix(Violation v, StandardsModel standards, Transaction tr, Database db)
        {
            if (!TryGetObjectIdFromHandle(db, v.EntityHandle, out ObjectId id))
                return false;

            var ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
            if (ent == null) return false;

            try
            {
                switch (v.Type)
                {
                    case ViolationType.Layer:
                        string targetLayer = v.Expected ?? "0";
                        if (!string.IsNullOrEmpty(targetLayer))
                        {
                            // Create layer if it doesn't exist
                            EnsureLayerExists(tr, db, targetLayer);
                            ent.Layer = targetLayer;
                        }
                        return true;

                    case ViolationType.Linetype:
                        ent.Linetype = v.Expected ?? "ByLayer";
                        return true;

                    case ViolationType.TextStyle:
                        if (ent is MText mtext)
                        {
                            var styleId = GetTextStyleId(tr, db, v.Expected ?? standards.RequiredTextStyle ?? "Standard");
                            if (!styleId.IsNull)
                            {
                                mtext.TextStyleId = styleId;
                                return true;
                            }
                        }
                        else if (ent is DBText dbtext)
                        {
                            var styleId = GetTextStyleId(tr, db, v.Expected ?? standards.RequiredTextStyle ?? "Standard");
                            if (!styleId.IsNull)
                            {
                                dbtext.TextStyleId = styleId;
                                return true;
                            }
                        }
                        else if (ent is AttributeReference attRef)
                        {
                            var styleId = GetTextStyleId(tr, db, v.Expected ?? standards.RequiredTextStyle ?? "Standard");
                            if (!styleId.IsNull)
                            {
                                attRef.TextStyleId = styleId;
                                return true;
                            }
                        }
                        return false;

                    case ViolationType.DimStyle:
                        if (ent is Dimension dim)
                        {
                            var dimStyleId = GetDimStyleId(tr, db, v.Expected ?? standards.RequiredDimStyleName ?? "Standard");
                            if (!dimStyleId.IsNull)
                            {
                                dim.DimensionStyle = dimStyleId;
                                return true;
                            }
                        }
                        return false;

                    case ViolationType.TextSize:
                        // Fix text height violations
                        if (ent is MText mtext2)
                        {
                            if (double.TryParse(v.Expected, out double expectedHeight))
                            {
                                mtext2.TextHeight = expectedHeight;
                                return true;
                            }
                            // If no expected value, use standards
                            else if (standards.RequiredFontSize > 0)
                            {
                                mtext2.TextHeight = standards.RequiredFontSize;
                                return true;
                            }
                        }
                        else if (ent is DBText dbtext2)
                        {
                            if (double.TryParse(v.Expected, out double expectedHeight))
                            {
                                dbtext2.Height = expectedHeight;
                                return true;
                            }
                            else if (standards.RequiredFontSize > 0)
                            {
                                dbtext2.Height = standards.RequiredFontSize;
                                return true;
                            }
                        }
                        else if (ent is AttributeReference attRef2)
                        {
                            if (double.TryParse(v.Expected, out double expectedHeight))
                            {
                                attRef2.Height = expectedHeight;
                                return true;
                            }
                            else if (standards.RequiredFontSize > 0)
                            {
                                attRef2.Height = standards.RequiredFontSize;
                                return true;
                            }
                        }
                        return false;

                    case ViolationType.TextFont:
                        // Fix font violations by changing text style
                        if (ent is MText mtext3)
                        {
                            var styleId = GetTextStyleId(tr, db, standards.RequiredTextStyle ?? "Standard");
                            if (!styleId.IsNull)
                            {
                                mtext3.TextStyleId = styleId;
                                return true;
                            }
                        }
                        else if (ent is DBText dbtext3)
                        {
                            var styleId = GetTextStyleId(tr, db, standards.RequiredTextStyle ?? "Standard");
                            if (!styleId.IsNull)
                            {
                                dbtext3.TextStyleId = styleId;
                                return true;
                            }
                        }
                        return false;

                    case ViolationType.Annotative:
                        // Fix annotative property violations
                        bool shouldBeAnnotative = v.Expected?.Contains("Annotative", StringComparison.OrdinalIgnoreCase) == true;

                        if (shouldBeAnnotative)
                        {
                            if (ent is MText mtext4)
                            {
                                // Set the Annotative property directly
                                mtext4.Annotative = AnnotativeStates.True;
                                return true;
                            }
                            else if (ent is DBText dbtext4)
                            {
                                // DBText also has Annotative property
                                dbtext4.Annotative = AnnotativeStates.True;
                                return true;
                            }
                        }
                        return false;

                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static ObjectId GetTextStyleId(Transaction tr, Database db, string styleName)
        {
            var textStyleTable = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            return textStyleTable.Has(styleName) ? textStyleTable[styleName] : ObjectId.Null;
        }

        private static ObjectId GetOrCreateAnnotativeTextStyle(Transaction tr, Database db, StandardsModel standards)
        {
            var textStyleTable = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);

            // Try to find an existing annotative text style by common naming conventions
            string[] annotativeStyleNames = new[] 
            { 
                "ANNOTATIVE", 
                "ANNO", 
                standards.RequiredTextStyle ?? "STANDARD",
                "A-ANNO",
                "STANDARD"
            };

            foreach (var styleName in annotativeStyleNames)
            {
                if (textStyleTable.Has(styleName))
                {
                    var styleId = textStyleTable[styleName];
                    // For now, assume if it exists with these names, it's suitable for annotative text
                    return styleId;
                }
            }

            // Fall back to the first available text style
            foreach (ObjectId styleId in textStyleTable)
            {
                return styleId; // Return first available style
            }

            return ObjectId.Null;
        }

        private static ObjectId GetDimStyleId(Transaction tr, Database db, string styleName)
        {
            var dimStyleTable = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
            return dimStyleTable.Has(styleName) ? dimStyleTable[styleName] : ObjectId.Null;
        }

        private static void EnsureLayerExists(Transaction tr, Database db, string layerName)
        {
            var layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            if (!layerTable.Has(layerName))
            {
                // Layer doesn't exist - create it
                layerTable.UpgradeOpen();

                var newLayer = new LayerTableRecord();
                newLayer.Name = layerName;
                newLayer.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 7); // White

                layerTable.Add(newLayer);
                tr.AddNewlyCreatedDBObject(newLayer, true);

                layerTable.DowngradeOpen();
            }
        }

        private static bool TryGetObjectIdFromHandle(Database db, string handleStr, out ObjectId objectId)
        {
            objectId = ObjectId.Null;
            try
            {
                if (long.TryParse(handleStr, System.Globalization.NumberStyles.HexNumber, null, out long hexValue))
                {
                    var h = new Handle(hexValue);
                    return db.TryGetObjectId(h, out objectId);
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    public enum SmartFixMode
    {
        SafeOnly,               // Apply only "Safe" fixes
        SafeAndRecommended,     // Apply "Safe" and "Recommended" fixes
        All                     // Apply all auto-fixable violations
    }

    public class SmartFixResult
    {
        public int AppliedCount { get; set; }
        public int SkippedCount { get; set; }
        public int FailedCount { get; set; }
        
        public List<Violation> AppliedViolations { get; set; } = new();
        public List<Violation> SkippedViolations { get; set; } = new();
        public List<Violation> FailedViolations { get; set; } = new();
        public List<string> ErrorMessages { get; set; } = new();

        public int TotalProcessed => AppliedCount + SkippedCount + FailedCount;
        
        public double SuccessRate => TotalProcessed > 0 
            ? (double)AppliedCount / TotalProcessed * 100 
            : 0;
    }
}
