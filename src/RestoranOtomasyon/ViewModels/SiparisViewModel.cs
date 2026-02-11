using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RestoranOtomasyon.Models;
using RestoranOtomasyon.Services;
using System.Collections.ObjectModel;

namespace RestoranOtomasyon.ViewModels;

/// <summary>
/// Sipariş ekranı ViewModel'i
/// </summary>
public partial class SiparisViewModel : ViewModelBase, INavigationAware
{
    private readonly ILogger<SiparisViewModel> _logger;
    private readonly IKategoriService _kategoriService;
    private readonly IUrunService _urunService;
    private readonly IAdisyonService _adisyonService;
    private readonly IMasaService _masaService;
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private Masa? _currentMasa;

    [ObservableProperty]
    private Adisyon? _currentAdisyon;

    [ObservableProperty]
    private ObservableCollection<Kategori> _kategoriler = new();

    [ObservableProperty]
    private ObservableCollection<UrunItemViewModel> _urunler = new();

    [ObservableProperty]
    private ObservableCollection<UrunItemViewModel> _favoriler = new();

    [ObservableProperty]
    private ObservableCollection<AdisyonKalemViewModel> _kalemler = new();

    [ObservableProperty]
    private ObservableCollection<UrunNotu> _urunNotlari = new();

    [ObservableProperty]
    private Kategori? _selectedKategori;

    [ObservableProperty]
    private AdisyonKalemViewModel? _selectedKalem;

    [ObservableProperty]
    private decimal _toplamTutar;

    [ObservableProperty]
    private string _masaBaslik = "Sipariş";

    [ObservableProperty]
    private bool _isNotPopupOpen;

    [ObservableProperty]
    private string _kalemNotu = "";

    // Yarım porsiyon seçim popup
    [ObservableProperty]
    private bool _isPorsiyonPopupOpen;

    [ObservableProperty]
    private UrunItemViewModel? _pendingUrun;

    // Fiyat düzenleme popup
    [ObservableProperty]
    private bool _isFiyatPopupOpen;

    [ObservableProperty]
    private AdisyonKalemViewModel? _fiyatDuzenlenenKalem;

    [ObservableProperty]
    private decimal _yeniFiyat;

    // Ödeme popup
    [ObservableProperty]
    private bool _isOdemePopupOpen;

    [ObservableProperty]
    private OdemeViewModel? _odemeViewModel;

    // Gel-Al/Paket için bekleyen tip (ürün eklenince adisyon oluşacak)
    private AdisyonTip? _pendingAdisyonTip;

    public SiparisViewModel(
        ILogger<SiparisViewModel> logger,
        IKategoriService kategoriService,
        IUrunService urunService,
        IAdisyonService adisyonService,
        IMasaService masaService,
        ISessionService sessionService,
        INavigationService navigationService)
    {
        _logger = logger;
        _kategoriService = kategoriService;
        _urunService = urunService;
        _adisyonService = adisyonService;
        _masaService = masaService;
        _sessionService = sessionService;
        _navigationService = navigationService;

        _logger.LogDebug("SiparisViewModel oluşturuldu");
    }

    public void SetOdemeViewModel(OdemeViewModel odemeVm)
    {
        // Önceki event'i temizle
        if (OdemeViewModel != null)
        {
            OdemeViewModel.OdemeCompleted -= OnOdemeCompleted;
        }
        
        OdemeViewModel = odemeVm;
        
        // Yeni event'e abone ol
        if (OdemeViewModel != null)
        {
            OdemeViewModel.OdemeCompleted += OnOdemeCompleted;
        }
    }

    private void OnOdemeCompleted()
    {
        // Ödeme tamamlandığında popup'ı kapat ve masalara dön
        IsOdemePopupOpen = false;
        _navigationService.NavigateToMasalar();
    }

    public async void OnNavigatedTo(object? parameter)
    {
        if (parameter is int id)
        {
            // ID masa mı yoksa adisyon mu kontrol et
            var adisyon = await _adisyonService.GetByIdAsync(id);
            if (adisyon != null && adisyon.MasaId == null)
            {
                // Bu bir Gel-Al veya Paket adisyonu
                await LoadAdisyonAsync(adisyon);
            }
            else
            {
                // Bu bir masa ID'si
                await LoadMasaAsync(id);
            }
        }
        else if (parameter is string tip && tip == "GelAl")
        {
            await CreateGelAlAdisyonAsync();
        }
        else if (parameter is string tip2 && tip2 == "Paket")
        {
            await CreatePaketAdisyonAsync();
        }

        await LoadKategorilerAsync();
        await LoadFavorilerAsync();
        await LoadUrunNotlariAsync();
    }

