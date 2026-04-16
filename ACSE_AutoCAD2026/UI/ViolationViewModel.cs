using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ACSE.AutoCAD2026.Compliance;

namespace ACSE.AutoCAD2026.UI
{
    /// <summary>
    /// ViewModel wrapper for Violation to support UI binding and user customization.
    /// </summary>
    public class ViolationViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string? _customFont;
        private double? _customSize;
        private bool? _customAnnotative;
        private string? _customLayer;
        private string? _customStyle;

        public Violation Violation { get; }

        // User selection
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        // Available options (populated from drawing)
        public List<string> AvailableFonts { get; set; } = new();
        public List<string> AvailableLayers { get; set; } = new();
        public List<string> AvailableTextStyles { get; set; } = new();
        public List<string> AvailableDimStyles { get; set; } = new();

        // User-customizable values (null = use standard)
        public string? CustomFont
        {
            get => _customFont;
            set { _customFont = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayFont)); }
        }

        public double? CustomSize
        {
            get => _customSize;
            set { _customSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplaySize)); }
        }

        public bool? CustomAnnotative
        {
            get => _customAnnotative;
            set { _customAnnotative = value; OnPropertyChanged(); }
        }

        public string? CustomLayer
        {
            get => _customLayer;
            set { _customLayer = value; OnPropertyChanged(); }
        }

        public string? CustomStyle
        {
            get => _customStyle;
            set { _customStyle = value; OnPropertyChanged(); }
        }

        // Display values (show custom or expected)
        public string DisplayFont => CustomFont ?? Violation.Expected ?? "(none)";
        public string DisplaySize => CustomSize?.ToString("F3") ?? Violation.Expected ?? "(none)";

        // Quick access to violation properties
        public string Type => Violation.Type.ToString();
        public string RuleId => Violation.RuleId;
        public string Message => Violation.Message;
        public string EntityName => Violation.EntityName;
        public string Layer => Violation.Layer;
        public string Expected => Violation.Expected ?? "";
        public string Actual => Violation.Actual ?? "";
        public bool AutoFixable => Violation.AutoFixable;

        // Smart Auto-Fix properties
        public string ConfidenceText => $"{Violation.Confidence * 100:F0}%";
        public string RecommendationText => Violation.Recommendation.ToString();
        public string SuggestedAction => Violation.SuggestedAction ?? "";

        public System.Windows.Media.Color RecommendationColor => Violation.Recommendation switch
        {
            FixRecommendation.Safe => System.Windows.Media.Colors.Green,
            FixRecommendation.Recommended => System.Windows.Media.Colors.Orange,
            FixRecommendation.ManualReview => System.Windows.Media.Colors.Red,
            _ => System.Windows.Media.Colors.Gray
        };

        public ViolationViewModel(Violation violation)
        {
            Violation = violation;
            _isSelected = violation.AutoFixable; // Auto-select fixable violations
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
