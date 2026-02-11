using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RestoranOtomasyon.Models;
using RestoranOtomasyon.Services;
using System.Collections.ObjectModel;

namespace RestoranOtomasyon.ViewModels;

/// <summary>
/// Masa ekranı ViewModel'i
/// </summary>
public partial class MasaViewModel : ViewModelBase, INavigationAware
{
    private readonly ILogger<MasaViewModel> _logger;
    private readonly IMasaService _masaService;
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly IAdisyonService _adisyonService;

    [ObservableProperty]
    private ObservableCollection<MasaItemViewModel> _masalar = new();

    [ObservableProperty]
    private ObservableCollection<MasaItemViewModel> _icMasalar = new();

    [ObservableProperty]
    private ObservableCollection<MasaItemViewModel> _disMasalar = new();

    [ObservableProperty]
    private ObservableCollection<MasaItemViewModel> _paketGelAlMasalar = new();

    [ObservableProperty]
    private ObservableCollection<AktifSiparisItemViewModel> _aktifGelAlPaketler = new();

    [ObservableProperty]
    private ObservableCollection<MasaItemViewModel> _currentMasalar = new();

    [ObservableProperty]
    private MasaItemViewModel? _selectedMasa;

    [ObservableProperty]
    private MasaBolum _selectedBolum = MasaBolum.Iceri;

    [ObservableProperty]
    private int _bosMasaSayisi;

    [ObservableProperty]
    private int _doluMasaSayisi;

    [ObservableProperty]
    private int _hesapBekleyenSayisi;

    public MasaViewModel(
        ILogger<MasaViewModel> logger,
        IMasaService masaService,
        ISessionService sessionService,
        INavigationService navigationService,
        IAdisyonService adisyonService)
    {
        _logger = logger;
        _masaService = masaService;
        _sessionService = sessionService;
        _navigationService = navigationService;
        _adisyonService = adisyonService;

        _logger.LogDebug("MasaViewModel oluşturuldu");
    }

    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadMasalarAsync();
    }

    public void OnNavigatedFrom()
    {
        // Cleanup if needed
    }

    [RelayCommand]
    private async Task LoadMasalarAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var masalar = await _masaService.GetAllMasalarAsync();
            
            Masalar.Clear();
            IcMasalar.Clear();
            DisMasalar.Clear();
            PaketGelAlMasalar.Clear();
            AktifGelAlPaketler.Clear();

            int bos = 0, dolu = 0, hesap = 0;

            foreach (var masa in masalar)
            {
                var vm = new MasaItemViewModel(masa);
                Masalar.Add(vm);

                switch (masa.Bolum)
                {
                    case MasaBolum.Iceri:
                        IcMasalar.Add(vm);
                        // Sadece gerçek masaları say (İç ve Dış mekan)
                        switch (masa.Durum)
                        {
                            case MasaDurum.Bos: bos++; break;
                            case MasaDurum.Dolu: dolu++; break;
                            case MasaDurum.Hesap: hesap++; break;
                        }
                        break;
                    case MasaBolum.Disari:
                        DisMasalar.Add(vm);
                        // Sadece gerçek masaları say (İç ve Dış mekan)
                        switch (masa.Durum)
                        {
                            case MasaDurum.Bos: bos++; break;
                            case MasaDurum.Dolu: dolu++; break;
                            case MasaDurum.Hesap: hesap++; break;
                        }
                        break;
                    case MasaBolum.GelAl:
                    case MasaBolum.Paket:
                        PaketGelAlMasalar.Add(vm);
                        // Gel-Al ve Paket sayılmaz
                        break;
                }
            }

            // Aktif Gel-Al ve Paket siparişlerini yükle
            var aktifSiparisler = await _adisyonService.GetAktifGelAlPaketAsync();
            foreach (var adisyon in aktifSiparisler)
            {
                AktifGelAlPaketler.Add(new AktifSiparisItemViewModel(adisyon));
            }

            // İlk açılışta seçili bölümün masalarını göster
            UpdateCurrentMasalar();

            BosMasaSayisi = bos;
            DoluMasaSayisi = dolu;
            HesapBekleyenSayisi = hesap;

            _logger.LogDebug("Masalar yüklendi: {Total} toplam, {Bos} boş, {Dolu} dolu, {Hesap} hesap, {Aktif} aktif gel-al/paket",
                masalar.Count(), bos, dolu, hesap, AktifGelAlPaketler.Count);
        });
    }

    [RelayCommand]
    private void SelectMasa(MasaItemViewModel? masaVm)
    {
        if (masaVm == null) return;

        SelectedMasa = masaVm;
        _logger.LogDebug("Masa seçildi: {MasaNo}", masaVm.Masa.MasaNo);

        // Masa durumuna göre işlem
        switch (masaVm.Masa.Durum)
        {
            case MasaDurum.Bos:
                // Sipariş ekranına git (masa durumu sipariş eklendiğinde değişir)
                OpenMasa(masaVm);
                break;
            
            case MasaDurum.Dolu:
                // Sipariş ekranına git
                _navigationService.NavigateToSiparis(masaVm.Masa.Id);
                break;
            
            case MasaDurum.Hesap:
                // Hesap bekleyen masaya tıklandığında sipariş ekranına git (ödeme için)
                _navigationService.NavigateToSiparis(masaVm.Masa.Id);
                break;
        }
    }

    private void OpenMasa(MasaItemViewModel masaVm)
    {
        // Boş masaya tıklandığında direkt sipariş ekranına git
        // Masa durumu sipariş eklendiğinde değişecek
        _logger.LogInformation("Boş masaya girildi: {MasaNo}", masaVm.Masa.MasaNo);
        _navigationService.NavigateToSiparis(masaVm.Masa.Id);
    }

    [RelayCommand]
    private async Task CloseMasaAsync(MasaItemViewModel? masaVm)
    {
        if (masaVm == null || masaVm.Masa.Durum == MasaDurum.Bos) return;

        // Sadece yönetici/admin kapatabilir (aktif adisyon varsa)
        if (!_sessionService.CurrentUser?.HasPermission("masa.kapat") ?? true)
        {
            _logger.LogWarning("Masa kapatma yetkisi yok");
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var oldDurum = masaVm.Masa.Durum;
            var success = await _masaService.UpdateMasaDurumAsync(masaVm.Masa.Id, MasaDurum.Bos);
            
            if (success)
            {
                masaVm.Masa.Durum = MasaDurum.Bos;
                masaVm.RefreshStatus();
                
                if (oldDurum == MasaDurum.Dolu) DoluMasaSayisi--;
                else if (oldDurum == MasaDurum.Hesap) HesapBekleyenSayisi--;
                BosMasaSayisi++;
                
                _logger.LogInformation("Masa kapatıldı: {MasaNo}", masaVm.Masa.MasaNo);
            }
        });
    }

    [RelayCommand]
    private void SelectBolum(MasaBolum bolum)
    {
        SelectedBolum = bolum;
        UpdateCurrentMasalar();
        _logger.LogDebug("Bölüm seçildi: {Bolum}", bolum);
    }

    private void UpdateCurrentMasalar()
    {
        CurrentMasalar.Clear();
        var source = SelectedBolum == MasaBolum.Disari ? DisMasalar : IcMasalar;
        foreach (var masa in source)
        {
            CurrentMasalar.Add(masa);
        }
    }

    [RelayCommand]
    private void GelAlSec()
    {
        _logger.LogInformation("Gel-Al seçildi");
        _navigationService.NavigateToGelAl();
    }

    [RelayCommand]
    private void PaketSec()
    {
        _logger.LogInformation("Paket sipariş seçildi");
        _navigationService.NavigateToPaket();
    }

    [RelayCommand]
    private void OpenAktifSiparis(AktifSiparisItemViewModel? siparis)
    {
        if (siparis == null) return;
        _logger.LogInformation("Aktif sipariş açılıyor: {AdisyonId}", siparis.Adisyon.Id);
        _navigationService.NavigateTo<SiparisViewModel>(siparis.Adisyon.Id);
    }
}

