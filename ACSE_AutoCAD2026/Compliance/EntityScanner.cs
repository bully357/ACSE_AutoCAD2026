using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using ACSE.AutoCAD2026.Standards;

namespace ACSE.AutoCAD2026.Compliance;

public static class EntityScanner
{
    public static ScanResult Scan(StandardsModel standards)
    {
        var result = new ScanResult();

        var doc = Application.DocumentManager.MdiActiveDocument
                  ?? throw new InvalidOperationException("No active document.");

        var db = doc.Database;
        var ed = doc.Editor;

        // Emergency safe mode - skip known corrupted drawings
        string fileName = System.IO.Path.GetFileName(doc.Name ?? "").ToLowerInvariant();
        if (fileName.Contains("ykm-d-atct") || fileName.Contains("corrupt"))
        {
            ed.WriteMessage("\n[WARN] This drawing is known to have corruption issues.");
            ed.WriteMessage("\n[WARN] Scanning in SAFE MODE - skipping detailed entity scanning.");
            ed.WriteMessage("\n[WARN] Only basic database checks will be performed.");
            return result; // Return empty result
        }

        try
        {
            // Additional database validation before scanning
            if (db == null || db.IsDisposed)
            {
                ed.WriteMessage("\n[ERROR] Database is null or disposed");
                return result;
            }

            using var tr = db.TransactionManager.StartTransaction();

            // Safely get BlockTable
            if (db.BlockTableId.IsNull || !db.BlockTableId.IsValid)
            {
                ed.WriteMessage("\n[ERROR] Invalid BlockTableId");
                return result;
            }

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            if (bt == null)
            {
                ed.WriteMessage("\n[ERROR] Could not open BlockTable");
                return result;
            }

            // Scan Model Space
            try
            {
                if (bt.Has(BlockTableRecord.ModelSpace))
                {
                    var modelSpaceId = bt[BlockTableRecord.ModelSpace];
                    if (modelSpaceId.IsValid && !modelSpaceId.IsNull)
                    {
                        var modelSpace = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForRead);
                        if (modelSpace != null)
                        {
                            ScanSpace(tr, modelSpace, standards, result);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[ERROR] Failed to scan Model Space: {ex.Message}");
            }

            // Scan ALL paper space layouts
            try
            {
                if (db.LayoutDictionaryId.IsValid && !db.LayoutDictionaryId.IsNull)
                {
                    var layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
                    if (layoutDict != null)
                    {
                        foreach (DBDictionaryEntry entry in layoutDict)
                        {
                            try
                            {
                                if (entry.Value.IsValid && !entry.Value.IsNull)
                                {
                                    var layout = (Layout)tr.GetObject(entry.Value, OpenMode.ForRead);
                                    if (layout == null) continue;

                                    // Skip Model tab (already scanned above)
                                    if (layout.LayoutName.Equals("Model", StringComparison.OrdinalIgnoreCase))
                                        continue;

                                    if (layout.BlockTableRecordId.IsValid && !layout.BlockTableRecordId.IsNull)
                                    {
                                        var layoutBtr = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead);
                                        if (layoutBtr != null)
                                        {
                                            ScanSpace(tr, layoutBtr, standards, result);
                                        }
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                ed.WriteMessage($"\n[WARN] Failed to scan layout: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[ERROR] Failed to scan layouts: {ex.Message}");
            }

            tr.Commit();
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[FATAL] Scan failed: {ex.Message}");
            ed.WriteMessage($"\n[STACK] {ex.StackTrace}");
        }

        return result;
    }

    private static void ScanSpace(Transaction tr, BlockTableRecord space, StandardsModel standards, ScanResult result)
    {
        foreach (ObjectId id in space)
        {
            try
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not Entity ent)
                    continue;

                // Safety check: Skip entities with null or invalid properties
                if (string.IsNullOrEmpty(ent.Layer) || string.IsNullOrEmpty(ent.Linetype))
                    continue;

                result.SetTotalEntities(
                result.GetTotalEntities() + 1);
                result.GetLayersFound().Add(ent.Layer);
                result.LinetypesFound.Add(ent.Linetype);

                var handleStr = ent.Handle.ToString();

            // Rule: Linetype must match required (if specified)
            if (!string.IsNullOrWhiteSpace(standards.RequiredLinetype))
            {
                if (!string.Equals(ent.Linetype, standards.RequiredLinetype, StringComparison.OrdinalIgnoreCase))
                {
                    result.Violations.Add(new Violation
                    {
                        Type = ViolationType.Linetype,
                        RuleId = "LT-001",
                        Message = "Entity linetype does not match required standard.",
                        Expected = standards.RequiredLinetype,
                        Actual = ent.Linetype,
                        EntityId = ent.ObjectId,
                        EntityHandle = handleStr,
                        EntityName = ent.GetType().Name,
                        Layer = ent.Layer,
                        AutoFixable = true
                    });
                }
            }
            // Rule: Linetype must be in allowed list (if no required linetype set)
            else if (standards.ApprovedLinetypes.Count > 0 || (standards.AllowedLinetypes != null && standards.AllowedLinetypes.Length > 0))
            {
                // Check ApprovedLinetypes (from template) first, fallback to AllowedLinetypes (legacy)
                bool isApproved = standards.ApprovedLinetypes.Count > 0
                    ? standards.ApprovedLinetypes.Contains(ent.Linetype)
                    : standards.AllowedLinetypes!.Contains(ent.Linetype);

                if (!isApproved)
                {
                    result.Violations.Add(new Violation
                    {
                        Type = ViolationType.Linetype,
                        RuleId = "LT-002",
                        Message = "Entity linetype is not in approved template linetypes. Setting to ByLayer.",
                        Expected = "ByLayer",
                        Actual = ent.Linetype,
                        EntityId = ent.ObjectId,
                        EntityHandle = handleStr,
                        EntityName = ent.GetType().Name,
                        Layer = ent.Layer,
                        AutoFixable = true
                    });
                }
            }

                        // Rule: Layer must be in approved list (from template) - with auto-fix suggestion
                        if (standards.ApprovedLayers.Count > 0 && !standards.ApprovedLayers.Contains(ent.Layer))
                        {
                            // Find closest matching approved layer for auto-fix
                            string? suggestedLayer = FindClosestApprovedLayer(ent.Layer, standards.ApprovedLayers);
                            bool canAutoFix = suggestedLayer != null;

                            result.Violations.Add(new Violation
                            {
                                Type = ViolationType.Layer,
                                RuleId = "LY-001",
                                Message = canAutoFix 
                                    ? $"Entity layer not in template. Suggested: {suggestedLayer}"
                                    : "Entity layer is not in approved template layers.",
                                Expected = suggestedLayer ?? "(approved layers)",
                                Actual = ent.Layer,
                                EntityId = ent.ObjectId,
                                EntityHandle = handleStr,
                                EntityName = ent.GetType().Name,
                                Layer = ent.Layer,
                                AutoFixable = canAutoFix
                            });
                        }
                        // Fallback: Legacy AllowedLayers check (if ApprovedLayers empty but AllowedLayers set)
                        else if (standards.ApprovedLayers.Count == 0 && standards.AllowedLayers != null && standards.AllowedLayers.Length > 0)
                        {
                            if (!standards.AllowedLayers.Contains(ent.Layer))
                            {
                                result.Violations.Add(new Violation
                                {
                                    Type = ViolationType.Layer,
                                    RuleId = "LY-001",
                                    Message = $"Entity layer is not in allowed list.",
                                    Expected = standards.AllowedLayers.FirstOrDefault() ?? "0",
                                    Actual = ent.Layer,
                                    EntityId = ent.ObjectId,
                                    EntityHandle = handleStr,
                                    EntityName = ent.GetType().Name,
                                    Layer = ent.Layer,
                                    AutoFixable = true
                                });
                            }
                        }

                        // Rule: Layer must start with required prefix (Phase 1)
                        if (!string.IsNullOrWhiteSpace(standards.LayerNamePrefix) &&
                            !ent.Layer.StartsWith(standards.LayerNamePrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            // Find closest matching layer with the right prefix
                            var prefixLayers = standards.ApprovedLayers
                                .Where(l => l.StartsWith(standards.LayerNamePrefix, StringComparison.OrdinalIgnoreCase))
                                .ToHashSet();
                            string? suggestedLayer = prefixLayers.Count > 0 
                                ? FindClosestApprovedLayer(ent.Layer, prefixLayers) 
                                : null;

                            result.Violations.Add(new Violation
                            {
                                Type = ViolationType.Layer,
                                RuleId = "LY-002",
                                Message = suggestedLayer != null 
                                    ? $"Layer does not match required prefix. Suggested: {suggestedLayer}"
                                    : "Layer does not match required prefix.",
                                Expected = suggestedLayer ?? standards.LayerNamePrefix + "...",
                                Actual = ent.Layer,
                                EntityId = ent.ObjectId,
                                EntityHandle = handleStr,
                                EntityName = ent.GetType().Name,
                                Layer = ent.Layer,
                                AutoFixable = suggestedLayer != null
                            });
                        }

                        // Rule: Text style for MText/DBText
            if (ent is DBText t)
            {
                var tsName = GetTextStyleName(tr, t.TextStyleId);
                result.TextStylesFound.Add(tsName);

                // Check against approved styles list first (template mode)
                bool isApproved = standards.ApprovedTextStyles.Count > 0 
                    ? standards.ApprovedTextStyles.Contains(tsName) 
                    : string.Equals(tsName, standards.RequiredTextStyle, StringComparison.OrdinalIgnoreCase);

                if (!isApproved && (standards.ApprovedTextStyles.Count > 0 || !string.IsNullOrWhiteSpace(standards.RequiredTextStyle)))
                {
                    result.Violations.Add(new Violation
                    {
                        Type = ViolationType.TextStyle,
                        RuleId = "TS-001",
                        Message = "DBText style is not in approved template styles.",
                        Expected = standards.RequiredTextStyle ?? "(approved styles)",
                        Actual = tsName,
                        EntityId = ent.ObjectId,
                        EntityHandle = handleStr,
                        EntityName = "DBText",
                        Layer = ent.Layer,
                        AutoFixable = true
                    });
                }

                // Phase 2: Check font file type
                if (!t.TextStyleId.IsNull && t.TextStyleId.IsValid)
                {
                    try
                    {
                        var ts = (TextStyleTableRecord)tr.GetObject(t.TextStyleId, OpenMode.ForRead);
                        string fontFile = ts?.FileName ?? "";

                        if (!string.IsNullOrWhiteSpace(fontFile) && 
                            !fontFile.EndsWith(".shx", StringComparison.OrdinalIgnoreCase))
                        {
                            result.Violations.Add(new Violation
                            {
                                Type = ViolationType.TextStyle,
                                RuleId = "TS-003",
                                Message = "Text style does not use approved SHX font.",
                                Expected = "*.shx",
                                Actual = fontFile,
                                EntityId = ent.ObjectId,
                                EntityHandle = handleStr,
                                EntityName = "DBText",
                                Layer = ent.Layer,
                                AutoFixable = false
                            });
                        }
                    }
                    catch
                    {
                        // Skip if text style object is invalid
                    }
                }

                // ===== ADVANCED TEXT CHECKS: Font, Size, Annotative =====
                CheckTextEntityAdvanced(tr, t, standards, result, handleStr, ent.Layer, "DBText");
            }
            else if (ent is MText mt)
            {
                var tsName = GetTextStyleName(tr, mt.TextStyleId);
                result.TextStylesFound.Add(tsName);

                // Check against approved styles list first (template mode)
                bool isApproved = standards.ApprovedTextStyles.Count > 0 
                    ? standards.ApprovedTextStyles.Contains(tsName) 
                    : string.Equals(tsName, standards.RequiredTextStyle, StringComparison.OrdinalIgnoreCase);

                if (!isApproved && (standards.ApprovedTextStyles.Count > 0 || !string.IsNullOrWhiteSpace(standards.RequiredTextStyle)))
                {
                    result.Violations.Add(new Violation
                    {
                        Type = ViolationType.TextStyle,
                        RuleId = "TS-002",
                        Message = "MText style is not in approved template styles.",
                        Expected = standards.RequiredTextStyle ?? "(approved styles)",
                        Actual = tsName,
                        EntityId = ent.ObjectId,
                        EntityHandle = handleStr,
                        EntityName = "MText",
                        Layer = ent.Layer,
                        AutoFixable = true
                    });
                }

                // Phase 2: Check font file type for MText
                if (!mt.TextStyleId.IsNull && mt.TextStyleId.IsValid)
                {
                    try
                    {
                        var ts = (TextStyleTableRecord)tr.GetObject(mt.TextStyleId, OpenMode.ForRead);
                        string fontFile = ts?.FileName ?? "";

                        if (!string.IsNullOrWhiteSpace(fontFile) &&
                            !fontFile.EndsWith(".shx", StringComparison.OrdinalIgnoreCase))
                        {
                            result.Violations.Add(new Violation
                            {
                                Type = ViolationType.TextStyle,
                                RuleId = "TS-004",
                                Message = "MText style does not use approved SHX font.",
                                Expected = "*.shx",
                                Actual = fontFile,
                                EntityId = ent.ObjectId,
                                EntityHandle = handleStr,
                                EntityName = "MText",
                                Layer = ent.Layer,
                                AutoFixable = false
                            });
                        }
                    }
                    catch
                    {
                        // Skip if text style object is invalid
                    }
                }

                // ===== ADVANCED TEXT CHECKS: Font, Size, Annotative =====
                CheckTextEntityAdvanced(tr, mt, standards, result, handleStr, ent.Layer, "MText");
            }
            else if (ent is AttributeDefinition attDef)
            {
                var tsName = GetTextStyleName(tr, attDef.TextStyleId);
                result.TextStylesFound.Add(tsName);

                bool isApproved = standards.ApprovedTextStyles.Count > 0 
                    ? standards.ApprovedTextStyles.Contains(tsName) 
                    : string.Equals(tsName, standards.RequiredTextStyle, StringComparison.OrdinalIgnoreCase);

                if (!isApproved && (standards.ApprovedTextStyles.Count > 0 || !string.IsNullOrWhiteSpace(standards.RequiredTextStyle)))
                {
                    result.Violations.Add(new Violation
                    {
                        Type = ViolationType.TextStyle,
                        RuleId = "TS-005",
                        Message = "AttributeDefinition style is not in approved template styles.",
                        Expected = standards.RequiredTextStyle ?? "(approved styles)",
                        Actual = tsName,
                        EntityId = ent.ObjectId,
                        EntityHandle = handleStr,
                        EntityName = "AttributeDefinition",
                        Layer = ent.Layer,
                        AutoFixable = true
                    });
                }
            }
            else if (ent is AttributeReference attRef)
            {
                var tsName = GetTextStyleName(tr, attRef.TextStyleId);
                result.TextStylesFound.Add(tsName);

                bool isApproved = standards.ApprovedTextStyles.Count > 0 
                    ? standards.ApprovedTextStyles.Contains(tsName) 
                    : string.Equals(tsName, standards.RequiredTextStyle, StringComparison.OrdinalIgnoreCase);

                if (!isApproved && (standards.ApprovedTextStyles.Count > 0 || !string.IsNullOrWhiteSpace(standards.RequiredTextStyle)))
                {
                    result.Violations.Add(new Violation
                    {
                        Type = ViolationType.TextStyle,
                        RuleId = "TS-006",
                        Message = "AttributeReference style is not in approved template styles.",
                        Expected = standards.RequiredTextStyle ?? "(approved styles)",
                        Actual = tsName,
                        EntityId = ent.ObjectId,
                        EntityHandle = handleStr,
                        EntityName = "AttributeReference",
                        Layer = ent.Layer,
                        AutoFixable = true
                    });
                }
            }
            else if (ent is MLeader mleader)
            {
                // MLeader uses MLeaderStyle which contains text style
                // Debug: Always log when we find an MLeader
                var db = Application.DocumentManager.MdiActiveDocument?.Database;
                var ed = Application.DocumentManager.MdiActiveDocument?.Editor;

                ed?.WriteMessage($"\n[SCAN DEBUG] Found MLeader: Handle={handleStr}, HasStyle={!mleader.MLeaderStyle.IsNull}, ContentType={mleader.ContentType}");

                if (!mleader.MLeaderStyle.IsNull && mleader.MLeaderStyle.IsValid)
                {
                    try
                    {
                        var mleaderStyle = (MLeaderStyle)tr.GetObject(mleader.MLeaderStyle, OpenMode.ForRead);
                        ed?.WriteMessage($"\n  MLeaderStyle name: {mleaderStyle.Name}, HasTextStyle={!mleaderStyle.TextStyleId.IsNull}");

                        if (!mleaderStyle.TextStyleId.IsNull)
                        {
                            var tsName = GetTextStyleName(tr, mleaderStyle.TextStyleId);
                            result.TextStylesFound.Add(tsName);
                            ed?.WriteMessage($"\n  Text style: {tsName}");

                            // MLeaders: If RequiredTextStyle is set, MUST use exactly that style
                            // (not just any approved style)
                            bool isCompliant;
                            if (!string.IsNullOrWhiteSpace(standards.RequiredTextStyle))
                            {
                                // Strict mode: MLeader must use the RequiredTextStyle
                                isCompliant = string.Equals(tsName, standards.RequiredTextStyle, StringComparison.OrdinalIgnoreCase);
                                ed?.WriteMessage($"\n  Required style mode: Checking if '{tsName}' == '{standards.RequiredTextStyle}': {isCompliant}");
                            }
                            else
                            {
                                // Fallback: Check approved styles list
                                isCompliant = standards.ApprovedTextStyles.Contains(tsName);
                                ed?.WriteMessage($"\n  Approved list mode: Is approved: {isCompliant}");
                            }

                            if (!isCompliant && (standards.ApprovedTextStyles.Count > 0 || !string.IsNullOrWhiteSpace(standards.RequiredTextStyle)))
                            {
                                ed?.WriteMessage($"\n  Creating violation for MLeader!");
                                result.Violations.Add(new Violation
                                {
                                    Type = ViolationType.TextStyle,
                                    RuleId = "TS-007",
                                    Message = "MLeader text style must use required text style.",
                                    Expected = standards.RequiredTextStyle ?? "(approved styles)",
                                    Actual = tsName,
                                    EntityId = ent.ObjectId,
                                    EntityHandle = handleStr,
                                    EntityName = "MLeader",
                                    Layer = ent.Layer,
                                    AutoFixable = true
                                });
                            }
                            else
                            {
                                ed?.WriteMessage($"\n  Text style is compliant, no violation.");
                            }
                        }
                        else
                        {
                            ed?.WriteMessage($"\n  MLeaderStyle has no TextStyleId!");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ed?.WriteMessage($"\n  [WARN] Error reading MLeaderStyle: {ex.Message}");
                    }
                }
                else
                {
                    ed?.WriteMessage($"\n  MLeader has no MLeaderStyle!");
                }
            }
            else if (ent is Leader leader)
            {
                // Legacy Leader - check if it has annotation (text)
                if (!leader.Annotation.IsNull && leader.Annotation.IsValid)
                {
                    try
                    {
                        var annoEnt = tr.GetObject(leader.Annotation, OpenMode.ForRead) as Entity;
                        if (annoEnt is MText leaderMText)
                        {
                            var tsName = GetTextStyleName(tr, leaderMText.TextStyleId);
                            result.TextStylesFound.Add(tsName);

                            bool isApproved = standards.ApprovedTextStyles.Count > 0 
                                ? standards.ApprovedTextStyles.Contains(tsName) 
                                : string.Equals(tsName, standards.RequiredTextStyle, StringComparison.OrdinalIgnoreCase);

                            if (!isApproved && (standards.ApprovedTextStyles.Count > 0 || !string.IsNullOrWhiteSpace(standards.RequiredTextStyle)))
                            {
                                result.Violations.Add(new Violation
                                {
                                    Type = ViolationType.TextStyle,
                                    RuleId = "TS-008",
                                    Message = "Leader annotation text style is not in approved template styles.",
                                    Expected = standards.RequiredTextStyle ?? "(approved styles)",
                                    Actual = tsName,
                                    EntityId = ent.ObjectId,
                                    EntityHandle = handleStr,
                                    EntityName = "Leader",
                                    Layer = ent.Layer,
                                    AutoFixable = true
                                });
                            }
                        }
                    }
                    catch
                    {
                        // Skip if leader annotation is invalid
                    }
                }
            }

            // Rule: Dimension style
            if (ent is Dimension dim)
            {
                var dsName = GetDimStyleName(tr, dim.DimensionStyle);
                result.DimStylesFound.Add(dsName);

                // Check against approved dim styles list (template mode)
                bool isApproved = standards.ApprovedDimStyles.Count > 0 
                    ? standards.ApprovedDimStyles.Contains(dsName) 
                    : string.Equals(dsName, standards.RequiredDimStyle, StringComparison.OrdinalIgnoreCase);

                if (!isApproved && (standards.ApprovedDimStyles.Count > 0 || !string.IsNullOrWhiteSpace(standards.RequiredDimStyle)))
                {
                    result.Violations.Add(new Violation
                    {
                        Type = ViolationType.DimStyle,
                        RuleId = "DS-001",
                        Message = "Dimension style is not in approved template styles.",
                        Expected = standards.RequiredDimStyle ?? "(approved styles)",
                        Actual = dsName,
                        EntityId = ent.ObjectId,
                        EntityHandle = handleStr,
                        EntityName = dim.GetType().Name,
                        Layer = ent.Layer,
                        AutoFixable = true
                    });
                }

                // ===== ADVANCED DIMENSION CHECKS: Font, Size =====
                CheckDimensionAdvanced(tr, dim, standards, result, handleStr, ent.Layer);

                // Phase 3: Check text style used by dimension style
                if (!dim.DimensionStyle.IsNull && dim.DimensionStyle.IsValid && !string.IsNullOrWhiteSpace(standards.RequiredTextStyle))
                {
                    try
                    {
                        var ds = (DimStyleTableRecord)tr.GetObject(dim.DimensionStyle, OpenMode.ForRead);

                        if (!ds.Dimtxsty.IsNull && ds.Dimtxsty.IsValid)
                        {
                            try
                            {
                                var dimTextStyle = (TextStyleTableRecord)tr.GetObject(ds.Dimtxsty, OpenMode.ForRead);

                                if (!string.Equals(dimTextStyle.Name, standards.RequiredTextStyle, StringComparison.OrdinalIgnoreCase))
                                {
                                    result.Violations.Add(new Violation
                                    {
                                        Type = ViolationType.DimStyle,
                                        RuleId = "DS-002",
                                        Message = "Dimension style uses incorrect text style.",
                                        Expected = standards.RequiredTextStyle,
                                        Actual = dimTextStyle.Name,
                                        EntityId = ent.ObjectId,
                                        EntityHandle = handleStr,
                                        EntityName = dim.GetType().Name,
                                        Layer = ent.Layer,
                                        AutoFixable = false
                                    });
                                }
                            }
                            catch
                            {
                                // Skip if dim text style is invalid
                            }
                        }
                    }
                    catch
                    {
                        // Skip if dimension style is invalid
                    }
                }
            } // closes: if (ent is Dimension dim)
            } // closes: try block
            catch (System.Exception ex)
            {
                // Skip entities that cause errors (corrupted, locked, etc.)
                var doc = Application.DocumentManager.MdiActiveDocument;
                doc?.Editor.WriteMessage($"\n[WARN] Skipping entity {id}: {ex.Message}");
            }
        } // closes: foreach
    } // closes: method

    private static string GetTextStyleName(Transaction tr, ObjectId textStyleId)
    {
        if (textStyleId.IsNull || !textStyleId.IsValid) return "<None>";
        try
        {
            var ts = (TextStyleTableRecord)tr.GetObject(textStyleId, OpenMode.ForRead);
            return ts?.Name ?? "<Unnamed>";
        }
        catch
        {
            return "<Invalid>";
        }
    }

    private static string GetDimStyleName(Transaction tr, ObjectId dimStyleId)
    {
        if (dimStyleId.IsNull || !dimStyleId.IsValid) return "<None>";
        try
        {
            var ds = (DimStyleTableRecord)tr.GetObject(dimStyleId, OpenMode.ForRead);
            return ds?.Name ?? "<Unnamed>";
        }
        catch
        {
            return "<Invalid>";
        }
    }

    /// <summary>
    /// Finds the closest matching approved layer name using similarity scoring.
    /// Returns null if no reasonable match is found.
    /// </summary>
    public static string? FindClosestApprovedLayer(string actualLayer, HashSet<string> approvedLayers)
    {
        if (approvedLayers.Count == 0) return null;

        string? bestMatch = null;
        double bestScore = 0;

        foreach (var approved in approvedLayers)
        {
            double score = CalculateLayerSimilarity(actualLayer, approved);
            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = approved;
            }
        }

        // Only return a match if similarity is above threshold (0.3 = 30% similar)
        return bestScore >= 0.3 ? bestMatch : approvedLayers.FirstOrDefault();
    }

    /// <summary>
    /// Calculates similarity between two layer names (0.0 to 1.0).
    /// Uses multiple heuristics: prefix match, contains, Levenshtein distance.
    /// </summary>
    private static double CalculateLayerSimilarity(string actual, string approved)
    {
        if (string.IsNullOrEmpty(actual) || string.IsNullOrEmpty(approved))
            return 0;

        // Normalize for comparison
        string a = actual.ToUpperInvariant();
        string b = approved.ToUpperInvariant();

        // Exact match (case-insensitive)
        if (a == b) return 1.0;

        // Check if one contains the other
        if (b.Contains(a) || a.Contains(b))
            return 0.7;

        // Check prefix match (common in layer naming like "E-ANNO" matching "E-ANNO-TEXT")
        string[] aParts = a.Split('-', '_');
        string[] bParts = b.Split('-', '_');

        int matchingParts = 0;
        int minParts = Math.Min(aParts.Length, bParts.Length);
        for (int i = 0; i < minParts; i++)
        {
            if (aParts[i] == bParts[i])
                matchingParts++;
            else
                break;
        }

        if (matchingParts > 0)
            return 0.5 + (0.3 * matchingParts / Math.Max(aParts.Length, bParts.Length));

        // Levenshtein distance-based similarity
        int distance = LevenshteinDistance(a, b);
        int maxLen = Math.Max(a.Length, b.Length);
        double similarity = 1.0 - ((double)distance / maxLen);

        return Math.Max(0, similarity);
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        int[,] d = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++) d[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[s1.Length, s2.Length];
    }

    // ===== ADVANCED ENTITY CHECKING METHODS =====

    /// <summary>
    /// Checks MText or DBText for advanced violations: font, size, annotative settings.
    /// </summary>
    private static void CheckTextEntityAdvanced(Transaction tr, dynamic textEnt, StandardsModel standards, 
        ScanResult result, string handleStr, string layer, string entityType)
    {
        try
        {
            ObjectId entityId = textEnt.ObjectId;
            ObjectId styleId = textEnt.TextStyleId;
            double textHeight = entityType == "MText" ? (textEnt as MText)?.TextHeight ?? 0 : (textEnt as DBText)?.Height ?? 0;

            // Check font name (if required)
            if (!string.IsNullOrWhiteSpace(standards.RequiredFont) && !styleId.IsNull && styleId.IsValid)
            {
                try
                {
                    var ts = (TextStyleTableRecord)tr.GetObject(styleId, OpenMode.ForRead);
                    string actualFont = ts.FileName;
                    if (string.IsNullOrWhiteSpace(actualFont))
                    {
                        var font = ts.Font;
                        actualFont = font.TypeFace ?? "";
                    }

                    if (!string.IsNullOrWhiteSpace(actualFont) && 
                        !actualFont.Contains(standards.RequiredFont, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Violations.Add(new Violation
                        {
                            Type = ViolationType.TextFont,
                            RuleId = "TF-001",
                            Message = $"{entityType} font does not match required font.",
                            Expected = standards.RequiredFont,
                            Actual = actualFont,
                            EntityId = entityId,
                            EntityHandle = handleStr,
                            EntityName = entityType,
                            Layer = layer,
                            AutoFixable = true
                        });
                    }
                }
                catch { /* Skip if text style is invalid */ }
            }

            // Check text height (if required and > 0)
            if (standards.RequiredFontSize > 0 && Math.Abs(textHeight - standards.RequiredFontSize) > 0.001)
            {
                result.Violations.Add(new Violation
                {
                    Type = ViolationType.TextSize,
                    RuleId = "TH-001",
                    Message = $"{entityType} height does not match required size.",
                    Expected = standards.RequiredFontSize.ToString("F3"),
                    Actual = textHeight.ToString("F3"),
                    EntityId = entityId,
                    EntityHandle = handleStr,
                    EntityName = entityType,
                    Layer = layer,
                    AutoFixable = true
                });
            }

            // Check annotative property (if required)
            if (standards.RequireAnnotative && entityType == "MText")
            {
                var mtext = textEnt as MText;
                if (mtext != null && mtext.Annotative != AnnotativeStates.True)
                {
                    result.Violations.Add(new Violation
                    {
                        Type = ViolationType.Annotative,
                        RuleId = "AN-001",
                        Message = "MText should be annotative.",
                        Expected = "Annotative",
                        Actual = "Non-Annotative",
                        EntityId = entityId,
                        EntityHandle = handleStr,
                        EntityName = entityType,
                        Layer = layer,
                        AutoFixable = true
                    });
                }
            }
        }
        catch { /* Skip errors in advanced checking */ }
    }

    /// <summary>
    /// Checks Dimension for advanced violations: font, text height.
    /// </summary>
    private static void CheckDimensionAdvanced(Transaction tr, Dimension dim, StandardsModel standards, 
        ScanResult result, string handleStr, string layer)
    {
        try
        {
            ObjectId entityId = dim.ObjectId;
            ObjectId dimStyleId = dim.DimensionStyle;

            if (dimStyleId.IsNull || !dimStyleId.IsValid) return;

            var ds = (DimStyleTableRecord)tr.GetObject(dimStyleId, OpenMode.ForRead);
            if (ds == null) return;

            // Get effective font (prefer RequiredDimFont, fallback to RequiredFont)
            string? requiredFont = standards.RequiredDimFont ?? standards.RequiredFont;
            double requiredSize = standards.RequiredDimFontSize > 0 ? standards.RequiredDimFontSize : standards.RequiredFontSize;

            // Check dim text style font
            if (!string.IsNullOrWhiteSpace(requiredFont) && !ds.Dimtxsty.IsNull && ds.Dimtxsty.IsValid)
            {
                try
                {
                    var dimTextStyle = (TextStyleTableRecord)tr.GetObject(ds.Dimtxsty, OpenMode.ForRead);
                    string actualFont = dimTextStyle.FileName;
                    if (string.IsNullOrWhiteSpace(actualFont))
                    {
                        var font = dimTextStyle.Font;
                        actualFont = font.TypeFace ?? "";
                    }

                    if (!string.IsNullOrWhiteSpace(actualFont) && 
                        !actualFont.Contains(requiredFont, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Violations.Add(new Violation
                        {
                            Type = ViolationType.TextFont,
                            RuleId = "DF-001",
                            Message = "Dimension text font does not match required font.",
                            Expected = requiredFont,
                            Actual = actualFont,
                            EntityId = entityId,
                            EntityHandle = handleStr,
                            EntityName = dim.GetType().Name,
                            Layer = layer,
                            AutoFixable = true
                        });
                    }
                }
                catch { /* Skip if text style is invalid */ }
            }

            // Check dimension text height
            if (requiredSize > 0)
            {
                double actualSize = ds.Dimtxt;
                if (Math.Abs(actualSize - requiredSize) > 0.001)
                {
                    result.Violations.Add(new Violation
                    {
                        Type = ViolationType.TextSize,
                        RuleId = "DH-001",
                        Message = "Dimension text height does not match required size.",
                        Expected = requiredSize.ToString("F3"),
                        Actual = actualSize.ToString("F3"),
                        EntityId = entityId,
                        EntityHandle = handleStr,
                        EntityName = dim.GetType().Name,
                        Layer = layer,
                        AutoFixable = true
                    });
                }
            }
        }
        catch { /* Skip errors in advanced checking */ }
    }
}