    public void OnNavigatedFrom()
    {
        // Cleanup
    }

    private async Task LoadMasaAsync(int masaId)
    {
        await ExecuteBusyAsync(async () =>
        {
            CurrentMasa = await _masaService.GetMasaByIdAsync(masaId);
            if (CurrentMasa != null)
            {
                MasaBaslik = $"Masa {CurrentMasa.MasaNo}";
                
                // Aktif adisyon var mı?
                CurrentAdisyon = await _adisyonService.GetAktifByMasaAsync(masaId);
                
                if (CurrentAdisyon == null)
                {
                    // Yeni adisyon oluştur
                    var adisyon = new Adisyon
                    {
                        MasaId = masaId,
                        KullaniciId = _sessionService.CurrentUser?.Id ?? 0,
                        Tip = AdisyonTip.Masa
                    };
                    var adisyonId = await _adisyonService.CreateAsync(adisyon);
                    CurrentAdisyon = await _adisyonService.GetByIdAsync(adisyonId);
                }
                
                await LoadKalemlerAsync();
                _logger.LogInformation("Masa yüklendi: {MasaNo}, Adisyon: {AdisyonId}", 
                    CurrentMasa.MasaNo, CurrentAdisyon?.Id);
            }
        });
    }

    private async Task LoadAdisyonAsync(Adisyon adisyon)
    {
        await ExecuteBusyAsync(async () =>
        {
            CurrentMasa = null;
            CurrentAdisyon = adisyon;
            
            if (adisyon.Tip == AdisyonTip.GelAl)
                MasaBaslik = $"Gel-Al #{adisyon.Id}";
            else if (adisyon.Tip == AdisyonTip.Paket)
                MasaBaslik = $"Paket #{adisyon.Id}";
            else
                MasaBaslik = $"Sipariş #{adisyon.Id}";
            
            await LoadKalemlerAsync();
            _logger.LogInformation("Adisyon yüklendi: {AdisyonId}, Tip: {Tip}", adisyon.Id, adisyon.Tip);
        });
    }

    private Task CreateGelAlAdisyonAsync()
    {
        // Adisyon hemen oluşturulmasın, ilk ürün eklendiğinde oluşacak
        MasaBaslik = "Yeni Gel-Al";
        CurrentMasa = null;
        CurrentAdisyon = null;
        _pendingAdisyonTip = AdisyonTip.GelAl;
        Kalemler.Clear();
        ToplamTutar = 0;
        _logger.LogInformation("Gel-Al hazırlandı, ürün bekliyor");
        return Task.CompletedTask;
    }

