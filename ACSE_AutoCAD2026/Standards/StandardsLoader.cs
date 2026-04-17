using System;
using System.IO;
using System.Text.Json;

namespace ACSE.AutoCAD2026.Standards
{
    public static class StandardsLoader
    {
        // Lazy-initialize to avoid crash during command discovery
        private static JsonSerializerOptions s_jsonOptions;
        private static JsonSerializerOptions JsonOptions
        {
            get
            {
                if (s_jsonOptions == null)
                {
                    s_jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                }
                return s_jsonOptions;
            }
        }

        public static StandardsModel Load(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Standards file not found.", filePath);

            // Read with proper encoding to handle BOM
            string json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);

            var model = JsonSerializer.Deserialize<StandardsModel>(json, JsonOptions)
                ?? throw new InvalidDataException($"Failed to deserialize standards from file: {filePath}");

            NormalizeCollections(model);
            return model;
        }

        /// <summary>
        /// System.Text.Json ignores the custom StringComparer set in the property
        /// initializers and rebuilds each HashSet / Dictionary with the default
        /// ordinal (case-sensitive) comparer. Rebuild them here so layer/style/font
        /// lookups remain case-insensitive as AutoCAD requires.
        /// </summary>
        private static void NormalizeCollections(StandardsModel model)
        {
            var ci = System.StringComparer.OrdinalIgnoreCase;

            if (model.ApprovedTextStyles != null)
                model.ApprovedTextStyles = new System.Collections.Generic.HashSet<string>(model.ApprovedTextStyles, ci);
            if (model.ApprovedDimStyles != null)
                model.ApprovedDimStyles = new System.Collections.Generic.HashSet<string>(model.ApprovedDimStyles, ci);
            if (model.ApprovedLayers != null)
                model.ApprovedLayers = new System.Collections.Generic.HashSet<string>(model.ApprovedLayers, ci);
            if (model.ApprovedLinetypes != null)
                model.ApprovedLinetypes = new System.Collections.Generic.HashSet<string>(model.ApprovedLinetypes, ci);
            if (model.TextStyleFonts != null)
                model.TextStyleFonts = new System.Collections.Generic.Dictionary<string, string>(model.TextStyleFonts, ci);
            if (model.LayerMap != null)
                model.LayerMap = new System.Collections.Generic.Dictionary<string, string>(model.LayerMap, ci);
        }

        public static StandardsModel GetDefaultStandards()
        {
            var standards = new StandardsModel
            {
                RequiredLinetype = null,
                RequiredTextStyle = "STANDARD",
                RequiredDimStyle = "STANDARD",
                LayerNamePrefix = "",
                LayerMap = []
            };

            return standards;
        }
    }
}