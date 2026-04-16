using System;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.Runtime;

// TEMPORARILY DISABLED - Testing if this causes the crash
// [assembly: ExtensionApplication(typeof(ACSE.AutoCAD2026.AssemblyLoadLogger))]

namespace ACSE.AutoCAD2026
{
	public class AssemblyLoadLogger : IExtensionApplication
	{
		private static string logPath = @"C:\Temp\ACSE_LoadLog.txt";

		public void Initialize()
		{
			try
			{
				// Create log directory
				Directory.CreateDirectory(Path.GetDirectoryName(logPath));

				// Log initialization start
				LogMessage("=== ACSE Plugin Initialize START ===");

				// Subscribe to assembly load events
				AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
				AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

				// Log loaded assemblies
				LogMessage("Currently loaded assemblies:");
				foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
				{
					LogMessage($"  {asm.GetName().Name} - {asm.GetName().Version}");
				}

				LogMessage("=== ACSE Plugin Initialize COMPLETE ===");

				// Show success message
				var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
				if (doc != null)
				{
					doc.Editor.WriteMessage("\nACSE Plugin loaded successfully. Check: " + logPath + "\n");
				}
			}
			catch (Exception ex)
			{
				LogMessage($"EXCEPTION in Initialize: {ex.ToString()}");
				throw;
			}
		}

		public void Terminate()
		{
			LogMessage("=== ACSE Plugin Terminate ===");
		}

		private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
		{
			LogMessage($"Assembly LOADED: {args.LoadedAssembly.GetName().Name}");
		}

		private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
		{
			LogMessage($"Assembly RESOLVE REQUESTED: {args.Name}");

			// Log the requesting assembly
			if (args.RequestingAssembly != null)
			{
				LogMessage($"  Requested by: {args.RequestingAssembly.GetName().Name}");
			}

			// Try to load from plugin directory
			try
			{
				string assemblyName = new AssemblyName(args.Name).Name;
				string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				string assemblyPath = Path.Combine(pluginDir, assemblyName + ".dll");

				LogMessage($"  Looking for: {assemblyPath}");

				if (File.Exists(assemblyPath))
				{
					LogMessage($"  FOUND and loading: {assemblyPath}");
					return Assembly.LoadFrom(assemblyPath);
				}
				else
				{
					LogMessage($"  NOT FOUND: {assemblyPath}");
				}
			}
			catch (Exception ex)
			{
				LogMessage($"  ERROR resolving assembly: {ex.Message}");
			}

			return null;
		}

		private static void LogMessage(string message)
		{
			try
			{
				string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
				File.AppendAllText(logPath, $"[{timestamp}] {message}\n");
			}
			catch
			{
				// Suppress logging errors
			}
		}
	}
}
