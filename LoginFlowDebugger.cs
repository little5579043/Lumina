using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Threading.Tasks;

namespace Lumina
{
    /// <summary>
    /// Specialized debugger for catching XAML exceptions in login-triggered events
    /// </summary>
    public static class LoginFlowDebugger
    {
        private static string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "LoginFlowDebug.txt");


        /// Enhanced login button click handler with comprehensive XAML exception catching
        /// Use this to replace your current login button click handler

        public static void DebugLoginButtonClick(object sender, RoutedEventArgs e, Action originalLoginAction)
        {
            LogInfo("=== LOGIN BUTTON CLICKED ===");
            LogInfo($"Time: {DateTime.Now}");
            LogInfo($"Sender: {sender?.GetType().Name ?? "null"}");
            
            try
            {
                // Set up global exception catching for this login flow
                SetupGlobalExceptionHandling();
                
                LogInfo("Executing original login action...");
                
                // Execute the original login logic with exception wrapping
                originalLoginAction();
                
                LogInfo("✅ Login action completed successfully");
                
            }
            catch (XamlParseException xamlEx)
            {
                HandleLoginXamlException(xamlEx, "Direct XAML Exception");
            }
            catch (Exception ex) when (ex.InnerException is XamlParseException innerXaml)
            {
                HandleLoginXamlException(innerXaml, "Inner XAML Exception");
            }
            catch (Exception ex)
            {
                LogInfo($"❌ Non-XAML Exception: {ex.GetType().Name}");
                LogInfo($"Message: {ex.Message}");
                LogInfo($"Stack Trace: {ex.StackTrace}");
                
                // Check for nested XAML exceptions
                var current = ex;
                int depth = 0;
                while (current.InnerException != null && depth < 10)
                {
                    current = current.InnerException;
                    depth++;
                    LogInfo($"Inner Exception Level {depth}: {current.GetType().Name} - {current.Message}");
                    
                    if (current is XamlParseException nestedXaml)
                    {
                        HandleLoginXamlException(nestedXaml, $"Nested XAML Exception (Level {depth})");
                        return;
                    }
                }
                
                ShowDebugDialog("Login Flow Error", $"Non-XAML error during login:\n{ex.Message}\n\nCheck LoginFlowDebug.txt for details.");
            }
        }


        /// Call this in your login button click before doing anything else

        public static void PrepareLoginDebugging()
        {
            LogInfo("=== PREPARING LOGIN DEBUGGING ===");
            
            // Clear previous log
            try { File.Delete(logPath); } catch { }
            
            // Log current application state
            LogAppState();
            
            // Set up comprehensive exception handling
            SetupGlobalExceptionHandling();
            
            LogInfo("Login debugging prepared ✅");
        }


        /// Wrap any specific login operations (window creation, navigation, etc.)

        public static T ExecuteWithDebug<T>(Func<T> operation, string operationName)
        {
            LogInfo($"🔄 Starting: {operationName}");
            try
            {
                var result = operation();
                LogInfo($"✅ Completed: {operationName}");
                return result;
            }
            catch (XamlParseException xamlEx)
            {
                LogInfo($"❌ XAML Exception in {operationName}:");
                HandleLoginXamlException(xamlEx, operationName);
                throw; // Re-throw to maintain original behavior
            }
            catch (Exception ex)
            {
                LogInfo($"❌ Exception in {operationName}: {ex.Message}");
                throw; // Re-throw to maintain original behavior
            }
        }


        /// Use for operations that don't return values

        public static void ExecuteWithDebug(Action operation, string operationName)
        {
            LogInfo($"🔄 Starting: {operationName}");
            try
            {
                operation();
                LogInfo($"✅ Completed: {operationName}");
            }
            catch (XamlParseException xamlEx)
            {
                LogInfo($"❌ XAML Exception in {operationName}:");
                HandleLoginXamlException(xamlEx, operationName);
                throw; // Re-throw to maintain original behavior
            }
            catch (Exception ex)
            {
                LogInfo($"❌ Exception in {operationName}: {ex.Message}");
                throw; // Re-throw to maintain original behavior
            }
        }

