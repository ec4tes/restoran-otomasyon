using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using Serilog;

namespace RestoranOtomasyon.Infrastructure;

/// <summary>
/// Global exception handler
/// </summary>
public class GlobalExceptionHandler
{
    private readonly Microsoft.Extensions.Logging.ILogger<GlobalExceptionHandler>? _logger;

    public GlobalExceptionHandler(Microsoft.Extensions.Logging.ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    private GlobalExceptionHandler() { }

    /// <summary>
    /// Uygulama başlatılırken erken aşamada exception handler'ları kaydet (DI olmadan)
    /// </summary>
    public static void RegisterGlobalHandlers(Application app)
    {
        // UI thread exceptions
        app.DispatcherUnhandledException += (sender, e) =>
        {
            Log.Error(e.Exception, "UI Thread Exception: {Message}", e.Exception.Message);

            var result = MessageBox.Show(
                $"Beklenmeyen bir hata oluştu:\n\n{e.Exception.Message}\n\nUygulama çalışmaya devam etsin mi?",
                "Hata",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes)
            {
                e.Handled = true;
            }
        };

        // Non-UI thread exceptions
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var exception = e.ExceptionObject as Exception;
            Log.Fatal(exception, "Kritik Hata (IsTerminating: {IsTerminating}): {Message}",
                e.IsTerminating, exception?.Message ?? "Unknown");

            if (e.IsTerminating)
            {
                Log.CloseAndFlush();

                MessageBox.Show(
                    $"Kritik bir hata oluştu ve uygulama kapanacak:\n\n{exception?.Message}\n\nLütfen log dosyalarını kontrol edin.",
                    "Kritik Hata",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        };

        // Task exceptions
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Log.Error(e.Exception, "Unobserved Task Exception: {Message}", e.Exception.Message);
            e.SetObserved();
        };

        Log.Debug("Global exception handler'lar kaydedildi");
    }

    /// <summary>
    /// Uygulama seviyesinde exception handler'ları kaydet (DI ile)
    /// </summary>
    public void Register()
    {
        // UI thread exceptions
        Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

        // Non-UI thread exceptions
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // Task exceptions
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _logger?.LogInformation("Global exception handler'lar kaydedildi");
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "UI Thread Exception: {Message}", e.Exception.Message);

        var result = MessageBox.Show(
            $"Beklenmeyen bir hata oluştu:\n\n{e.Exception.Message}\n\nUygulama çalışmaya devam etsin mi?",
            "Hata",
            MessageBoxButton.YesNo,
            MessageBoxImage.Error);

        if (result == MessageBoxResult.Yes)
        {
            e.Handled = true;
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        _logger.LogCritical(exception, "Kritik Hata (IsTerminating: {IsTerminating}): {Message}", 
            e.IsTerminating, exception?.Message ?? "Unknown");

        if (e.IsTerminating)
        {
            // Son bir loglama şansı
            Log.CloseAndFlush();

            MessageBox.Show(
                $"Kritik bir hata oluştu ve uygulama kapanacak:\n\n{exception?.Message}\n\nLütfen log dosyalarını kontrol edin.",
                "Kritik Hata",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "Unobserved Task Exception: {Message}", e.Exception.Message);
        e.SetObserved(); // Prevent crash
    }
}
