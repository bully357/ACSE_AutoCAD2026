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

            return JsonSerializer.Deserialize<StandardsModel>(json, JsonOptions)
                ?? throw new InvalidDataException($"Failed to deserialize standards from file: {filePath}");
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