using System;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using ACSE.AutoCAD2026.Standards;

namespace ACSE.AutoCAD2026.Commands
{
    /// <summary>
    /// Commands for template-based standards management.
    /// </summary>
    public class TemplateCommands
    {
        // Singleton instance
        private static readonly TemplateCommands _instance = new TemplateCommands();
        private static GCHandle _pinnedInstance;

        static TemplateCommands()
        {
            _pinnedInstance = GCHandle.Alloc(_instance, GCHandleType.Normal);
        }

        public TemplateCommands()
        {
        }

        private StandardsModel? _templateStandards;

        /// <summary>
        /// Gets the last loaded template standards.
        /// </summary>
        public StandardsModel? TemplateStandards => _templateStandards;

        /// <summary>
        /// Loads standards from a DWT template file.
        /// Usage: ACSE_LOAD_TEMPLATE
        /// </summary>
        [CommandMethod("ACSE_LOAD_TEMPLATE")]
        public void LoadTemplate()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            // Prompt for template path
            var result = ed.GetString("\nEnter template path (or press Enter for default): ");

            string path;
            if (result.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK || 
                string.IsNullOrWhiteSpace(result.StringResult))
            {
                // Default path - change this to your template location
                path = @"C:\Templates\FAA_002_acad.dwt";
                ed.WriteMessage($"\nUsing default path: {path}");
            }
            else
            {
                path = result.StringResult;
            }

            try
            {
                _templateStandards = TemplateStandardsExtractor.LoadFromTemplate(path);

                ed.WriteMessage($"\n\n========== TEMPLATE LOADED ==========");
                ed.WriteMessage($"\nSource: {path}");
                ed.WriteMessage($"\n--------------------------------------");
                ed.WriteMessage($"\nText Styles:  {_templateStandards.ApprovedTextStyles.Count}");
                ed.WriteMessage($"\nDim Styles:   {_templateStandards.ApprovedDimStyles.Count}");
                ed.WriteMessage($"\nLayers:       {_templateStandards.ApprovedLayers.Count}");
                ed.WriteMessage($"\nLinetypes:    {_templateStandards.ApprovedLinetypes.Count}");
                ed.WriteMessage($"\n--------------------------------------");
                ed.WriteMessage($"\nRequired Text Style: {_templateStandards.RequiredTextStyle}");
                ed.WriteMessage($"\nRequired Dim Style:  {_templateStandards.RequiredDimStyle}");
                ed.WriteMessage($"\n======================================\n");
            }
            catch (System.IO.FileNotFoundException ex)
            {
                ed.WriteMessage($"\nError: {ex.Message}");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError loading template: {ex.Message}");
            }
        }

        /// <summary>
        /// Lists all text styles from the loaded template.
        /// Usage: ACSE_LIST_TEMPLATE_STYLES
        /// </summary>
        [CommandMethod("ACSE_LIST_TEMPLATE_STYLES")]
        public void ListTemplateStyles()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            if (_templateStandards == null)
            {
                ed.WriteMessage("\nNo template loaded. Run ACSE_LOAD_TEMPLATE first.");
                return;
            }

            ed.WriteMessage($"\n\n===== TEXT STYLES FROM TEMPLATE =====");
            foreach (var style in _templateStandards.ApprovedTextStyles)
            {
                var font = _templateStandards.TextStyleFonts.TryGetValue(style, out var f) ? f : "(unknown)";
                ed.WriteMessage($"\n  {style} -> {font}");
            }

            ed.WriteMessage($"\n\n===== DIM STYLES FROM TEMPLATE =====");
            foreach (var style in _templateStandards.ApprovedDimStyles)
            {
                ed.WriteMessage($"\n  {style}");
            }

            ed.WriteMessage($"\n\n===== LAYERS FROM TEMPLATE =====");
            foreach (var layer in _templateStandards.ApprovedLayers)
            {
                ed.WriteMessage($"\n  {layer}");
            }

            ed.WriteMessage("\n");
        }

        /// <summary>
        /// Scans current drawing using template-derived standards.
        /// Usage: ACSE_SCAN_WITH_TEMPLATE
        /// </summary>
        [CommandMethod("ACSE_SCAN_WITH_TEMPLATE")]
        public void ScanWithTemplate()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            if (_templateStandards == null)
            {
                ed.WriteMessage("\nNo template loaded. Run ACSE_LOAD_TEMPLATE first.");
                return;
            }

            try
            {
                var scanResult = Compliance.EntityScanner.Scan(_templateStandards);

                ed.WriteMessage($"\n\n========== SCAN RESULTS ==========");
                ed.WriteMessage($"\nTotal Entities:      {scanResult.GetTotalEntities()}");
                ed.WriteMessage($"\nTotal Violations:    {scanResult.TotalViolations}");
                ed.WriteMessage($"\n----------------------------------");
                ed.WriteMessage($"\nLinetype Violations: {scanResult.LinetypeViolations}");
                ed.WriteMessage($"\nLayer Violations:    {scanResult.LayerViolations}");
                ed.WriteMessage($"\nTextStyle Violations:{scanResult.TextStyleViolations}");
                ed.WriteMessage($"\nDimStyle Violations: {scanResult.DimStyleViolations}");
                ed.WriteMessage($"\n==================================\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError during scan: {ex.Message}");
            }
        }
    }
}
