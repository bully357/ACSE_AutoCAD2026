using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using ACSE.AutoCAD2026.UI;  // Your WPF window namespace

// Keep other usings as needed, but WPF-specific ones can stay here

namespace ACSE.AutoCAD2026.Commands
{
    public partial class ComplianceCommands
    {
        // CRITICAL: Store as static object to allow cleanup from static method
        private static object? s_windowObj = null;
        private static object? s_controlPanelObj = null;

        // Public method to close WPF window on plugin unload
        public static void CloseUi()
        {
            try
            {
                if (s_windowObj is AcseScanWindow window && window.IsVisible)
                {
                    window.Close();
                    s_windowObj = null;
                }

                if (s_controlPanelObj is StandardsControlPanel panel && panel.IsVisible)
                {
                    panel.Close();
                    s_controlPanelObj = null;
                }
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }

        [CommandMethod("ACSE_UI")]
        public void ShowUi()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;

            if (doc == null || ed == null)
            {
                Application.ShowAlertDialog("No active document. Please open a drawing first.");
                return;
            }

            try
            {
                // Initialize WPF dispatcher first (ensures WPF threading is ready)
                var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

                // Ensure WPF Application instance exists (create only once per AutoCAD session)
                if (System.Windows.Application.Current == null)
                {
                    // Create on the dispatcher thread
                    dispatcher.Invoke(() =>
                    {
                        var app = new System.Windows.Application();
                        // CRITICAL: Prevent app from shutting down when windows close
                        app.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
                    });
                }

                // Create or reuse the window
                AcseScanWindow window;
                if (s_windowObj == null || !(s_windowObj is AcseScanWindow w && w.IsVisible))
                {
                    window = new AcseScanWindow();
                    s_windowObj = window;

                    // IMPORTANT: Parent to AutoCAD main window
                    var acadMainWin = System.Windows.Interop.HwndSource.FromHwnd(Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Handle);
                    if (acadMainWin != null)
                    {
                        window.Owner = acadMainWin.RootVisual as System.Windows.Window;
                    }

                    window.Show();  // Modeless
                }
                else
                {
                    window = (AcseScanWindow)s_windowObj;
                    window.Activate();
                    window.Focus();
                }
            }
            catch (System.Exception ex)
            {
                string errorMsg = $"ACSE_UI failed: {ex.GetType().Name}\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMsg += $"\nInner: {ex.InnerException.Message}";
                }
                errorMsg += $"\nStack: {ex.StackTrace}";

                ed.WriteMessage($"\n{errorMsg}");
                Application.ShowAlertDialog(errorMsg);

                // Optional: LogToFile(errorMsg);
            }
        }

        /// <summary>
        /// Command: ACSE_STANDARDS - Opens the unified Standards Control Panel with Smart Auto-Fix
        /// </summary>
        [CommandMethod("ACSE_STANDARDS")]
        public void OpenStandardsControlPanel()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;

            if (doc == null || ed == null)
            {
                Application.ShowAlertDialog("No active document. Please open a drawing first.");
                return;
            }

            try
            {
                // Initialize WPF dispatcher
                var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

                // Ensure WPF Application instance exists
                if (System.Windows.Application.Current == null)
                {
                    dispatcher.Invoke(() =>
                    {
                        var app = new System.Windows.Application();
                        app.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
                    });
                }

                // Create or reuse the Standards Control Panel
                StandardsControlPanel window;
                if (s_controlPanelObj == null || !(s_controlPanelObj is StandardsControlPanel cp && cp.IsVisible))
                {
                    window = new StandardsControlPanel();
                    s_controlPanelObj = window;

                    // Parent to AutoCAD main window
                    var acadMainWin = System.Windows.Interop.HwndSource.FromHwnd(Application.MainWindow.Handle);
                    if (acadMainWin != null)
                    {
                        window.Owner = acadMainWin.RootVisual as System.Windows.Window;
                    }

                    window.Show();

                    ed.WriteMessage("\n========================================");
                    ed.WriteMessage("\n*** ACSE Standards Control Panel ***");
                    ed.WriteMessage("\n========================================");
                    ed.WriteMessage("\nFeatures:");
                    ed.WriteMessage("\n- 🎯 Smart Auto-Fix with Confidence Scoring");
                    ed.WriteMessage("\n- 📊 Real-time Violations DataGrid");
                    ed.WriteMessage("\n- 🛡️ Apply Safe Fixes Only (90%+ confidence)");
                    ed.WriteMessage("\n- ⚡ Apply Recommended Fixes");
                    ed.WriteMessage("\n- 🔍 Filter & Search violations");
                    ed.WriteMessage("\n- ☑ Select All / Deselect All");
                    ed.WriteMessage("\n- 📊 Export to CSV");
                    ed.WriteMessage("\n- ✏️ Manual property editing");
                    ed.WriteMessage("\n========================================");
                }
                else
                {
                    window = (StandardsControlPanel)s_controlPanelObj;
                    window.Activate();
                    window.Focus();
                    ed.WriteMessage("\nACSE Standards Control Panel activated.");
                }
            }
            catch (System.Exception ex)
            {
                string errorMsg = $"ACSE_STANDARDS failed: {ex.GetType().Name}\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMsg += $"\nInner: {ex.InnerException.Message}";
                }
                errorMsg += $"\nStack: {ex.StackTrace}";

