using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using ACSE.AutoCAD2026.Standards;

namespace ACSE.AutoCAD2026.Tools
{
    /// <summary>
    /// Dimension Style Management Tool for ACSE Plugin.
    /// Audits, creates, updates, and standardizes dimension styles.
    /// </summary>
    public static class DimensionStyleManager
    {
        /// <summary>
        /// Main management method - audits and standardizes dimension styles.
        /// </summary>
        /// <param name="standards">Standards model with dimension style requirements</param>
        /// <param name="interactive">If true, prompts for user overrides</param>
        /// <returns>Summary of operations performed</returns>
        public static DimStyleResult ManageDimStyles(StandardsModel standards, bool interactive = true)
        {
            var result = new DimStyleResult();
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                result.ErrorMessage = "No active document.";
                return result;
            }

            var db = doc.Database;
            var ed = doc.Editor;

            try
            {
                using var docLock = doc.LockDocument();
                using var tr = db.TransactionManager.StartTransaction();

                // Step 1: Audit existing dim styles
                ed.WriteMessage("\n=== Dimension Style Audit ===");
                var dimStyleTable = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);

                foreach (ObjectId id in dimStyleTable)
                {
                    var dimStyle = (DimStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    bool isCompliant = CheckCompliance(dimStyle, standards);

                    result.AuditedStyles.Add(new DimStyleAudit
                    {
                        StyleName = dimStyle.Name,
                        IsCompliant = isCompliant,
                        ObjectId = id
                    });

                    ed.WriteMessage($"\nStyle: {dimStyle.Name,-20} Compliant: {(isCompliant ? "✓" : "✗")}");

                    if (!isCompliant && dimStyle.Name == standards.RequiredDimStyleName)
                    {
                        LogViolations(dimStyle, standards, ed);
                    }
                }

                // Step 2: Create or update required style
                dimStyleTable.UpgradeOpen();
                DimStyleTableRecord requiredStyle;

                if (dimStyleTable.Has(standards.RequiredDimStyleName))
                {
                    requiredStyle = (DimStyleTableRecord)tr.GetObject(
                        dimStyleTable[standards.RequiredDimStyleName], OpenMode.ForWrite);
                    ed.WriteMessage($"\n\nUpdating existing style '{standards.RequiredDimStyleName}' to standards...");
                    result.UpdatedStyles++;
                }
                else
                {
                    requiredStyle = new DimStyleTableRecord { Name = standards.RequiredDimStyleName };
                    dimStyleTable.Add(requiredStyle);
                    tr.AddNewlyCreatedDBObject(requiredStyle, true);
                    ed.WriteMessage($"\n\nCreated new style '{standards.RequiredDimStyleName}'.");
                    result.CreatedStyles++;
                }

                // Apply standards
                ApplyStandards(requiredStyle, standards, tr, db);

                // Step 3: Interactive overrides (if enabled)
                if (interactive)
                {
                    ed.WriteMessage("\n\n=== Interactive Overrides (press Enter to skip) ===");

                    var heightOpt = new PromptDoubleOptions($"\nText height (current: {requiredStyle.Dimtxt:F3}): ");
                    heightOpt.AllowNone = true;
                    var heightRes = ed.GetDouble(heightOpt);
                    if (heightRes.Status == PromptStatus.OK)
                    {
                        requiredStyle.Dimtxt = heightRes.Value;
                        ed.WriteMessage($"  Updated to {heightRes.Value:F3}");
                    }

                    var arrowOpt = new PromptDoubleOptions($"\nArrow size (current: {requiredStyle.Dimasz:F3}): ");
                    arrowOpt.AllowNone = true;
                    var arrowRes = ed.GetDouble(arrowOpt);
                    if (arrowRes.Status == PromptStatus.OK)
                    {
                        requiredStyle.Dimasz = arrowRes.Value;
                        ed.WriteMessage($"  Updated to {arrowRes.Value:F3}");
                    }
                }

                // Step 4: Set as active if configured
                if (standards.SetDimStyleAsCurrent)
                {
                    db.Dimstyle = requiredStyle.ObjectId;
                    ed.WriteMessage($"\n\nSet '{standards.RequiredDimStyleName}' as active dimension style.");
                    result.SetAsCurrent = true;
                }

                // Step 5: Update existing dimensions to use the new style
                ed.WriteMessage("\n\n=== Updating Existing Dimensions ===");
                int updatedDimensions = UpdateDimensionsToStyle(requiredStyle.ObjectId, tr, db, ed);
                ed.WriteMessage($"\nUpdated {updatedDimensions} dimension(s) to '{standards.RequiredDimStyleName}' style.");
                result.UpdatedDimensions = updatedDimensions;

                // Step 6: Purge unused styles (simplified - just report, don't purge yet)
                if (standards.PurgeUnusedDimStyles)
                {
                    ed.WriteMessage("\n\nNote: Purge functionality not yet implemented.");
                }

                tr.Commit();
                result.Success = true;
                ed.WriteMessage("\n\n=== Dimension Style Management Complete ===");
                ed.WriteMessage($"\nCreated: {result.CreatedStyles}, Updated: {result.UpdatedStyles}");
                ed.WriteMessage($"\nDimensions updated: {result.UpdatedDimensions}");
            }
            catch (System.Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                ed.WriteMessage($"\n\nERROR: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Apply all standards to a dimension style.
        /// </summary>
        private static void ApplyStandards(DimStyleTableRecord dimStyle, StandardsModel standards, Transaction tr, Database db)
        {
            // Text properties
            dimStyle.Dimtxt = standards.DimTextHeight;
            dimStyle.Dimclrt = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, (short)standards.DimTextColor);
            dimStyle.Dimgap = standards.DimTextGap;

            // Arrow properties
            dimStyle.Dimasz = standards.DimArrowSize;
            // Note: Dimblk requires ObjectId. Empty string means default closed filled arrow.
            // For custom blocks, would need to look up block table record.
            // Skipping arrow block for now to avoid complexity.

            // Extension line properties
            dimStyle.Dimexe = standards.DimExtensionLineExtend;
            dimStyle.Dimexo = standards.DimExtensionLineOffset;
            dimStyle.Dimclre = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, (short)standards.DimExtLineColor);
            dimStyle.Dimlwe = (LineWeight)standards.DimExtLineWeight;

            // Dimension line properties
            dimStyle.Dimclrd = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, (short)standards.DimLineColor);
            dimStyle.Dimlwd = (LineWeight)standards.DimLineWeight;

