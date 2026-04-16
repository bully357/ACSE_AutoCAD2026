using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ACSE.AutoCAD2026.Compliance
{
    /// <summary>
    /// Global text/mtext property modifier for bulk updates.
    /// Allows filtering and modifying text properties across entire drawing.
    /// </summary>
    public static class GlobalTextModifier
    {
        /// <summary>
        /// Scans drawing for all text/mtext entities with optional filtering.
        /// </summary>
        public static List<TextEntityInfo> ScanTextEntities(TextFilterCriteria? filter = null)
        {
            var results = new List<TextEntityInfo>();
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return results;

            var db = doc.Database;

            try
            {
                // NOTE: NO document lock here - EntityScanner.Scan works without it when called from UI
                // The modeless window allows AutoCAD to manage document context
                using var tr = db.TransactionManager.StartTransaction();

                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId id in modelSpace)
                {
                    try
                    {
                        var obj = tr.GetObject(id, OpenMode.ForRead);

                        if (obj is DBText dbText)
                        {
                            try
                            {
                                var info = CreateTextInfo(dbText, tr, db);
                                if (filter == null || MatchesFilter(info, filter))
                                {
                                    results.Add(info);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                // Skip this entity if it causes errors
                                System.Diagnostics.Debug.WriteLine($"Error processing DBText {id.Handle}: {ex.Message}");
                            }
                        }
                        else if (obj is MText mtext)
                        {
                            try
                            {
                                var info = CreateMTextInfo(mtext, tr, db);
                                if (filter == null || MatchesFilter(info, filter))
                                {
                                    results.Add(info);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                // Skip this entity if it causes errors
                                System.Diagnostics.Debug.WriteLine($"Error processing MText {id.Handle}: {ex.Message}");
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        // Skip this entity if we can't even open it
                        System.Diagnostics.Debug.WriteLine($"Error opening entity {id.Handle}: {ex.Message}");
                    }
                }

                tr.Commit();
            }
            catch (System.Exception ex)
            {
                // Log the error but don't crash
                System.Diagnostics.Debug.WriteLine($"ScanTextEntities failed: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Applies modifications to selected text entities.
        /// </summary>
        public static ModificationResult ApplyModifications(
            List<ObjectId> entityIds,
            TextModificationProperties modifications,
            bool previewMode = false)
        {
            var result = new ModificationResult();
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                result.ErrorMessage = "No active document.";
                return result;
            }

            var db = doc.Database;
            using var docLock = doc.LockDocument();
            using var tr = db.TransactionManager.StartTransaction();

            foreach (var id in entityIds)
            {
                try
                {
                    var obj = tr.GetObject(id, OpenMode.ForWrite);

                    if (obj is DBText dbText)
                    {
                        if (!previewMode)
                        {
                            ApplyToDBText(dbText, modifications, tr, db);
                        }
                        result.ModifiedCount++;
                    }
                    else if (obj is MText mtext)
                    {
                        if (!previewMode)
                        {
                            ApplyToMText(mtext, modifications, tr, db);
                        }
                        result.ModifiedCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add($"Entity {id.Handle}: {ex.Message}");
                }
            }

            if (!previewMode)
            {
                tr.Commit();
            }

            return result;
        }

        private static TextEntityInfo CreateTextInfo(DBText dbText, Transaction tr, Database db)
        {
            var info = new TextEntityInfo
            {
                EntityId = dbText.ObjectId,
                Handle = dbText.Handle.ToString(),
                EntityType = "DBText",
                TextString = dbText.TextString ?? "",
                Layer = dbText.Layer ?? "0",
                Height = dbText.Height,
                Rotation = dbText.Rotation * (180.0 / Math.PI), // Convert to degrees
                WidthFactor = dbText.WidthFactor,
                Position = dbText.Position,
                HorizontalMode = dbText.HorizontalMode,
                VerticalMode = dbText.VerticalMode,
                IsMirrored = dbText.IsMirroredInX || dbText.IsMirroredInY,
                TextStyle = "Standard", // Default
                FontFile = "Unknown" // Default
            };

            // Get text style name (with error handling)
            try
            {
                if (!dbText.TextStyleId.IsNull)
                {
                    var ts = (TextStyleTableRecord)tr.GetObject(dbText.TextStyleId, OpenMode.ForRead);
                    info.TextStyle = ts.Name ?? "Standard";

                    // Get font info
                    try
                    {
                        if (!string.IsNullOrEmpty(ts.FileName))
                        {
                            info.FontFile = ts.FileName;
                        }
                        else if (ts.Font != null)
                        {
                            var font = ts.Font;
                            info.FontFile = font.TypeFace ?? "Unknown";
                        }
                    }
                    catch
                    {
                        // Keep default "Unknown"
                    }
                }
            }
            catch
            {
                // Keep defaults if text style access fails
            }

            // Check if annotative (DBText doesn't have direct annotative property)
            info.IsAnnotative = false;

            return info;
        }

        private static TextEntityInfo CreateMTextInfo(MText mtext, Transaction tr, Database db)
        {
            var info = new TextEntityInfo
            {
                EntityId = mtext.ObjectId,
                Handle = mtext.Handle.ToString(),
                EntityType = "MText",
                TextString = mtext.Contents ?? "",
                Layer = mtext.Layer ?? "0",
                Height = mtext.TextHeight,
                Rotation = mtext.Rotation * (180.0 / Math.PI),
                WidthFactor = 1.0, // MText doesn't have width factor
                Position = mtext.Location,
                Attachment = mtext.Attachment,
                IsAnnotative = mtext.Annotative == AnnotativeStates.True,
                TextStyle = "Standard", // Default
                FontFile = "Unknown" // Default
            };

            // Get text style name (with error handling)
            try
            {
                if (!mtext.TextStyleId.IsNull)
                {
                    var ts = (TextStyleTableRecord)tr.GetObject(mtext.TextStyleId, OpenMode.ForRead);
                    info.TextStyle = ts.Name ?? "Standard";

                    // Get font info
                    try
                    {
                        if (!string.IsNullOrEmpty(ts.FileName))
                        {
                            info.FontFile = ts.FileName;
                        }
                        else if (ts.Font != null)
                        {
                            var font = ts.Font;
                            info.FontFile = font.TypeFace ?? "Unknown";
                        }
                    }
                    catch
                    {
                        // Keep default "Unknown"
                    }
                }
            }
            catch
            {
                // Keep defaults if text style access fails
            }

            return info;
        }

        private static bool MatchesFilter(TextEntityInfo info, TextFilterCriteria filter)
        {
            if (!string.IsNullOrEmpty(filter.TextStyle) && 
                !info.TextStyle.Equals(filter.TextStyle, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrEmpty(filter.Layer) && 
                !info.Layer.Equals(filter.Layer, StringComparison.OrdinalIgnoreCase))
                return false;

            if (filter.MinHeight.HasValue && info.Height < filter.MinHeight.Value)
                return false;

            if (filter.MaxHeight.HasValue && info.Height > filter.MaxHeight.Value)
                return false;

            if (filter.IsAnnotative.HasValue && info.IsAnnotative != filter.IsAnnotative.Value)
                return false;

            if (!string.IsNullOrEmpty(filter.FontFile) &&
                !info.FontFile.Contains(filter.FontFile, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private static void ApplyToDBText(DBText dbText, TextModificationProperties mods, Transaction tr, Database db)
        {
            if (!string.IsNullOrEmpty(mods.NewTextStyle))
            {
                var styleId = GetTextStyleId(db, tr, mods.NewTextStyle);
                if (!styleId.IsNull)
                    dbText.TextStyleId = styleId;
            }

            if (mods.NewHeight.HasValue)
            {
                dbText.Height = mods.NewHeight.Value;
            }

            if (!string.IsNullOrEmpty(mods.NewLayer))
            {
                dbText.Layer = mods.NewLayer;
            }

            if (mods.NewRotation.HasValue)
            {
                dbText.Rotation = mods.NewRotation.Value * (Math.PI / 180.0); // Convert to radians
            }

            if (mods.NewWidthFactor.HasValue)
            {
                dbText.WidthFactor = mods.NewWidthFactor.Value;
            }

            if (mods.NewHorizontalMode.HasValue)
            {
                dbText.HorizontalMode = mods.NewHorizontalMode.Value;
            }

            if (mods.NewVerticalMode.HasValue)
            {
                dbText.VerticalMode = mods.NewVerticalMode.Value;
            }
        }

        private static void ApplyToMText(MText mtext, TextModificationProperties mods, Transaction tr, Database db)
        {
            if (!string.IsNullOrEmpty(mods.NewTextStyle))
            {
                var styleId = GetTextStyleId(db, tr, mods.NewTextStyle);
                if (!styleId.IsNull)
                    mtext.TextStyleId = styleId;
            }

            if (mods.NewHeight.HasValue)
            {
                mtext.TextHeight = mods.NewHeight.Value;
            }

            if (!string.IsNullOrEmpty(mods.NewLayer))
            {
                mtext.Layer = mods.NewLayer;
            }

            if (mods.NewRotation.HasValue)
            {
                mtext.Rotation = mods.NewRotation.Value * (Math.PI / 180.0);
            }

            if (mods.NewAnnotative.HasValue)
            {
                mtext.Annotative = mods.NewAnnotative.Value ? AnnotativeStates.True : AnnotativeStates.False;
            }

            if (mods.NewAttachment.HasValue)
            {
                mtext.Attachment = mods.NewAttachment.Value;
            }
        }

        private static ObjectId GetTextStyleId(Database db, Transaction tr, string styleName)
        {
            var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            if (tst.Has(styleName))
                return tst[styleName];
            return ObjectId.Null;
        }

        /// <summary>
        /// Gets all available text styles in the database.
        /// </summary>
        public static List<string> GetAvailableTextStyles(Database db)
        {
            var styles = new List<string>();
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return styles;

            // NO document lock - called from UI thread in modeless window
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
        /// Gets all available layers in the database.
        /// </summary>
        public static List<string> GetAvailableLayers(Database db)
        {
            var layers = new List<string>();
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return layers;

            // NO document lock - called from UI thread in modeless window
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
    }

    /// <summary>
    /// Information about a text entity.
    /// </summary>
    public class TextEntityInfo
    {
        public ObjectId EntityId { get; set; }
        public string Handle { get; set; } = "";
        public string EntityType { get; set; } = ""; // "DBText" or "MText"
        public string TextString { get; set; } = "";
        public string Layer { get; set; } = "";
        public string TextStyle { get; set; } = "";
        public string FontFile { get; set; } = "";
        public double Height { get; set; }
        public double Rotation { get; set; } // In degrees
        public double WidthFactor { get; set; }
        public bool IsAnnotative { get; set; }
        public Point3d Position { get; set; }
        
        // DBText specific
        public TextHorizontalMode HorizontalMode { get; set; }
        public TextVerticalMode VerticalMode { get; set; }
        public bool IsMirrored { get; set; }
        
        // MText specific
        public AttachmentPoint Attachment { get; set; }
    }

    /// <summary>
    /// Filter criteria for scanning text entities.
    /// </summary>
    public class TextFilterCriteria
    {
        public string? TextStyle { get; set; }
        public string? Layer { get; set; }
        public string? FontFile { get; set; }
        public double? MinHeight { get; set; }
        public double? MaxHeight { get; set; }
        public bool? IsAnnotative { get; set; }
    }

    /// <summary>
    /// Properties to modify on text entities.
    /// </summary>
    public class TextModificationProperties
    {
        public string? NewTextStyle { get; set; }
        public double? NewHeight { get; set; }
        public string? NewLayer { get; set; }
        public double? NewRotation { get; set; } // In degrees
        public double? NewWidthFactor { get; set; }
        public bool? NewAnnotative { get; set; }
        
        // DBText specific
        public TextHorizontalMode? NewHorizontalMode { get; set; }
        public TextVerticalMode? NewVerticalMode { get; set; }
        
        // MText specific
        public AttachmentPoint? NewAttachment { get; set; }
    }

    /// <summary>
    /// Result of a modification operation.
    /// </summary>
    public class ModificationResult
    {
        public int ModifiedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string? ErrorMessage { get; set; }
        public bool Success => string.IsNullOrEmpty(ErrorMessage) && FailedCount == 0;
    }
}
