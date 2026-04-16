using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ACSE.AutoCAD2026.Standards
{
    /// <summary>
    /// Extracts standards from a DWT template file.
    /// This enables template-driven compliance enforcement.
    /// </summary>
    public static class TemplateStandardsExtractor
    {
        /// <summary>
        /// Loads standards from a DWT template file.
        /// </summary>
        /// <param name="templatePath">Full path to the .dwt file</param>
        /// <returns>StandardsModel populated from template</returns>
        public static StandardsModel LoadFromTemplate(string templatePath)
        {
            if (string.IsNullOrWhiteSpace(templatePath))
                throw new ArgumentException("Template path cannot be empty.", nameof(templatePath));

            if (!System.IO.File.Exists(templatePath))
                throw new System.IO.FileNotFoundException($"Template not found: {templatePath}");

            var model = new StandardsModel
            {
                TemplatePath = templatePath
            };

            using var templateDb = new Database(false, true);
            templateDb.ReadDwgFile(templatePath, System.IO.FileShare.Read, true, "");

            using var tr = templateDb.TransactionManager.StartTransaction();

            ExtractTextStyles(tr, templateDb, model);
            ExtractDimStyles(tr, templateDb, model);
            ExtractLayers(tr, templateDb, model);
            ExtractLinetypes(tr, templateDb, model);

            tr.Commit();
            return model;
        }

        private static void ExtractTextStyles(Transaction tr, Database db, StandardsModel model)
        {
            var textTable = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);

            foreach (ObjectId id in textTable)
            {
                var ts = (TextStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);

                // Skip anonymous styles (start with *)
                if (ts.Name.StartsWith("*"))
                    continue;

                model.ApprovedTextStyles.Add(ts.Name);
                model.TextStyleFonts[ts.Name] = ts.FileName ?? "";

                // Set first non-anonymous style as required (if not already set)
                if (string.IsNullOrEmpty(model.RequiredTextStyle))
                {
                    model.RequiredTextStyle = ts.Name;
                }
            }
        }

        private static void ExtractDimStyles(Transaction tr, Database db, StandardsModel model)
        {
            var dimTable = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);

            foreach (ObjectId id in dimTable)
            {
                var ds = (DimStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);

                model.ApprovedDimStyles.Add(ds.Name);

                // Set first style as required (if not already set)
                if (string.IsNullOrEmpty(model.RequiredDimStyle))
                {
                    model.RequiredDimStyle = ds.Name;
                }
            }
        }

        private static void ExtractLayers(Transaction tr, Database db, StandardsModel model)
        {
            var layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            foreach (ObjectId id in layerTable)
            {
                var layer = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                model.ApprovedLayers.Add(layer.Name);
            }
        }

        private static void ExtractLinetypes(Transaction tr, Database db, StandardsModel model)
        {
            var ltTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);

            foreach (ObjectId id in ltTable)
            {
                var lt = (LinetypeTableRecord)tr.GetObject(id, OpenMode.ForRead);
                model.ApprovedLinetypes.Add(lt.Name);
            }
        }
    }
}
