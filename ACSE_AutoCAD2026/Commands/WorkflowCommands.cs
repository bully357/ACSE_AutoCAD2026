using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using ACSE.AutoCAD2026.Compliance;
using ACSE.AutoCAD2026.Standards;
using ACSE.AutoCAD2026.Tools;

namespace ACSE.AutoCAD2026.Commands
{
    public class WorkflowCommands
    {
        /// <summary>
        /// Complete workflow: Standardize dimension styles → Scan → Show results
        /// </summary>
        [CommandMethod("ACSE_FULL_WORKFLOW", CommandFlags.Modal)]
        public void FullWorkflow()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var ed = doc.Editor;

            try
            {
                // Step 1: Prompt for standards file
                ed.WriteMessage("\n=== ACSE Complete Workflow ===\n");
                var fileRes = ed.GetFileNameForOpen("\nSelect Standards JSON file: ");
                if (fileRes.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    ed.WriteMessage("\nOperation cancelled.");
                    return;
                }

                // Load standards
                var standards = StandardsLoader.Load(fileRes.StringResult);
                ed.WriteMessage($"\n✓ Standards loaded: {fileRes.StringResult}");

                // Step 2: Standardize dimension styles
                ed.WriteMessage("\n\n=== Step 1: Standardizing Dimension Styles ===");
                var dimResult = DimensionStyleManager.ManageDimStyles(standards, interactive: false);
                
                if (dimResult.Success)
                {
                    ed.WriteMessage($"\n✓ Dimension styles standardized");
                    ed.WriteMessage($"\n  Created: {dimResult.CreatedStyles}");
                    ed.WriteMessage($"\n  Updated: {dimResult.UpdatedStyles}");
                    ed.WriteMessage($"\n  Purged: {dimResult.PurgedStyles}");
                }
                else
                {
                    ed.WriteMessage($"\n✗ Dimension style standardization failed: {dimResult.ErrorMessage}");
                    ed.WriteMessage("\nContinuing with scan...");
                }

                // Step 3: Scan for violations
                ed.WriteMessage("\n\n=== Step 2: Scanning Drawing ===");
                var scanResult = EntityScanner.Scan(standards);
                
                ed.WriteMessage($"\n✓ Scan complete");
                ed.WriteMessage($"\n  Total entities: {scanResult.GetTotalEntities()}");
                ed.WriteMessage($"\n  Violations: {scanResult.TotalViolations}");
                ed.WriteMessage($"\n    TextStyle: {scanResult.TextStyleViolations}");
                ed.WriteMessage($"\n    DimStyle: {scanResult.DimStyleViolations}");
                ed.WriteMessage($"\n    Layer: {scanResult.LayerViolations}");
                ed.WriteMessage($"\n    Linetype: {scanResult.LinetypeViolations}");

                // Step 4: Calculate score
                double score = ScoringEngine.CalculateScore(scanResult);
                ed.WriteMessage($"\n\n=== Compliance Score: {score:F1}% ===");

                // Step 5: Optional - Auto-fix violations
                if (scanResult.TotalViolations > 0)
                {
                    var fixPrompt = new Autodesk.AutoCAD.EditorInput.PromptKeywordOptions("\nAuto-fix violations? ");
                    fixPrompt.Keywords.Add("Yes");
                    fixPrompt.Keywords.Add("No");
                    fixPrompt.Keywords.Default = "No";
                    fixPrompt.AllowNone = true;

                    var fixRes = ed.GetKeywords(fixPrompt);
                    if (fixRes.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK && fixRes.StringResult == "Yes")
                    {
                        ed.WriteMessage("\n\n=== Step 3: Auto-Fixing Violations ===");
                        int fixedCount = FixEngine.ApplyAutoFixes(scanResult, standards);

                        ed.WriteMessage($"\n✓ Auto-fix complete");
                        ed.WriteMessage($"\n  Fixed: {fixedCount}");

                        if (fixedCount > 0)
                        {
                            ed.WriteMessage("\n\n✓ Workflow complete! Use UNDO if needed.");
                        }
                    }
                    else
                    {
                        ed.WriteMessage("\n\nUse ACSE_SCAN_INTERACTIVE to fix violations step-by-step.");
                    }
                }
                else
                {
                    ed.WriteMessage("\n\n✓ No violations found! Drawing is compliant.");
                }

                ed.WriteMessage("\n\n=== Workflow Complete ===\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n✗ Workflow failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Quick workflow: Standardize dimension styles only
        /// </summary>
        [CommandMethod("ACSE_QUICK_DIMSTYLE", CommandFlags.Modal)]
        public void QuickDimStyle()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var ed = doc.Editor;

            try
            {
                // Prompt for standards file
                var fileRes = ed.GetFileNameForOpen("\nSelect Standards JSON file: ");
                if (fileRes.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    ed.WriteMessage("\nOperation cancelled.");
                    return;
                }

                // Load and apply
                var standards = StandardsLoader.Load(fileRes.StringResult);
                ed.WriteMessage("\n=== Quick Dimension Style Standardization ===\n");
                
                var result = DimensionStyleManager.ManageDimStyles(standards, interactive: false);
                
                if (result.Success)
                {
                    ed.WriteMessage($"\n✓ Success!");
                    ed.WriteMessage($"\n  Created: {result.CreatedStyles}");
                    ed.WriteMessage($"\n  Updated: {result.UpdatedStyles}");
                    ed.WriteMessage($"\n  Purged: {result.PurgedStyles}");
                    
                    if (result.SetAsCurrent)
                    {
                        ed.WriteMessage($"\n  '{standards.RequiredDimStyleName}' set as current");
                    }
                    
                    ed.WriteMessage("\n\n✓ Dimension styles standardized!\n");
                }
                else
                {
                    ed.WriteMessage($"\n✗ Failed: {result.ErrorMessage}\n");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n✗ Error: {ex.Message}\n");
            }
        }
    }
}