    private Task CreatePaketAdisyonAsync()
    {
        // Adisyon hemen oluşturulmasın, ilk ürün eklendiğinde oluşacak
        MasaBaslik = "Yeni Paket";
        CurrentMasa = null;
        CurrentAdisyon = null;
        _pendingAdisyonTip = AdisyonTip.Paket;
        Kalemler.Clear();
        ToplamTutar = 0;
        _logger.LogInformation("Paket hazırlandı, ürün bekliyor");
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task LoadKategorilerAsync()
    {
        var kategoriler = await _kategoriService.GetActiveAsync();
        Kategoriler.Clear();
        foreach (var k in kategoriler)
        {
            Kategoriler.Add(k);
        }

        if (Kategoriler.Any() && SelectedKategori == null)
        {
            SelectedKategori = Kategoriler.First();
        }
    }

    [RelayCommand]
    private async Task LoadFavorilerAsync()
    {
        var favoriler = await _urunService.GetFavorilerAsync();
        Favoriler.Clear();
        Urunler.Clear();
        SelectedKategori = null; // Kategori seçimini kaldır
        
        foreach (var u in favoriler)
        {
            var vm = new UrunItemViewModel(u);
            Favoriler.Add(vm);
            Urunler.Add(vm); // Ürünlere de ekle ki UI'da görünsün
        }
    }

    private async Task LoadUrunNotlariAsync()
    {
        var notlar = await _urunService.GetUrunNotlariAsync();
        UrunNotlari.Clear();
        foreach (var n in notlar)
        {
            UrunNotlari.Add(n);
        }
    }

    partial void OnSelectedKategoriChanged(Kategori? value)
    {
        if (value != null)
        {
            _ = LoadUrunlerAsync(value.Id);
        }
    }

    [RelayCommand]
    private async Task SelectKategoriAsync(Kategori kategori)
    {
        SelectedKategori = kategori;
        await LoadUrunlerAsync(kategori.Id);
    }

    private async Task LoadUrunlerAsync(int kategoriId)
    {
        var urunler = await _urunService.GetByKategoriAsync(kategoriId);
        Urunler.Clear();
        foreach (var u in urunler)
        {
            Urunler.Add(new UrunItemViewModel(u));
        }
    }

    [RelayCommand]
    private async Task AddUrunAsync(UrunItemViewModel urunVm)
    {
        // Yarım porsiyon varsa seçim popup'ı göster
        if (urunVm.Urun.YarimPorsiyonVar)
        {
            PendingUrun = urunVm;
            IsPorsiyonPopupOpen = true;
            return;
        }

        // Yarım porsiyon yoksa direkt tam porsiyon ekle
        await AddUrunWithPorsiyonAsync(urunVm, false);
    }

    [RelayCommand]
    private async Task SelectTamPorsiyonAsync()
    {
        if (PendingUrun == null) return;
        IsPorsiyonPopupOpen = false;
        await AddUrunWithPorsiyonAsync(PendingUrun, false);
        PendingUrun = null;
    }

    [RelayCommand]
    private async Task SelectYarimPorsiyonAsync()
    {
        if (PendingUrun == null) return;
        IsPorsiyonPopupOpen = false;
        await AddUrunWithPorsiyonAsync(PendingUrun, true);
        PendingUrun = null;
    }

    [RelayCommand]
    private void ClosePorsiyonPopup()
    {
        IsPorsiyonPopupOpen = false;
        PendingUrun = null;
    }

    private async Task AddUrunWithPorsiyonAsync(UrunItemViewModel urunVm, bool yarimPorsiyon)
    {
        await ExecuteBusyAsync(async () =>
        {
            // Eğer pending Gel-Al/Paket varsa, şimdi adisyon oluştur
            if (CurrentAdisyon == null && _pendingAdisyonTip != null)
            {
                var adisyon = new Adisyon
                {
                    MasaId = null,
                    KullaniciId = _sessionService.CurrentUser?.Id ?? 0,
                    Tip = _pendingAdisyonTip.Value
                };
                var adisyonId = await _adisyonService.CreateAsync(adisyon);
                CurrentAdisyon = await _adisyonService.GetByIdAsync(adisyonId);
                
                // Başlığı güncelle
                if (_pendingAdisyonTip == AdisyonTip.GelAl)
                    MasaBaslik = $"Gel-Al #{adisyonId}";
                else
                    MasaBaslik = $"Paket #{adisyonId}";
                
                _pendingAdisyonTip = null;
                _logger.LogInformation("Adisyon oluşturuldu: {AdisyonId}", adisyonId);
            }

            if (CurrentAdisyon == null) return;

            // Fiyatı belirle
            var fiyat = yarimPorsiyon && urunVm.Urun.YarimPorsiyonFiyat.HasValue
                ? urunVm.Urun.YarimPorsiyonFiyat.Value
                : urunVm.Urun.Fiyat;

            var kalem = new AdisyonKalem
            {
                AdisyonId = CurrentAdisyon.Id,
                UrunId = urunVm.Urun.Id,
                Adet = 1,
                BirimFiyat = fiyat,
                YarimPorsiyon = yarimPorsiyon
            };

            await _adisyonService.AddKalemAsync(kalem);
            await LoadKalemlerAsync();
            
            // İlk ürün eklendiğinde masayı dolu yap
            if (CurrentMasa != null && CurrentMasa.Durum == MasaDurum.Bos)
            {
                await _masaService.UpdateMasaDurumAsync(CurrentMasa.Id, MasaDurum.Dolu);
                CurrentMasa.Durum = MasaDurum.Dolu;
                _logger.LogInformation("Masa dolu yapıldı: {MasaNo}", CurrentMasa.MasaNo);
            }
            
            var porsiyonText = yarimPorsiyon ? " (Yarım)" : "";
            _logger.LogDebug("Ürün eklendi: {UrunAd}{Porsiyon}", urunVm.Urun.Ad, porsiyonText);
        });
    }

    private async Task LoadKalemlerAsync()
    {
        if (CurrentAdisyon == null) return;

        var kalemler = await _adisyonService.GetKalemlerAsync(CurrentAdisyon.Id);
        Kalemler.Clear();
        
        foreach (var k in kalemler)
        {
            Kalemler.Add(new AdisyonKalemViewModel(k));
        }

        ToplamTutar = await _adisyonService.CalculateTotalAsync(CurrentAdisyon.Id);
    }

    [RelayCommand]
    private async Task IncreaseAdetAsync(AdisyonKalemViewModel kalemVm)
    {
        await ExecuteBusyAsync(async () =>
        {
            await _adisyonService.UpdateKalemAdetAsync(kalemVm.Kalem.Id, kalemVm.Kalem.Adet + 1);
            await LoadKalemlerAsync();
        });
    }

    [RelayCommand]
    private async Task DecreaseAdetAsync(AdisyonKalemViewModel kalemVm)
    {
        if (kalemVm.Kalem.Adet <= 1)
        {
            // Silme onayı iste
            await RemoveKalemAsync(kalemVm);
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            await _adisyonService.UpdateKalemAdetAsync(kalemVm.Kalem.Id, kalemVm.Kalem.Adet - 1);
            await LoadKalemlerAsync();
        });
    }

