using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using ACSE.AutoCAD2026.Standards;
using System.IO;
using System.Text.Json;
using ACSE.AutoCAD2026.Compliance;

namespace ACSE.AutoCAD2026.Commands
{
    public partial class ComplianceCommands
    {
        // Lazy-initialize to avoid crash during command discovery
        private JsonSerializerOptions s_jsonOptions;
        private JsonSerializerOptions JsonOptions
        {
            get
            {
                if (s_jsonOptions == null)
                {
                    s_jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                }
                return s_jsonOptions;
            }
        }

        [CommandMethod("ACSE_RUN")]
        public void RunCompliancePopup()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;

            if (doc == null)
            {
                Application.ShowAlertDialog("ACSE: No active drawing open.");
                return;
            }

            var ed = doc.Editor;

            try
            {
                // 1) Load standards
                string originalPath = @"C:\ACSE\Config\Standards.json";
                string standardsPath = originalPath;

                // If original path missing, try user AppData fallback and create defaults if necessary
                if (!File.Exists(standardsPath))
                {
                    string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ACSE");
                    string fallbackPath = Path.Combine(appDataDir, "Standards.json");

                    if (File.Exists(fallbackPath))
                    {
                        standardsPath = fallbackPath;
                    }
                    else
                    {
                        // create directory and write a default standards file
                        if (!string.IsNullOrEmpty(appDataDir))
                        {
                            Directory.CreateDirectory(appDataDir);
                        }
                        var defaultModel = new StandardsModel
                        {
                            RequiredLinetype = null,
                            RequiredTextStyle = null,
                            RequiredDimStyle = null,
                            AllowedLinetypes = new string[] { "ByLayer", "Continuous", "ByBlock" },
                            AllowedLayers = Array.Empty<string>()
                        };
                        var json = JsonSerializer.Serialize(defaultModel, JsonOptions);
                        File.WriteAllText(fallbackPath, json);
                        standardsPath = fallbackPath;
                        Application.ShowAlertDialog($"ACSE: Standards file not found at {originalPath}. A minimal default was created at {fallbackPath}.");
                    }
                }

                var standards = StandardsLoader.Load(standardsPath);

                // 1.5) Optional: Standardize dimension styles first
                ed.WriteMessage("\n\n[Optional] Standardize dimension styles before scanning? (Y/N): ");
                string response = ed.GetString(new PromptStringOptions("\nStandardize dimension styles? ") 
                { 
                    AllowSpaces = false,
                    DefaultValue = "N"
                }).StringResult.ToUpper();

                if (response == "Y" || response == "YES")
                {
                    ed.WriteMessage("\n\n=== Pre-Scan: Standardizing Dimension Styles ===");
                    var dimResult = ACSE.AutoCAD2026.Tools.DimensionStyleManager.ManageDimStyles(standards, interactive: false);
                    if (dimResult.Success)
                    {
                        ed.WriteMessage($"\n✓ Dimension styles standardized (Created: {dimResult.CreatedStyles}, Updated: {dimResult.UpdatedStyles})");
                    }
                    else
                    {
                        ed.WriteMessage($"\n✗ Dimension style standardization failed: {dimResult.ErrorMessage}");
                    }
                }

                // 2) Scan drawing
                var scanResult = EntityScanner.Scan(standards);

                // 3) Score
                double score = ScoringEngine.CalculateScore(scanResult);

                // 4) Report
                ed.WriteMessage("\n====================================");
                ed.WriteMessage("\n      ACSE COMPLIANCE REPORT");
                ed.WriteMessage("\n====================================");
                ed.WriteMessage($"\nDrawing: {doc.Name}");
                ed.WriteMessage($"\nTimestamp: {DateTime.Now}");
                ed.WriteMessage($"\nTotal Entities: {scanResult.GetTotalEntities()}");
                ed.WriteMessage($"\nUnique Layers Found: {scanResult.GetLayersFound().Count}");
                ed.WriteMessage($"\n");
                ed.WriteMessage($"\nTotal Violations: {scanResult.TotalViolations}");
                ed.WriteMessage($"\nLayer Violations: {scanResult.LayerViolations}");
                ed.WriteMessage($"\nLinetype Violations: {scanResult.LinetypeViolations}");
                ed.WriteMessage($"\nTextStyle Violations: {scanResult.TextStyleViolations}");
                ed.WriteMessage($"\nDimStyle Violations: {scanResult.DimStyleViolations}");
                ed.WriteMessage($"\n");
                ed.WriteMessage($"\nCompliance Score: {score:F2}%");
                ed.WriteMessage("\n====================================");

                int show = Math.Min(10, scanResult.Violations.Count);
                if (show > 0)
                {
                    ed.WriteMessage("\n\nTop Violations:");
                    ed.WriteMessage("\n------------------------------------");
                    for (int i = 0; i < show; i++)
                    {
                        var v = scanResult.Violations[i];
                        ed.WriteMessage($"\n[{v.RuleId}] {v.EntityName} (H={v.EntityHandle}) Layer={v.Layer} Expected={v.Expected} Actual={v.Actual}");
                    }
                }
                ed.WriteMessage("\n====================================\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nACSE Error: {ex.Message}");
            }
        }

