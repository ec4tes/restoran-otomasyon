using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RestoranOtomasyon.Models;
using RestoranOtomasyon.Services;
using System.Collections.ObjectModel;

namespace RestoranOtomasyon.ViewModels;

/// <summary>
/// Ödeme tipi enum
/// </summary>
public enum OdemeTipSecim
{
    Nakit,
    Kart,
    Karisik
}

/// <summary>
/// Ödeme ekranı ViewModel'i
/// </summary>
public partial class OdemeViewModel : ViewModelBase
{
    private readonly ILogger<OdemeViewModel> _logger;
    private readonly IOdemeService _odemeService;
    private readonly IAdisyonService _adisyonService;
    private readonly IAuthService _authService;
    private readonly ISessionService _sessionService;

    // Ödeme tamamlandığında tetiklenir
    public event Action? OdemeCompleted;

    [ObservableProperty]
    private int _adisyonId;

    [ObservableProperty]
    private decimal _toplamTutar;

    [ObservableProperty]
    private decimal _indirimTutar;

    [ObservableProperty]
    private decimal _odenecekTutar;

    [ObservableProperty]
    private decimal _alinanTutar;

    [ObservableProperty]
    private decimal _paraUstu;

    [ObservableProperty]
    private decimal _nakitTutar;

    [ObservableProperty]
    private decimal _kartTutar;

    [ObservableProperty]
    private OdemeTipSecim _selectedOdemeTipi = OdemeTipSecim.Nakit;

    [ObservableProperty]
    private string _resultMessage = "";

    // Alman usulü - ürün bazlı ödeme modu
    [ObservableProperty]
    private bool _isAlmanUsuluMode;

    [ObservableProperty]
    private decimal _almanUsuluOdenenTutar;

    [ObservableProperty]
    private decimal _almanUsuluKalanTutar;

    [ObservableProperty]
    private bool _isSuccess;

    [ObservableProperty]
    private bool _showResult;

    [ObservableProperty]
    private bool _isOdemeCompleted;

    // İndirim popup
    [ObservableProperty]
    private bool _isIndirimPopupOpen;

    [ObservableProperty]
    private bool _isIndirimYuzde = true;

    [ObservableProperty]
    private decimal _indirimDeger;

    [ObservableProperty]
    private string _indirimNeden = "";

    // Yönetici PIN popup
    [ObservableProperty]
    private bool _isYoneticiPinPopupOpen;

    [ObservableProperty]
    private string _yoneticiPin = "";

    [ObservableProperty]
    private string _pinErrorMessage = "";

    [ObservableProperty]
    private string _pendingAction = ""; // "indirim" veya "ikram"

    // İkram için
    [ObservableProperty]
    private int _ikramKalemId;

    [ObservableProperty]
    private string _ikramNeden = "";

    [ObservableProperty]
    private bool _isIkramPopupOpen;

    [ObservableProperty]
    private ObservableCollection<AdisyonKalemViewModel> _kalemler = new();

    [ObservableProperty]
    private AdisyonKalemViewModel? _selectedKalem;

    // Fiş yazdırma servisi
    private readonly IFisYazdirmaService _fisYazdirmaService;
    private Adisyon? _currentAdisyon;

    public OdemeViewModel(
        ILogger<OdemeViewModel> logger,
        IOdemeService odemeService,
        IAdisyonService adisyonService,
        IAuthService authService,
        ISessionService sessionService,
        IFisYazdirmaService fisYazdirmaService)
    {
        _logger = logger;
        _odemeService = odemeService;
        _adisyonService = adisyonService;
        _authService = authService;
        _sessionService = sessionService;
        _fisYazdirmaService = fisYazdirmaService;
    }

    public async Task InitializeAsync(int adisyonId)
    {
        AdisyonId = adisyonId;
        await LoadAdisyonAsync();
    }

