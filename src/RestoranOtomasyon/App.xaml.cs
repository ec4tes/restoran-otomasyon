using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RestoranOtomasyon.Data;
using RestoranOtomasyon.Infrastructure;
using RestoranOtomasyon.Services;
using RestoranOtomasyon.ViewModels;
using Serilog;
using System.Windows;

namespace RestoranOtomasyon;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    public App()
    {
        // Serilog konfigürasyonu
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: "logs/restoran-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("=== Restoran Otomasyon Sistemi Başlatılıyor ===");
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            // Global exception handler'ları kaydet
            GlobalExceptionHandler.RegisterGlobalHandlers(this);

            // Host builder ile DI container oluştur
            _host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .Build();

            await _host.StartAsync();

            // Database'i initialize et
            var dbInitializer = _host.Services.GetRequiredService<IDatabaseInitializer>();
            await dbInitializer.InitializeAsync();
            Log.Information("Veritabanı başarıyla initialize edildi");

            // MainWindow'u DI'dan al ve göster
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            Log.Information("Uygulama başarıyla başlatıldı");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Uygulama başlatılırken kritik hata oluştu");
            MessageBox.Show(
                $"Uygulama başlatılamadı:\n{ex.Message}",
                "Kritik Hata",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Log.Information("=== Restoran Otomasyon Sistemi Kapatılıyor ===");

        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Data Layer
        services.AddSingleton<IDatabaseConnection, DatabaseConnection>();
        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();

        // Services
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IMasaService, MasaService>();
        services.AddSingleton<IKategoriService, KategoriService>();
        services.AddSingleton<IUrunService, UrunService>();
        services.AddSingleton<IAdisyonService, AdisyonService>();
        services.AddSingleton<IOdemeService, OdemeService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IFisYazdirmaService, FisYazdirmaService>();
        services.AddSingleton<IRaporService, RaporService>();
        services.AddSingleton<ISifirlamaService, SifirlamaService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MasaViewModel>();
        services.AddTransient<SiparisViewModel>();
        services.AddTransient<OdemeViewModel>();
        
        // Yönetim ViewModels
        services.AddTransient<YonetimViewModel>();
        services.AddTransient<KategoriYonetimViewModel>();
        services.AddTransient<UrunYonetimViewModel>();
        services.AddTransient<KullaniciYonetimViewModel>();
        services.AddTransient<MasaYonetimViewModel>();
        services.AddTransient<RaporViewModel>();
        services.AddTransient<SifirlamaViewModel>();
        services.AddTransient<FiyatGuncellemeViewModel>();

        // Views
        services.AddTransient<MainWindow>();
        
        // Yönetim Views
        services.AddTransient<Views.YonetimView>();
        services.AddTransient<Views.KategoriYonetimView>();
        services.AddTransient<Views.UrunYonetimView>();
        services.AddTransient<Views.KullaniciYonetimView>();
        services.AddTransient<Views.MasaYonetimView>();
        services.AddTransient<Views.RaporView>();
        services.AddTransient<Views.SifirlamaView>();
        services.AddTransient<Views.FiyatGuncellemeView>();

        Log.Debug("Servisler DI container'a kaydedildi");
    }
}