        [CommandMethod("ACSE_RESET")]
        public void ResetStandards()
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
                string originalPath = @"C:\ACSE\Config\Standards.json";
                string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ACSE");
                string fallbackPath = Path.Combine(appDataDir, "Standards.json");

                // Delete both possible locations
                bool deletedAny = false;
                if (File.Exists(originalPath))
                {
                    File.Delete(originalPath);
                    ed.WriteMessage($"\nDeleted: {originalPath}");
                    deletedAny = true;
                }
                if (File.Exists(fallbackPath))
                {
                    File.Delete(fallbackPath);
                    ed.WriteMessage($"\nDeleted: {fallbackPath}");
                    deletedAny = true;
                }

                if (!deletedAny)
                {
                    ed.WriteMessage("\nNo existing standards file found.");
                }

                // Create new default at original path
                var dirPath = Path.GetDirectoryName(originalPath);
                if (!string.IsNullOrEmpty(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                var defaultModel = new StandardsModel
                {
                    RequiredLinetype = null,
                    RequiredTextStyle = null,
                    RequiredDimStyle = null,
                    AllowedLinetypes = new string[] { "ByLayer", "Continuous", "ByBlock" },
                    AllowedLayers = Array.Empty<string>()
                };
                var json = JsonSerializer.Serialize(defaultModel, JsonOptions);
                File.WriteAllText(originalPath, json);

                ed.WriteMessage($"\n\nCreated new minimal standards at: {originalPath}");
                ed.WriteMessage("\n\nStandards reset complete. Run ACSE_RUN to test.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nACSE_RESET Error: {ex.Message}");
            }
        }

        [CommandMethod("ACSE_PING")]
        public void Ping()
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application
                .DocumentManager.MdiActiveDocument;

