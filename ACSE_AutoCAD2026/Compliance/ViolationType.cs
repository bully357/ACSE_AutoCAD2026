namespace ACSE.AutoCAD2026.Compliance
{
    public enum ViolationType
    {
        Layer,
        Linetype,
        TextStyle,
        DimStyle,
        Block,
        Xref,
        TextFont,        // Font name violations
        TextSize,        // Text height violations
        Annotative,      // Annotative property violations
        AnnotativeScale, // Annotative scale violations
        Other
    }
}