        private static void HandleLoginXamlException(XamlParseException xamlEx, string context)
        {
            string details = $@"
🔥 XAML PARSE EXCEPTION FOUND 🔥
Context: {context}
Time: {DateTime.Now}

📍 ERROR LOCATION:
Line: {xamlEx.LineNumber}
Position: {xamlEx.LinePosition}
Source: {xamlEx.Source ?? "Unknown"}

💬 ERROR MESSAGE:
{xamlEx.Message}

🔗 INNER EXCEPTION:
Type: {xamlEx.InnerException?.GetType().Name ?? "None"}
Message: {xamlEx.InnerException?.Message ?? "None"}

📚 FULL STACK TRACE:
{xamlEx.StackTrace}

==========================================";

            LogInfo(details);

            // Show immediate feedback to user
            string userMessage = $@"🔥 Found the Login XAML Issue! 🔥

Context: {context}
File: {xamlEx.Source ?? "Unknown"}  
Line: {xamlEx.LineNumber}, Position: {xamlEx.LinePosition}

Error: {xamlEx.Message}

{(xamlEx.InnerException != null ? $"\nRoot Cause: {xamlEx.InnerException.Message}" : "")}

Full details saved to: LoginFlowDebug.txt on Desktop";

            ShowDebugDialog("XAML Parse Exception Found!", userMessage);
        }

        private static void SetupGlobalExceptionHandling()
        {
            // Remove any existing handlers to avoid duplicates
            Application.Current.DispatcherUnhandledException -= OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            
            // Add new handlers
            Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogInfo($"🚨 DISPATCHER UNHANDLED EXCEPTION: {e.Exception.GetType().Name}");
            LogInfo($"Message: {e.Exception.Message}");
            
            if (e.Exception is XamlParseException xamlEx)
            {
                HandleLoginXamlException(xamlEx, "Dispatcher Unhandled Exception");
                e.Handled = true; // Prevent app crash
            }
            else if (e.Exception.InnerException is XamlParseException innerXaml)
            {
                HandleLoginXamlException(innerXaml, "Dispatcher Inner XAML Exception");
                e.Handled = true; // Prevent app crash
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is XamlParseException xamlEx)
            {
                HandleLoginXamlException(xamlEx, "AppDomain Unhandled Exception");
            }
        }

        private static void LogAppState()
        {
            try
            {
                LogInfo("📊 APPLICATION STATE:");
                LogInfo($"Current Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                LogInfo($"Main Window: {Application.Current.MainWindow?.GetType().Name ?? "null"}");
                LogInfo($"Main Window Loaded: {Application.Current.MainWindow?.IsLoaded ?? false}");
                LogInfo($"Windows Count: {Application.Current.Windows.Count}");
                
                foreach (Window window in Application.Current.Windows)
                {
                    LogInfo($"  - {window.GetType().Name}: {window.Title} (Visible: {window.IsVisible})");
                }
            }
            catch (Exception ex)
            {
                LogInfo($"Error logging app state: {ex.Message}");
            }
        }

        private static void LogInfo(string message)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
                
                // Also output to debug console
                Debug.WriteLine(logEntry);
            }
            catch { /* Ignore logging errors */ }
        }

        private static void ShowDebugDialog(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Try to open the log file
            try
            {
                Process.Start("notepad.exe", logPath);
            }
            catch { /* Ignore if can't open notepad */ }
        }
    }

    /// <summary>
    /// Example of how to use in your MainWindow or wherever your login button is
    /// </summary>
    public static class LoginButtonExample
    {
        // Replace your existing login button click handler with this pattern:
        public static void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Prepare debugging
            LoginFlowDebugger.PrepareLoginDebugging();
            
            // Use the debug wrapper for your login action
            LoginFlowDebugger.DebugLoginButtonClick(sender, e, () =>
            {
                // PUT YOUR ORIGINAL LOGIN CODE HERE
                // For example:
                
                // If you open a new window:
                // var loginWindow = LoginFlowDebugger.ExecuteWithDebug(
                //     () => new LoginWindow(), "Create Login Window");
                // LoginFlowDebugger.ExecuteWithDebug(
                //     () => loginWindow.ShowDialog(), "Show Login Dialog");
                
                // If you navigate or change UI:
                // LoginFlowDebugger.ExecuteWithDebug(
                //     () => NavigateToMainPage(), "Navigate to Main Page");
                
                // If you modify visibility or load controls:
                // LoginFlowDebugger.ExecuteWithDebug(
                //     () => LoadUserInterface(), "Load User Interface");
                
                MessageBox.Show("REPLACE THIS with your actual login logic!");
            });
        }
    }
}
