using System;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;

namespace ACSE.AutoCAD2026
{
    public class AcsePlugin : IExtensionApplication
    {
        public void Initialize()
        {
            try
            {
                // Log initialization
                LogToFile("ACSE Plugin initialized successfully");

                // Show welcome message immediately (safer than waiting for DocumentActivated)
                ShowWelcomeMessage();

                // Still subscribe to DocumentActivated for future documents
                Application.DocumentManager.DocumentActivated += OnDocumentActivated;
            }
            catch (System.Exception ex)
            {
                LogToFile($"Initialize ERROR: {ex.Message}");
                LogToFile($"Stack: {ex.StackTrace}");
            }
        }

        private static bool s_welcomeShown = false;

        private void ShowWelcomeMessage()
        {
            if (s_welcomeShown) return;
            s_welcomeShown = true;

            try
            {
                // Try to get active document, but don't crash if none exists
                var doc = Application.DocumentManager.MdiActiveDocument;

                if (doc != null && doc.Editor != null)
                {
                    var ed = doc.Editor;
                    ed.WriteMessage("\n========================================");
                    ed.WriteMessage("\n*** ACSE AutoCAD 2026 Plugin Loaded ***");
                    ed.WriteMessage("\n========================================");
                    ed.WriteMessage("\nCommands available:");
                    ed.WriteMessage("\n  ACSE_TEST - Test if DLL is working");
                    ed.WriteMessage("\n  ACSE_RUN - Run compliance scan");
                    ed.WriteMessage("\n  ACSE_RUN_SIMPLE - Simple compliance test");
                    ed.WriteMessage("\n  ACSE_RESET - Reset standards file");
                    ed.WriteMessage("\n  ACSE_PING - Simple ping test");
                    ed.WriteMessage("\n  ACSE_TEMPLATE - Template-based scan");
                    ed.WriteMessage("\n  ACSE_FIX - Auto-fix violations");
                    ed.WriteMessage("\n  ACSE_UI - Open compliance scanner UI");
                    ed.WriteMessage("\n  ACSE_GLOBAL_TEXT - Global text property modifier");
                    ed.WriteMessage("\n  ACSE_EXTRACT - Extract standards from template");
                    ed.WriteMessage("\n  ACSE_LOAD_TEMPLATE - Load template standards");
                    ed.WriteMessage("\n  ACSE_LIST_TEMPLATE_STYLES - List template styles");
                    ed.WriteMessage("\n========================================");

                    LogToFile("Welcome message displayed to user");
                }
                else
                {
                    // No document open - use ShowAlertDialog as fallback
                    Application.ShowAlertDialog(
                        "ACSE AutoCAD 2026 Plugin Loaded!\n\n" +
                        "Commands available:\n" +
                        "  ACSE_TEST, ACSE_RUN, ACSE_TEMPLATE,\n" +
                        "  ACSE_FIX, ACSE_UI, ACSE_EXTRACT\n\n" +
                        "Open a drawing to see full command list."
                    );
                    LogToFile("Welcome message shown via popup (no active document)");
                }
            }
            catch (System.Exception ex)
            {
                LogToFile($"ShowWelcomeMessage ERROR: {ex.Message}");
            }
        }

        private void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            // Show welcome message when first document becomes active (if not shown yet)
            if (!s_welcomeShown && e.Document != null)
            {
                ShowWelcomeMessage();
            }
        }

        public void Terminate()
        {
            try
            {
                // CRITICAL: Close WPF window first
                Commands.ComplianceCommands.CloseUi();

                // CRITICAL: Free all GCHandles to prevent crash on exit
                FreeAllGCHandles();

                // Unsubscribe from event
                Application.DocumentManager.DocumentActivated -= OnDocumentActivated;

                // Safe to access document during explicit unload
                var doc = Application.DocumentManager.MdiActiveDocument;
                doc?.Editor.WriteMessage("\nACSE Plugin unloaded.");
                LogToFile("ACSE Plugin terminated successfully");
            }
            catch (System.Exception ex)
            {
                LogToFile($"Terminate ERROR: {ex.Message}");
            }
        }

        private void FreeAllGCHandles()
        {
            try
            {
                // Free GCHandles from all command classes using reflection
                var commandTypes = new[]
                {
                    typeof(Commands.ComplianceCommands),
                    typeof(Commands.TemplateCommands),
                    typeof(Commands.ExtractCommands)
                };

                foreach (var type in commandTypes)
                {
                    try
                    {
                        var field = type.GetField("_pinnedInstance", 
                            System.Reflection.BindingFlags.Static | 
                            System.Reflection.BindingFlags.NonPublic);

                        if (field != null)
                        {
                            var handle = (GCHandle)field.GetValue(null);
                            if (handle.IsAllocated)
                            {
                                handle.Free();
                                LogToFile($"Freed GCHandle for {type.Name}");
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        LogToFile($"Error freeing GCHandle for {type.Name}: {ex.Message}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogToFile($"FreeAllGCHandles ERROR: {ex.Message}");
            }
        }

        private static void LogToFile(string message)
        {
            try
            {
                string logPath = @"C:\ACSE\acse_debug.log";
                string dir = System.IO.Path.GetDirectoryName(logPath);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                System.IO.File.AppendAllText(logPath, $"[{timestamp}] {message}\n");
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }
}
