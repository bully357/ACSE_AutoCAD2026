using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using ACSE.AutoCAD2026.Compliance;

namespace ACSE.AutoCAD2026.UI
{
    public partial class GlobalTextModifierWindow : Window
    {
        private ObservableCollection<TextEntityViewModel> _textEntities = new ObservableCollection<TextEntityViewModel>();

        public GlobalTextModifierWindow()
        {
            InitializeComponent();

            // CRITICAL: Wait for window to load before accessing UI elements
            this.Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Safe to access UI elements now
            if (ResultsDataGrid != null)
            {
                ResultsDataGrid.ItemsSource = _textEntities;
            }

            // Load options on AutoCAD thread using Application.Idle
            Autodesk.AutoCAD.ApplicationServices.Application.Idle += LoadOptionsOnIdle;
        }

        private void LoadOptionsOnIdle(object sender, EventArgs e)
        {
            // Remove handler immediately (one-time execution)
            Autodesk.AutoCAD.ApplicationServices.Application.Idle -= LoadOptionsOnIdle;

            LoadAvailableOptions();
        }

        private void LoadAvailableOptions()
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                var db = doc.Database;

                // Load text styles (we're on AutoCAD thread now - safe!)
                var textStyles = GlobalTextModifier.GetAvailableTextStyles(db);
                var layers = GlobalTextModifier.GetAvailableLayers(db);

                // Update UI on UI thread
                this.Dispatcher.Invoke(() =>
                {
                    // Guard: Ensure UI elements are initialized
                    if (FilterTextStyleComboBox == null || NewTextStyleComboBox == null ||
                        FilterLayerComboBox == null || NewLayerComboBox == null ||
                        NewHorizontalModeComboBox == null || NewVerticalModeComboBox == null ||
                        NewAttachmentComboBox == null)
                    {
                        return; // UI not ready yet
                    }

                    FilterTextStyleComboBox.ItemsSource = textStyles;
                    NewTextStyleComboBox.ItemsSource = textStyles;

                    FilterLayerComboBox.ItemsSource = layers;
                    NewLayerComboBox.ItemsSource = layers;

                    // Set default selections for justification
                    NewHorizontalModeComboBox.SelectedIndex = 0;
                    NewVerticalModeComboBox.SelectedIndex = 0;
                    NewAttachmentComboBox.SelectedIndex = 4; // Middle Center
                });
            }
            catch (System.Exception ex)
            {
                // Silent failure during initialization - user can try scanning anyway
                System.Diagnostics.Debug.WriteLine($"LoadAvailableOptions error: {ex.Message}");
            }
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            // Guard: Ensure UI elements are initialized
            if (StatusTextBlock == null || StatusDetailTextBlock == null || 
                ResultsSummaryTextBlock == null || FilterTextStyleComboBox == null)
            {
                return; // UI not ready yet
            }

            StatusTextBlock.Text = "Scanning...";
            StatusDetailTextBlock.Text = "Please wait...";

            // Build filter criteria
            var filter = new TextFilterCriteria();

            if (!string.IsNullOrWhiteSpace(FilterTextStyleComboBox.Text))
                filter.TextStyle = FilterTextStyleComboBox.Text;

            if (!string.IsNullOrWhiteSpace(FilterLayerComboBox.Text))
                filter.Layer = FilterLayerComboBox.Text;

            if (!string.IsNullOrWhiteSpace(FilterFontTextBox.Text))
                filter.FontFile = FilterFontTextBox.Text;

            if (FilterAnnotativeCheckBox.IsChecked == true)
                filter.IsAnnotative = true;
            else if (FilterAnnotativeCheckBox.IsChecked == false)
                filter.IsAnnotative = false;

            if (double.TryParse(FilterMinHeightTextBox.Text, out double minHeight))
                filter.MinHeight = minHeight;

            if (double.TryParse(FilterMaxHeightTextBox.Text, out double maxHeight))
                filter.MaxHeight = maxHeight;

            var hasAnyFilter = filter.TextStyle != null || filter.Layer != null || 
                              filter.FontFile != null || filter.IsAnnotative != null ||
                              filter.MinHeight != null || filter.MaxHeight != null;

