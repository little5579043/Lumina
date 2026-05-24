using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Threading.Tasks;

namespace Lumina
{
    /// <summary>
    /// Global XAML exception catcher that hooks into ALL possible exception sources
    /// This will catch XAML exceptions no matter where they occur
    /// </summary>
    public static class GlobalXamlCatcher
    {
        private static string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "GlobalXamlCatcher.txt");
        private static bool isSetup = false;


        /// Call this once at app startup to catch ALL XAML exceptions globally

        public static void Setup()
        {
            if (isSetup) return;

            try
            {
                // Clear previous log
                File.Delete(logPath);
            }
            catch { }

            LogInfo("=== GLOBAL XAML CATCHER SETUP ===");
            LogInfo($"Time: {DateTime.Now}");
            LogInfo("Setting up comprehensive exception handling...");

            // Remove any existing handlers first
            RemoveExistingHandlers();

            // Set up all possible exception handlers
            SetupDispatcherException();
            SetupAppDomainException();
            SetupTaskException();
            SetupCurrentDomainException();

            isSetup = true;
            LogInfo("✅ Global XAML catcher is active!");
            LogInfo("Now try clicking login - any XAML exception will be caught here.");
            LogInfo("===============================\n");
        }

        private static void RemoveExistingHandlers()
        {
            try
            {
                Application.Current.DispatcherUnhandledException -= OnDispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
                TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            }
            catch { }
        }

        private static void SetupDispatcherException()
        {
            Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
            LogInfo("✓ Dispatcher exception handler set");
        }

        private static void SetupAppDomainException()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            LogInfo("✓ AppDomain exception handler set");
        }

        private static void SetupTaskException()
        {
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            LogInfo("✓ Task exception handler set");
        }

        private static void SetupCurrentDomainException()
        {
            AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
            LogInfo("✓ First chance exception handler set");
        }

        private static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogInfo("🚨 DISPATCHER UNHANDLED EXCEPTION CAUGHT!");
            LogInfo($"Exception Type: {e.Exception.GetType().Name}");
            LogInfo($"Message: {e.Exception.Message}");

            CheckForXamlException(e.Exception, "Dispatcher Thread");

            // Don't crash the app
            e.Handled = true;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogInfo("🚨 APPDOMAIN UNHANDLED EXCEPTION CAUGHT!");

            if (e.ExceptionObject is Exception ex)
            {
                LogInfo($"Exception Type: {ex.GetType().Name}");
                LogInfo($"Message: {ex.Message}");
                CheckForXamlException(ex, "AppDomain");
            }
            else
            {
                LogInfo($"Non-Exception object: {e.ExceptionObject?.ToString() ?? "null"}");
            }
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogInfo("🚨 UNOBSERVED TASK EXCEPTION CAUGHT!");
            LogInfo($"Exception: {e.Exception.GetType().Name}");

            CheckForXamlException(e.Exception, "Background Task");

            // Mark as observed to prevent app crash
            e.SetObserved();
        }

        private static void OnFirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            // Only log XAML-related first chance exceptions to avoid spam
            if (e.Exception is XamlParseException ||
                e.Exception.Message.Contains("XAML") ||
                e.Exception.Message.Contains("StaticResource") ||
                e.Exception.Message.Contains("Cannot find resource"))
            {
                LogInfo("🔍 FIRST CHANCE XAML EXCEPTION CAUGHT!");
                CheckForXamlException(e.Exception, "First Chance");
            }
        }

        private static void CheckForXamlException(Exception ex, string context)
        {
            // Check the main exception
            if (ex is XamlParseException xamlEx)
            {
                HandleXamlParseException(xamlEx, context);
                return;
            }

            // Check inner exceptions (up to 5 levels deep)
            var current = ex;
            int depth = 0;
            while (current != null && depth < 5)
            {
                if (current is XamlParseException innerXaml)
                {
                    HandleXamlParseException(innerXaml, $"{context} (Inner Level {depth})");
                    return;
                }

                // Also check for resource-related exceptions
                if (current.Message.Contains("StaticResource") ||
                    current.Message.Contains("Cannot find resource") ||
                    current.Message.Contains("resource named") ||
                    current.GetType().Name.Contains("Resource"))
                {
                    HandleResourceException(current, $"{context} (Resource Error)");
                    return;
                }

                current = current.InnerException;
                depth++;
            }

            // Log non-XAML exception for reference
            LogInfo($"Non-XAML Exception in {context}: {ex.GetType().Name} - {ex.Message}");
        }

        private static void HandleXamlParseException(XamlParseException xamlEx, string context)
        {
            string details = $@"
🔥🔥🔥 XAML PARSE EXCEPTION FOUND! 🔥🔥🔥
Context: {context}
Time: {DateTime.Now}

📍 EXACT ERROR LOCATION:
File: {xamlEx.Source ?? "Unknown"}
Line: {xamlEx.LineNumber}
Position: {xamlEx.LinePosition}

💬 ERROR MESSAGE:
{xamlEx.Message}

🔗 INNER EXCEPTION:
{xamlEx.InnerException?.GetType().Name ?? "None"}: {xamlEx.InnerException?.Message ?? "None"}

📚 FULL STACK TRACE:
{xamlEx.StackTrace}

🎯 LIKELY FIX:
If this is a missing resource error, add the missing resource to AppResourceDefinitions.xaml
or reference AppResourceDefinitions.xaml in the failing XAML file.

========================================";

            LogInfo(details);

            // Show user dialog
            string userMessage = $@"🔥 FOUND THE LOGIN XAML ISSUE! 🔥

File: {xamlEx.Source ?? "Unknown"}
Line: {xamlEx.LineNumber}, Position: {xamlEx.LinePosition}

Error: {xamlEx.Message}

{(xamlEx.InnerException != null ? $"Root Cause: {xamlEx.InnerException.Message}" : "")}

Full details in GlobalXamlCatcher.txt on Desktop!";

            ShowDialog("XAML Parse Exception Caught!", userMessage);
        }

        private static void HandleResourceException(Exception ex, string context)
        {
            string details = $@"
⚠️ RESOURCE-RELATED EXCEPTION FOUND! ⚠️
Context: {context}
Time: {DateTime.Now}

Exception Type: {ex.GetType().Name}
Message: {ex.Message}

Stack Trace:
{ex.StackTrace}

🎯 LIKELY FIX:
This appears to be a missing resource. Check AppResourceDefinitions.xaml
or add the missing resource reference.

========================================";

            LogInfo(details);

            ShowDialog("Resource Exception Found!", $"Resource Error: {ex.Message}\n\nCheck GlobalXamlCatcher.txt for details.");
        }

        private static void LogInfo(string message)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                File.AppendAllText(logPath, logEntry + Environment.NewLine);

                // Also output to debug console if attached
                Debug.WriteLine(logEntry);
                Console.WriteLine(logEntry);
            }
            catch { }
        }

        private static void ShowDialog(string title, string message)
        {
            try
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                }));
            }
            catch
            {
                // Fallback if dispatcher isn't available
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Try to open the log file
            try
            {
                Process.Start("notepad.exe", logPath);
            }
            catch { }
        }


        /// Call this to manually log that login button was clicked

        public static void LogLoginClick()
        {
            LogInfo("🔘 LOGIN BUTTON CLICKED - monitoring for XAML exceptions...");
        }
    }
}