using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using ACSE.AutoCAD2026.Compliance;
using ACSE.AutoCAD2026.Standards;

namespace ACSE.AutoCAD2026.UI
{
    /// <summary>
    /// Interactive compliance scanner window with user customization support.
    /// </summary>
    public partial class InteractiveScanWindow : Window
    {
        private ObservableCollection<ViolationViewModel> _violations = new();
        private StandardsModel? _currentStandards;
        private ScanResult? _lastScanResult;

        public InteractiveScanWindow()
        {
            InitializeComponent();
            ViolationsGrid.ItemsSource = _violations;
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc == null)
                {
                    MessageBox.Show("No active AutoCAD document.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var ed = doc.Editor;
                ed.WriteMessage("\n========================================");
                ed.WriteMessage("\n*** ACSE INTERACTIVE SCAN ***");
                ed.WriteMessage("\n========================================");

                // Load standards from template
                string templatePath = TemplatePathTextBox.Text;
                if (string.IsNullOrEmpty(templatePath) || !System.IO.File.Exists(templatePath))
                {
                    MessageBox.Show($"Template not found: {templatePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _currentStandards = TemplateStandardsExtractor.LoadFromTemplate(templatePath);
                ed.WriteMessage($"\nLoaded standards from: {templatePath}");

                // Perform scan
                _lastScanResult = EntityScanner.Scan(_currentStandards);

                // Populate violations into ViewModels
                _violations.Clear();
                
                var db = doc.Database;
                var availableFonts = InteractiveFixEngine.GetAvailableFonts(db);
                var availableLayers = InteractiveFixEngine.GetAvailableLayers(db);
                var availableTextStyles = InteractiveFixEngine.GetAvailableTextStyles(db);
                var availableDimStyles = InteractiveFixEngine.GetAvailableDimStyles(db);

                foreach (var v in _lastScanResult.Violations)
                {
                    var vm = new ViolationViewModel(v)
                    {
                        AvailableFonts = availableFonts,
                        AvailableLayers = availableLayers,
                        AvailableTextStyles = availableTextStyles,
                        AvailableDimStyles = availableDimStyles
                    };
                    _violations.Add(vm);
                }

                // Update UI
                UpdateSummary();
                ApplyFilters();

                ed.WriteMessage($"\nScan complete: {_violations.Count} violations found.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Scan failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FixSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStandards == null || _violations.Count == 0)
            {
                MessageBox.Show("Please run a scan first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var selected = _violations.Where(v => v.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("No violations selected for fixing.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Apply fixes to {selected.Count} selected violations?\n\nThis action cannot be undone (use AutoCAD's UNDO if needed).",
                "Confirm Fix",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var fixResult = InteractiveFixEngine.ApplyCustomFixes(selected, _currentStandards, previewMode: false);

                MessageBox.Show(
                    $"Fixed: {fixResult.FixedCount}\nFailed: {fixResult.FailedCount}",
                    "Fix Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Remove fixed violations from list
                foreach (var vm in selected.Where(v => v.Violation.AutoFixable))
                {
                    _violations.Remove(vm);
                }

                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fix failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FixAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStandards == null || _violations.Count == 0)
            {
                MessageBox.Show("Please run a scan first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var autoFixable = _violations.Where(v => v.AutoFixable).ToList();
            if (autoFixable.Count == 0)
            {
                MessageBox.Show("No auto-fixable violations found.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Apply automatic fixes to all {autoFixable.Count} auto-fixable violations?\n\nThis action cannot be undone.",
                "Confirm Fix All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                // Select all auto-fixable
                foreach (var vm in autoFixable)
                    vm.IsSelected = true;

                var fixResult = InteractiveFixEngine.ApplyCustomFixes(autoFixable, _currentStandards, previewMode: false);

                MessageBox.Show(
                    $"Fixed: {fixResult.FixedCount}\nFailed: {fixResult.FailedCount}",
                    "Fix All Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Remove fixed from list
                foreach (var vm in autoFixable)
                {
                    _violations.Remove(vm);
                }

                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fix failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreviewSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStandards == null)
            {
                MessageBox.Show("Please run a scan first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var selected = _violations.Where(v => v.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("No violations selected for preview.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                var ed = doc.Editor;

                ed.WriteMessage("\n========================================");
                ed.WriteMessage("\n*** PREVIEW MODE ***");
                ed.WriteMessage("\n========================================");

                InteractiveFixEngine.ApplyCustomFixes(selected, _currentStandards, previewMode: true);

                ed.WriteMessage($"\n{selected.Count} entities would be fixed.");
                ed.WriteMessage("\nClick 'Fix Selected' to apply changes.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Preview failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RescanButton_Click(object sender, RoutedEventArgs e)
        {
            ScanButton_Click(sender, e);
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var vm in _violations)
                vm.IsSelected = true;
            UpdateSummary();
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var vm in _violations)
                vm.IsSelected = false;
            UpdateSummary();
        }

        private void InvertSelection_Click(object sender, RoutedEventArgs e)
        {
            foreach (var vm in _violations)
                vm.IsSelected = !vm.IsSelected;
            UpdateSummary();
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            // Guard: Ensure UI elements are initialized
            if (ShowTextStyleCheckBox == null || ShowDimStyleCheckBox == null || 
                ShowLayerCheckBox == null || ShowLinetypeCheckBox == null || 
                ViolationsGrid == null)
            {
                return; // UI not ready yet
            }

            // Filter violations based on checkboxes
            var filtered = _violations.AsEnumerable();

            if (!ShowTextStyleCheckBox.IsChecked.GetValueOrDefault())
                filtered = filtered.Where(v => v.Type != "TextStyle" && v.Type != "TextFont" && v.Type != "TextSize");

            if (!ShowDimStyleCheckBox.IsChecked.GetValueOrDefault())
                filtered = filtered.Where(v => v.Type != "DimStyle");

            if (!ShowLayerCheckBox.IsChecked.GetValueOrDefault())
                filtered = filtered.Where(v => v.Type != "Layer");

            if (!ShowLinetypeCheckBox.IsChecked.GetValueOrDefault())
                filtered = filtered.Where(v => v.Type != "Linetype");

            ViolationsGrid.ItemsSource = filtered.ToList();
            UpdateSummary();
        }

        private void ViolationsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            // Guard: Ensure UI elements are initialized
            if (DrawingTextBlock == null || TotalsTextBlock == null || 
                SelectedCountTextBlock == null || ScoreTextBlock == null)
            {
                return; // UI not ready yet
            }

            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                DrawingTextBlock.Text = System.IO.Path.GetFileName(doc.Name);
            }

            TotalsTextBlock.Text = $"{_violations.Count} total";
            SelectedCountTextBlock.Text = $"{_violations.Count(v => v.IsSelected)} selected";

            if (_lastScanResult != null)
            {
                var score = ScoringEngine.CalculateScore(_lastScanResult);
                ScoreTextBlock.Text = $"{score:F1}%";
                ScoreTextBlock.Foreground = score >= 90 ? System.Windows.Media.Brushes.Green :
                                           score >= 70 ? System.Windows.Media.Brushes.Orange :
                                           System.Windows.Media.Brushes.Red;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