            // CRITICAL: Use Application.Idle to execute scan on AutoCAD's application thread
            // This is the ONLY safe way to access database from WPF UI thread!
            var scanButton = sender as System.Windows.Controls.Button;
            if (scanButton != null)
                scanButton.IsEnabled = false;

            // Define handler as variable so we can properly remove it
            EventHandler idleHandler = null;
            idleHandler = (s, ev) =>
            {
                // Remove this event handler immediately (one-time execution)
                Autodesk.AutoCAD.ApplicationServices.Application.Idle -= idleHandler;

                try
                {
                    // NOW we're on AutoCAD's application thread - safe to access database!
                    var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                    if (doc == null)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("No active document.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            StatusTextBlock.Text = "Scan failed";
                            if (scanButton != null) scanButton.IsEnabled = true;
                        });
                        return;
                    }

                    // Execute scan (we're on AutoCAD thread here)
                    doc.Editor.WriteMessage("\n[SCAN] Starting scan...");
                    var results = GlobalTextModifier.ScanTextEntities(hasAnyFilter ? filter : null);
                    doc.Editor.WriteMessage($"\n[SCAN] Scan complete: {results?.Count ?? 0} entities found");

                    // Update UI on UI thread (NO AutoCAD API calls inside!)
                    this.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            // Guard: Check if window is still valid
                            if (_textEntities == null || ResultsSummaryTextBlock == null || StatusTextBlock == null)
                            {
                                return;
                            }

                            _textEntities.Clear();

                            if (results != null && results.Count > 0)
                            {
                                foreach (var info in results)
                                {
                                    try
                                    {
                                        var vm = new TextEntityViewModel(info);
                                        _textEntities.Add(vm);
                                    }
                                    catch (System.Exception ex)
                                    {
                                        // Skip problematic entity but log it
                                        System.Diagnostics.Debug.WriteLine($"Error adding entity to view: {ex.Message}");
                                    }
                                }
                            }