            // Units and precision
            dimStyle.Dimlunit = standards.DimUnits;
            dimStyle.Dimdec = standards.DimDecimals;
            dimStyle.Dimlfac = standards.DimScaleFactor;

            // Annotative property
            // Note: Annotative is not a standard property on DimStyleTableRecord
            // It's controlled via the dimension itself, not the style
            // Skipping annotative property for dimension styles
        }

        /// <summary>
        /// Update all dimensions in the drawing to use the specified style.
        /// </summary>
        private static int UpdateDimensionsToStyle(ObjectId styleId, Transaction tr, Database db, Editor ed)
        {
            int count = 0;
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var modelSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

            // Process model space
            count += ProcessBlockForDimensions(modelSpace, styleId, tr, ed);

            // Process paper space layouts
            var paperSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.PaperSpace], OpenMode.ForRead);
            count += ProcessBlockForDimensions(paperSpace, styleId, tr, ed);

            return count;
        }

        /// <summary>
        /// Process a block table record and update all dimensions to the specified style.
        /// </summary>
        private static int ProcessBlockForDimensions(BlockTableRecord block, ObjectId styleId, Transaction tr, Editor ed)
        {
            int count = 0;

            foreach (ObjectId id in block)
            {
                var obj = tr.GetObject(id, OpenMode.ForRead);

                // Check if it's a dimension
                if (obj is Dimension dim)
                {
                    // Only update if it's using a different style
                    if (dim.DimensionStyle != styleId)
                    {
                        dim.UpgradeOpen();
                        dim.DimensionStyle = styleId;
                        count++;
                    }
                }
                // Also check for block references (dimensions might be in blocks)
                else if (obj is BlockReference blockRef && !blockRef.IsDynamicBlock)
                {
                    var blockDef = (BlockTableRecord)tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead);
                    if (!blockDef.IsLayout && !blockDef.IsAnonymous)
                    {
                        count += ProcessBlockForDimensions(blockDef, styleId, tr, ed);
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Check if dimension style matches standards.
        /// </summary>
        private static bool CheckCompliance(DimStyleTableRecord dimStyle, StandardsModel standards)
        {
            return Math.Abs(dimStyle.Dimtxt - standards.DimTextHeight) < 0.0001 &&
                   Math.Abs(dimStyle.Dimasz - standards.DimArrowSize) < 0.0001 &&
                   Math.Abs(dimStyle.Dimexe - standards.DimExtensionLineExtend) < 0.0001 &&
                   Math.Abs(dimStyle.Dimexo - standards.DimExtensionLineOffset) < 0.0001 &&
                   dimStyle.Dimlunit == standards.DimUnits &&
                   dimStyle.Dimdec == standards.DimDecimals &&
                   Math.Abs(dimStyle.Dimlfac - standards.DimScaleFactor) < 0.0001;
        }

        /// <summary>
        /// Log violations for a non-compliant style.
        /// </summary>
        private static void LogViolations(DimStyleTableRecord dimStyle, StandardsModel standards, Editor ed)
        {
            ed.WriteMessage("\n  Violations:");

            if (Math.Abs(dimStyle.Dimtxt - standards.DimTextHeight) >= 0.0001)
                ed.WriteMessage($"\n    Text Height: {dimStyle.Dimtxt:F3} (expected {standards.DimTextHeight:F3})");

            if (Math.Abs(dimStyle.Dimasz - standards.DimArrowSize) >= 0.0001)
                ed.WriteMessage($"\n    Arrow Size: {dimStyle.Dimasz:F3} (expected {standards.DimArrowSize:F3})");

            if (Math.Abs(dimStyle.Dimexe - standards.DimExtensionLineExtend) >= 0.0001)
                ed.WriteMessage($"\n    Extension Extend: {dimStyle.Dimexe:F3} (expected {standards.DimExtensionLineExtend:F3})");

            if (Math.Abs(dimStyle.Dimexo - standards.DimExtensionLineOffset) >= 0.0001)
                ed.WriteMessage($"\n    Extension Offset: {dimStyle.Dimexo:F3} (expected {standards.DimExtensionLineOffset:F3})");

            if (dimStyle.Dimlunit != standards.DimUnits)
                ed.WriteMessage($"\n    Units: {dimStyle.Dimlunit} (expected {standards.DimUnits})");

            if (dimStyle.Dimdec != standards.DimDecimals)
                ed.WriteMessage($"\n    Decimals: {dimStyle.Dimdec} (expected {standards.DimDecimals})");
        }

        /// <summary>
        /// Command: ACSE_DIMMGR - Interactive dimension style management
        /// </summary>
        [CommandMethod("ACSE_DIMMGR", CommandFlags.Modal)]
        public static void DimManagerCommand()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                // Prompt user to select standards file using AutoCAD's file dialog
                var fileRes = doc.Editor.GetFileNameForOpen("\nSelect Standards JSON file: ");
                if (fileRes.Status != PromptStatus.OK)
                {
                    doc.Editor.WriteMessage("\nOperation cancelled.");
                    return;
                }

                // Load standards
                var standards = StandardsLoader.Load(fileRes.StringResult);

                doc.Editor.WriteMessage("\n=== ACSE Dimension Style Manager ===\n");
                var result = ManageDimStyles(standards, interactive: true);

                if (result.Success)
                {
                    doc.Editor.WriteMessage("\n\n✓ Dimension style management completed successfully!");
                }
                else
                {
                    doc.Editor.WriteMessage($"\n\n✗ Management failed: {result.ErrorMessage}");
                }
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\n\n✗ Command failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Command: ACSE_DIMMGR_BATCH - Non-interactive batch mode
        /// </summary>
        [CommandMethod("ACSE_DIMMGR_BATCH", CommandFlags.Modal)]
        public static void DimManagerBatchCommand()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                // Prompt user to select standards file using AutoCAD's file dialog
                var fileRes = doc.Editor.GetFileNameForOpen("\nSelect Standards JSON file: ");
                if (fileRes.Status != PromptStatus.OK)
                {
                    doc.Editor.WriteMessage("\nOperation cancelled.");
                    return;
                }

                // Load standards
                var standards = StandardsLoader.Load(fileRes.StringResult);

                doc.Editor.WriteMessage("\n=== ACSE Dimension Style Manager (Batch Mode) ===\n");
                var result = ManageDimStyles(standards, interactive: false);

                if (result.Success)
                {
                    doc.Editor.WriteMessage("\n\n✓ Dimension style management completed!");
                    doc.Editor.WriteMessage($"\nCreated: {result.CreatedStyles}, Updated: {result.UpdatedStyles}, Purged: {result.PurgedStyles}");
                }
                else
                {
                    doc.Editor.WriteMessage($"\n\n✗ Management failed: {result.ErrorMessage}");
                }
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\n\n✗ Command failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Result from dimension style management operation.
    /// </summary>
    public class DimStyleResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public int CreatedStyles { get; set; }
        public int UpdatedStyles { get; set; }
        public int PurgedStyles { get; set; }
        public int UpdatedDimensions { get; set; }
        public bool SetAsCurrent { get; set; }
        public List<DimStyleAudit> AuditedStyles { get; set; } = new List<DimStyleAudit>();
    }

    /// <summary>
    /// Audit information for a single dimension style.
    /// </summary>
    public class DimStyleAudit
    {
        public string StyleName { get; set; } = "";
        public bool IsCompliant { get; set; }
        public ObjectId ObjectId { get; set; }
    }
}
