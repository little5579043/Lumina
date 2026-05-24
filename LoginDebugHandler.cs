using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Markup;

namespace Lumina
{
    /// <summary>
    /// Enhanced debugging class to catch and analyze XAML parse exceptions during login
    /// Add this code to your login button click handler or App.xaml.cs
    /// </summary>
    public static class LoginDebugHandler
    {
        private static string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "XamlDebugLog.txt");


        /// Wrap your login logic with this method to get detailed XAML error info

        public static void ExecuteWithXamlDebugging(Action loginAction, string actionDescription = "Login Action")
        {
            try
            {
                LogDebugInfo($"Starting {actionDescription}...");
                loginAction();
                LogDebugInfo($"{actionDescription} completed successfully.");
            }
            catch (XamlParseException xamlEx)
            {
                HandleXamlParseException(xamlEx, actionDescription);
            }
            catch (Exception ex) when (ex.InnerException is XamlParseException innerXamlEx)
            {
                HandleXamlParseException(innerXamlEx, actionDescription);
            }
            catch (Exception ex)
            {
                LogDebugInfo($"Non-XAML Exception in {actionDescription}: {ex.GetType().Name}");
                LogDebugInfo($"Message: {ex.Message}");
                LogDebugInfo($"StackTrace: {ex.StackTrace}");

                // Check if there's a XAML exception deeper in the chain
                Exception current = ex;
                while (current.InnerException != null)
                {
                    current = current.InnerException;
                    if (current is XamlParseException deepXamlEx)
                    {
                        HandleXamlParseException(deepXamlEx, actionDescription);
                        return;
                    }
                }

                MessageBox.Show($"Error during {actionDescription}:\n{ex.Message}\n\nCheck XamlDebugLog.txt on desktop for details.",
                               "Debug Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static void HandleXamlParseException(XamlParseException xamlEx, string context)
        {
            string errorDetails = $@"
=== XAML Parse Exception - {context} ===
Time: {DateTime.Now}
Line Number: {xamlEx.LineNumber}
Line Position: {xamlEx.LinePosition}
Message: {xamlEx.Message}
Source: {xamlEx.Source ?? "Unknown"}

Inner Exception: {xamlEx.InnerException?.Message ?? "None"}
Inner Exception Type: {xamlEx.InnerException?.GetType().Name ?? "None"}

Stack Trace:
{xamlEx.StackTrace}

=== End Debug Info ===
";

            LogDebugInfo(errorDetails);

            // Show user-friendly dialog
            string userMessage = $@"XAML Parse Error Details:

File/Location: {xamlEx.Source ?? "Unknown"}
Line: {xamlEx.LineNumber}
Position: {xamlEx.LinePosition}

Error: {xamlEx.Message}

{(xamlEx.InnerException != null ? $"Cause: {xamlEx.InnerException.Message}" : "")}

Full details saved to: XamlDebugLog.txt on your Desktop";

            MessageBox.Show(userMessage, "XAML Parse Exception", MessageBoxButton.OK, MessageBoxImage.Error);

            // Try to open the log file
            try
            {
                Process.Start("notepad.exe", logPath);
            }
            catch { /* Ignore if can't open notepad */ }
        }

        private static void LogDebugInfo(string message)
        {
            try
            {
                File.AppendAllText(logPath, message + Environment.NewLine + Environment.NewLine);
            }
            catch
            {
                // If can't write to desktop, try temp
                try
                {
                    string tempPath = Path.Combine(Path.GetTempPath(), "XamlDebugLog.txt");
                    File.AppendAllText(tempPath, message + Environment.NewLine + Environment.NewLine);
                }
                catch { /* Ignore logging errors */ }
            }
        }


        /// Check for common XAML issues before they cause exceptions

        public static void PreflightCheck()
        {
            LogDebugInfo("=== XAML Preflight Check ===");

            // Check if resources are available
            try
            {
                var app = Application.Current;
                if (app?.Resources != null)
                {
                    // Test key resources
                    string[] criticalResources = {
                        "TransparentWindow", "Math", "DividorColor", "LightBackColor",
                        "InactiveColor", "AccentColor", "CloseIcon", "Slider"
                    };

                    foreach (string resourceKey in criticalResources)
                    {
                        try
                        {
                            var resource = app.TryFindResource(resourceKey);
                            LogDebugInfo($"Resource '{resourceKey}': {(resource != null ? "✓ Found" : "✗ Missing")}");
                        }
                        catch (Exception ex)
                        {
                            LogDebugInfo($"Resource '{resourceKey}': ✗ Error accessing - {ex.Message}");
                        }
                    }
                }
                else
                {
                    LogDebugInfo("✗ Application.Current.Resources is null");
                }
            }
            catch (Exception ex)
            {
                LogDebugInfo($"Preflight check failed: {ex.Message}");
            }

            LogDebugInfo("=== End Preflight Check ===");
        }
    }
}
    /// <summary>
    /// Example of how to use the debug handler in your login button click
    /// </summary>
