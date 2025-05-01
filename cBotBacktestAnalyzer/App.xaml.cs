using System;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace cBotBacktestAnalyzer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static string LogFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_log.txt");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Set culture to English (US) for the application
        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
        
        // Log application start
        LogMessage("Application startup initiated");
        
        // Register handlers for unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        Current.DispatcherUnhandledException += Application_DispatcherUnhandledException;
        
        try
        {
            // Log .NET version information
            LogMessage($"Runtime: {Environment.Version}");
            LogMessage($"OS: {Environment.OSVersion}");
            LogMessage($"64-bit OS: {Environment.Is64BitOperatingSystem}");
            LogMessage($"64-bit Process: {Environment.Is64BitProcess}");
            LogMessage($"Current Directory: {Environment.CurrentDirectory}");
            LogMessage($"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
            
            // Log assembly dependencies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            StringBuilder sb = new StringBuilder("Loaded Assemblies:");
            foreach (var assembly in assemblies)
            {
                sb.AppendLine($" - {assembly.FullName}");
            }
            LogMessage(sb.ToString());
        }
        catch (Exception ex)
        {
            LogMessage($"Error while logging startup information: {ex}");
        }
    }
    
    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogMessage($"Unhandled dispatcher exception: {e.Exception}");
        MessageBox.Show($"An error occurred: {e.Exception.Message}\n\nPlease check the log file for details: {LogFilePath}", 
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
    
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogMessage($"Fatal unhandled exception: {ex}");
        }
        else
        {
            LogMessage($"Unknown fatal error: {e.ExceptionObject}");
        }
    }
    
    public static void LogMessage(string message)
    {
        try
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
        }
        catch
        {
            // Do nothing if logging itself fails
        }
    }
}

