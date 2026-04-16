using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

using ACSE.AutoCAD2026.Compliance;
using ACSE.AutoCAD2026.Standards;

namespace ACSE.AutoCAD2026.UI
{
    public partial class AcseScanWindow : Window
    {
        private StandardsModel? _standards;
        private ScanResult? _lastScan;

        // For the DataGrid binding (filtered view)
        private List<Violation> _filteredViolations = new();

        public AcseScanWindow()
        {
            try
            {
                LogToFile("AcseScanWindow constructor started");

                LogToFile("Calling InitializeComponent...");
                InitializeComponent();
                LogToFile("InitializeComponent completed");

                LogToFile("Setting text blocks...");
                DrawingTextBlock.Text = "";
                TemplateInfoTextBlock.Text = "";
                TotalsTextBlock.Text = "";
                ScoreTextBlock.Text = "";
                FilterCountTextBlock.Text = "";
                LogToFile("Text blocks set successfully");

                LogToFile("AcseScanWindow constructor completed successfully");
            }
            catch (Exception ex)
            {
                LogToFile($"CONSTRUCTOR ERROR: {ex.GetType().Name}: {ex.Message}");
                LogToFile($"STACK: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    LogToFile($"INNER: {ex.InnerException.Message}");
                    LogToFile($"INNER STACK: {ex.InnerException.StackTrace}");
                }
                throw;
            }
        }

        private static void LogToFile(string message)
        {
            try
            {
                string logPath = @"C:\ACSE\acse_debug.log";
                string dir = System.IO.Path.GetDirectoryName(logPath);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                System.IO.File.AppendAllText(logPath, $"[{timestamp}] {message}\n");
            }
            catch
            {
                // Ignore logging errors
            }
        }