    private async Task LoadAdisyonAsync()
    {
        var adisyon = await _adisyonService.GetByIdAsync(AdisyonId);
        if (adisyon == null) return;

        _currentAdisyon = adisyon;
        ToplamTutar = adisyon.ToplamTutar;
        IndirimTutar = adisyon.IndirimTutar;
        OdenecekTutar = ToplamTutar - IndirimTutar;
        AlinanTutar = OdenecekTutar;

        var kalemler = await _adisyonService.GetKalemlerAsync(AdisyonId);
        Kalemler.Clear();
        
        // Her adeti ayrı satır olarak ekle (Alman usulü için)
        foreach (var k in kalemler)
        {
            // Adet kadar ayrı satır oluştur
            for (int i = 0; i < k.Adet; i++)
            {
                // Her biri için adet=1 olan kopya oluştur
                var tekKalem = new AdisyonKalem
                {
                    Id = k.Id,
                    AdisyonId = k.AdisyonId,
                    UrunId = k.UrunId,
                    UrunAd = k.UrunAd,
                    Adet = 1, // Her zaman 1
                    BirimFiyat = k.BirimFiyat,
                    Notlar = k.Notlar,
                    Durum = k.Durum
                };
                Kalemler.Add(new AdisyonKalemViewModel(tekKalem));
            }
        }

        // Alman usulü değerlerini sıfırla
        AlmanUsuluOdenenTutar = 0;
        AlmanUsuluKalanTutar = OdenecekTutar;
        IsAlmanUsuluMode = false;

        UpdateCalculations();
    }

    partial void OnAlinanTutarChanged(decimal value)
    {
        // Alman usulü modda seçili tutar değişmemeli, sadece para üstü hesapla
        if (IsAlmanUsuluMode)
        {
            var selectedTotal = Kalemler.Where(k => k.IsSelected && !k.IsOdendi).Sum(k => k.KalemToplam);
            if (SelectedOdemeTipi == OdemeTipSecim.Nakit)
            {
                ParaUstu = value - selectedTotal;
                if (ParaUstu < 0) ParaUstu = 0;
            }
            return;
        }
        UpdateCalculations();
    }

    partial void OnNakitTutarChanged(decimal value)
    {
        if (SelectedOdemeTipi == OdemeTipSecim.Karisik)
        {
            KartTutar = OdenecekTutar - value;
            if (KartTutar < 0) KartTutar = 0;
        }
    }

    partial void OnSelectedOdemeTipiChanged(OdemeTipSecim value)
    {
        // Alman usulü modda ödeme tipi değişince seçili tutarı koru
        if (IsAlmanUsuluMode)
        {
            var selectedTotal = Kalemler.Where(k => k.IsSelected && !k.IsOdendi).Sum(k => k.KalemToplam);
            OdenecekTutar = selectedTotal;
            AlinanTutar = selectedTotal;
            ParaUstu = 0;
            return;
        }
        UpdateCalculations();
    }

    private void UpdateCalculations()
    {
        // Alman usulü modda bu fonksiyon çağrılmamalı
        if (IsAlmanUsuluMode) return;
        
        // Normal mod
        OdenecekTutar = ToplamTutar - IndirimTutar;

        switch (SelectedOdemeTipi)
        {
            case OdemeTipSecim.Nakit:
                ParaUstu = AlinanTutar - OdenecekTutar;
                if (ParaUstu < 0) ParaUstu = 0;
                break;

            case OdemeTipSecim.Kart:
                ParaUstu = 0;
                KartTutar = OdenecekTutar;
                NakitTutar = 0;
                break;

            case OdemeTipSecim.Karisik:
                ParaUstu = 0;
                if (NakitTutar > OdenecekTutar)
                    NakitTutar = OdenecekTutar;
                KartTutar = OdenecekTutar - NakitTutar;
                break;
        }
    }

    [RelayCommand]
    private void AddDigit(string digit)
    {
        if (SelectedOdemeTipi == OdemeTipSecim.Nakit)
        {
            var current = AlinanTutar.ToString("0");
            if (current == "0")
                current = digit;
            else
                current += digit;
            
            if (decimal.TryParse(current, out var value))
                AlinanTutar = value;
        }
        else if (SelectedOdemeTipi == OdemeTipSecim.Karisik)
        {
            var current = NakitTutar.ToString("0");
            if (current == "0")
                current = digit;
            else
                current += digit;
            
            if (decimal.TryParse(current, out var value))
                NakitTutar = value;
        }
    }

    [RelayCommand]
    private void ClearAmount()
    {
        if (SelectedOdemeTipi == OdemeTipSecim.Nakit)
            AlinanTutar = 0;
        else if (SelectedOdemeTipi == OdemeTipSecim.Karisik)
            NakitTutar = 0;
    }