            doc?.Editor.WriteMessage("\nACSE_PING ok.");
        }

        [CommandMethod("HELLO")]
        public static void HelloWorld()
        {
            Application.ShowAlertDialog("HELLO WORLD - NEW COMMAND WORKS!");
        }

        /// <summary>
        /// Runs template-based compliance check.
        /// Usage: ACSE_TEMPLATE
        /// </summary>
        [CommandMethod("ACSE_TEMPLATE")]
        public void RunTemplateCompliance()
        {
            Application.ShowAlertDialog("ACSE_TEMPLATE started!");

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                Application.ShowAlertDialog("ACSE: No active drawing open.");
                return;
            }

            var ed = doc.Editor;

            try
            {
                string templatePath = @"C:\ACSE\config\Standards\FAA_002_acad.dwt";
                ed.WriteMessage($"\n*** ACSE_TEMPLATE ***");
                ed.WriteMessage($"\nUsing template: {templatePath}");

                if (!File.Exists(templatePath))
                {
                    ed.WriteMessage($"\nERROR: Template not found at: {templatePath}");
                    Application.ShowAlertDialog($"Template not found:\n{templatePath}");
                    return;
                }

                ed.WriteMessage("\nLoading template...");
                var standards = TemplateStandardsExtractor.LoadFromTemplate(templatePath);

                ed.WriteMessage($"\n  Text Styles: {standards.ApprovedTextStyles.Count}");
                ed.WriteMessage($"\n  Dim Styles: {standards.ApprovedDimStyles.Count}");
                ed.WriteMessage($"\n  Layers: {standards.ApprovedLayers.Count}");
                ed.WriteMessage($"\n  Linetypes: {standards.ApprovedLinetypes.Count}");

                ed.WriteMessage("\n\nScanning drawing...");
                var scanResult = EntityScanner.Scan(standards);

                double score = ScoringEngine.CalculateScore(scanResult);

                ed.WriteMessage("\n\n====================================");
                ed.WriteMessage("\n  ACSE TEMPLATE COMPLIANCE REPORT");
                ed.WriteMessage("\n====================================");
                ed.WriteMessage($"\nTemplate: {templatePath}");
                ed.WriteMessage($"\nDrawing: {doc.Name}");
                ed.WriteMessage("\n------------------------------------");
                ed.WriteMessage($"\nTotal Entities:       {scanResult.GetTotalEntities()}");
                ed.WriteMessage($"\nTotal Violations:     {scanResult.TotalViolations}");
                ed.WriteMessage($"\nTextStyle Violations: {scanResult.TextStyleViolations}");
                ed.WriteMessage($"\nDimStyle Violations:  {scanResult.DimStyleViolations}");
                ed.WriteMessage($"\nLayer Violations:     {scanResult.LayerViolations}");
                ed.WriteMessage($"\nLinetype Violations:  {scanResult.LinetypeViolations}");
                ed.WriteMessage("\n------------------------------------");
                ed.WriteMessage($"\nCOMPLIANCE SCORE: {score:F2}%");
                ed.WriteMessage("\n====================================\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nACSE Error: {ex.Message}");
                Application.ShowAlertDialog($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Auto-fix text and dim style violations using template standards.
        /// Usage: ACSE_FIX
        /// </summary>
        [CommandMethod("ACSE_FIX")]
        public void FixTemplateViolations()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                Application.ShowAlertDialog("ACSE: No active drawing open.");
                return;
            }

            var ed = doc.Editor;

            try
            {
                string templatePath = @"C:\ACSE\config\Standards\FAA_002_acad.dwt";
                ed.WriteMessage($"\n*** ACSE_FIX - Auto-Fix Template Violations ***");
                ed.WriteMessage($"\nUsing template: {templatePath}");

                if (!File.Exists(templatePath))
                {
                    ed.WriteMessage($"\nERROR: Template not found at: {templatePath}");
                    return;
                }

                // Load standards from template
                var standards = TemplateStandardsExtractor.LoadFromTemplate(templatePath);
                standards.CreateMissingTextStyle = true; // Enable style creation

                // Initial scan
                ed.WriteMessage("\n\nScanning drawing (before fix)...");
                var beforeScan = EntityScanner.Scan(standards);
                double beforeScore = ScoringEngine.CalculateScore(beforeScan);

                ed.WriteMessage($"\n  Before: {beforeScan.TotalViolations} violations");
                ed.WriteMessage($"\n  TextStyle: {beforeScan.TextStyleViolations}");
                ed.WriteMessage($"\n  DimStyle: {beforeScan.DimStyleViolations}");
                ed.WriteMessage($"\n  Score: {beforeScore:F2}%");

                // Fix text styles
                ed.WriteMessage("\n\nFixing text styles...");
                var textResult = FixEngine.AutoFixTextStylesFromTemplate(beforeScan, standards);
                ed.WriteMessage($"\n  Fixed: {textResult.FixedCount}");
                if (!textResult.Success)
                    ed.WriteMessage($"\n  Warning: {textResult.ErrorMessage}");

                // Fix dim styles
                ed.WriteMessage("\n\nFixing dimension styles...");
                var dimResult = FixEngine.AutoFixDimStylesFromTemplate(beforeScan, standards);
                ed.WriteMessage($"\n  Fixed: {dimResult.FixedCount}");
                if (!dimResult.Success)
                    ed.WriteMessage($"\n  Warning: {dimResult.ErrorMessage}");

                // Re-scan to verify
                ed.WriteMessage("\n\nScanning drawing (after fix)...");
                var afterScan = EntityScanner.Scan(standards);
                double afterScore = ScoringEngine.CalculateScore(afterScan);

                // Report
                ed.WriteMessage("\n\n====================================");
                ed.WriteMessage("\n      ACSE AUTO-FIX REPORT");
                ed.WriteMessage("\n====================================");
                ed.WriteMessage($"\nViolations Before: {beforeScan.TotalViolations}");
                ed.WriteMessage($"\nText Fixes Applied: {textResult.FixedCount}");
                ed.WriteMessage($"\nDim Fixes Applied: {dimResult.FixedCount}");
                ed.WriteMessage($"\nViolations After: {afterScan.TotalViolations}");
                ed.WriteMessage("\n------------------------------------");
                ed.WriteMessage($"\nScore Before: {beforeScore:F2}%");
                ed.WriteMessage($"\nScore After: {afterScore:F2}%");
                ed.WriteMessage($"\nImprovement: +{afterScore - beforeScore:F2}%");
                ed.WriteMessage("\n====================================\n");

                if (afterScan.TotalViolations > 0)
                {
                    ed.WriteMessage($"\nRemaining {afterScan.TotalViolations} violations require manual review.");
                }
                else
                {
                    ed.WriteMessage("\n*** ALL VIOLATIONS FIXED! ***");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nACSE Error: {ex.Message}");
            }
        }
    }
}