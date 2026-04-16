using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using ACSE.AutoCAD2026.Compliance;
using ACSE.AutoCAD2026.Standards;
using ACSE.AutoCAD2026.Tools;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ACSE.AutoCAD2026.UI
{
    public partial class StandardsControlPanel : Window
    {
        private StandardsModel? _standards;
        private ScanResult? _lastScan;
        private readonly ObservableCollection<ViolationViewModel> _violations = new();
        private ICollectionView _violationsView;

        public StandardsControlPanel()
        {
            InitializeComponent();
            ViolationsDataGrid.ItemsSource = _violations;
            _violationsView = CollectionViewSource.GetDefaultView(_violations);
            _violationsView.Filter = FilterViolations;
            PopulateComboBoxes();
        }

        private void LoadStandards_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    Title = "Select Standards File",
                    DefaultExt = "json"
                };

                // Try to set initial directory, but don't fail if it doesn't exist
                try
                {
                    var docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    var repoPath = Path.Combine(docsPath, @"..\source\repos\ACSE_AutoCAD2026");
                    if (Directory.Exists(repoPath))
                    {
                        openFileDialog.InitialDirectory = repoPath;
                    }
                }
                catch
                {
                    // Ignore if path doesn't exist, will use default
                }

                if (openFileDialog.ShowDialog() == true)
                {
                    _standards = StandardsLoader.Load(openFileDialog.FileName);
                    StandardsPathTextBox.Text = Path.GetFileName(openFileDialog.FileName);
                    MessageBox.Show($"Standards loaded successfully!\n\nFile: {Path.GetFileName(openFileDialog.FileName)}\n\nNow click 'Scan Drawing' to detect violations.", 
                                  "ACSE - Standards Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load standards:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ScanDrawing_Click(object sender, RoutedEventArgs e)
        {
            if (_standards == null)
            {
                MessageBox.Show("Load standards first!", "ACSE", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Show progress bar
                ScanProgressBar.Visibility = System.Windows.Visibility.Visible;

                _lastScan = EntityScanner.Scan(_standards);

                // Clear and populate violations
                _violations.Clear();

                foreach (var violation in _lastScan.Violations)
                {
                    _violations.Add(new ViolationViewModel(violation));
                }

                // Update summary
                UpdateSummary();

                // Hide progress bar
                ScanProgressBar.Visibility = System.Windows.Visibility.Collapsed;

                MessageBox.Show($"Scan complete!\n\nFound {_lastScan.TotalViolations} violations.\n\nSelect violations and click 'Apply Selected Fixes' to correct them.", 
                              "ACSE", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ScanProgressBar.Visibility = System.Windows.Visibility.Collapsed;
                MessageBox.Show($"Scan failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshStatus_Click(object sender, RoutedEventArgs e)
        {
            // Re-scan the drawing
            ScanDrawing_Click(sender, e);
        }

        private void UpdateSummary()
        {
            if (_lastScan == null) return;

            TotalViolationsText.Text = $"Total Violations: {_lastScan.TotalViolations}";
            TotalViolationsText.Foreground = _lastScan.TotalViolations == 0 
                ? new SolidColorBrush(Colors.Green) 
                : new SolidColorBrush(Colors.Red);

            TotalEntitiesText.Text = $"Total Entities: {_lastScan.GetTotalEntities()}";

            DimViolationsText.Text = $"Dimension Issues: {_lastScan.DimStyleViolations}";
            TextViolationsText.Text = $"Text Issues: {_lastScan.TextStyleViolations}";
            LayerViolationsText.Text = $"Layer Issues: {_lastScan.LayerViolations}";
            LinetypeViolationsText.Text = $"Linetype Issues: {_lastScan.LinetypeViolations}";

            // Update safety ratings
            int safeCount = _violations.Count(v => v.Violation.Recommendation == FixRecommendation.Safe);
            int recommendedCount = _violations.Count(v => v.Violation.Recommendation == FixRecommendation.Recommended);
            int manualCount = _violations.Count(v => v.Violation.Recommendation == FixRecommendation.ManualReview);

            SafeFixesText.Text = $"Safe: {safeCount}";
            RecommendedFixesText.Text = $"Recommended: {recommendedCount}";
            ManualReviewText.Text = $"Manual Review: {manualCount}";

            UpdateSelectionCount();
        }

        private void UpdateSelectionCount()
        {
            int selectedCount = _violations.Count(v => v.IsSelected);
            SelectedCountText.Text = $"Selected: {selectedCount}";
        }

        private void ViolationsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateSelectionCount();
        }

        private void PopulateComboBoxes()
        {
            try
            {
                var doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc == null) return;

                var db = doc.Database;
                using var tr = db.TransactionManager.StartTransaction();

                // Populate Layers
                LayerComboBox.Items.Clear();
                var layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in layerTable)
                {
                    var layer = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    LayerComboBox.Items.Add(layer.Name);
                }

                // Populate Linetypes
                LinetypeComboBox.Items.Clear();
                LinetypeComboBox.Items.Add("ByLayer");
                LinetypeComboBox.Items.Add("ByBlock");
                var linetypeTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                foreach (ObjectId id in linetypeTable)
                {
                    var linetype = (LinetypeTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (!linetype.Name.Equals("ByLayer", StringComparison.OrdinalIgnoreCase) &&
                        !linetype.Name.Equals("ByBlock", StringComparison.OrdinalIgnoreCase))
                    {
                        LinetypeComboBox.Items.Add(linetype.Name);
                    }
                }

                // Populate Text Styles
                TextStyleComboBox.Items.Clear();
                var textStyleTable = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                foreach (ObjectId id in textStyleTable)
                {
                    var textStyle = (TextStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    TextStyleComboBox.Items.Add(textStyle.Name);
                }

                // Populate Colors
                ColorComboBox.Items.Clear();
                ColorComboBox.Items.Add("ByLayer");
                ColorComboBox.Items.Add("ByBlock");
                ColorComboBox.Items.Add("Red");
                ColorComboBox.Items.Add("Yellow");
                ColorComboBox.Items.Add("Green");
                ColorComboBox.Items.Add("Cyan");
                ColorComboBox.Items.Add("Blue");
                ColorComboBox.Items.Add("Magenta");
                ColorComboBox.Items.Add("White");

                tr.Commit();
            }
            catch
            {
                // Silently fail if no document is open
            }
        }

        private void ApplyPropertiesToSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = _violations.Where(v => v.IsSelected).ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show("No violations selected!", "ACSE", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Get selected properties
            string? newLayer = LayerComboBox.SelectedItem as string;
            string? newLinetype = LinetypeComboBox.SelectedItem as string;
            string? newTextStyle = TextStyleComboBox.SelectedItem as string;
            string? newColor = ColorComboBox.SelectedItem as string;

            double? newTextHeight = null;
            if (double.TryParse(TextHeightBox.Text, out double height) && height > 0)
            {
                newTextHeight = height;
            }

            bool makeAnnotative = AnnotativeCheckBox.IsChecked == true;

            // Apply on AutoCAD thread
            AcadApp.Idle += (s, ev) =>
            {
                AcadApp.Idle -= (s2, ev2) => { };
                ApplyPropertiesOnIdle(selected, newLayer, newLinetype, newTextStyle, newColor, newTextHeight, makeAnnotative);
            };

            this.Hide();
        }

            private void ApplyPropertiesOnIdle(List<ViolationViewModel> selected, string? newLayer, string? newLinetype, 
                string? newTextStyle, string? newColor, double? newTextHeight, bool makeAnnotative)
            {
                try
                {
                    var doc = AcadApp.DocumentManager.MdiActiveDocument;
                    var db = doc.Database;
                    int successCount = 0;

                    using var tr = db.TransactionManager.StartTransaction();

                    foreach (var vm in selected)
                    {
                        try
                        {
                            if (vm.Violation.EntityId.IsNull || !vm.Violation.EntityId.IsValid)
                                continue;

                            var entity = tr.GetObject(vm.Violation.EntityId, OpenMode.ForWrite) as Entity;
                            if (entity == null) continue;

                            // Apply Layer
                            if (!string.IsNullOrEmpty(newLayer))
                            {
                                entity.Layer = newLayer;
                            }

                            // Apply Linetype
                            if (!string.IsNullOrEmpty(newLinetype))
                            {
                                entity.Linetype = newLinetype;
                            }

                            // Apply Color
                            if (!string.IsNullOrEmpty(newColor))
                            {
                                entity.Color = ParseColor(newColor);
                            }

                            // Apply Text-specific properties
                            if (entity is DBText dbText)
                            {
                                if (!string.IsNullOrEmpty(newTextStyle))
                                {
                                    var textStyleId = GetTextStyleId(tr, db, newTextStyle);
                                    if (!textStyleId.IsNull)
                                        dbText.TextStyleId = textStyleId;
                                }

                                if (newTextHeight.HasValue)
                                    dbText.Height = newTextHeight.Value;
                            }
                            else if (entity is MText mtext)
                            {
                                if (!string.IsNullOrEmpty(newTextStyle))
                                {
                                    var textStyleId = GetTextStyleId(tr, db, newTextStyle);
                                    if (!textStyleId.IsNull)
                                        mtext.TextStyleId = textStyleId;
                                }

                                if (newTextHeight.HasValue)
                                    mtext.TextHeight = newTextHeight.Value;
                            }
                            else if (entity is Dimension dim)
                            {
                                // Apply dimension style if specified
                                if (!string.IsNullOrEmpty(newTextStyle))
                                {
                                    var dimStyleId = GetDimStyleId(tr, db, newTextStyle);
                                    if (!dimStyleId.IsNull)
                                        dim.DimensionStyle = dimStyleId;
                                }
                            }

                            successCount++;
                        }
                        catch
                        {
                            // Skip entity if error occurs
                        }
                    }

                    tr.Commit();
                    doc.Editor.Regen();

                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Applied properties to {successCount} of {selected.Count} entities.", 
                                      "ACSE", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Show();
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Error applying properties:\n{ex.Message}", 
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Show();
                    });
                }
            }

            private Autodesk.AutoCAD.Colors.Color ParseColor(string colorName)
            {
                return colorName.ToLower() switch
                {
                    "bylayer" => Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256),
                    "byblock" => Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByBlock, 0),
                    "red" => Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1),
                    "yellow" => Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 2),
                    "green" => Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 3),
                    "cyan" => Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 4),
                    "blue" => Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 5),
                    "magenta" => Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 6),
                    "white" => Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 7),
                    _ => Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256)
                };
            }

            private ObjectId GetTextStyleId(Transaction tr, Database db, string styleName)
            {
                var textStyleTable = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                return textStyleTable.Has(styleName) ? textStyleTable[styleName] : ObjectId.Null;
            }

            private ObjectId GetDimStyleId(Transaction tr, Database db, string styleName)
            {
                var dimStyleTable = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
                return dimStyleTable.Has(styleName) ? dimStyleTable[styleName] : ObjectId.Null;
            }

        private void ApplySelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = _violations.Where(v => v.IsSelected).ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show("No violations selected! Check the boxes next to violations you want to fix.", 
                              "ACSE", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Apply automatic fixes to {selected.Count} selected violations?", 
                                       "ACSE", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ApplyAutomaticFixes(selected);
            }
        }

        private void ApplyAll_Click(object sender, RoutedEventArgs e)
        {
            if (_violations.Count == 0)
            {
                MessageBox.Show("No violations to fix! Scan the drawing first.", 
                              "ACSE", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Apply automatic fixes to ALL {_violations.Count} violations?", 
                                       "ACSE - Fix All", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Select all
                foreach (var v in _violations)
                    v.IsSelected = true;

                ApplyAutomaticFixes(_violations.ToList());
            }
        }

        private void ApplyAutomaticFixes(List<ViolationViewModel> violations)
        {
            AcadApp.Idle += (s, ev) =>
            {
                AcadApp.Idle -= (s2, ev2) => { };
                ApplyAutomaticFixesOnIdle(violations);
            };

            this.Hide();
        }

        private void ApplyAutomaticFixesOnIdle(List<ViolationViewModel> violations)
        {
            try
            {
                var doc = AcadApp.DocumentManager.MdiActiveDocument;
                var db = doc.Database;
                int successCount = 0;

                using var tr = db.TransactionManager.StartTransaction();

                foreach (var vm in violations)
                {
                    try
                    {
                        var v = vm.Violation;

                        if (v.EntityId.IsNull || !v.EntityId.IsValid || !v.AutoFixable)
                            continue;

                        var entity = tr.GetObject(v.EntityId, OpenMode.ForWrite) as Entity;
                        if (entity == null) continue;

                        // Apply fix based on violation type
                        switch (v.Type)
                        {
                            case ViolationType.Layer:
                                if (!string.IsNullOrEmpty(v.Expected))
                                    entity.Layer = v.Expected;
                                successCount++;
                                break;

                            case ViolationType.Linetype:
                                if (!string.IsNullOrEmpty(v.Expected))
                                    entity.Linetype = v.Expected;
                                successCount++;
                                break;

                            case ViolationType.TextStyle:
                                if (entity is DBText dbText)
                                {
                                    var styleId = GetTextStyleId(tr, db, v.Expected ?? _standards?.RequiredTextStyle ?? "Standard");
                                    if (!styleId.IsNull)
                                    {
                                        dbText.TextStyleId = styleId;
                                        successCount++;
                                    }
                                }
                                else if (entity is MText mtext)
                                {
                                    var styleId = GetTextStyleId(tr, db, v.Expected ?? _standards?.RequiredTextStyle ?? "Standard");
                                    if (!styleId.IsNull)
                                    {
                                        mtext.TextStyleId = styleId;
                                        successCount++;
                                    }
                                }
                                break;

                            case ViolationType.DimStyle:
                                if (entity is Dimension dim && !string.IsNullOrEmpty(v.Expected))
                                {
                                    var dimStyleId = GetDimStyleId(tr, db, v.Expected);
                                    if (!dimStyleId.IsNull)
                                    {
                                        dim.DimensionStyle = dimStyleId;
                                        successCount++;
                                    }
                                }
                                break;
                        }
                    }
                    catch
                    {
                        // Skip entity if error occurs
                    }
                }

                tr.Commit();
                doc.Editor.Regen();

                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Applied automatic fixes to {successCount} of {violations.Count} entities.\n\nClick 'Rescan Drawing' to verify results.", 
                                  "ACSE - Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Show();

                    // Auto-rescan
                    RefreshStatus_Click(this, new RoutedEventArgs());                    RefreshStatus_Click(this, new RoutedEventArgs());
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error applying fixes:\n{ex.Message}", 
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Show();
                });
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ==================== NEW FEATURES ====================

        private void ApplySafeFixes_Click(object sender, RoutedEventArgs e)
        {
            if (_standards == null || _violations.Count == 0)
            {
                MessageBox.Show("Load standards and scan drawing first!", "ACSE", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Apply ONLY Safe fixes (90%+ confidence)?\n\nThis will fix violations marked as 'Safe' automatically.", 
                "ACSE - Smart Auto-Fix", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ApplySmartFixes(SmartFixMode.SafeOnly);
            }
        }

        private void ApplyRecommendedFixes_Click(object sender, RoutedEventArgs e)
        {
            if (_standards == null || _violations.Count == 0)
            {
                MessageBox.Show("Load standards and scan drawing first!", "ACSE", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Apply Safe + Recommended fixes?\n\nThis will skip only 'Manual Review' violations.", 
                "ACSE - Smart Auto-Fix", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ApplySmartFixes(SmartFixMode.SafeAndRecommended);
            }
        }

        private void ApplySmartFixes(SmartFixMode mode)
        {
            try
            {
                ScanProgressBar.Visibility = System.Windows.Visibility.Visible;

                var selectedViolations = _violations.Select(vm => vm.Violation).ToList();
                var result = SmartFixEngine.ApplySmartFixes(selectedViolations, _standards!, mode);

                ScanProgressBar.Visibility = System.Windows.Visibility.Collapsed;

                string modeText = mode switch
                {
                    SmartFixMode.SafeOnly => "Safe Fixes Only",
                    SmartFixMode.SafeAndRecommended => "Safe + Recommended Fixes",
                    _ => "All Fixes"
                };

                string summary = $"✅ Smart Auto-Fix Complete!\n\n" +
                                $"Mode: {modeText}\n\n" +
                                $"Applied: {result.AppliedCount}\n" +
                                $"Skipped: {result.SkippedCount}\n" +
                                $"Failed: {result.FailedCount}\n\n" +
                                $"Success Rate: {result.SuccessRate:F1}%\n\n" +
                                $"Click 'Rescan Drawing' to verify results.";

                MessageBox.Show(summary, "ACSE - Smart Fix Results", MessageBoxButton.OK, MessageBoxImage.Information);

                // Auto-rescan
                RefreshStatus_Click(this, new RoutedEventArgs());
            }
            catch (Exception ex)
            {
                ScanProgressBar.Visibility = System.Windows.Visibility.Collapsed;
                MessageBox.Show($"Smart fix failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            // ✅ FIXED: Select only VISIBLE/FILTERED violations
            foreach (var item in _violationsView)
            {
                if (item is ViolationViewModel vm)
                {
                    vm.IsSelected = true;
                }
            }
            UpdateSelectionCount();
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            // ✅ FIXED: Deselect only VISIBLE/FILTERED violations
            foreach (var item in _violationsView)
            {
                if (item is ViolationViewModel vm)
                {
                    vm.IsSelected = false;
                }
            }
            UpdateSelectionCount();
        }

        private void FilterTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _violationsView.Refresh();
            UpdateSelectionCount();
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterTextBox.Text = string.Empty;
        }

        private bool FilterViolations(object item)
        {
            if (string.IsNullOrWhiteSpace(FilterTextBox?.Text))
                return true;

            if (item is not ViolationViewModel vm)
                return false;

            string filter = FilterTextBox.Text.ToLower();

            return vm.Type.ToLower().Contains(filter) ||
                   vm.EntityName.ToLower().Contains(filter) ||
                   vm.Layer.ToLower().Contains(filter) ||
                   vm.Message.ToLower().Contains(filter) ||
                   vm.Actual.ToLower().Contains(filter) ||
                   vm.Expected.ToLower().Contains(filter);
        }

        private void ExportCSV_Click(object sender, RoutedEventArgs e)
        {
            if (_violations.Count == 0)
            {
                MessageBox.Show("No violations to export! Scan the drawing first.", 
                              "ACSE", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Export Violations to CSV",
                DefaultExt = "csv",
                FileName = $"ACSE_Violations_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    ExportViolationsToCSV(saveFileDialog.FileName);
                    MessageBox.Show($"Exported {_violations.Count} violations to:\n\n{saveFileDialog.FileName}", 
                                  "ACSE - Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed:\n{ex.Message}", 
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportViolationsToCSV(string filePath)
        {
            var csv = new StringBuilder();

            // Header
            csv.AppendLine("Type,Entity,Layer,Issue,Current Value,Expected Value,Rule ID,Auto-Fixable");

            // Data rows
            foreach (var v in _violations)
            {
                csv.AppendLine($"\"{v.Type}\",\"{v.EntityName}\",\"{v.Layer}\",\"{EscapeCSV(v.Message)}\",\"{v.Actual}\",\"{v.Expected}\",\"{v.RuleId}\",\"{v.AutoFixable}\"");
            }

            File.WriteAllText(filePath, csv.ToString());
        }

        private string EscapeCSV(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Escape quotes and commas for CSV
            return value.Replace("\"", "\"\"");
        }
    }
}
// Bug fix applied - filter selection 2026-04-03
