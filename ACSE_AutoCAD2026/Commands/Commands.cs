using System;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;

namespace ACSE.AutoCAD2026.Commands

{
    public partial class ComplianceCommands
    {
        // Singleton instance - prevents GC from collecting command delegates
        private static readonly ComplianceCommands _instance = new ComplianceCommands();

        // Pin instance to prevent GC collection
        private static GCHandle _pinnedInstance;

        // Static constructor to pin the instance
        static ComplianceCommands()
        {
            _pinnedInstance = GCHandle.Alloc(_instance, GCHandleType.Normal);
        }

        // Public constructor required by AutoCAD
        public ComplianceCommands()
        {
        }

        [CommandMethod("ACSE_TEST")]
        public void TestCommand()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("No active document.");
                return;
            }

            doc.Editor.WriteMessage("\n========================================");
            doc.Editor.WriteMessage("\n*** ACSE TEST COMMAND ***");
            doc.Editor.WriteMessage("\n========================================");
            doc.Editor.WriteMessage("\nDLL loaded successfully!");
            doc.Editor.WriteMessage("\nDrawing: " + doc.Name);
            doc.Editor.WriteMessage("\nDatabase valid: " + (doc.Database != null));
            doc.Editor.WriteMessage("\n========================================");

            Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("ACSE DLL is working!\n\nCheck command line for details.");
        }

        [CommandMethod("ACSE_RUN_SIMPLE")]
        public void RunCompliance()
        {
            // 1) Popup you cannot miss
            Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog(
                "ACSE_RUN_SIMPLE executed (popup).");

            // 2) Command line output (if a document exists)
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog(
                    "No active document (MdiActiveDocument is null).");
                return;
            }

            doc.Editor.WriteMessage("\nACSE_RUN_SIMPLE executed (command line).");
        }
    }
}