    [RelayCommand]
    private async Task RemoveKalemAsync(AdisyonKalemViewModel kalemVm)
    {
        // TODO: Onay dialog'u ekle
        await ExecuteBusyAsync(async () =>
        {
            await _adisyonService.RemoveKalemAsync(kalemVm.Kalem.Id);
            await LoadKalemlerAsync();
            _logger.LogInformation("Kalem silindi: {UrunAd}", kalemVm.UrunAd);
            
            // Eğer hiç kalem kalmadıysa masayı boşalt
            await CheckAndUpdateMasaDurumAsync();
        });
    }

    [RelayCommand]
    private void OpenNotPopup(AdisyonKalemViewModel kalemVm)
    {
        SelectedKalem = kalemVm;
        KalemNotu = kalemVm.Kalem.Notlar ?? "";
        IsNotPopupOpen = true;
    }

    [RelayCommand]
    private void AddHazirNot(UrunNotu not)
    {
        if (string.IsNullOrEmpty(KalemNotu))
            KalemNotu = not.Ad;
        else
            KalemNotu += ", " + not.Ad;
    }

    [RelayCommand]
    private async Task SaveKalemNotAsync()
    {
        if (SelectedKalem == null) return;

        await ExecuteBusyAsync(async () =>
        {
            await _adisyonService.SetKalemNotAsync(SelectedKalem.Kalem.Id, KalemNotu);
            await LoadKalemlerAsync();
            IsNotPopupOpen = false;
            _logger.LogDebug("Kalem notu kaydedildi: {Not}", KalemNotu);
        });
    }

    [RelayCommand]
    private void CloseNotPopup()
    {
        IsNotPopupOpen = false;
        KalemNotu = "";
        SelectedKalem = null;
    }

    // Fiyat düzenleme popup işlemleri
    [RelayCommand]
    private void OpenFiyatPopup(AdisyonKalemViewModel kalemVm)
    {
        FiyatDuzenlenenKalem = kalemVm;
        YeniFiyat = kalemVm.Kalem.BirimFiyat;
        IsFiyatPopupOpen = true;
    }

    [RelayCommand]
    private void CloseFiyatPopup()
    {
        IsFiyatPopupOpen = false;
        FiyatDuzenlenenKalem = null;
        YeniFiyat = 0;
    }

    [RelayCommand]
    private async Task SaveKalemFiyatAsync()
    {
        if (FiyatDuzenlenenKalem == null || YeniFiyat <= 0) return;

        await ExecuteBusyAsync(async () =>
        {
            await _adisyonService.UpdateKalemFiyatAsync(FiyatDuzenlenenKalem.Kalem.Id, YeniFiyat);
            await LoadKalemlerAsync();
            IsFiyatPopupOpen = false;
            _logger.LogInformation("Kalem fiyatı güncellendi: {UrunAd} -> {Fiyat}₺", 
                FiyatDuzenlenenKalem.UrunAd, YeniFiyat);
            FiyatDuzenlenenKalem = null;
        });
    }

    [RelayCommand]
    private void AdjustFiyat(string amount)
    {
        if (decimal.TryParse(amount, out var delta))
        {
            YeniFiyat = Math.Max(0, YeniFiyat + delta);
        }
    }

