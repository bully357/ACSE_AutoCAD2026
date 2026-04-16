using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Runtime;

// COM visibility
[assembly: ComVisible(false)]

// Register the main extension application
[assembly: ExtensionApplication(typeof(ACSE.AutoCAD2026.AcsePlugin))]

// Register all command classes
[assembly: CommandClass(typeof(ACSE.AutoCAD2026.Commands.ComplianceCommands))]
[assembly: CommandClass(typeof(ACSE.AutoCAD2026.Commands.TemplateCommands))]
[assembly: CommandClass(typeof(ACSE.AutoCAD2026.Commands.ExtractCommands))]
[assembly: CommandClass(typeof(ACSE.AutoCAD2026.Tools.DimensionStyleManager))]
[assembly: CommandClass(typeof(ACSE.AutoCAD2026.Commands.WorkflowCommands))]