/// <summary>
/// Tek masa için ViewModel (UI binding için)
/// </summary>
public partial class MasaItemViewModel : ObservableObject
{
    public Masa Masa { get; }

    [ObservableProperty]
    private string _backgroundColor = "#27ae60";

    [ObservableProperty]
    private string _statusIcon = "🟢";

    [ObservableProperty]
    private string _displayName = "";

    public MasaItemViewModel(Masa masa)
    {
        Masa = masa;
        RefreshStatus();
    }

    public void RefreshStatus()
    {
        DisplayName = Masa.MasaNo;
        
        switch (Masa.Durum)
        {
            case MasaDurum.Bos:
                BackgroundColor = "#27ae60"; // Yeşil
                StatusIcon = "🟢";
                break;
            case MasaDurum.Dolu:
                BackgroundColor = "#e74c3c"; // Kırmızı
                StatusIcon = "🔴";
                break;
            case MasaDurum.Hesap:
                BackgroundColor = "#f39c12"; // Turuncu
                StatusIcon = "🟡";
                break;
        }
    }
}

/// <summary>
/// Aktif Gel-Al/Paket siparişi için ViewModel
/// </summary>
public partial class AktifSiparisItemViewModel : ObservableObject
{
    public Adisyon Adisyon { get; }

    [ObservableProperty]
    private string _backgroundColor = "#9b59b6";

    [ObservableProperty]
    private string _statusIcon = "🛍️";

    [ObservableProperty]
    private string _displayName = "";

    [ObservableProperty]
    private string _tutarText = "";

    [ObservableProperty]
    private string _saatText = "";

    public AktifSiparisItemViewModel(Adisyon adisyon)
    {
        Adisyon = adisyon;
        RefreshStatus();
    }

    public void RefreshStatus()
    {
        if (Adisyon.Tip == AdisyonTip.GelAl)
        {
            DisplayName = $"Gel-Al #{Adisyon.Id}";
            StatusIcon = "🛍️";
            BackgroundColor = "#9b59b6"; // Mor
        }
        else
        {
            DisplayName = $"Paket #{Adisyon.Id}";
            StatusIcon = "📦";
            BackgroundColor = "#3498db"; // Mavi
        }

        TutarText = $"₺{Adisyon.ToplamTutar:N2}";
        SaatText = Adisyon.OlusturmaTarihi.ToString("HH:mm");
    }
}
