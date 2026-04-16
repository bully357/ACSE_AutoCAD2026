using System;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using ACSE.AutoCAD2026.Standards;

namespace ACSE.AutoCAD2026.Compliance
{
    public static class FixEngine
    {
        public static int ApplyAutoFixes(ScanResult scanResult, StandardsModel standards)
        {
            var doc = Application.DocumentManager.MdiActiveDocument
                      ?? throw new InvalidOperationException("No active document.");

            var db = doc.Database;
            int fixedCount = 0;

            using var tr = db.TransactionManager.StartTransaction();

            foreach (var violation in scanResult.Violations)
            {
                if (!violation.AutoFixable)
                    continue;

                try
                {
                    // Use robust handle parsing
                    if (string.IsNullOrEmpty(violation.EntityHandle))
                        continue;

                    if (!TryGetObjectIdFromHandle(db, violation.EntityHandle, out ObjectId id))
                        continue;

                    var ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (ent == null)
                        continue;

                    switch (violation.Type)
                    {
                        case ViolationType.Linetype:
                            if (!string.IsNullOrWhiteSpace(standards.RequiredLinetype))
                            {
                                ent.Linetype = standards.RequiredLinetype;
                                fixedCount++;
                            }
                            break;

                        case ViolationType.Layer:
                            // Example: Auto-fix layer name if standards provide mapping
                            if (standards.LayerMap != null && 
                                standards.LayerMap.TryGetValue(violation.Actual, out string? mappedLayer))
                            {
                                ent.Layer = mappedLayer;
                                fixedCount++;
                            }
                            break;

                        case ViolationType.TextStyle:
                            if (ent is DBText dbText && !string.IsNullOrWhiteSpace(standards.RequiredTextStyle))
                            {
                                dbText.TextStyleId = GetTextStyleId(db, tr, standards.RequiredTextStyle);
                                fixedCount++;
                            }
                            else if (ent is MText mtext && !string.IsNullOrWhiteSpace(standards.RequiredTextStyle))
                            {
                                mtext.TextStyleId = GetTextStyleId(db, tr, standards.RequiredTextStyle);
                                fixedCount++;
                            }
                            break;

                        case ViolationType.DimStyle:
                            if (ent is Dimension dim && !string.IsNullOrWhiteSpace(standards.RequiredDimStyle))
                            {
                                dim.DimensionStyle = GetDimStyleId(db, tr, standards.RequiredDimStyle);
                                fixedCount++;
                            }
                            break;

                        // ===== NEW ADVANCED FIXES =====

                        case ViolationType.TextFont:
                            // Fix text/dim font by updating text style
                            if (ent is DBText || ent is MText)
                            {
                                if (ApplyTextFontFix(tr, db, ent, standards))
                                    fixedCount++;
                            }
                            else if (ent is Dimension dimForFont)
                            {
                                if (ApplyDimensionFontFix(tr, db, dimForFont, standards))
                                    fixedCount++;
                            }
                            break;

                        case ViolationType.TextSize:
                            // Fix text/dim size
                            if (ent is DBText dbTextSize)
                            {
                                double targetSize = standards.RequiredFontSize;
                                if (standards.MatchDrawingScale)
                                    targetSize *= GetDrawingScale(db);
                                dbTextSize.Height = targetSize;
                                fixedCount++;
                            }
                            else if (ent is MText mtextSize)
                            {
                                double targetSize = standards.RequiredFontSize;
                                if (standards.MatchDrawingScale)
                                    targetSize *= GetDrawingScale(db);
                                mtextSize.TextHeight = targetSize;
                                fixedCount++;
                            }
                            else if (ent is Dimension dimSize)
                            {
                                if (ApplyDimensionSizeFix(tr, db, dimSize, standards))
                                    fixedCount++;
                            }
                            break;

                        case ViolationType.Annotative:
                            // Fix annotative property for MText
                            if (ent is MText mtextAnnot)
                            {
                                mtextAnnot.Annotative = AnnotativeStates.True;

                                // Note: Full annotative scale implementation requires additional AutoCAD APIs
                                // For now, just set the annotative property

                                fixedCount++;
                            }
                            break;
                    }
                }
                catch
                {
                    // Skip violations that can't be fixed
                }
            }

            tr.Commit();
            return fixedCount;
        }