    [RelayCommand]
    private void SetExactAmount()
    {
        if (SelectedOdemeTipi == OdemeTipSecim.Nakit)
            AlinanTutar = OdenecekTutar;
        else if (SelectedOdemeTipi == OdemeTipSecim.Karisik)
            NakitTutar = OdenecekTutar;
    }

    #region Alman Usulü (Ürün Bazlı Ödeme)

    [RelayCommand]
    private void ToggleAlmanUsulu()
    {
        IsAlmanUsuluMode = !IsAlmanUsuluMode;
        
        if (IsAlmanUsuluMode)
        {
            // Alman usulü moda geçildi
            foreach (var kalem in Kalemler)
            {
                kalem.IsSelected = false;
            }
            AlmanUsuluOdenenTutar = 0;
            AlmanUsuluKalanTutar = ToplamTutar - IndirimTutar;
            OdenecekTutar = 0;
            AlinanTutar = 0;
        }
        else
        {
            // Normal moda geri dön
            AlmanUsuluOdenenTutar = 0;
            AlmanUsuluKalanTutar = 0;
            OdenecekTutar = ToplamTutar - IndirimTutar;
            AlinanTutar = OdenecekTutar;
        }
        
        // UI'ı zorla güncelle
        OnPropertyChanged(nameof(OdenecekTutar));
        OnPropertyChanged(nameof(AlinanTutar));
    }

    [RelayCommand]
    private void ToggleKalemSecim(AdisyonKalemViewModel kalem)
    {
        if (!IsAlmanUsuluMode || kalem.IsOdendi) return;

        kalem.IsSelected = !kalem.IsSelected;
        CalculateAlmanUsuluTutar();
    }

    [RelayCommand]
    private void SelectAllKalemler()
    {
        if (!IsAlmanUsuluMode) return;

        foreach (var kalem in Kalemler.Where(k => !k.IsOdendi))
        {
            kalem.IsSelected = true;
        }
        CalculateAlmanUsuluTutar();
    }

    private void CalculateAlmanUsuluTutar()
    {
        // Seçili ürünlerin toplamı = Ödenecek tutar
        var selectedTotal = Kalemler.Where(k => k.IsSelected && !k.IsOdendi).Sum(k => k.KalemToplam);
        
        OdenecekTutar = selectedTotal;
        AlinanTutar = selectedTotal;
        AlmanUsuluKalanTutar = ToplamTutar - IndirimTutar - AlmanUsuluOdenenTutar - selectedTotal;
    }

    [RelayCommand]
    private async Task ProcessAlmanUsuluOdemeAsync()
    {
        if (!IsAlmanUsuluMode) return;

        var selectedKalemler = Kalemler.Where(k => k.IsSelected && !k.IsOdendi).ToList();
        if (!selectedKalemler.Any())
        {
            ResultMessage = "Lütfen ödenecek ürünleri seçin!";
            ShowResult = true;
            IsSuccess = false;
            return;
        }

        var selectedTotal = selectedKalemler.Sum(k => k.KalemToplam);

        // Ödemeyi işle (seçilen tutara göre)
        OdemeSonuc result;

        switch (SelectedOdemeTipi)
        {
            case OdemeTipSecim.Nakit:
                // Nakit'te alınan tutar >= seçilen tutar olmalı
                if (AlinanTutar < selectedTotal)
                {
                    ResultMessage = $"Yetersiz tutar! En az ₺{selectedTotal:N2} alınmalı.";
                    ShowResult = true;
                    IsSuccess = false;
                    return;
                }
                ParaUstu = AlinanTutar - selectedTotal;
                result = new OdemeSonuc { Basarili = true, Mesaj = "Kısmi ödeme alındı", ParaUstu = ParaUstu };
                break;

            case OdemeTipSecim.Kart:
                result = new OdemeSonuc { Basarili = true, Mesaj = "Kart ile kısmi ödeme alındı", ParaUstu = 0 };
                break;

            default:
                result = new OdemeSonuc { Basarili = true, Mesaj = "Kısmi ödeme alındı", ParaUstu = 0 };
                break;
        }

        if (result.Basarili)
        {
            // Seçilen kalemleri ödendi olarak işaretle
            foreach (var kalem in selectedKalemler)
            {
                kalem.IsSelected = false;
                kalem.IsOdendi = true;
            }

            AlmanUsuluOdenenTutar += selectedTotal;
            AlmanUsuluKalanTutar = ToplamTutar - IndirimTutar - AlmanUsuluOdenenTutar;
            
            ResultMessage = $"₺{selectedTotal:N2} ödeme alındı. Para üstü: ₺{ParaUstu:N2}";
            ShowResult = true;
            IsSuccess = true;

            // Tüm kalemler ödendiyse hesabı kapat
            if (Kalemler.All(k => k.IsOdendi || k.IsIkram))
            {
                // Tam ödeme olarak işle
                await _odemeService.ProcessNakitAsync(AdisyonId, ToplamTutar);
                IsOdemeCompleted = true;
                ResultMessage = "Tüm ödemeler tamamlandı!";
                _logger.LogInformation("Alman usulü ödeme tamamlandı: Adisyon {AdisyonId}", AdisyonId);
                
                // Event tetikle
                OdemeCompleted?.Invoke();
            }

            AlinanTutar = 0;
            OdenecekTutar = 0;
        }
    }

