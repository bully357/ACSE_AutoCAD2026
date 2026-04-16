using Autodesk.AutoCAD.DatabaseServices;

namespace ACSE.AutoCAD2026.Compliance
{
    public class Violation
    {
        public ViolationType Type { get; set; }
        public string RuleId { get; set; } = "";
        public string Message { get; set; } = "";
        public string Expected { get; set; } = "";
        public string Actual { get; set; } = "";
        public ObjectId EntityId { get; set; }
        public string EntityHandle { get; set; } = "";
        public string EntityName { get; set; } = "";
        public string Layer { get; set; } = "";
        public bool AutoFixable { get; set; }

        // Smart Auto-Fix properties
        public double Confidence { get; set; } = 0.85;
        public FixRecommendation Recommendation { get; set; } = FixRecommendation.ManualReview;
        public string SuggestedAction { get; set; } = "";
    }

    public enum FixRecommendation
    {
        Safe,           // Very safe to apply automatically (green)
        Recommended,    // Good to apply, but user should review (yellow)
        ManualReview    // User must decide (orange)
    }
}
