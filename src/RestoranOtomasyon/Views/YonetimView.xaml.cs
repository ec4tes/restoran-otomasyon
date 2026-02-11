using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using RestoranOtomasyon.ViewModels;

namespace RestoranOtomasyon.Views;

public partial class YonetimView : UserControl
{
    private readonly YonetimViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public YonetimView(YonetimViewModel viewModel, IServiceProvider serviceProvider)
    {
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        DataContext = viewModel;
        InitializeComponent();
        
        // İlk tab'ı yükle
        LoadTab("Kategoriler");
        
        // Tab değişikliğini izle
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.SelectedTab))
            {
                LoadTab(viewModel.SelectedTab);
            }
        };
    }

    private void LoadTab(string tab)
    {
        UserControl view = tab switch
        {
            "Kategoriler" => _serviceProvider.GetRequiredService<KategoriYonetimView>(),
            "Urunler" => _serviceProvider.GetRequiredService<UrunYonetimView>(),
            "Kullanicilar" => _serviceProvider.GetRequiredService<KullaniciYonetimView>(),
            "Masalar" => _serviceProvider.GetRequiredService<MasaYonetimView>(),
            "Raporlar" => _serviceProvider.GetRequiredService<RaporView>(),
            "FiyatGuncelleme" => _serviceProvider.GetRequiredService<FiyatGuncellemeView>(),
            "Sifirlama" => _serviceProvider.GetRequiredService<SifirlamaView>(),
            _ => _serviceProvider.GetRequiredService<KategoriYonetimView>()
        };
        
        ContentArea.Content = view;
    }
}