    #endregion

    [RelayCommand]
    private async Task ProcessOdemeAsync()
    {
        // Alman usulü modda farklı işle
        if (IsAlmanUsuluMode)
        {
            await ProcessAlmanUsuluOdemeAsync();
            return;
        }

        OdemeSonuc result;

        switch (SelectedOdemeTipi)
        {
            case OdemeTipSecim.Nakit:
                result = await _odemeService.ProcessNakitAsync(AdisyonId, AlinanTutar);
                break;

            case OdemeTipSecim.Kart:
                result = await _odemeService.ProcessKartAsync(AdisyonId);
                break;

            case OdemeTipSecim.Karisik:
                result = await _odemeService.ProcessKarisikAsync(AdisyonId, NakitTutar);
                break;

            default:
                return;
        }

        ResultMessage = result.Mesaj ?? "";
        IsSuccess = result.Basarili;
        ShowResult = true;

        if (result.Basarili)
        {
            ParaUstu = result.ParaUstu;
            IsOdemeCompleted = true;
            _logger.LogInformation("Ödeme tamamlandı: Adisyon {AdisyonId}", AdisyonId);
            
            // Event tetikle - ödeme tamamlandı
            OdemeCompleted?.Invoke();
        }
    }

    [RelayCommand]
    private void SelectOdemeTipi(string tip)
    {
        SelectedOdemeTipi = tip switch
        {
            "Nakit" => OdemeTipSecim.Nakit,
            "Kart" => OdemeTipSecim.Kart,
            "Karisik" => OdemeTipSecim.Karisik,
            _ => OdemeTipSecim.Nakit
        };
    }

    #region İndirim

    [RelayCommand]
    private void OpenIndirimPopup()
    {
        // Çalışan ise yönetici PIN gerekli
        if (_sessionService.CurrentUser?.Rol == KullaniciRol.Calisan)
        {
            PendingAction = "indirim";
            IsYoneticiPinPopupOpen = true;
            YoneticiPin = "";
            PinErrorMessage = "";
        }
        else
        {
            // Yönetici/Admin direkt açabilir
            IsIndirimPopupOpen = true;
            IndirimDeger = 0;
            IndirimNeden = "";
        }
    }

    [RelayCommand]
    private async Task ApplyIndirimAsync()
    {
        if (IndirimDeger <= 0 || string.IsNullOrWhiteSpace(IndirimNeden))
        {
            return;
        }

        var indirim = new IndirimBilgi
        {
            IsYuzde = IsIndirimYuzde,
            Deger = IndirimDeger,
            Neden = IndirimNeden,
            OnaylayanId = _sessionService.CurrentUser?.Id ?? 0
        };

        var success = await _odemeService.ApplyIndirimAsync(AdisyonId, indirim);
        
        if (success)
        {
            await LoadAdisyonAsync();
            IsIndirimPopupOpen = false;
            _logger.LogInformation("İndirim uygulandı: {Deger}{Tip}, Neden: {Neden}",
                IndirimDeger, IsIndirimYuzde ? "%" : "₺", IndirimNeden);
        }
    }

    [RelayCommand]
    private void CloseIndirimPopup()
    {
        IsIndirimPopupOpen = false;
    }

    #endregion

    #region İkram