        // -------------------------
        // 1) SCAN
        // -------------------------
        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                MessageBox.Show("No active document. Please open a drawing first.", "ACSE", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ed = doc.Editor;

            try
            {
                string templatePath = (TemplatePathTextBox.Text ?? "").Trim();

                if (string.IsNullOrWhiteSpace(templatePath))
                {
                    MessageBox.Show("Please enter a valid .DWT template path.", "ACSE", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Load template-derived standards
                _standards = TemplateStandardsExtractor.LoadFromTemplate(templatePath);

                // Ensure TemplatePath is set (FixEngine uses standards.TemplatePath)
                _standards.TemplatePath = templatePath;

                // Scan drawing
                _lastScan = EntityScanner.Scan(_standards);

                // Update UI
                UpdateSummary(doc, _standards, _lastScan);
                ApplyFilterAndBind();
            }
            catch (Exception ex)
            {
                ed?.WriteMessage($"\nACSE Scan Error: {ex.Message}\nStack: {ex.StackTrace}");
                MessageBox.Show($"{ex.Message}\n\nSee command line for details.", "ACSE Scan Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // -------------------------
        // 2) FIX ALL (IMPORTANT: uses FULL scan results, NOT grid filter)
        // -------------------------
        private void FixAllButton_Click(object sender, RoutedEventArgs e)
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                MessageBox.Show("No active document. Please open a drawing first.", "ACSE", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ed = doc.Editor;

            try
            {
                if (_standards == null || _lastScan == null)
                {
                    MessageBox.Show("Run Scan first.", "ACSE", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Count fixable violations by type (from FULL scan)
                int fixText = _lastScan.Violations.Count(v => v.Type == ViolationType.TextStyle && v.AutoFixable);
                int fixDim  = _lastScan.Violations.Count(v => v.Type == ViolationType.DimStyle && v.AutoFixable);
                int fixLy   = _lastScan.Violations.Count(v => v.Type == ViolationType.Layer && v.AutoFixable);
                int fixLt   = _lastScan.Violations.Count(v => v.Type == ViolationType.Linetype && v.AutoFixable);

                var confirm = MessageBox.Show(
                    $"Fix All will attempt:\n\n" +
                    $"TextStyle: {fixText}\nDimStyle: {fixDim}\nLayer: {fixLy}\nLinetype: {fixLt}\n\nContinue?",
                    "ACSE - Fix All",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                ed.WriteMessage("\n========================================");
                ed.WriteMessage("\n*** ACSE FIX ALL ***");
                ed.WriteMessage("\n========================================");
                ed.WriteMessage($"\nTemplate: {_standards.TemplatePath}");
                ed.WriteMessage($"\nTarget text style: {_standards.RequiredTextStyle}");
                ed.WriteMessage($"\nTarget dim style: {_standards.RequiredDimStyle}");
                ed.WriteMessage($"\nViolations to fix: Text={fixText}, Dim={fixDim}, Layer={fixLy}, Linetype={fixLt}");

                // Run fixes (FixEngine methods already lock document)
                var textResult = FixEngine.AutoFixTextStylesFromTemplate(_lastScan, _standards);
                var dimResult  = FixEngine.AutoFixDimStylesFromTemplate(_lastScan, _standards);
                var layerResult= FixEngine.AutoFixLayersFromTemplate(_lastScan, _standards);
                var ltResult   = FixEngine.AutoFixLinetypesToByLayer(_lastScan, _standards);

                ed.WriteMessage($"\nText fix result: Fixed={textResult.FixedCount}, Failed={textResult.FailedCount}");
                ed.WriteMessage($"\nDim fix result: Fixed={dimResult.FixedCount}, Failed={dimResult.FailedCount}");
                ed.WriteMessage($"\nLayer fix result: Fixed={layerResult.FixedCount}, Failed={layerResult.FailedCount}");
                ed.WriteMessage($"\nLinetype fix result: Fixed={ltResult.FixedCount}, Failed={ltResult.FailedCount}");

                // Regen, then rescan to show changes
                doc.Editor.WriteMessage("\nRegenerating model.");
                doc.SendStringToExecute("_.REGENALL ", true, false, false);

                // Rescan immediately to refresh counts/grid
                _lastScan = EntityScanner.Scan(_standards);

                UpdateSummary(doc, _standards, _lastScan);
                ApplyFilterAndBind();
            }
            catch (Exception ex)
            {
                ed?.WriteMessage($"\nACSE Fix Error: {ex.Message}\nStack: {ex.StackTrace}");
                MessageBox.Show($"{ex.Message}\n\nSee command line for details.", "ACSE Fix Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // -------------------------
        // 3) RESCAN
        // -------------------------
        private void RescanButton_Click(object sender, RoutedEventArgs e)
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;

            try
            {
                if (_standards == null)
                {
                    MessageBox.Show("Run Scan first.", "ACSE", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _lastScan = EntityScanner.Scan(_standards);
                UpdateSummary(doc, _standards, _lastScan);
                ApplyFilterAndBind();
            }
            catch (Exception ex)
            {
                ed?.WriteMessage($"\nACSE Rescan Error: {ex.Message}");
                MessageBox.Show(ex.Message, "ACSE Rescan Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // -------------------------
        // 4) FILTER (checkboxes)
        // -------------------------
        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            // Prevent execution during XAML initialization
            if (!IsLoaded) return;

            ApplyFilterAndBind();
        }

        private void ApplyFilterAndBind()
        {
            // Guard against calls during initialization
            if (ViolationsGrid == null || FilterCountTextBlock == null)
                return;

            if (_lastScan == null)
            {
                ViolationsGrid.ItemsSource = null;
                FilterCountTextBlock.Text = "";
                return;
            }

            bool showTS = ShowTextStyleCheckBox?.IsChecked == true;
            bool showDS = ShowDimStyleCheckBox?.IsChecked == true;
            bool showLY = ShowLayerCheckBox?.IsChecked == true;
            bool showLT = ShowLinetypeCheckBox?.IsChecked == true;

            _filteredViolations = _lastScan.Violations
                .Where(v =>
                    (showTS && v.Type == ViolationType.TextStyle) ||
                    (showDS && v.Type == ViolationType.DimStyle) ||
                    (showLY && v.Type == ViolationType.Layer) ||
                    (showLT && v.Type == ViolationType.Linetype))
                .ToList();

            ViolationsGrid.ItemsSource = _filteredViolations;
            FilterCountTextBlock.Text = $"Showing {_filteredViolations.Count} of {_lastScan.Violations.Count}";
        }

        // -------------------------
        // 5) FIX SELECTED
        // -------------------------
        private void FixSelected_Click(object sender, RoutedEventArgs e)
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;

            try
            {
                if (_standards == null || _lastScan == null)
                {
                    MessageBox.Show("Run Scan first.", "ACSE", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var selected = ViolationsGrid.SelectedItems.Cast<Violation>().ToList();
                if (selected.Count == 0)
                {
                    MessageBox.Show("Select one or more violations first.", "ACSE", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int ok = 0, fail = 0;
                foreach (var v in selected)
                {
                    try
                    {
                        if (!v.AutoFixable) { fail++; continue; }
                        FixEngine.ApplySingleFix(v);
                        ok++;
                    }
                    catch
                    {
                        fail++;
                    }
                }

                ed?.WriteMessage($"\nACSE Fix Selected: Fixed={ok}, Failed={fail}");

                // Rescan + refresh
                _lastScan = EntityScanner.Scan(_standards);
                UpdateSummary(doc, _standards, _lastScan);
                ApplyFilterAndBind();
            }
            catch (Exception ex)
            {
                ed?.WriteMessage($"\nACSE Fix Selected Error: {ex.Message}");
                MessageBox.Show(ex.Message, "ACSE Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // -------------------------
        // 6) SELECT IN AUTOCAD (optional)
        // -------------------------
        private void SelectInAcad_Click(object sender, RoutedEventArgs e)
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;

            if (doc == null || ed == null) return;

            var selected = ViolationsGrid.SelectedItems.Cast<Violation>().ToList();
            if (selected.Count == 0) return;

            try
            {
                var ids = new ObjectIdCollection();
                foreach (var v in selected)
                {
                    if (string.IsNullOrWhiteSpace(v.EntityHandle)) continue;
                    if (TryResolveObjectIdFromHandle(doc.Database, v.EntityHandle, out var id))
                        ids.Add(id);
                }

                if (ids.Count > 0)
                {
                    ed.SetImpliedSelection(ids.Cast<ObjectId>().ToArray());
                    ed.WriteMessage($"\nSelected {ids.Count} item(s).");
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nSelect error: {ex.Message}");
            }
        }

        private static bool TryResolveObjectIdFromHandle(Database db, string handleStr, out ObjectId objectId)
        {
            objectId = ObjectId.Null;
            try
            {
                if (long.TryParse(handleStr, System.Globalization.NumberStyles.HexNumber, null, out long hex))
                    return db.TryGetObjectId(new Handle(hex), out objectId);

                if (long.TryParse(handleStr, out long dec))
                    return db.TryGetObjectId(new Handle(dec), out objectId);

                return false;
            }
            catch { return false; }
        }

        // -------------------------
        // Summary helpers
        // -------------------------
        private static double CalculateScore(ScanResult scan)
        {
            if (scan.GetTotalEntities() <= 0) return 100.0;
            // Simple score: (1 - violations/entities) * 100
            double raw = 1.0 - ((double)scan.TotalViolations / scan.GetTotalEntities());
            return Math.Max(0, Math.Min(100, raw * 100.0));
        }

        private void UpdateSummary(Document? doc, StandardsModel standards, ScanResult scan)
        {
            DrawingTextBlock.Text = doc?.Name ?? "(No doc)";
            TemplateInfoTextBlock.Text =
                $"TextStyles: {standards.ApprovedTextStyles.Count} | " +
                $"DimStyles: {standards.ApprovedDimStyles.Count} | " +
                $"Layers: {standards.ApprovedLayers.Count} | " +
                $"Linetypes: {standards.ApprovedLinetypes.Count}";

            TotalsTextBlock.Text =
                $"Total: {scan.TotalViolations} | " +
                $"TS: {scan.TextStyleViolations} | " +
                $"DS: {scan.DimStyleViolations} | " +
                $"LY: {scan.LayerViolations} | " +
                $"LT: {scan.LinetypeViolations}";

            ScoreTextBlock.Text = $"{CalculateScore(scan):F2}%";
        }

        private void ViolationsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Optional: on double-click, select in AutoCAD
            SelectInAcad_Click(sender, e);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // -------------------------
        // MANAGE DIMENSION STYLES
        // -------------------------
        private void ManageDimStyles_Click(object sender, RoutedEventArgs e)
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                MessageBox.Show("No active document.", "ACSE", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_standards == null)
                {
                    MessageBox.Show("Run Scan first to load standards.", "ACSE", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Close window temporarily
                this.Hide();

                // Run dimension style manager on AutoCAD thread using Application.Idle
                Autodesk.AutoCAD.ApplicationServices.Application.Idle += ManageDimStylesOnIdle;
            }
            catch (Exception ex)
            {
                doc.Editor?.WriteMessage($"\nACSE Dimension Style Manager Error: {ex.Message}");
                MessageBox.Show(ex.Message, "ACSE Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Show();
            }
        }

        private void ManageDimStylesOnIdle(object sender, EventArgs e)
        {
            // Unregister immediately
            Autodesk.AutoCAD.ApplicationServices.Application.Idle -= ManageDimStylesOnIdle;

            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                // Run dimension style manager in batch mode (no prompts)
                doc.Editor.WriteMessage("\n=== ACSE Dimension Style Manager (from UI) ===\n");
                var result = ACSE.AutoCAD2026.Tools.DimensionStyleManager.ManageDimStyles(_standards, interactive: false);

                if (result.Success)
                {
                    doc.Editor.WriteMessage($"\n✓ Dimension styles standardized!");
                    doc.Editor.WriteMessage($"\nCreated: {result.CreatedStyles}, Updated: {result.UpdatedStyles}, Purged: {result.PurgedStyles}");

                    // Show result to user
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            $"Dimension styles standardized!\n\n" +
                            $"Created: {result.CreatedStyles}\n" +
                            $"Updated: {result.UpdatedStyles}\n" +
                            $"Purged: {result.PurgedStyles}\n\n" +
                            $"Run Rescan to check for remaining violations.",
                            "ACSE - Dimension Style Manager",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        this.Show();
                    });
                }
                else
                {
                    doc.Editor.WriteMessage($"\n✗ Failed: {result.ErrorMessage}");
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Failed: {result.ErrorMessage}", "ACSE Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Show();
                    });
                }
            }
            catch (Exception ex)
            {
                doc.Editor?.WriteMessage($"\n✗ Error: {ex.Message}");
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error: {ex.Message}", "ACSE Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Show();
                });
            }
        }
    }
}
