using Microsoft.Extensions.Logging;
using RestoranOtomasyon.ViewModels;

namespace RestoranOtomasyon.Services;

/// <summary>
/// Sayfa geçiş olayı argümanları
/// </summary>
public class NavigationEventArgs : EventArgs
{
    public ViewModelBase ViewModel { get; }
    public object? Parameter { get; }

    public NavigationEventArgs(ViewModelBase viewModel, object? parameter = null)
    {
        ViewModel = viewModel;
        Parameter = parameter;
    }
}

/// <summary>
/// Sayfa navigasyon servisi
/// </summary>
public interface INavigationService
{
    event EventHandler<NavigationEventArgs>? Navigated;
    ViewModelBase? CurrentViewModel { get; }
    
    void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase;
    void NavigateToMasalar();
    void NavigateToSiparis(int masaId);
    void NavigateToGelAl();
    void NavigateToPaket();
    void NavigateToYonetim();
    bool CanGoBack { get; }
    void GoBack();
}

/// <summary>
/// Navigation servisi implementasyonu
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NavigationService> _logger;
    private readonly Stack<(Type ViewModelType, object? Parameter)> _navigationStack = new();
    
    public event EventHandler<NavigationEventArgs>? Navigated;
    public ViewModelBase? CurrentViewModel { get; private set; }
    public bool CanGoBack => _navigationStack.Count > 1;

    public NavigationService(IServiceProvider serviceProvider, ILogger<NavigationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase
    {
        var viewModel = _serviceProvider.GetService(typeof(TViewModel)) as ViewModelBase;
        
        if (viewModel == null)
        {
            _logger.LogError("ViewModel bulunamadı: {Type}", typeof(TViewModel).Name);
            return;
        }

        // Initialize if needed
        if (viewModel is INavigationAware navAware)
        {
            navAware.OnNavigatedTo(parameter);
        }

        CurrentViewModel = viewModel;
        _navigationStack.Push((typeof(TViewModel), parameter));
        
        _logger.LogDebug("Navigasyon: {ViewModelType}", typeof(TViewModel).Name);
        Navigated?.Invoke(this, new NavigationEventArgs(viewModel, parameter));
    }

    public void NavigateToMasalar()
    {
        NavigateTo<MasaViewModel>();
    }

    public void NavigateToSiparis(int masaId)
    {
        NavigateTo<SiparisViewModel>(masaId);
    }

    public void NavigateToGelAl()
    {
        NavigateTo<SiparisViewModel>("GelAl");
    }

    public void NavigateToPaket()
    {
        NavigateTo<SiparisViewModel>("Paket");
    }

    public void NavigateToYonetim()
    {
        NavigateTo<YonetimViewModel>();
    }

    public void GoBack()
    {
        if (!CanGoBack) return;

        _navigationStack.Pop(); // Current'ı çıkar
        var (viewModelType, parameter) = _navigationStack.Peek();

        var viewModel = _serviceProvider.GetService(viewModelType) as ViewModelBase;
        if (viewModel != null)
        {
            if (viewModel is INavigationAware navAware)
            {
                navAware.OnNavigatedTo(parameter);
            }

            CurrentViewModel = viewModel;
            _logger.LogDebug("Geri navigasyon: {ViewModelType}", viewModelType.Name);
            Navigated?.Invoke(this, new NavigationEventArgs(viewModel, parameter));
        }
    }
}

/// <summary>
/// Navigation-aware ViewModel interface
/// </summary>
public interface INavigationAware
{
    void OnNavigatedTo(object? parameter);
    void OnNavigatedFrom();
}