        private static ObjectId GetTextStyleId(Database db, Transaction tr, string styleName)
        {
            var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            if (tst.Has(styleName))
            {
                return tst[styleName];
            }
            return db.Textstyle; // fallback to current
        }

        /// <summary>
        /// Gets or creates a text style with the specified name.
        /// </summary>
        private static ObjectId GetOrCreateTextStyleId(Database db, Transaction tr, string styleName, string? fontFile = null)
        {
            var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);

            if (tst.Has(styleName))
                return tst[styleName];

            // Create the missing text style
            tst.UpgradeOpen();
            var newStyle = new TextStyleTableRecord
            {
                Name = styleName
            };

            if (!string.IsNullOrWhiteSpace(fontFile))
                newStyle.FileName = fontFile;

            ObjectId newId = tst.Add(newStyle);
            tr.AddNewlyCreatedDBObject(newStyle, true);

            return newId;
        }

        /// <summary>
        /// Applies text style fixes with option to create missing styles.
        /// </summary>
        public static int AutoFixTextStyle(ScanResult scan, StandardsModel standards)
        {
            var doc = Application.DocumentManager.MdiActiveDocument
                      ?? throw new InvalidOperationException("No active document.");
            var db = doc.Database;

            if (string.IsNullOrWhiteSpace(standards.RequiredTextStyle))
                return 0;

            int applied = 0;

            using var tr = db.TransactionManager.StartTransaction();

            // Get or create the required text style
            ObjectId requiredStyleId;
            if (standards.CreateMissingTextStyle)
            {
                string? fontFile = standards.ApprovedTextFontFiles?.Length > 0 
                    ? standards.ApprovedTextFontFiles[0] 
                    : null;
                requiredStyleId = GetOrCreateTextStyleId(db, tr, standards.RequiredTextStyle, fontFile);
            }
            else
            {
                var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                if (!tst.Has(standards.RequiredTextStyle))
                    throw new InvalidOperationException($"TextStyle '{standards.RequiredTextStyle}' not found. Enable CreateMissingTextStyle or add it manually.");
                requiredStyleId = tst[standards.RequiredTextStyle];
            }

            foreach (var v in scan.Violations)
            {
                if (v.Type != ViolationType.TextStyle) continue;
                if (!v.AutoFixable) continue;
                if (!v.EntityId.IsValid || v.EntityId.IsNull) continue;

                try
                {
                    if (tr.GetObject(v.EntityId, OpenMode.ForWrite, false) is not Entity ent)
                        continue;

                    if (ent is DBText t && t.TextStyleId != requiredStyleId)
                    {
                        t.TextStyleId = requiredStyleId;
                        applied++;
                    }
                    else if (ent is MText mt && mt.TextStyleId != requiredStyleId)
                    {
                        mt.TextStyleId = requiredStyleId;
                        applied++;
                    }
                }
                catch
                {
                    // Skip entities that can't be fixed
                }
            }

            tr.Commit();
            return applied;
        }

        private static ObjectId GetDimStyleId(Database db, Transaction tr, string styleName)
        {
            var dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
            if (dst.Has(styleName))
            {
                return dst[styleName];
            }
            return db.Dimstyle; // fallback to current
        }

        public static int ApplySingleFix(Violation v)
        {
            var doc = Application.DocumentManager.MdiActiveDocument
                      ?? throw new InvalidOperationException("No active document.");
            var db = doc.Database;

            using var tr = db.TransactionManager.StartTransaction();

            if (tr.GetObject(v.EntityId, OpenMode.ForWrite, false) is not Entity ent)
                return 0;

            switch (v.Type)
            {
                case ViolationType.Linetype:
                    ent.Linetype = v.Expected;
                    break;

                case ViolationType.Layer:
                    ent.Layer = v.Expected;
                    break;

                case ViolationType.TextStyle:
                    ApplyTextStyleFix(tr, db, ent, v.Expected);
                    break;

                case ViolationType.DimStyle:
                    ApplyDimStyleFix(tr, db, ent, v.Expected);
                    break;

                case ViolationType.TextSize:
                    if (ent is DBText dbText)
                        dbText.Height = double.Parse(v.Expected);
                    else if (ent is MText mtext)
                        mtext.TextHeight = double.Parse(v.Expected);
                    break;

                case ViolationType.Annotative:
                    if (ent is MText mtextAnnot)
                        mtextAnnot.Annotative = AnnotativeStates.True;
                    break;

                default:
                    return 0;
            }

            tr.Commit();
            return 1;
        }

