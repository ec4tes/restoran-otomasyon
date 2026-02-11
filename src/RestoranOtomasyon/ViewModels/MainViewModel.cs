using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RestoranOtomasyon.Models;
using RestoranOtomasyon.Services;

namespace RestoranOtomasyon.ViewModels;

/// <summary>
/// Ana pencere ViewModel'i
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly ISessionService _sessionService;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private Kullanici? _currentUser;

    [ObservableProperty]
    private string _title = "Restoran Otomasyonu";

    [ObservableProperty]
    private string _statusMessage = "Hazır";

    [ObservableProperty]
    private bool _isLoggedIn;

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    private string _currentTime = DateTime.Now.ToString("HH:mm");

    [ObservableProperty]
    private string _currentDate = DateTime.Now.ToString("dd.MM.yyyy dddd");

    public MainViewModel(
        ILogger<MainViewModel> logger,
        ISessionService sessionService,
        IAuthService authService,
        INavigationService navigationService)
    {
        _logger = logger;
        _sessionService = sessionService;
        _authService = authService;
        _navigationService = navigationService;

        // Session değişikliklerini dinle
        _sessionService.UserChanged += OnUserChanged;

        // Saat güncellemesi için timer başlat
        StartClockTimer();

        _logger.LogInformation("MainViewModel oluşturuldu");
    }

    private void OnUserChanged(object? sender, Kullanici? user)
    {
        CurrentUser = user;
        IsLoggedIn = user != null;
        IsAdmin = user?.Rol == KullaniciRol.Admin;
        
        if (user != null)
        {
            StatusMessage = $"Hoş geldiniz, {user.Ad}";
            _logger.LogInformation("Kullanıcı değişti: {UserName}", user.Ad);
        }
        else
        {
            StatusMessage = "Çıkış yapıldı";
        }
    }

    private void StartClockTimer()
    {
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        
        timer.Tick += (s, e) =>
        {
            CurrentTime = DateTime.Now.ToString("HH:mm");
            CurrentDate = DateTime.Now.ToString("dd.MM.yyyy dddd");
        };
        
        timer.Start();
    }

    [RelayCommand]
    private async Task LoginAsync(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
        {
            StatusMessage = "PIN girişi gerekli";
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var user = await _authService.ValidatePinAsync(pin);
            
            if (user != null)
            {
                _sessionService.Login(user);
            }
            else
            {
                StatusMessage = "Geçersiz PIN!";
                _logger.LogWarning("Başarısız giriş denemesi");
            }
        }, "Giriş yapılıyor...");
    }

    [RelayCommand]
    private void Logout()
    {
        _sessionService.Logout();
        _logger.LogInformation("Kullanıcı çıkış yaptı");
    }

    [RelayCommand]
    private void OpenYonetim()
    {
        _navigationService.NavigateToYonetim();
        _logger.LogInformation("Yönetim paneli açıldı");
    }
}
