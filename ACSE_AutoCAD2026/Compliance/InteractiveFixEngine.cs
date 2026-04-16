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

            // CRITICAL: Lock the document. This method is called from a modeless
            // WPF window; without the lock, every ForWrite open throws eLockViolation
            // and the outer catch silently counts it as "failed".
            // In preview mode we only read, but locking is cheap and still safe.
            using var docLock = doc.LockDocument();
            using var tr = db.TransactionManager.StartTransaction();

            foreach (var vm in violations.Where(v => v.IsSelected && v.AutoFixable))
            {
                try
                {
                    if (!FixEngine.TryGetObjectIdFromHandle(db, vm.Violation.EntityHandle, out ObjectId id))
                    {
                        ed.WriteMessage($"\n[ACSE] Stale handle, skipping: {vm.Violation.EntityHandle}");
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
                    ed.WriteMessage($"\n[ERROR] Fix failed for {vm.EntityName} {vm.Violation.EntityHandle} ({vm.Violation.Type}): {ex.Message}");
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
                    if (string.IsNullOrEmpty(targetStyle))
                        break;

                    // Resolve target style. If it's not in the drawing, attempt to
                    // import from the template (same behavior as Fix-All). If still
                    // missing, refuse the fix rather than silently assigning the
                    // current default text style.
                    // GetOrImportTextStyleByName checks the drawing first, then imports
                    // from the template if missing. Returns ObjectId.Null on failure.
                    ObjectId styleId = FixEngine.GetOrImportTextStyleByName(db, tr, targetStyle, standards.TemplatePath ?? "");
                    if (styleId.IsNull || !styleId.IsValid)
                    {
                        var ed = Application.DocumentManager.MdiActiveDocument?.Editor;
                        ed?.WriteMessage($"\n[ACSE] TextStyle '{targetStyle}' not found in drawing or template; skipping.");
                        return false;
                    }

                    // Apply to every entity type the scanner flags for TextStyle.
                    switch (ent)
                    {
                        case DBText dbText:
                            dbText.TextStyleId = styleId;
                            return true;
                        case MText mtext:
                            mtext.TextStyleId = styleId;
                            return true;
                        case AttributeDefinition attDef:
                            attDef.TextStyleId = styleId;
                            return true;
                        case AttributeReference attRef:
                            attRef.TextStyleId = styleId;
                            return true;
                        case MLeader mleader:
                            // Per-instance override (doesn't mutate the shared MLeaderStyle).
                            mleader.TextStyleId = styleId;
                            return true;
                        case Leader leader when !leader.Annotation.IsNull && leader.Annotation.IsValid:
                            if (tr.GetObject(leader.Annotation, OpenMode.ForWrite) is MText leaderMText)
                            {
                                leaderMText.TextStyleId = styleId;
                                return true;
                            }
                            break;
                    }
                    return false;

                case ViolationType.DimStyle:
                    string targetDimStyle = vm.CustomStyle ?? v.Expected ?? standards.RequiredDimStyle;
                    if (string.IsNullOrEmpty(targetDimStyle) || ent is not Dimension dim)
                        break;

                    ObjectId dimStyleId = FixEngine.GetOrImportDimStyle(db, tr, targetDimStyle, standards.TemplatePath ?? "");
                    if (dimStyleId.IsNull || !dimStyleId.IsValid)
                    {
                        var ed = Application.DocumentManager.MdiActiveDocument?.Editor;
                        ed?.WriteMessage($"\n[ACSE] DimStyle '{targetDimStyle}' not found in drawing or template; skipping.");
                        return false;
                    }
                    dim.DimensionStyle = dimStyleId;
                    dim.RecomputeDimensionBlock(true);
                    return true;

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

        // ==========================================================================
        // Per-entity font & size fixes
        // --------------------------------------------------------------------------
        // Design note: Font is a property of a TextStyle, not a text entity. Likewise
        // dimension text height is a property of a DimStyle, not of an individual
        // dimension. The old implementation "fixed" a single entity by mutating the
        // shared TextStyle/DimStyle record, which silently changed every other entity
        // using that style — a huge footgun.
        //
        // The new approach: find or create a sibling style that has the required
        // font/height, then ASSIGN that style to this one entity. Nothing shared is
        // mutated. If the user wants a drawing-wide change, they should fix the
        // style itself through the Template Extractor / Style Manager UI.
        // ==========================================================================

        private static bool ApplyFontFix(Transaction tr, Database db, Entity ent, string font, StandardsModel standards)
        {
            try
            {
                // Dimension fonts live on the DimStyle's Dimtxsty, not the dimension
                // itself. Instead of mutating that TextStyle, swap the dimension to a
                // DimStyle variant whose text style has the right font.
                if (ent is Dimension dim)
                {
                    ObjectId newDimStyleId = FindOrCreateDimStyleVariant(
                        db, tr, dim.DimensionStyle,
                        requiredTextHeight: null,
                        requiredFont: font);
                    if (newDimStyleId.IsNull) return false;
                    dim.DimensionStyle = newDimStyleId;
                    dim.RecomputeDimensionBlock(true);
                    return true;
                }

                // For regular text entities, swap to a text style that has the required font.
                ObjectId currentTsId = GetEntityTextStyleId(ent, tr);
                if (currentTsId.IsNull || !currentTsId.IsValid) return false;

                ObjectId newTsId = FindOrCreateTextStyleWithFont(db, tr, currentTsId, font);
                if (newTsId.IsNull || !newTsId.IsValid) return false;

                return AssignTextStyleToEntity(ent, tr, newTsId);
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

                // DBText and MText have a true per-instance height — just set it.
                if (ent is DBText dbText)
                {
                    dbText.Height = size;
                    return true;
                }
                if (ent is MText mtext)
                {
                    mtext.TextHeight = size;
                    return true;
                }
                if (ent is AttributeReference attRef)
                {
                    attRef.Height = size;
                    return true;
                }
                if (ent is AttributeDefinition attDef)
                {
                    attDef.Height = size;
                    return true;
                }

                // Dimension text height lives on the DimStyle (DIMTXT). Swap to a
                // DimStyle variant with the correct height instead of mutating the
                // shared record.
                if (ent is Dimension dim)
                {
                    ObjectId newDimStyleId = FindOrCreateDimStyleVariant(
                        db, tr, dim.DimensionStyle,
                        requiredTextHeight: size,
                        requiredFont: null);
                    if (newDimStyleId.IsNull) return false;
                    dim.DimensionStyle = newDimStyleId;
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

        // ---------- helpers for font/size fixes ----------

        /// <summary>
        /// Returns the current text style ObjectId for a given entity, or Null if
        /// the entity type doesn't carry a text style directly.
        /// </summary>
        private static ObjectId GetEntityTextStyleId(Entity ent, Transaction tr)
        {
            switch (ent)
            {
                case DBText t: return t.TextStyleId;
                case MText m: return m.TextStyleId;
                case AttributeDefinition ad: return ad.TextStyleId;
                case AttributeReference ar: return ar.TextStyleId;
                case MLeader ml:
                    // Prefer the per-instance override; fall back to the MLeaderStyle's text style.
                    if (!ml.TextStyleId.IsNull && ml.TextStyleId.IsValid)
                        return ml.TextStyleId;
                    if (!ml.MLeaderStyle.IsNull && ml.MLeaderStyle.IsValid)
                    {
                        try
                        {
                            var mls = (MLeaderStyle)tr.GetObject(ml.MLeaderStyle, OpenMode.ForRead);
                            return mls.TextStyleId;
                        }
                        catch { }
                    }
                    return ObjectId.Null;
                default:
                    return ObjectId.Null;
            }
        }

        /// <summary>
        /// Assigns a text style to an entity that supports per-instance text styles.
        /// Never mutates the shared TextStyleTableRecord.
        /// </summary>
        private static bool AssignTextStyleToEntity(Entity ent, Transaction tr, ObjectId styleId)
        {
            switch (ent)
            {
                case DBText t: t.TextStyleId = styleId; return true;
                case MText m: m.TextStyleId = styleId; return true;
                case AttributeDefinition ad: ad.TextStyleId = styleId; return true;
                case AttributeReference ar: ar.TextStyleId = styleId; return true;
                case MLeader ml: ml.TextStyleId = styleId; return true;
                default: return false;
            }
        }

        /// <summary>
        /// Finds a TextStyle in the current drawing whose font matches <paramref name="font"/>,
        /// or creates a new one cloned from <paramref name="baseStyleId"/>'s properties. Never
        /// mutates the caller's base style.
        /// </summary>
        private static ObjectId FindOrCreateTextStyleWithFont(
            Database db, Transaction tr, ObjectId baseStyleId, string font)
        {
            // 1) If the current style already matches, keep using it.
            try
            {
                var baseTs = (TextStyleTableRecord)tr.GetObject(baseStyleId, OpenMode.ForRead);
                if (FontMatches(baseTs, font)) return baseStyleId;
            }
            catch { /* fall through */ }

            var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);

            // 2) Look for any existing style in the drawing with the right font.
            foreach (ObjectId id in tst)
            {
                try
                {
                    var ts = (TextStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (FontMatches(ts, font)) return id;
                }
                catch { }
            }

            // 3) Create a new style. Name is derived from base name + sanitized font
            //    so repeated fixes of the same font share one style instead of
            //    generating `Style_Arial_2`, `_3`, etc.
            string baseName = "Style";
            try
            {
                baseName = ((TextStyleTableRecord)tr.GetObject(baseStyleId, OpenMode.ForRead)).Name;
            }
            catch { }

            string desiredName = $"ACSE_{SanitizeName(baseName)}_{SanitizeName(System.IO.Path.GetFileNameWithoutExtension(font))}";
            if (tst.Has(desiredName)) return tst[desiredName];

            try
            {
                tst.UpgradeOpen();
                var newTs = new TextStyleTableRecord { Name = desiredName };
                if (font.EndsWith(".shx", StringComparison.OrdinalIgnoreCase))
                {
                    newTs.FileName = font;
                }
                else
                {
                    newTs.Font = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor(
                        font, false, false, 0, 0);
                }

                // Copy a few benign properties from the base style so the created
                // style feels like a variant rather than a blank.
                try
                {
                    var baseTs = (TextStyleTableRecord)tr.GetObject(baseStyleId, OpenMode.ForRead);
                    newTs.TextSize = baseTs.TextSize;
                    newTs.XScale = baseTs.XScale;
                    newTs.ObliquingAngle = baseTs.ObliquingAngle;
                    newTs.IsVertical = baseTs.IsVertical;
                    newTs.IsAnnotative = baseTs.IsAnnotative;
                }
                catch { }

                ObjectId newId = tst.Add(newTs);
                tr.AddNewlyCreatedDBObject(newTs, true);
                return newId;
            }
            catch
            {
                return ObjectId.Null;
            }
        }

        /// <summary>
        /// Finds a DimStyle in the current drawing matching the requested text height
        /// and/or text-style font, or creates a clone of <paramref name="baseDimStyleId"/>
        /// with those properties overridden. Never mutates the caller's base dim style.
        /// </summary>
        private static ObjectId FindOrCreateDimStyleVariant(
            Database db, Transaction tr, ObjectId baseDimStyleId,
            double? requiredTextHeight, string? requiredFont)
        {
            if (baseDimStyleId.IsNull || !baseDimStyleId.IsValid) return ObjectId.Null;

            DimStyleTableRecord? baseDs;
            try { baseDs = (DimStyleTableRecord)tr.GetObject(baseDimStyleId, OpenMode.ForRead); }
            catch { return ObjectId.Null; }
            if (baseDs == null) return ObjectId.Null;

            // 1) If the base already satisfies the request, use it.
            if (DimStyleMatches(tr, baseDs, requiredTextHeight, requiredFont))
                return baseDimStyleId;

            var dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);

            // 2) Look for an existing matching dim style in the drawing.
            foreach (ObjectId id in dst)
            {
                if (id == baseDimStyleId) continue;
                try
                {
                    var ds = (DimStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (DimStyleMatches(tr, ds, requiredTextHeight, requiredFont))
                        return id;
                }
                catch { }
            }

            // 3) Create a variant cloned from the base.
            string heightTag = requiredTextHeight.HasValue
                ? $"h{requiredTextHeight.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}"
                : "";
            string fontTag = !string.IsNullOrWhiteSpace(requiredFont)
                ? SanitizeName(System.IO.Path.GetFileNameWithoutExtension(requiredFont!))
                : "";
            string desiredName = $"ACSE_{SanitizeName(baseDs.Name)}_{heightTag}{(heightTag.Length > 0 && fontTag.Length > 0 ? "_" : "")}{fontTag}";
            if (dst.Has(desiredName)) return dst[desiredName];

            try
            {
                dst.UpgradeOpen();
                var newDs = new DimStyleTableRecord();
                newDs.CopyFrom(baseDs);
                newDs.Name = desiredName;

                if (requiredTextHeight.HasValue && requiredTextHeight.Value > 0)
                    newDs.Dimtxt = requiredTextHeight.Value;

                if (!string.IsNullOrWhiteSpace(requiredFont))
                {
                    ObjectId textStyleForDim = FindOrCreateTextStyleWithFont(
                        db, tr,
                        !newDs.Dimtxsty.IsNull ? newDs.Dimtxsty : db.Textstyle,
                        requiredFont);
                    if (!textStyleForDim.IsNull) newDs.Dimtxsty = textStyleForDim;
                }

                ObjectId newId = dst.Add(newDs);
                tr.AddNewlyCreatedDBObject(newDs, true);
                return newId;
            }
            catch
            {
                return ObjectId.Null;
            }
        }

        private static bool FontMatches(TextStyleTableRecord ts, string font)
        {
            if (ts == null || string.IsNullOrWhiteSpace(font)) return false;

            if (font.EndsWith(".shx", StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrEmpty(ts.FileName)
                    && string.Equals(System.IO.Path.GetFileName(ts.FileName), font,
                        StringComparison.OrdinalIgnoreCase);
            }

            // TTF / system font — compare against the FontDescriptor typeface.
            try
            {
                var typeFace = ts.Font?.TypeFace ?? "";
                return string.Equals(typeFace, font, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool DimStyleMatches(Transaction tr, DimStyleTableRecord ds,
            double? requiredTextHeight, string? requiredFont)
        {
            if (requiredTextHeight.HasValue && requiredTextHeight.Value > 0)
            {
                if (Math.Abs(ds.Dimtxt - requiredTextHeight.Value) > 0.0001)
                    return false;
            }
            if (!string.IsNullOrWhiteSpace(requiredFont))
            {
                if (ds.Dimtxsty.IsNull || !ds.Dimtxsty.IsValid) return false;
                try
                {
                    var ts = (TextStyleTableRecord)tr.GetObject(ds.Dimtxsty, OpenMode.ForRead);
                    if (!FontMatches(ts, requiredFont)) return false;
                }
                catch { return false; }
            }
            return true;
        }

        /// <summary>
        /// Produces a string safe for use in AutoCAD symbol table names: alphanumerics
        /// plus '-' and '_' only, max ~30 chars.
        /// </summary>
        private static string SanitizeName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "X";
            var sb = new System.Text.StringBuilder();
            foreach (var c in input)
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_') sb.Append(c);
            }
            if (sb.Length == 0) sb.Append('X');
            if (sb.Length > 30) sb.Length = 30;
            return sb.ToString();
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

        // NOTE: GetTextStyleId / GetDimStyleId removed — InteractiveFixEngine never
        // called them. The apply path uses GetOrImportTextStyleByName and the
        // dedicated FindOrCreate*Variant helpers instead. Other engines
        // (FixEngine, SmartFixEngine, GlobalTextModifier) keep their own copies.

        private static double? ParseDouble(string? value)
        {
            // Culture-invariant parse — European locales use comma as decimal
            // separator. Using the plain overload would fail there or misread values.
            if (double.TryParse(value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double result))
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
        /// Returns fonts actually used by text styles in the drawing. The previous
        /// implementation sprinkled in a hardcoded list ("Arial", "romans.shx"…),
        /// which misled users into picking fonts that weren't available on the
        /// target system. Callers that want a broader list (e.g. installed Windows
        /// fonts) should enumerate InstalledFontCollection themselves.
        /// </summary>
        public static List<string> GetAvailableFonts(Database db)
        {
            var fonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
            return fonts.ToList();
        }
    }
}