                            ResultsSummaryTextBlock.Text = $"Found {_textEntities.Count} text entities";
                            StatusTextBlock.Text = "Scan complete";
                            StatusDetailTextBlock.Text = $"{_textEntities.Count} entities found";
                        }
                        catch (System.Exception ex)
                        {
                            // Catch any UI update errors
                            MessageBox.Show($"Error updating results: {ex.Message}\n\nStack:\n{ex.StackTrace}",
                                          "UI Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        finally
                        {
                            if (scanButton != null)
                                scanButton.IsEnabled = true;
                        }
                    });

                    doc.Editor.WriteMessage($"\nScan complete: {_textEntities.Count} text entities found.");
                }
                catch (System.Exception ex)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Scan failed: {ex.Message}\n\nStack:\n{ex.StackTrace}", "Scan Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusTextBlock.Text = "Scan failed";
                        StatusDetailTextBlock.Text = ex.Message;
                        if (scanButton != null)
                            scanButton.IsEnabled = true;
                    });
                }
            };

            // Register the handler
            Autodesk.AutoCAD.ApplicationServices.Application.Idle += idleHandler;
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            // Guard: Check selected entities
            var selected = _textEntities.Where(vm => vm.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("No entities selected.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Guard: Check if any modifications are selected
            var modifications = BuildModificationProperties();
            if (!HasAnyModifications(modifications))
            {
                MessageBox.Show("No properties selected for modification.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Disable preview button during operation
            var previewButton = sender as System.Windows.Controls.Button;
            if (previewButton != null)
                previewButton.IsEnabled = false;

            StatusTextBlock.Text = "Previewing...";
            StatusDetailTextBlock.Text = "Please wait...";

            // Get entity IDs before switching threads
            var entityIds = selected.Select(vm => vm.TextInfo.EntityId).ToList();

            // Use Application.Idle to execute on AutoCAD thread
            EventHandler idleHandler = null;
            idleHandler = (s, ev) =>
            {
                // Remove handler immediately
                Autodesk.AutoCAD.ApplicationServices.Application.Idle -= idleHandler;

                try
                {
                    var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                    if (doc == null)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("No active document.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            if (previewButton != null) previewButton.IsEnabled = true;
                        });
                        return;
                    }

                    // Execute preview (on AutoCAD thread - previewMode = true means no actual changes)
                    var result = GlobalTextModifier.ApplyModifications(entityIds, modifications, previewMode: true);

                    // Update UI on UI thread
                    this.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var summary = $"Preview: {result.ModifiedCount} entities will be modified.\n\n";
                            summary += "Changes to apply:\n";

                            if (ChangeTextStyleCheckBox.IsChecked == true)
                                summary += $"• Text Style → {NewTextStyleComboBox.Text}\n";
                            if (ChangeHeightCheckBox.IsChecked == true)
                                summary += $"• Height → {NewHeightTextBox.Text}\n";
                            if (ChangeLayerCheckBox.IsChecked == true)
                                summary += $"• Layer → {NewLayerComboBox.Text}\n";
                            if (ChangeRotationCheckBox.IsChecked == true)
                                summary += $"• Rotation → {NewRotationTextBox.Text}°\n";
                            if (ChangeWidthFactorCheckBox.IsChecked == true)
                                summary += $"• Width Factor → {NewWidthFactorTextBox.Text}\n";
                            if (ChangeAnnotativeCheckBox.IsChecked == true)
                                summary += $"• Annotative → {NewAnnotativeComboBox.Text}\n";
                            if (ChangeJustificationCheckBox.IsChecked == true)
                                summary += $"• Justification → H:{NewHorizontalModeComboBox.Text}, V:{NewVerticalModeComboBox.Text}\n";
                            if (ChangeAttachmentCheckBox.IsChecked == true)
                                summary += $"• Attachment → {NewAttachmentComboBox.Text}\n";

                            PreviewTextBlock.Text = summary;
                            StatusTextBlock.Text = "Preview complete";
                            StatusDetailTextBlock.Text = $"{result.ModifiedCount} entities will be modified";

                            doc.Editor.WriteMessage($"\nPreview: {result.ModifiedCount} entities will be modified.");
                        }
                        finally
                        {
                            if (previewButton != null)
                                previewButton.IsEnabled = true;
                        }
                    });
                }
                catch (System.Exception ex)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Preview failed: {ex.Message}\n\nStack:\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        if (previewButton != null)
                            previewButton.IsEnabled = true;
                    });
                }
            };

            // Register handler
            Autodesk.AutoCAD.ApplicationServices.Application.Idle += idleHandler;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            // Guard: Check selected entities
            var selected = _textEntities.Where(vm => vm.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("No entities selected.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Guard: Check if any modifications are selected
            var modifications = BuildModificationProperties();
            if (!HasAnyModifications(modifications))
            {
                MessageBox.Show("No properties selected for modification.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Confirm
            var result = MessageBox.Show(
                $"Apply changes to {selected.Count} selected entities?\n\nThis action cannot be undone (use AutoCAD's UNDO if needed).",
                "Confirm Apply",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            StatusTextBlock.Text = "Applying changes...";
            StatusDetailTextBlock.Text = "Please wait...";

            // Disable apply button during operation
            var applyButton = sender as System.Windows.Controls.Button;
            if (applyButton != null)
                applyButton.IsEnabled = false;

            // Get entity IDs before switching threads
            var entityIds = selected.Select(vm => vm.TextInfo.EntityId).ToList();

            // Use Application.Idle to execute on AutoCAD thread
            EventHandler idleHandler = null;
            idleHandler = (s, ev) =>
            {
                // Remove handler immediately
                Autodesk.AutoCAD.ApplicationServices.Application.Idle -= idleHandler;

                try
                {
                    var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                    if (doc == null)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("No active document.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            StatusTextBlock.Text = "Apply failed";
                            if (applyButton != null) applyButton.IsEnabled = true;
                        });
                        return;
                    }

                    // Execute apply (on AutoCAD thread - safe to lock document)
                    doc.Editor.WriteMessage("\n[APPLY] Starting modifications...");
                    var modResult = GlobalTextModifier.ApplyModifications(entityIds, modifications, previewMode: false);
                    doc.Editor.WriteMessage($"\n[APPLY] Modified {modResult.ModifiedCount} entities.");

                    // Update UI on UI thread
                    this.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            if (modResult.Success)
                            {
                                MessageBox.Show(
                                    $"Successfully modified {modResult.ModifiedCount} entities!",
                                    "Success",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);

                                StatusTextBlock.Text = "Changes applied";
                                StatusDetailTextBlock.Text = $"{modResult.ModifiedCount} entities modified";

                                // Rescan to update display
                                ScanButton_Click(sender, e);
                            }
                            else
                            {
                                var errorMsg = $"Modified: {modResult.ModifiedCount}\nFailed: {modResult.FailedCount}";
                                if (!string.IsNullOrEmpty(modResult.ErrorMessage))
                                    errorMsg += $"\n\nError: {modResult.ErrorMessage}";
                                if (modResult.Errors.Count > 0)
                                    errorMsg += $"\n\nErrors:\n{string.Join("\n", modResult.Errors.Take(10))}";

                                MessageBox.Show(errorMsg, "Partial Success", MessageBoxButton.OK, MessageBoxImage.Warning);

                                StatusTextBlock.Text = "Apply completed with errors";
                                StatusDetailTextBlock.Text = $"{modResult.ModifiedCount} modified, {modResult.FailedCount} failed";
                            }
                        }
                        finally
                        {
                            if (applyButton != null)
                                applyButton.IsEnabled = true;
                        }
                    });
                }
                catch (System.Exception ex)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Apply failed: {ex.Message}\n\nStack:\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusTextBlock.Text = "Apply failed";
                        StatusDetailTextBlock.Text = ex.Message;
                        if (applyButton != null)
                            applyButton.IsEnabled = true;
                    });
                }
            };

            // Register handler
            Autodesk.AutoCAD.ApplicationServices.Application.Idle += idleHandler;
        }

        private TextModificationProperties BuildModificationProperties()
        {
            var mods = new TextModificationProperties();

            if (ChangeTextStyleCheckBox.IsChecked == true && !string.IsNullOrWhiteSpace(NewTextStyleComboBox.Text))
                mods.NewTextStyle = NewTextStyleComboBox.Text;

            if (ChangeHeightCheckBox.IsChecked == true && double.TryParse(NewHeightTextBox.Text, out double height))
                mods.NewHeight = height;

            if (ChangeLayerCheckBox.IsChecked == true && !string.IsNullOrWhiteSpace(NewLayerComboBox.Text))
                mods.NewLayer = NewLayerComboBox.Text;

            if (ChangeRotationCheckBox.IsChecked == true && double.TryParse(NewRotationTextBox.Text, out double rotation))
                mods.NewRotation = rotation;

            if (ChangeWidthFactorCheckBox.IsChecked == true && double.TryParse(NewWidthFactorTextBox.Text, out double widthFactor))
                mods.NewWidthFactor = widthFactor;

            if (ChangeAnnotativeCheckBox.IsChecked == true && NewAnnotativeComboBox.SelectedItem != null)
            {
                var item = (ComboBoxItem)NewAnnotativeComboBox.SelectedItem;
                mods.NewAnnotative = item.Tag.ToString() == "True";
            }

            if (ChangeJustificationCheckBox.IsChecked == true)
            {
                if (NewHorizontalModeComboBox.SelectedIndex >= 0)
                    mods.NewHorizontalMode = (TextHorizontalMode)NewHorizontalModeComboBox.SelectedIndex;
                
                if (NewVerticalModeComboBox.SelectedIndex >= 0)
                    mods.NewVerticalMode = (TextVerticalMode)NewVerticalModeComboBox.SelectedIndex;
            }

            if (ChangeAttachmentCheckBox.IsChecked == true && NewAttachmentComboBox.SelectedIndex >= 0)
            {
                mods.NewAttachment = (AttachmentPoint)NewAttachmentComboBox.SelectedIndex;
            }

            return mods;
        }

        private bool HasAnyModifications(TextModificationProperties mods)
        {
            return mods.NewTextStyle != null || mods.NewHeight.HasValue || 
                   mods.NewLayer != null || mods.NewRotation.HasValue ||
                   mods.NewWidthFactor.HasValue || mods.NewAnnotative.HasValue ||
                   mods.NewHorizontalMode.HasValue || mods.NewVerticalMode.HasValue ||
                   mods.NewAttachment.HasValue;
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            // Guard: Ensure UI elements are initialized
            if (FilterTextStyleComboBox == null || FilterLayerComboBox == null ||
                FilterFontTextBox == null || FilterAnnotativeCheckBox == null ||
                FilterMinHeightTextBox == null || FilterMaxHeightTextBox == null)
            {
                return; // UI not ready yet
            }

            FilterTextStyleComboBox.SelectedIndex = -1;
            FilterTextStyleComboBox.Text = "";
            FilterLayerComboBox.SelectedIndex = -1;
            FilterLayerComboBox.Text = "";
            FilterFontTextBox.Text = "";
            FilterAnnotativeCheckBox.IsChecked = null;
            FilterMinHeightTextBox.Text = "";
            FilterMaxHeightTextBox.Text = "";
        }

        private void ResetModifications_Click(object sender, RoutedEventArgs e)
        {
            // Guard: Ensure UI elements are initialized
            if (ChangeTextStyleCheckBox == null || NewTextStyleComboBox == null ||
                PreviewTextBlock == null)
            {
                return; // UI not ready yet
            }

            ChangeTextStyleCheckBox.IsChecked = false;
            ChangeHeightCheckBox.IsChecked = false;
            ChangeLayerCheckBox.IsChecked = false;
            ChangeRotationCheckBox.IsChecked = false;
            ChangeWidthFactorCheckBox.IsChecked = false;
            ChangeAnnotativeCheckBox.IsChecked = false;
            ChangeJustificationCheckBox.IsChecked = false;
            ChangeAttachmentCheckBox.IsChecked = false;

            NewTextStyleComboBox.SelectedIndex = -1;
            NewLayerComboBox.SelectedIndex = -1;
            NewHeightTextBox.Text = "";
            NewRotationTextBox.Text = "";
            NewWidthFactorTextBox.Text = "";
            NewAnnotativeComboBox.SelectedIndex = -1;
            PreviewTextBlock.Text = "Preview cleared. Set new properties and click 'Preview' again.";
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var vm in _textEntities)
                vm.IsSelected = true;
            UpdateSelectionCount();
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var vm in _textEntities)
                vm.IsSelected = false;
            UpdateSelectionCount();
        }

        private void InvertSelection_Click(object sender, RoutedEventArgs e)
        {
            foreach (var vm in _textEntities)
                vm.IsSelected = !vm.IsSelected;
            UpdateSelectionCount();
        }

        private void ResultsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectionCount();
        }

        private void UpdateSelectionCount()
        {
            // Guard: Ensure UI element is initialized
            if (SelectionCountTextBlock == null)
            {
                return; // UI not ready yet
            }

            var count = _textEntities.Count(vm => vm.IsSelected);
            SelectionCountTextBlock.Text = $"{count} selected";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    /// <summary>
    /// ViewModel wrapper for TextEntityInfo to support UI binding.
    /// </summary>
    public class TextEntityViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isSelected;

        public TextEntityInfo TextInfo { get; }

        public TextEntityViewModel(TextEntityInfo textInfo)
        {
            TextInfo = textInfo ?? throw new ArgumentNullException(nameof(textInfo));
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        // Expose properties for data binding with null safety
        public string EntityType => TextInfo.EntityType ?? "Unknown";
        public string TextStyle => TextInfo.TextStyle ?? "Standard";
        public string Layer => TextInfo.Layer ?? "0";
        public double Height => TextInfo.Height;
        public double Rotation => TextInfo.Rotation;
        public double WidthFactor => TextInfo.WidthFactor;
        public bool IsAnnotative => TextInfo.IsAnnotative;
        public string TextString 
        {
            get
            {
                try
                {
                    var text = TextInfo.TextString ?? "";
                    return text.Length > 50 ? text.Substring(0, 50) + "..." : text;
                }
                catch
                {
                    return "(error reading text)";
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}
