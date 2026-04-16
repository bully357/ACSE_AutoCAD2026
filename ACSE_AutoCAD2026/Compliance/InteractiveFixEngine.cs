using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using ACSE.AutoCAD2026.Standards;
using ACSE.AutoCAD2026.UI;

namespace ACSE.AutoCAD2026.Compliance
{
    /// <summary>
    /// Interactive fix engine that supports user-customized fixes.
    /// </summary>
    public static class InteractiveFixEngine
    {
        /// <summary>
        /// Applies fixes from ViolationViewModels, using user customizations if provided.
        /// </summary>
        public static FixEngine.FixResult ApplyCustomFixes(
            IEnumerable<ViolationViewModel> violations,
            StandardsModel standards,
            bool previewMode = false)
        {
            var result = new FixEngine.FixResult();
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) throw new InvalidOperationException("No active document.");

            var db = doc.Database;
            var ed = doc.Editor;

            using var tr = db.TransactionManager.StartTransaction();

            foreach (var vm in violations.Where(v => v.IsSelected && v.AutoFixable))
            {
                try
                {
                    if (!FixEngine.TryGetObjectIdFromHandle(db, vm.Violation.EntityHandle, out ObjectId id))
                    {
                        result.FailedCount++;
                        continue;
                    }

                    var ent = tr.GetObject(id, previewMode ? OpenMode.ForRead : OpenMode.ForWrite) as Entity;
                    if (ent == null)
                    {
                        result.FailedCount++;
                        continue;
                    }

                    if (previewMode)
                    {
                        // Preview mode: Just highlight (implement highlighting later)
                        ed.WriteMessage($"\n[PREVIEW] Would fix {vm.EntityName} {vm.Violation.EntityHandle}");
                        result.FixedCount++;
                        continue;
                    }

                    // Apply actual fixes based on violation type and custom values
                    bool wasFixed = ApplySingleCustomFix(tr, db, ent, vm, standards);
                    if (wasFixed)
                        result.FixedCount++;
                    else
                        result.FailedCount++;
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\n[ERROR] Fix failed for {vm.Violation.EntityHandle}: {ex.Message}");
                    result.FailedCount++;
                }
            }

            if (!previewMode)
                tr.Commit();

            return result;
        }

        /// <summary>
        /// Applies a single fix with custom user values.
        /// </summary>
        private static bool ApplySingleCustomFix(
            Transaction tr,
            Database db,
            Entity ent,
            ViolationViewModel vm,
            StandardsModel standards)
        {
            var v = vm.Violation;

            switch (v.Type)
            {
                case ViolationType.Layer:
                    string targetLayer = vm.CustomLayer ?? v.Expected;
                    if (!string.IsNullOrEmpty(targetLayer))
                    {
                        ent.Layer = targetLayer;
                        return true;
                    }
                    break;

                case ViolationType.Linetype:
                    ent.Linetype = v.Expected ?? standards.RequiredLinetype ?? "ByLayer";
                    return true;

                case ViolationType.TextStyle:
                    string targetStyle = vm.CustomStyle ?? v.Expected ?? standards.RequiredTextStyle;
                    if (!string.IsNullOrEmpty(targetStyle))
                    {
                        ObjectId styleId = GetTextStyleId(db, tr, targetStyle);
                        if (ent is DBText dbText)
                            dbText.TextStyleId = styleId;
                        else if (ent is MText mtext)
                            mtext.TextStyleId = styleId;
                        return true;
                    }
                    break;

                case ViolationType.DimStyle:
                    string targetDimStyle = vm.CustomStyle ?? v.Expected ?? standards.RequiredDimStyle;
                    if (!string.IsNullOrEmpty(targetDimStyle) && ent is Dimension dim)
                    {
                        ObjectId dimStyleId = GetDimStyleId(db, tr, targetDimStyle);
                        dim.DimensionStyle = dimStyleId;
                        return true;
                    }
                    break;

                case ViolationType.TextFont:
                    string targetFont = vm.CustomFont ?? v.Expected ?? standards.RequiredFont;
                    if (!string.IsNullOrEmpty(targetFont))
                    {
                        return ApplyFontFix(tr, db, ent, targetFont, standards);
                    }
                    break;

                case ViolationType.TextSize:
                    double targetSize = vm.CustomSize ?? ParseDouble(v.Expected) ?? standards.RequiredFontSize;
                    if (targetSize > 0)
                    {
                        return ApplySizeFix(tr, db, ent, targetSize, standards);
                    }
                    break;

                case ViolationType.Annotative:
                    bool makeAnnotative = vm.CustomAnnotative ?? true;
                    if (ent is MText mtextAnnot)
                    {
                        mtextAnnot.Annotative = makeAnnotative ? AnnotativeStates.True : AnnotativeStates.False;
                        return true;
                    }
                    break;
            }

            return false;
        }