                ed.WriteMessage($"\n{errorMsg}");
                Application.ShowAlertDialog(errorMsg);
            }
        }

        /// <summary>
        /// Command: ACSE_CONTROL - Alias for ACSE_STANDARDS
        /// </summary>
        [CommandMethod("ACSE_CONTROL")]
        public void OpenStandardsControlPanelAlias()
        {
            OpenStandardsControlPanel();
        }

        [CommandMethod("ACSE_UI_INTERACTIVE")]
        public void ShowInteractiveUi()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;

            if (doc == null || ed == null)
            {
                Application.ShowAlertDialog("No active document. Please open a drawing first.");
                return;
            }

            try
            {
                // Initialize WPF dispatcher
                var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

                // Ensure WPF Application instance exists
                if (System.Windows.Application.Current == null)
                {
                    dispatcher.Invoke(() =>
                    {
                        var app = new System.Windows.Application();
                        app.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
                    });
                }

                // Create interactive window
                var window = new InteractiveScanWindow();

                // Parent to AutoCAD main window
                var acadMainWin = System.Windows.Interop.HwndSource.FromHwnd(
                    Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Handle);
                if (acadMainWin != null)
                {
                    window.Owner = acadMainWin.RootVisual as System.Windows.Window;
                }

                window.Show();

                ed.WriteMessage("\n========================================");
                ed.WriteMessage("\n*** ACSE Interactive Scanner Opened ***");
                ed.WriteMessage("\n========================================");
                ed.WriteMessage("\nFeatures:");
                ed.WriteMessage("\n- Review and customize fixes before applying");
                ed.WriteMessage("\n- Select specific violations to fix");
                ed.WriteMessage("\n- Preview changes");
                ed.WriteMessage("\n- Choose custom fonts, styles, and sizes");
                ed.WriteMessage("\n========================================");
            }
            catch (System.Exception ex)
            {
                string errorMsg = $"ACSE_UI_INTERACTIVE failed: {ex.GetType().Name}\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMsg += $"\nInner: {ex.InnerException.Message}";
                }
                errorMsg += $"\nStack: {ex.StackTrace}";

                ed.WriteMessage($"\n{errorMsg}");
                Application.ShowAlertDialog(errorMsg);
            }
        }

        [CommandMethod("ACSE_GLOBAL_TEXT")]
        public void ShowGlobalTextModifier()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;

            if (doc == null || ed == null)
            {
                Application.ShowAlertDialog("No active document. Please open a drawing first.");
                return;
            }

            try
            {
                // Initialize WPF dispatcher
                var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

                // Ensure WPF Application instance exists
                if (System.Windows.Application.Current == null)
                {
                    dispatcher.Invoke(() =>
                    {
                        var app = new System.Windows.Application();
                        app.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
                    });
                }

                // Create global text modifier window
                var window = new GlobalTextModifierWindow();

                // Parent to AutoCAD main window
                var acadMainWin = System.Windows.Interop.HwndSource.FromHwnd(
                    Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Handle);
                if (acadMainWin != null)
                {
                    window.Owner = acadMainWin.RootVisual as System.Windows.Window;
                }

                window.Show();

                ed.WriteMessage("\n========================================");
                ed.WriteMessage("\n*** ACSE Global Text Modifier Opened ***");
                ed.WriteMessage("\n========================================");
                ed.WriteMessage("\nFeatures:");
                ed.WriteMessage("\n- Scan all text/mtext in drawing");
                ed.WriteMessage("\n- Filter by style, layer, font, height, etc.");
                ed.WriteMessage("\n- Modify properties globally:");
                ed.WriteMessage("\n  • Text Style");
                ed.WriteMessage("\n  • Height");
                ed.WriteMessage("\n  • Annotative");
                ed.WriteMessage("\n  • Layer");
                ed.WriteMessage("\n  • Justification (DBText)");
                ed.WriteMessage("\n  • Attachment (MText)");
                ed.WriteMessage("\n  • Rotation");
                ed.WriteMessage("\n  • Width Factor");
                ed.WriteMessage("\n- Preview before applying");
                ed.WriteMessage("\n========================================");
            }
            catch (System.Exception ex)
            {
                string errorMsg = $"ACSE_GLOBAL_TEXT failed: {ex.GetType().Name}\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMsg += $"\nInner: {ex.InnerException.Message}";
                }
                errorMsg += $"\nStack: {ex.StackTrace}";

                ed.WriteMessage($"\n{errorMsg}");
                Application.ShowAlertDialog(errorMsg);
            }
        }

        // If you have cleanup logic (e.g., on Terminate), you can add:
        // public static void CleanupUi()
        // {
        //     _window?.Close();
        //     _window = null;
        // }
    }
}