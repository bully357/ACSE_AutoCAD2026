using System;
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
        this.layersFound = new HashSet<string>(layersFound, StringComparer.OrdinalIgnoreCase);
    }

    // AutoCAD symbol-table names are case-insensitive (e.g. "STANDARD" == "Standard"),
    // so all "found" sets use OrdinalIgnoreCase to prevent duplicates and to align
    // with the case-insensitive comparisons used by StandardsModel.
    private readonly HashSet<string> layersFound = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> GetLayersFound()
    {
        return layersFound;
    }

    public HashSet<string> LinetypesFound { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> TextStylesFound { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> DimStylesFound { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<Violation> Violations { get; } = [];

    public int TotalViolations => Violations.Count;
    public int LinetypeViolations => Violations.FindAll(v => v.Type == ViolationType.Linetype).Count;
    public int LayerViolations => Violations.FindAll(v => v.Type == ViolationType.Layer).Count;
    public int TextStyleViolations => Violations.FindAll(v => v.Type == ViolationType.TextStyle).Count;
    public int DimStyleViolations => Violations.FindAll(v => v.Type == ViolationType.DimStyle).Count;
}