        private static void ApplyTextStyleFix(Transaction tr, Database db, Entity ent, string styleName)
        {
            var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            if (!tst.Has(styleName))
                throw new InvalidOperationException($"TextStyle '{styleName}' not found in drawing.");

            ObjectId styleId = tst[styleName];

            if (ent is DBText t) t.TextStyleId = styleId;
            else if (ent is MText mt) mt.TextStyleId = styleId;
        }

        private static void ApplyDimStyleFix(Transaction tr, Database db, Entity ent, string dimStyleName)
        {
            if (ent is not Dimension dim) return;

            var dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
            if (!dst.Has(dimStyleName))
                throw new InvalidOperationException($"DimStyle '{dimStyleName}' not found in drawing.");

            dim.DimensionStyle = dst[dimStyleName];
        }

        /// <summary>
        /// Auto-fix text styles using template-derived standards.
        /// Imports required style from template if missing in drawing.
        /// </summary>
        public static FixResult AutoFixTextStylesFromTemplate(ScanResult scan, StandardsModel standards)
        {
            var result = new FixResult();

            var doc = Application.DocumentManager.MdiActiveDocument
                      ?? throw new InvalidOperationException("No active document.");
            var db = doc.Database;
            var ed = doc.Editor;

            // Lock the document for modification (required when called from modeless dialog)
            using var docLock = doc.LockDocument();

            // Determine target style: use RequiredTextStyle, or first approved style
            string targetStyle = standards.RequiredTextStyle ?? "";
            if (string.IsNullOrWhiteSpace(targetStyle) && standards.ApprovedTextStyles.Count > 0)
            {
                targetStyle = standards.ApprovedTextStyles.First();
            }

            ed.WriteMessage($"\n[DEBUG] Target text style: '{targetStyle}'");
            ed.WriteMessage($"\n[DEBUG] Template path: '{standards.TemplatePath}'");
            ed.WriteMessage($"\n[DEBUG] Total violations in scan: {scan.Violations.Count}");
            ed.WriteMessage($"\n[DEBUG] TextStyle violations: {scan.TextStyleViolations}");

            if (string.IsNullOrWhiteSpace(targetStyle))
            {
                result.ErrorMessage = "No text style available to fix to.";
                return result;
            }

            // Temporarily set RequiredTextStyle for the helper method
            standards.RequiredTextStyle = targetStyle;

            using var tr = db.TransactionManager.StartTransaction();

            // Get or create the target text style
            ObjectId requiredStyleId = GetOrImportTextStyle(db, tr, standards);

            ed.WriteMessage($"\n[DEBUG] Required style ID: {requiredStyleId}, IsNull: {requiredStyleId.IsNull}");

            if (requiredStyleId.IsNull)
            {
                result.ErrorMessage = $"Could not find or create text style '{targetStyle}'.";
                return result;
            }

            int processedCount = 0;
            foreach (var v in scan.Violations)
            {
                if (v.Type != ViolationType.TextStyle) continue;
                if (!v.AutoFixable) continue;
                processedCount++;

                try
                {
                    // Always use handle for lookup - ObjectId can become stale between transactions
                    if (string.IsNullOrEmpty(v.EntityHandle))
                    {
                        ed.WriteMessage($"\n[DEBUG] Violation {processedCount}: Empty handle");
                        result.FailedCount++;
                        continue;
                    }

                    // Handle can be decimal or hex - try to parse it robustly
                    ObjectId entityId;
                    if (!TryGetObjectIdFromHandle(db, v.EntityHandle, out entityId))
                    {
                        ed.WriteMessage($"\n[DEBUG] Violation {processedCount}: Handle '{v.EntityHandle}' could not be resolved");
                        result.FailedCount++;
                        continue;
                    }

                    var ent = tr.GetObject(entityId, OpenMode.ForWrite, false) as Entity;
                    if (ent == null)
                    {
                        ed.WriteMessage($"\n[DEBUG] Violation {processedCount}: Entity null for handle '{v.EntityHandle}'");
                        result.FailedCount++;
                        continue;
                    }

                    // Debug: Show what entity type we're processing
                    ed.WriteMessage($"\n[DEBUG] Processing: {v.RuleId} - {ent.GetType().Name} (Handle: {v.EntityHandle})");

                    bool fixed_ = false;
                    if (ent is DBText t && t.TextStyleId != requiredStyleId)
                    {
                        t.TextStyleId = requiredStyleId;
                        fixed_ = true;
                    }
                    else if (ent is MText mt && mt.TextStyleId != requiredStyleId)
                    {
                        mt.TextStyleId = requiredStyleId;
                        fixed_ = true;
                    }
                    else if (ent is AttributeDefinition attDef && attDef.TextStyleId != requiredStyleId)
                    {
                        attDef.TextStyleId = requiredStyleId;
                        fixed_ = true;
                    }
                    else if (ent is AttributeReference attRef && attRef.TextStyleId != requiredStyleId)
                    {
                        attRef.TextStyleId = requiredStyleId;
                        fixed_ = true;
                    }
                    else if (ent is MLeader mleader)
                    {
                        bool changed = false;

                        // 1) Fix the shared MLeaderStyle definition
                        if (!mleader.MLeaderStyle.IsNull)
                        {
                            var mleaderStyle = (MLeaderStyle)tr.GetObject(mleader.MLeaderStyle, OpenMode.ForWrite);
                            if (mleaderStyle.TextStyleId != requiredStyleId)
                            {
                                ed.WriteMessage($"\n[DEBUG] MLeader {v.EntityHandle}: Updating MLeaderStyle '{mleaderStyle.Name}' TextStyleId");
                                mleaderStyle.TextStyleId = requiredStyleId;
                                changed = true;
                            }
                            else
                            {
                                ed.WriteMessage($"\n[DEBUG] MLeader {v.EntityHandle}: MLeaderStyle already correct");
                            }
                        }
                        else
                        {
                            ed.WriteMessage($"\n[DEBUG] MLeader {v.EntityHandle}: No MLeaderStyle reference");
                        }

                        // 2) Fix per-instance text content (MTextContent)
                        if (mleader.ContentType == ContentType.MTextContent)
                        {
                            try
                            {
                                var leaderMText = mleader.MText;
                                if (leaderMText != null && leaderMText.TextStyleId != requiredStyleId)
                                {
                                    leaderMText.TextStyleId = requiredStyleId;
                                    mleader.MText = leaderMText; // re-assign to persist
                                    ed.WriteMessage($"\n[DEBUG] MLeader {v.EntityHandle}: Updated MLeader MText content TextStyleId");
                                    changed = true;
                                }
                                else
                                {
                                    ed.WriteMessage($"\n[DEBUG] MLeader {v.EntityHandle}: MText content already correct");
                                }
                            }
                            catch (Exception ex)
                            {
                                ed.WriteMessage($"\n[DEBUG] MLeader {v.EntityHandle}: MText content update failed - {ex.Message}");
                                result.FailedCount++;
                            }
                        }

                        if (changed) fixed_ = true;
                    }
                    else if (ent is Leader leader)
                    {
                        // Legacy Leader - update the annotation text if it's MText
                        if (!leader.Annotation.IsNull)
                        {
                            var annoEnt = tr.GetObject(leader.Annotation, OpenMode.ForWrite, false) as MText;
                            if (annoEnt != null && annoEnt.TextStyleId != requiredStyleId)
                            {
                                annoEnt.TextStyleId = requiredStyleId;
                                fixed_ = true;
                            }
                        }
                    }

                    if (fixed_)
                    {
                        result.FixedCount++;
                    }
                    else if (ent is not DBText && ent is not MText && ent is not AttributeDefinition && ent is not AttributeReference && ent is not MLeader && ent is not Leader)
                    {
                        // Entity type not supported for text style fix
                        ed.WriteMessage($"\n[DEBUG] Violation {processedCount}: Unsupported entity type '{ent.GetType().Name}'");
                        result.FailedCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n[DEBUG] Violation {processedCount}: Exception - {ex.Message}");
                    result.FailedCount++;
                }
            }

            ed.WriteMessage($"\n[DEBUG] Processed {processedCount} text style violations");
            tr.Commit();
            return result;
        }

        /// <summary>
        /// Tries to get ObjectId from a handle string (supports both decimal and hex formats).
        /// </summary>
        public static bool TryGetObjectIdFromHandle(Database db, string handleStr, out ObjectId objectId)
        {
            objectId = ObjectId.Null;

            try
            {
                // AutoCAD Handle.ToString() returns hex without prefix
                // Try hex first (most common from Handle.ToString())
                if (long.TryParse(handleStr, System.Globalization.NumberStyles.HexNumber, null, out long hexValue))
                {
                    var h = new Handle(hexValue);
                    return db.TryGetObjectId(h, out objectId);
                }

                // Try decimal as fallback
                if (long.TryParse(handleStr, out long decValue))
                {
                    var h = new Handle(decValue);
                    return db.TryGetObjectId(h, out objectId);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Auto-fix dimension styles using template-derived standards.
        /// </summary>
        public static FixResult AutoFixDimStylesFromTemplate(ScanResult scan, StandardsModel standards)
        {
            var result = new FixResult();

            var doc = Application.DocumentManager.MdiActiveDocument
                      ?? throw new InvalidOperationException("No active document.");
            var db = doc.Database;

            // Lock the document for modification (required when called from modeless dialog)
            using var docLock = doc.LockDocument();

            // Determine target style: use RequiredDimStyle, or first approved style
            string targetStyle = standards.RequiredDimStyle ?? "";
            if (string.IsNullOrWhiteSpace(targetStyle) && standards.ApprovedDimStyles.Count > 0)
            {
                targetStyle = standards.ApprovedDimStyles.First();
            }

            if (string.IsNullOrWhiteSpace(targetStyle))
            {
                result.ErrorMessage = "No dim style available to fix to.";
                return result;
            }

            using var tr = db.TransactionManager.StartTransaction();

            // Get or import the target dim style
            ObjectId requiredStyleId = GetOrImportDimStyle(db, tr, targetStyle, standards.TemplatePath);

            if (requiredStyleId.IsNull)
            {
                result.ErrorMessage = $"DimStyle '{targetStyle}' not found and could not be imported from template.";
                return result;
            }

            foreach (var v in scan.Violations)
            {
                if (v.Type != ViolationType.DimStyle) continue;
                if (!v.AutoFixable) continue;

                try
                {
                    // Always use handle for lookup - ObjectId can become stale between transactions
                    if (string.IsNullOrEmpty(v.EntityHandle))
                    {
                        result.FailedCount++;
                        continue;
                    }

                    // Use robust handle parsing
                    if (!TryGetObjectIdFromHandle(db, v.EntityHandle, out ObjectId entityId))
                    {
                        result.FailedCount++;
                        continue;
                    }

                    if (tr.GetObject(entityId, OpenMode.ForWrite, false) is not Dimension dim)
                    {
                        result.FailedCount++;
                        continue;
                    }

                    if (dim.DimensionStyle != requiredStyleId)
                    {
                        dim.DimensionStyle = requiredStyleId;
                        result.FixedCount++;
                    }
                }
                catch
                {
                    result.FailedCount++;
                }
            }

            tr.Commit();
            return result;
        }

        /// <summary>
        /// Gets an existing dim style or imports it from the template.
        /// </summary>
        private static ObjectId GetOrImportDimStyle(Database db, Transaction tr, string styleName, string templatePath)
        {
            var dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);

            // Check if style already exists
            if (dst.Has(styleName))
                return dst[styleName];

            // Try to import from template
            if (!string.IsNullOrEmpty(templatePath) && System.IO.File.Exists(templatePath))
            {
                try
                {
                    using var sourceDb = new Database(false, true);
                    sourceDb.ReadDwgFile(templatePath, FileOpenMode.OpenForReadAndAllShare, true, "");

                    ObjectIdCollection sourceIds;

                    using var sourceTr = sourceDb.TransactionManager.StartTransaction();
                    var sourceDst = (DimStyleTable)sourceTr.GetObject(sourceDb.DimStyleTableId, OpenMode.ForRead);
                    if (!sourceDst.Has(styleName))
                    {
                        sourceTr.Commit();
                        return ObjectId.Null; // Style not in template either
                    }

                    sourceIds = new ObjectIdCollection { sourceDst[styleName] };
                    sourceTr.Commit();

                    // Clone the dim style into the current database
                    var mapping = new IdMapping();
                    sourceDb.WblockCloneObjects(sourceIds, db.DimStyleTableId, mapping, DuplicateRecordCloning.Replace, false);

                    // Refresh the dim style table reference
                    tr.TransactionManager.QueueForGraphicsFlush();

                    // Re-check if the style now exists
                    dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
                    if (dst.Has(styleName))
                        return dst[styleName];
                }
                catch
                {
                    // Import failed, return null
                }
            }

            return ObjectId.Null;
        }

        private static ObjectId GetOrImportTextStyle(Database db, Transaction tr, StandardsModel standards)
        {
            var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);

            // Check if style already exists
            if (tst.Has(standards.RequiredTextStyle))
                return tst[standards.RequiredTextStyle];

            // Try to import from template first
            if (!string.IsNullOrEmpty(standards.TemplatePath) && 
                System.IO.File.Exists(standards.TemplatePath) &&
                !string.IsNullOrEmpty(standards.RequiredTextStyle))
            {
                try
                {
                    using var sourceDb = new Database(false, true);
                    sourceDb.ReadDwgFile(standards.TemplatePath, FileOpenMode.OpenForReadAndAllShare, true, "");

                    using var sourceTr = sourceDb.TransactionManager.StartTransaction();
                    var sourceTst = (TextStyleTable)sourceTr.GetObject(sourceDb.TextStyleTableId, OpenMode.ForRead);

                    if (sourceTst.Has(standards.RequiredTextStyle))
                    {
                        var sourceIds = new ObjectIdCollection { sourceTst[standards.RequiredTextStyle] };
                        sourceTr.Commit();

                        // Clone the text style into the current database
                        var mapping = new IdMapping();
                        sourceDb.WblockCloneObjects(sourceIds, db.TextStyleTableId, mapping, DuplicateRecordCloning.Replace, false);

                        // Refresh and re-check
                        tr.TransactionManager.QueueForGraphicsFlush();
                        tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                        if (tst.Has(standards.RequiredTextStyle))
                            return tst[standards.RequiredTextStyle];
                    }
                    else
                    {
                        sourceTr.Commit();
                        // Style not in template, fall through to create
                    }
                }
                catch
                {
                    // Import failed, try to create instead
                }
            }

            // Fallback: Try to create it with font info from standards
            if (standards.CreateMissingTextStyle && !string.IsNullOrEmpty(standards.RequiredTextStyle))
            {
                string? fontFile = null;
                if (standards.TextStyleFonts.TryGetValue(standards.RequiredTextStyle, out var font))
                    fontFile = font;
                else if (standards.ApprovedTextFontFiles?.Length > 0)
                    fontFile = standards.ApprovedTextFontFiles[0];

                return GetOrCreateTextStyleId(db, tr, standards.RequiredTextStyle, fontFile);
            }

            return ObjectId.Null;
        }

        /// <summary>
        /// Auto-fix layer violations by moving entities to the suggested approved layer.
        /// Creates missing layers from template if needed.
        /// </summary>
        public static FixResult AutoFixLayersFromTemplate(ScanResult scan, StandardsModel standards)
        {
            var result = new FixResult();

            var doc = Application.DocumentManager.MdiActiveDocument
                      ?? throw new InvalidOperationException("No active document.");
            var db = doc.Database;
            var ed = doc.Editor;

            // Lock the document for modification (required when called from modeless dialog)
            using var docLock = doc.LockDocument();

            if (standards.ApprovedLayers.Count == 0)
            {
                result.ErrorMessage = "No approved layers defined in template.";
                return result;
            }

            using var tr = db.TransactionManager.StartTransaction();
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            foreach (var v in scan.Violations)
            {
                if (v.Type != ViolationType.Layer) continue;
                if (!v.AutoFixable) continue;

                try
                {
                    // The Expected field contains the suggested layer name
                    string targetLayer = v.Expected;

                    if (string.IsNullOrWhiteSpace(targetLayer) || targetLayer.StartsWith("("))
                    {
                        result.FailedCount++;
                        continue;
                    }

                    // Ensure target layer exists in drawing (create if needed)
                    if (!lt.Has(targetLayer))
                    {
                        // Try to import from template
                        if (!ImportLayerFromTemplate(db, targetLayer, standards.TemplatePath))
                        {
                            // Create a basic layer if import fails
                            lt.UpgradeOpen();
                            var newLayer = new LayerTableRecord { Name = targetLayer };
                            lt.Add(newLayer);
                            tr.AddNewlyCreatedDBObject(newLayer, true);
                            lt.DowngradeOpen();
                        }
                    }

                    // Get entity and change its layer
                    if (string.IsNullOrEmpty(v.EntityHandle))
                    {
                        result.FailedCount++;
                        continue;
                    }

                    if (!TryGetObjectIdFromHandle(db, v.EntityHandle, out ObjectId entityId))
                    {
                        result.FailedCount++;
                        continue;
                    }

                    if (tr.GetObject(entityId, OpenMode.ForWrite, false) is not Entity ent)
                    {
                        result.FailedCount++;
                        continue;
                    }

                    if (!string.Equals(ent.Layer, targetLayer, StringComparison.OrdinalIgnoreCase))
                    {
                        ent.Layer = targetLayer;
                        result.FixedCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n[DEBUG] Layer fix error: {ex.Message}");
                    result.FailedCount++;
                }
            }

            tr.Commit();
            return result;
        }

        /// <summary>
        /// Auto-fix linetype violations by setting entities to "ByLayer" or other specified linetype.
        /// </summary>
        public static FixResult AutoFixLinetypesToByLayer(ScanResult scan, StandardsModel standards)
        {
            var result = new FixResult();

            var doc = Application.DocumentManager.MdiActiveDocument
                      ?? throw new InvalidOperationException("No active document.");
            var db = doc.Database;

            // Required for modeless UI fixes
            using var docLock = doc.LockDocument();

            using var tr = db.TransactionManager.StartTransaction();

            foreach (var v in scan.Violations)
            {
                if (v.Type != ViolationType.Linetype) continue;
                if (!v.AutoFixable) continue;

                try
                {
                    if (string.IsNullOrEmpty(v.EntityHandle))
                    {
                        result.FailedCount++;
                        continue;
                    }

                    if (!TryGetObjectIdFromHandle(db, v.EntityHandle, out ObjectId entityId))
                    {
                        result.FailedCount++;
                        continue;
                    }

                    if (tr.GetObject(entityId, OpenMode.ForWrite, false) is not Entity ent)
                    {
                        result.FailedCount++;
                        continue;
                    }

                    // v.Expected contains the target linetype (usually "ByLayer")
                    string target = string.IsNullOrWhiteSpace(v.Expected) ? "ByLayer" : v.Expected;

                    if (!string.Equals(ent.Linetype, target, StringComparison.OrdinalIgnoreCase))
                    {
                        ent.Linetype = target;
                        result.FixedCount++;
                    }
                }
                catch
                {
                    result.FailedCount++;
                }
            }

            tr.Commit();
            return result;
        }

        /// <summary>
        /// Imports a layer from the template file into the current database.
        /// </summary>
        private static bool ImportLayerFromTemplate(Database db, string layerName, string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath) || !System.IO.File.Exists(templatePath))
                return false;

            try
            {
                using var sourceDb = new Database(false, true);
                sourceDb.ReadDwgFile(templatePath, FileOpenMode.OpenForReadAndAllShare, true, "");

                using var sourceTr = sourceDb.TransactionManager.StartTransaction();
                var sourceLt = (LayerTable)sourceTr.GetObject(sourceDb.LayerTableId, OpenMode.ForRead);

                if (!sourceLt.Has(layerName))
                {
                    sourceTr.Commit();
                    return false;
                }

                var sourceIds = new ObjectIdCollection { sourceLt[layerName] };
                sourceTr.Commit();

                // Clone the layer into the current database
                var mapping = new IdMapping();
                sourceDb.WblockCloneObjects(sourceIds, db.LayerTableId, mapping, DuplicateRecordCloning.Replace, false);

                return true;
            }
            catch
            {
                return false;
            }
        }

    // ===== ADVANCED FIX HELPER METHODS =====

    /// <summary>
    /// Gets the current drawing scale from CANNOSCALE system variable.
    /// Returns 1.0 if unavailable.
    /// </summary>
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

    /// <summary>
    /// Applies font fix to text entities by updating/creating the text style.
    /// </summary>
    private static bool ApplyTextFontFix(Transaction tr, Database db, Entity ent, StandardsModel standards)
    {
        try
        {
            string? requiredFont = standards.RequiredFont;
            if (string.IsNullOrWhiteSpace(requiredFont)) return false;

            ObjectId textStyleId = ObjectId.Null;

            if (ent is DBText dbText)
                textStyleId = dbText.TextStyleId;
            else if (ent is MText mtext)
                textStyleId = mtext.TextStyleId;
            else
                return false;

            if (textStyleId.IsNull || !textStyleId.IsValid) return false;

            // Update the text style's font
            var ts = (TextStyleTableRecord)tr.GetObject(textStyleId, OpenMode.ForWrite);

            // For SHX fonts
            if (requiredFont.EndsWith(".shx", StringComparison.OrdinalIgnoreCase))
            {
                ts.FileName = requiredFont;
            }
            // For TrueType fonts
            else
            {
                ts.Font = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor(
                    requiredFont, false, false, 0, 0);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Applies font fix to dimension by updating the dimension style's text style.
    /// </summary>
    private static bool ApplyDimensionFontFix(Transaction tr, Database db, Dimension dim, StandardsModel standards)
    {
        try
        {
            string? requiredFont = standards.RequiredDimFont ?? standards.RequiredFont;
            if (string.IsNullOrWhiteSpace(requiredFont)) return false;

            if (dim.DimensionStyle.IsNull || !dim.DimensionStyle.IsValid) return false;

            var ds = (DimStyleTableRecord)tr.GetObject(dim.DimensionStyle, OpenMode.ForWrite);
            if (ds.Dimtxsty.IsNull || !ds.Dimtxsty.IsValid) return false;

            // Update the dimension's text style font
            var ts = (TextStyleTableRecord)tr.GetObject(ds.Dimtxsty, OpenMode.ForWrite);

            if (requiredFont.EndsWith(".shx", StringComparison.OrdinalIgnoreCase))
            {
                ts.FileName = requiredFont;
            }
            else
            {
                ts.Font = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor(
                    requiredFont, false, false, 0, 0);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Applies text size fix to dimension by updating the dimension style's text height.
    /// </summary>
    private static bool ApplyDimensionSizeFix(Transaction tr, Database db, Dimension dim, StandardsModel standards)
    {
        try
        {
            double requiredSize = standards.RequiredDimFontSize > 0 
                ? standards.RequiredDimFontSize 
                : standards.RequiredFontSize;

            if (requiredSize <= 0) return false;

            if (dim.DimensionStyle.IsNull || !dim.DimensionStyle.IsValid) return false;

            var ds = (DimStyleTableRecord)tr.GetObject(dim.DimensionStyle, OpenMode.ForWrite);

            if (standards.MatchDrawingScale)
                requiredSize *= GetDrawingScale(db);

            ds.Dimtxt = requiredSize;
            dim.RecomputeDimensionBlock(true);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Result of an auto-fix operation.
    /// </summary>
    public class FixResult
    {
        public int FixedCount { get; set; }
        public int FailedCount { get; set; }
        public string? ErrorMessage { get; set; }
        public bool Success => string.IsNullOrEmpty(ErrorMessage);
    }
    }
}
