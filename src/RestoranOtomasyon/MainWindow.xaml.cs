using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using RestoranOtomasyon.Services;
using RestoranOtomasyon.ViewModels;
using RestoranOtomasyon.Views;

namespace RestoranOtomasyon;

/// <summary>
/// Ana pencere code-behind
/// </summary>
public partial class MainWindow : Window
{
    private string _currentPin = string.Empty;
    private readonly MainViewModel _viewModel;
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(
        MainViewModel viewModel, 
        INavigationService navigationService, 
        IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;
        DataContext = viewModel;

        // ViewModel'deki IsLoggedIn değişikliğini dinle
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsLoggedIn))
            {
                UpdatePanelVisibility();
            }
        };

        // Navigation değişikliklerini dinle
        _navigationService.Navigated += OnNavigated;
    }

    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        // ViewModel'e göre View oluştur
        UserControl? view = e.ViewModel switch
        {
            MasaViewModel vm => new MasaView { DataContext = vm },
            SiparisViewModel vm => CreateSiparisView(vm),
            YonetimViewModel vm => _serviceProvider.GetRequiredService<YonetimView>(),
            _ => null
        };

        if (view != null)
        {
            MainContent.Content = view;
        }
    }

    private SiparisView CreateSiparisView(SiparisViewModel vm)
    {
        // OdemeViewModel'i inject et
        var odemeVm = _serviceProvider.GetRequiredService<OdemeViewModel>();
        vm.SetOdemeViewModel(odemeVm);
        return new SiparisView { DataContext = vm };
    }

    private void UpdatePanelVisibility()
    {
        if (_viewModel.IsLoggedIn)
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            MainPanel.Visibility = Visibility.Visible;
            
            // Giriş yapıldığında masa ekranına git
            _navigationService.NavigateToMasalar();
        }
        else
        {
            LoginPanel.Visibility = Visibility.Visible;
            MainPanel.Visibility = Visibility.Collapsed;
            _currentPin = string.Empty;
            UpdatePinDisplay();
        }
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && _currentPin.Length < 6)
        {
            _currentPin += button.Content.ToString();
            UpdatePinDisplay();
        }
    }

    private void ClearPin_Click(object sender, RoutedEventArgs e)
    {
        _currentPin = string.Empty;
        UpdatePinDisplay();
    }

    private async void SubmitPin_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentPin))
        {
            await _viewModel.LoginCommand.ExecuteAsync(_currentPin);
            
            if (!_viewModel.IsLoggedIn)
            {
                // Giriş başarısız, PIN'i temizle
                _currentPin = string.Empty;
                UpdatePinDisplay();
            }
        }
    }

    private void UpdatePinDisplay()
    {
        PinDisplay.Text = new string('●', _currentPin.Length);
    }
}