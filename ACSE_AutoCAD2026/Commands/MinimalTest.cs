using System;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;

namespace ACSE.AutoCAD2026.Commands
{
    public class MinimalTest
    {
        // Singleton instance - prevents GC from collecting the command delegates
        private static readonly MinimalTest _instance = new MinimalTest();

        // CRITICAL: Pin this instance to prevent ANY GC collection
        private static GCHandle _pinnedInstance;

        // CRITICAL: Public parameterless constructor - AutoCAD MUST be able to instantiate this
        public MinimalTest()
        {
            // Log to help debug
            System.Diagnostics.Debug.WriteLine("MinimalTest instance created");
        }

        // Static constructor to pin the instance
        static MinimalTest()
        {
            _pinnedInstance = GCHandle.Alloc(_instance, GCHandleType.Normal);
            System.Diagnostics.Debug.WriteLine("MinimalTest static constructor - instance pinned");
        }

        // CRITICAL: Instance method, not static - prevents delegate GC issue
        [CommandMethod("TESTCMD")]
        public void TestMinimal()
        {
            Application.ShowAlertDialog("MINIMAL TEST WORKS - INSTANCE METHOD!\n\nDelegate is pinned and alive!");

            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage("\n*** TESTCMD executed successfully! ***\n");
        }

        [CommandMethod("HELLO")]
        public void HelloWorld()
        {
            Application.ShowAlertDialog("HELLO WORLD - INSTANCE METHOD WORKS!\n\nNo GC crash!");

            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage("\n*** HELLO executed successfully! ***\n");
        }
    }
}
