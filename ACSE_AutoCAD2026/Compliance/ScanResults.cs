using System.Collections.Generic;

namespace ACSE.AutoCAD2026.Compliance;

public record ScanResult
{
    public ScanResult() { }

    public ScanResult(int totalEntities) => SetTotalEntities(totalEntities);

    private int totalEntities;

    public int GetTotalEntities()
    {
        return totalEntities;
    }

    public void SetTotalEntities(int value)
    {
        totalEntities = value;
    }

    public ScanResult(HashSet<string> layersFound)
    {
        this.layersFound = layersFound;
    }

    private readonly HashSet<string> layersFound = [];

    public HashSet<string> GetLayersFound()
    {
        return layersFound;
    }

    public HashSet<string> LinetypesFound { get; } = [];
    public HashSet<string> TextStylesFound { get; } = [];
    public HashSet<string> DimStylesFound { get; } = [];

    public List<Violation> Violations { get; } = [];

    public int TotalViolations => Violations.Count;
    public int LinetypeViolations => Violations.FindAll(v => v.Type == ViolationType.Linetype).Count;
    public int LayerViolations => Violations.FindAll(v => v.Type == ViolationType.Layer).Count;
    public int TextStyleViolations => Violations.FindAll(v => v.Type == ViolationType.TextStyle).Count;
    public int DimStyleViolations => Violations.FindAll(v => v.Type == ViolationType.DimStyle).Count;
}