        private static bool ApplyFontFix(Transaction tr, Database db, Entity ent, string font, StandardsModel standards)
        {
            try
            {
                ObjectId styleId = ObjectId.Null;

                if (ent is DBText dbText)
                    styleId = dbText.TextStyleId;
                else if (ent is MText mtext)
                    styleId = mtext.TextStyleId;
                else if (ent is Dimension dim)
                {
                    var ds = (DimStyleTableRecord)tr.GetObject(dim.DimensionStyle, OpenMode.ForWrite);
                    styleId = ds.Dimtxsty;
                }

                if (styleId.IsNull || !styleId.IsValid) return false;

                var ts = (TextStyleTableRecord)tr.GetObject(styleId, OpenMode.ForWrite);

                if (font.EndsWith(".shx", StringComparison.OrdinalIgnoreCase))
                {
                    ts.FileName = font;
                }
                else
                {
                    ts.Font = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor(
                        font, false, false, 0, 0);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool ApplySizeFix(Transaction tr, Database db, Entity ent, double size, StandardsModel standards)
        {
            try
            {
                if (standards.MatchDrawingScale)
                    size *= GetDrawingScale(db);

                if (ent is DBText dbText)
                {
                    dbText.Height = size;
                    return true;
                }
                else if (ent is MText mtext)
                {
                    mtext.TextHeight = size;
                    return true;
                }
                else if (ent is Dimension dim)
                {
                    var ds = (DimStyleTableRecord)tr.GetObject(dim.DimensionStyle, OpenMode.ForWrite);
                    ds.Dimtxt = size;
                    dim.RecomputeDimensionBlock(true);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static double GetDrawingScale(Database db)
        {
            try
            {
                var annoScale = db.Cannoscale;
                if (annoScale != null)
                    return annoScale.Scale;
            }
            catch { }
            return 1.0;
        }

        private static ObjectId GetTextStyleId(Database db, Transaction tr, string styleName)
        {
            var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            if (tst.Has(styleName))
                return tst[styleName];
            return db.Textstyle;
        }

        private static ObjectId GetDimStyleId(Database db, Transaction tr, string styleName)
        {
            var dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
            if (dst.Has(styleName))
                return dst[styleName];
            return db.Dimstyle;
        }

        private static double? ParseDouble(string? value)
        {
            if (double.TryParse(value, out double result))
                return result;
            return null;
        }

        /// <summary>
        /// Gets available text styles from the drawing.
        /// </summary>
        public static List<string> GetAvailableTextStyles(Database db)
        {
            var styles = new List<string>();
            using var tr = db.TransactionManager.StartTransaction();
            var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            
            foreach (ObjectId id in tst)
            {
                var ts = (TextStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
                styles.Add(ts.Name);
            }
            
            tr.Commit();
            return styles;
        }

        /// <summary>
        /// Gets available dimension styles from the drawing.
        /// </summary>
        public static List<string> GetAvailableDimStyles(Database db)
        {
            var styles = new List<string>();
            using var tr = db.TransactionManager.StartTransaction();
            var dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
            
            foreach (ObjectId id in dst)
            {
                var ds = (DimStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
                styles.Add(ds.Name);
            }
            
            tr.Commit();
            return styles;
        }

        /// <summary>
        /// Gets available layers from the drawing.
        /// </summary>
        public static List<string> GetAvailableLayers(Database db)
        {
            var layers = new List<string>();
            using var tr = db.TransactionManager.StartTransaction();
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            
            foreach (ObjectId id in lt)
            {
                var layer = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                layers.Add(layer.Name);
            }
            
            tr.Commit();
            return layers;
        }

        /// <summary>
        /// Gets common font names from text styles in the drawing.
        /// </summary>
        public static List<string> GetAvailableFonts(Database db)
        {
            var fonts = new HashSet<string>();
            using var tr = db.TransactionManager.StartTransaction();
            var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            
            foreach (ObjectId id in tst)
            {
                var ts = (TextStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
                if (!string.IsNullOrEmpty(ts.FileName))
                    fonts.Add(ts.FileName);
                if (ts.Font != null && !string.IsNullOrEmpty(ts.Font.TypeFace))
                    fonts.Add(ts.Font.TypeFace);
            }
            
            tr.Commit();
            
            // Add common standard fonts
            fonts.Add("romans.shx");
            fonts.Add("txt.shx");
            fonts.Add("Arial");
            fonts.Add("Times New Roman");
            
            return fonts.ToList();
        }
    }
}