    [RelayCommand]
    private void OpenIkramPopup(AdisyonKalemViewModel? kalem)
    {
        if (kalem == null) return;
        
        SelectedKalem = kalem;
        IkramKalemId = kalem.Kalem.Id;
        IkramNeden = "";

        // Çalışan ise yönetici PIN gerekli
        if (_sessionService.CurrentUser?.Rol == KullaniciRol.Calisan)
        {
            PendingAction = "ikram";
            IsYoneticiPinPopupOpen = true;
            YoneticiPin = "";
            PinErrorMessage = "";
        }
        else
        {
            IsIkramPopupOpen = true;
        }
    }

    [RelayCommand]
    private async Task ApplyIkramAsync()
    {
        if (IkramKalemId == 0 || string.IsNullOrWhiteSpace(IkramNeden))
        {
            return;
        }

        var success = await _odemeService.ApplyIkramAsync(
            IkramKalemId, 
            IkramNeden, 
            _sessionService.CurrentUser?.Id ?? 0);
        
        if (success)
        {
            await LoadAdisyonAsync();
            IsIkramPopupOpen = false;
            _logger.LogInformation("İkram yapıldı: Kalem {KalemId}, Neden: {Neden}", IkramKalemId, IkramNeden);
        }
    }

    [RelayCommand]
    private void CloseIkramPopup()
    {
        IsIkramPopupOpen = false;
    }

    #endregion

    #region Yönetici PIN

    [RelayCommand]
    private void AddPinDigit(string digit)
    {
        if (YoneticiPin.Length < 6)
        {
            YoneticiPin += digit;
        }
    }

    [RelayCommand]
    private void ClearPin()
    {
        YoneticiPin = "";
        PinErrorMessage = "";
    }

    [RelayCommand]
    private async Task VerifyPinAsync()
    {
        if (string.IsNullOrWhiteSpace(YoneticiPin))
        {
            PinErrorMessage = "PIN giriniz";
            return;
        }

        var user = await _authService.ValidatePinAsync(YoneticiPin);
        
        if (user == null)
        {
            PinErrorMessage = "Geçersiz PIN!";
            YoneticiPin = "";
            return;
        }

        if (user.Rol == KullaniciRol.Calisan)
        {
            PinErrorMessage = "Yönetici yetkisi gerekli!";
            YoneticiPin = "";
            return;
        }

        // PIN doğru ve yetkili
        IsYoneticiPinPopupOpen = false;
        YoneticiPin = "";

        // Bekleyen aksiyonu gerçekleştir
        if (PendingAction == "indirim")
        {
            IsIndirimPopupOpen = true;
            IndirimDeger = 0;
            IndirimNeden = "";
        }
        else if (PendingAction == "ikram")
        {
            IsIkramPopupOpen = true;
        }

        PendingAction = "";
    }

    [RelayCommand]
    private void ClosePinPopup()
    {
        IsYoneticiPinPopupOpen = false;
        YoneticiPin = "";
        PinErrorMessage = "";
        PendingAction = "";
    }

    #endregion

    #region Fiş Yazdırma

    [RelayCommand]
    private void YazdirFis()
    {
        if (_currentAdisyon == null) return;
        
        try
        {
            var odemeTipi = SelectedOdemeTipi switch
            {
                OdemeTipSecim.Nakit => "Nakit",
                OdemeTipSecim.Kart => "Kredi Kartı",
                OdemeTipSecim.Karisik => "Nakit + Kart",
                _ => ""
            };
            
            _fisYazdirmaService.YazdirAdisyon(_currentAdisyon, odemeTipi);
            _logger.LogInformation("Fiş yazdırıldı: Adisyon #{AdisyonId}", _currentAdisyon.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fiş yazdırma hatası");
            ResultMessage = "Fiş yazdırılamadı!";
        }
    }

    [RelayCommand]
    private void OnizlemeFis()
    {
        if (_currentAdisyon == null) return;
        
        try
        {
            var odemeTipi = SelectedOdemeTipi switch
            {
                OdemeTipSecim.Nakit => "Nakit",
                OdemeTipSecim.Kart => "Kredi Kartı",
                OdemeTipSecim.Karisik => "Nakit + Kart",
                _ => ""
            };
            
            _fisYazdirmaService.YazdirOnizleme(_currentAdisyon, odemeTipi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fiş önizleme hatası");
        }
    }

    #endregion
}