    [RelayCommand]
    private async Task HesapIsteAsync()
    {
        if (CurrentAdisyon == null || CurrentMasa == null) return;

        await ExecuteBusyAsync(async () =>
        {
            await _adisyonService.UpdateDurumAsync(CurrentAdisyon.Id, AdisyonDurum.Hesap);
            await _masaService.UpdateMasaDurumAsync(CurrentMasa.Id, MasaDurum.Hesap);
            CurrentAdisyon.Durum = AdisyonDurum.Hesap;
            _logger.LogInformation("Hesap istendi: Masa {MasaNo}", CurrentMasa.MasaNo);
            
            // Hesap istendikten sonra ödeme ekranını aç
            if (OdemeViewModel != null)
            {
                await OdemeViewModel.InitializeAsync(CurrentAdisyon.Id);
                IsOdemePopupOpen = true;
            }
        });
    }

    [RelayCommand]
    private async Task OpenOdemePopupAsync()
    {
        if (CurrentAdisyon == null || Kalemler.Count == 0) return;

        if (OdemeViewModel != null)
        {
            await OdemeViewModel.InitializeAsync(CurrentAdisyon.Id);
            IsOdemePopupOpen = true;
            _logger.LogInformation("Ödeme ekranı açıldı: Adisyon {AdisyonId}", CurrentAdisyon.Id);
        }
    }

    [RelayCommand]
    private void CloseOdemePopup()
    {
        IsOdemePopupOpen = false;
        
        // Ödeme tamamlandıysa masalara dön
        if (OdemeViewModel?.IsOdemeCompleted == true)
        {
            _navigationService.NavigateToMasalar();
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        // Geri dönmeden önce masa durumunu kontrol et
        await CheckAndUpdateMasaDurumAsync();
        _navigationService.NavigateToMasalar();
    }
    
    /// <summary>
    /// Adisyonda kalem yoksa masayı boşalt
    /// </summary>
    private async Task CheckAndUpdateMasaDurumAsync()
    {
        if (CurrentMasa == null || CurrentAdisyon == null) return;
        
        // Aktif kalem sayısını kontrol et
        if (Kalemler.Count == 0)
        {
            // Hiç kalem yok - masayı boşalt
            await _masaService.UpdateMasaDurumAsync(CurrentMasa.Id, MasaDurum.Bos);
            CurrentMasa.Durum = MasaDurum.Bos;
            _logger.LogInformation("Masa boşaltıldı (kalem yok): Masa {MasaId}", CurrentMasa.Id);
        }
    }
}

/// <summary>
/// Ürün item ViewModel
/// </summary>
public partial class UrunItemViewModel : ObservableObject
{
    public Urun Urun { get; }

    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    private string _fiyatText;

    [ObservableProperty]
    private bool _isFavori;

    public UrunItemViewModel(Urun urun)
    {
        Urun = urun;
        _displayName = urun.Ad;
        _fiyatText = $"₺{urun.Fiyat:N2}";
        _isFavori = urun.Favori;
    }
}

/// <summary>
/// Adisyon kalem ViewModel
/// </summary>
public partial class AdisyonKalemViewModel : ObservableObject
{
    public AdisyonKalem Kalem { get; }

    [ObservableProperty]
    private string _urunAd;

    [ObservableProperty]
    private int _adet;

    [ObservableProperty]
    private string _birimFiyatText;

    [ObservableProperty]
    private string _toplamText;

    [ObservableProperty]
    private string _notlar;

    [ObservableProperty]
    private bool _hasNot;

    [ObservableProperty]
    private bool _isIkram;

    [ObservableProperty]
    private bool _isYarimPorsiyon;

    // Alman usulü ödeme için - bu kalem seçildi mi?
    [ObservableProperty]
    private bool _isSelected;

    // Bu kalem ödendi mi?
    [ObservableProperty]
    private bool _isOdendi;

    public decimal KalemToplam => Kalem.Adet * Kalem.BirimFiyat;

    public AdisyonKalemViewModel(AdisyonKalem kalem)
    {
        Kalem = kalem;
        // Yarım porsiyon ise (Y.P) ekle
        var yarimText = kalem.YarimPorsiyon ? " (Y.P)" : "";
        _urunAd = (kalem.UrunAd ?? $"Ürün #{kalem.UrunId}") + yarimText;
        _adet = kalem.Adet;
        _birimFiyatText = $"₺{kalem.BirimFiyat:N2}";
        _toplamText = $"₺{kalem.Adet * kalem.BirimFiyat:N2}";
        _notlar = kalem.Notlar ?? "";
        _hasNot = !string.IsNullOrEmpty(kalem.Notlar);
        _isIkram = kalem.Durum == KalemDurum.Ikram;
        _isYarimPorsiyon = kalem.YarimPorsiyon;
        _isSelected = false;
        _isOdendi = false;
    }
}
