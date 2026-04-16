using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using ACSE.AutoCAD2026.Standards;

namespace ACSE.AutoCAD2026.Commands
{ 
    public class ExtractCommands
    {
        // Singleton instance
        private static readonly ExtractCommands _instance = new ExtractCommands();
        private static GCHandle _pinnedInstance;

        static ExtractCommands()
        {
            _pinnedInstance = GCHandle.Alloc(_instance, GCHandleType.Normal);
        }

        // CRITICAL: Lazy initialization to prevent crash during command discovery
        private JsonSerializerOptions _jsonOptions;
        private JsonSerializerOptions JsonOptions
        {
            get
            {
                if (_jsonOptions == null)
                {
                    _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                }
                return _jsonOptions;
            }
        }

        public ExtractCommands()
        {
        }

        [CommandMethod("ACSE_EXTRACT")]
        public void ExtractStandards(string[] strings)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                Application.ShowAlertDialog("ACSE: No active document.");
                return;
            }

            var ed = doc.Editor;

            try
            {
                // Prompt for DWT file path
                var promptOptions = new Autodesk.AutoCAD.EditorInput.PromptOpenFileOptions("\nSelect FAA Template (.DWT) file:")
                {
                    Filter = "AutoCAD Template (*.dwt)|*.dwt|AutoCAD Drawing (*.dwg)|*.dwg|All files (*.*)|*.*",
                    InitialDirectory = @"C:\Users\jdbul\FAA_AutoCAD Standards"
                };

                var fileResult = ed.GetFileNameForOpen(promptOptions);
                if (fileResult.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    ed.WriteMessage("\nExtraction cancelled.");
                    return;
                }

                string templatePath = fileResult.StringResult;
                ed.WriteMessage($"\nExtracting standards from: {templatePath}");

                // Open the template file
                using Database templateDb = new(false, true);
                templateDb.ReadDwgFile(templatePath, FileOpenMode.OpenForReadAndAllShare, true, null);

                StandardsModel standardsModel = new()
                {
                    RequiredLinetype = null,
                    RequiredTextStyle = "STANDARD",
                    RequiredDimStyle = "STANDARD",
                    AllowedLinetypes = strings,
                    AllowedLayers = [],
                    LayerNamePrefix = "",
                    LayerMap = []
                };
                var standards = standardsModel;

                string[]? textStyleNames = null;
                string[]? dimStyleNames = null;

                using (Transaction tr = templateDb.TransactionManager.StartTransaction())
                        {
                        // Extract Layers
                        var layerTable = (LayerTable)tr.GetObject(templateDb.LayerTableId, OpenMode.ForRead);
                        var layerNames = layerTable.Cast<ObjectId>()
                            .Select(layerId => ((LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead)).Name)
                            .Order()
                            .ToArray();
                        standards.AllowedLayers = layerNames;
                        ed.WriteMessage($"\n  Found {layerNames.Length} layers");

                        // Extract Linetypes
                        var linetypeTable = (LinetypeTable)tr.GetObject(templateDb.LinetypeTableId, OpenMode.ForRead);
                        var linetypeNames = linetypeTable.Cast<ObjectId>()
                            .Select(ltId => ((LinetypeTableRecord)tr.GetObject(ltId, OpenMode.ForRead)).Name)
                            .Order()
                            .ToArray();
                        standards.AllowedLinetypes = linetypeNames;
                        ed.WriteMessage($"\n  Found {linetypeNames.Length} linetypes");

                        // Extract Text Styles
                        var textStyleTable = (TextStyleTable)tr.GetObject(templateDb.TextStyleTableId, OpenMode.ForRead);
                        textStyleNames = textStyleTable.Cast<ObjectId>()
                            .Select(tsId => ((TextStyleTableRecord)tr.GetObject(tsId, OpenMode.ForRead)).Name)
                            .ToArray();
                        ed.WriteMessage($"\n  Found {textStyleNames.Length} text styles");
                        if (textStyleNames.Contains("STANDARD"))
                            standards.RequiredTextStyle = "STANDARD";
                        else if (textStyleNames.Length > 0)
                            standards.RequiredTextStyle = textStyleNames[0];

                        // Extract Dimension Styles
                        var dimStyleTable = (DimStyleTable)tr.GetObject(templateDb.DimStyleTableId, OpenMode.ForRead);
                        dimStyleNames = dimStyleTable.Cast<ObjectId>()
                            .Select(dsId => ((DimStyleTableRecord)tr.GetObject(dsId, OpenMode.ForRead)).Name)
                            .ToArray();
                        ed.WriteMessage($"\n  Found {dimStyleNames.Length} dimension styles");
                        if (dimStyleNames.Contains("STANDARD"))
                            standards.RequiredDimStyle = "STANDARD";
                        else if (dimStyleNames.Length > 0)
                            standards.RequiredDimStyle = dimStyleNames[0];

                        tr.Commit();
                    }

                    // Save to JSON file
                    string outputPath = Path.Combine(
                        Path.GetDirectoryName(templatePath) ?? Environment.CurrentDirectory,
                        "Standards-Extracted.json"
                    );

                    var json = JsonSerializer.Serialize(standards, JsonOptions);
                    File.WriteAllText(outputPath, json);

                    ed.WriteMessage($"\n\nExtraction complete!");
                    ed.WriteMessage($"\nSaved to: {outputPath}");
                    ed.WriteMessage($"\n\nSummary:");
                    ed.WriteMessage($"\n  Layers: {standards.AllowedLayers.Length}");
                    ed.WriteMessage($"\n  Linetypes: {standards.AllowedLinetypes.Length}");
                    ed.WriteMessage($"\n  Text Styles: {textStyleNames.Length}");
                    ed.WriteMessage($"\n  Dim Styles: {dimStyleNames.Length}");
                    ed.WriteMessage($"\n\nTo use: Copy {outputPath} to C:\\ACSE\\Config\\Standards.json");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nACSE_EXTRACT Error: {ex.Message}");
                ed.WriteMessage($"\nStack: {ex.StackTrace}");
            }
        }
    }
}
