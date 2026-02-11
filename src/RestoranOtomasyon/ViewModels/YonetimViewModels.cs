using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RestoranOtomasyon.Models;
using RestoranOtomasyon.Services;
using System.Collections.ObjectModel;

namespace RestoranOtomasyon.ViewModels;

/// <summary>
/// Yönetim paneli ana ViewModel
/// </summary>
public partial class YonetimViewModel : ViewModelBase
{
    private readonly ILogger<YonetimViewModel> _logger;
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _selectedTab = "Kategoriler";

    [ObservableProperty]
    private bool _canAccess;

    public YonetimViewModel(
        ILogger<YonetimViewModel> logger,
        ISessionService sessionService,
        INavigationService navigationService)
    {
        _logger = logger;
        _sessionService = sessionService;
        _navigationService = navigationService;

        // Yönetici veya Admin yetkisi kontrol
        CanAccess = _sessionService.CurrentUser?.Rol != KullaniciRol.Calisan;
        
        _logger.LogDebug("YonetimViewModel oluşturuldu, Erişim: {CanAccess}", CanAccess);
    }

    [RelayCommand]
    private void SelectTab(string tab)
    {
        SelectedTab = tab;
        _logger.LogDebug("Tab seçildi: {Tab}", tab);
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.NavigateToMasalar();
    }
}

/// <summary>
/// Kategori yönetimi ViewModel
/// </summary>
public partial class KategoriYonetimViewModel : ViewModelBase
{
    private readonly ILogger<KategoriYonetimViewModel> _logger;
    private readonly IKategoriService _kategoriService;

    [ObservableProperty]
    private ObservableCollection<Kategori> _kategoriler = new();

    [ObservableProperty]
    private Kategori? _selectedKategori;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isNew;

    // Edit form
    [ObservableProperty]
    private string _editAd = "";

    [ObservableProperty]
    private string _editRenk = "#FF6B35";

    [ObservableProperty]
    private int _editSira = 1;

    [ObservableProperty]
    private bool _editAktif = true;

    public KategoriYonetimViewModel(
        ILogger<KategoriYonetimViewModel> logger,
        IKategoriService kategoriService)
    {
        _logger = logger;
        _kategoriService = kategoriService;
    }

    [RelayCommand]
    public async Task LoadKategorilerAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var kategoriler = await _kategoriService.GetAllAsync();
            Kategoriler.Clear();
            foreach (var k in kategoriler)
            {
                Kategoriler.Add(k);
            }
            _logger.LogDebug("{Count} kategori yüklendi", Kategoriler.Count);
        });
    }

    [RelayCommand]
    private void NewKategori()
    {
        SelectedKategori = null;
        IsNew = true;
        IsEditing = true;
        EditAd = "";
        EditRenk = "#FF6B35";
        EditSira = Kategoriler.Count + 1;
        EditAktif = true;
    }

    [RelayCommand]
    private void EditKategori(Kategori? kategori)
    {
        if (kategori == null) return;
        
        SelectedKategori = kategori;
        IsNew = false;
        IsEditing = true;
        EditAd = kategori.Ad;
        EditRenk = kategori.Renk ?? "#FF6B35";
        EditSira = kategori.Sira;
        EditAktif = kategori.Aktif;
    }

    [RelayCommand]
    private async Task SaveKategoriAsync()
    {
        if (string.IsNullOrWhiteSpace(EditAd))
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            if (IsNew)
            {
                var kategori = new Kategori
                {
                    Ad = EditAd.Trim(),
                    Renk = EditRenk,
                    Sira = EditSira,
                    Aktif = EditAktif
                };
                await _kategoriService.CreateAsync(kategori);
                _logger.LogInformation("Yeni kategori oluşturuldu: {Ad}", kategori.Ad);
            }
            else if (SelectedKategori != null)
            {
                SelectedKategori.Ad = EditAd.Trim();
                SelectedKategori.Renk = EditRenk;
                SelectedKategori.Sira = EditSira;
                SelectedKategori.Aktif = EditAktif;
                await _kategoriService.UpdateAsync(SelectedKategori);
                _logger.LogInformation("Kategori güncellendi: {Ad}", SelectedKategori.Ad);
            }

            IsEditing = false;
            await LoadKategorilerAsync();
        });
    }

    [RelayCommand]
    private async Task DeleteKategoriAsync(Kategori? kategori)
    {
        if (kategori == null) return;

        await ExecuteBusyAsync(async () =>
        {
            await _kategoriService.DeleteAsync(kategori.Id);
            _logger.LogInformation("Kategori silindi: {Ad}", kategori.Ad);
            await LoadKategorilerAsync();
        });
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        SelectedKategori = null;
    }
}

/// <summary>
/// Ürün yönetimi ViewModel
/// </summary>
public partial class UrunYonetimViewModel : ViewModelBase
{
    private readonly ILogger<UrunYonetimViewModel> _logger;
    private readonly IUrunService _urunService;
    private readonly IKategoriService _kategoriService;

    [ObservableProperty]
    private ObservableCollection<Urun> _urunler = new();

    [ObservableProperty]
    private ObservableCollection<Kategori> _kategoriler = new();

    [ObservableProperty]
    private Urun? _selectedUrun;

    [ObservableProperty]
    private Kategori? _filterKategori;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isNew;

    // Edit form
    [ObservableProperty]
    private string _editAd = "";

    [ObservableProperty]
    private decimal _editFiyat;

    [ObservableProperty]
    private decimal? _editYarimPorsiyonFiyat;

    [ObservableProperty]
    private int _editKategoriId;

    [ObservableProperty]
    private bool _editFavori;

    [ObservableProperty]
    private bool _editAktif = true;

    [ObservableProperty]
    private string _editAciklama = "";

    public UrunYonetimViewModel(
        ILogger<UrunYonetimViewModel> logger,
        IUrunService urunService,
        IKategoriService kategoriService)
    {
        _logger = logger;
        _urunService = urunService;
        _kategoriService = kategoriService;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            // Kategorileri yükle
            var kategoriler = await _kategoriService.GetActiveAsync();
            Kategoriler.Clear();
            foreach (var k in kategoriler)
            {
                Kategoriler.Add(k);
            }

            // Tüm ürünleri yükle
            await LoadUrunlerAsync();
        });
    }

    [RelayCommand]
    private async Task LoadUrunlerAsync()
    {
        var urunler = FilterKategori != null
            ? await _urunService.GetByKategoriAsync(FilterKategori.Id)
            : await _urunService.GetAllAsync();
        
        Urunler.Clear();
        foreach (var u in urunler)
        {
            Urunler.Add(u);
        }
        _logger.LogDebug("{Count} ürün yüklendi", Urunler.Count);
    }

    partial void OnFilterKategoriChanged(Kategori? value)
    {
        _ = LoadUrunlerAsync();
    }

    [RelayCommand]
    private void NewUrun()
    {
        SelectedUrun = null;
        IsNew = true;
        IsEditing = true;
        EditAd = "";
        EditFiyat = 0;
        EditYarimPorsiyonFiyat = null;
        EditKategoriId = Kategoriler.FirstOrDefault()?.Id ?? 0;
        EditFavori = false;
        EditAktif = true;
        EditAciklama = "";
    }

    [RelayCommand]
    private void EditUrun(Urun? urun)
    {
        if (urun == null) return;
        
        SelectedUrun = urun;
        IsNew = false;
        IsEditing = true;
        EditAd = urun.Ad;
        EditFiyat = urun.Fiyat;
        EditYarimPorsiyonFiyat = urun.YarimPorsiyonFiyat;
        EditKategoriId = urun.KategoriId;
        EditFavori = urun.Favori;
        EditAktif = urun.Aktif;
        EditAciklama = urun.Aciklama ?? "";
    }

    [RelayCommand]
    private async Task SaveUrunAsync()
    {
        if (string.IsNullOrWhiteSpace(EditAd) || EditFiyat <= 0)
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            if (IsNew)
            {
                var urun = new Urun
                {
                    Ad = EditAd.Trim(),
                    Fiyat = EditFiyat,
                    YarimPorsiyonFiyat = EditYarimPorsiyonFiyat > 0 ? EditYarimPorsiyonFiyat : null,
                    KategoriId = EditKategoriId,
                    Favori = EditFavori,
                    Aktif = EditAktif,
                    Aciklama = EditAciklama
                };
                await _urunService.CreateAsync(urun);
                _logger.LogInformation("Yeni ürün oluşturuldu: {Ad}, Fiyat: {Fiyat}", urun.Ad, urun.Fiyat);
            }
            else if (SelectedUrun != null)
            {
                SelectedUrun.Ad = EditAd.Trim();
                SelectedUrun.Fiyat = EditFiyat;
                SelectedUrun.YarimPorsiyonFiyat = EditYarimPorsiyonFiyat > 0 ? EditYarimPorsiyonFiyat : null;
                SelectedUrun.KategoriId = EditKategoriId;
                SelectedUrun.Favori = EditFavori;
                SelectedUrun.Aktif = EditAktif;
                SelectedUrun.Aciklama = EditAciklama;
                await _urunService.UpdateAsync(SelectedUrun);
                _logger.LogInformation("Ürün güncellendi: {Ad}", SelectedUrun.Ad);
            }

            IsEditing = false;
            await LoadUrunlerAsync();
        });
    }

    [RelayCommand]
    private async Task DeleteUrunAsync(Urun? urun)
    {
        if (urun == null) return;

        await ExecuteBusyAsync(async () =>
        {
            await _urunService.DeleteAsync(urun.Id);
            _logger.LogInformation("Ürün silindi: {Ad}", urun.Ad);
            await LoadUrunlerAsync();
        });
    }

    [RelayCommand]
    private async Task ToggleFavoriAsync(Urun? urun)
    {
        if (urun == null) return;

        await _urunService.ToggleFavoriAsync(urun.Id);
        urun.Favori = !urun.Favori;
        _logger.LogDebug("Ürün favori değişti: {Ad}, Favori: {Favori}", urun.Ad, urun.Favori);
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        SelectedUrun = null;
    }
}

/// <summary>
/// Kullanıcı yönetimi ViewModel
/// </summary>
public partial class KullaniciYonetimViewModel : ViewModelBase
{
    private readonly ILogger<KullaniciYonetimViewModel> _logger;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private ObservableCollection<Kullanici> _kullanicilar = new();

    [ObservableProperty]
    private Kullanici? _selectedKullanici;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isNew;

    // Edit form
    [ObservableProperty]
    private string _editAd = "";

    [ObservableProperty]
    private string _editPin = "";

    [ObservableProperty]
    private KullaniciRol _editRol = KullaniciRol.Calisan;

    [ObservableProperty]
    private bool _editAktif = true;

    public KullaniciYonetimViewModel(
        ILogger<KullaniciYonetimViewModel> logger,
        IAuthService authService)
    {
        _logger = logger;
        _authService = authService;
    }

    [RelayCommand]
    public async Task LoadKullanicilarAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var kullanicilar = await _authService.GetAllUsersAsync();
            Kullanicilar.Clear();
            foreach (var k in kullanicilar)
            {
                Kullanicilar.Add(k);
            }
            _logger.LogDebug("{Count} kullanıcı yüklendi", Kullanicilar.Count);
        });
    }

    [RelayCommand]
    private void NewKullanici()
    {
        SelectedKullanici = null;
        IsNew = true;
        IsEditing = true;
        EditAd = "";
        EditPin = "";
        EditRol = KullaniciRol.Calisan;
        EditAktif = true;
    }

    [RelayCommand]
    private void EditKullanici(Kullanici? kullanici)
    {
        if (kullanici == null) return;
        
        SelectedKullanici = kullanici;
        IsNew = false;
        IsEditing = true;
        EditAd = kullanici.Ad;
        EditPin = ""; // PIN gösterilmez, değiştirmek için yeni girilmeli
        EditRol = kullanici.Rol;
        EditAktif = kullanici.Aktif;
    }

    [RelayCommand]
    private async Task SaveKullaniciAsync()
    {
        if (string.IsNullOrWhiteSpace(EditAd))
        {
            return;
        }

        // Yeni kullanıcı için PIN zorunlu
        if (IsNew && string.IsNullOrWhiteSpace(EditPin))
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            if (IsNew)
            {
                var kullanici = new Kullanici
                {
                    Ad = EditAd.Trim(),
                    Rol = EditRol,
                    Aktif = EditAktif
                };
                await _authService.CreateUserAsync(kullanici, EditPin);
                _logger.LogInformation("Yeni kullanıcı oluşturuldu: {Ad}, Rol: {Rol}", kullanici.Ad, kullanici.Rol);
            }
            else if (SelectedKullanici != null)
            {
                SelectedKullanici.Ad = EditAd.Trim();
                SelectedKullanici.Rol = EditRol;
                SelectedKullanici.Aktif = EditAktif;
                
                // PIN değiştirilmek isteniyorsa
                string? newPin = string.IsNullOrWhiteSpace(EditPin) ? null : EditPin;
                await _authService.UpdateUserAsync(SelectedKullanici, newPin);
                _logger.LogInformation("Kullanıcı güncellendi: {Ad}", SelectedKullanici.Ad);
            }

            IsEditing = false;
            await LoadKullanicilarAsync();
        });
    }

    [RelayCommand]
    private async Task DeleteKullaniciAsync(Kullanici? kullanici)
    {
        if (kullanici == null) return;

        await ExecuteBusyAsync(async () =>
        {
            await _authService.DeleteUserAsync(kullanici.Id);
            _logger.LogInformation("Kullanıcı silindi: {Ad}", kullanici.Ad);
            await LoadKullanicilarAsync();
        });
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        SelectedKullanici = null;
    }
}

/// <summary>
/// Masa yönetimi ViewModel
/// </summary>
public partial class MasaYonetimViewModel : ViewModelBase
{
    private readonly ILogger<MasaYonetimViewModel> _logger;
    private readonly IMasaService _masaService;

    [ObservableProperty]
    private ObservableCollection<Masa> _masalar = new();

    [ObservableProperty]
    private Masa? _selectedMasa;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isNew;

    // Edit form
    [ObservableProperty]
    private string _editMasaNo = "";

    [ObservableProperty]
    private MasaBolum _editBolum = MasaBolum.Iceri;

    [ObservableProperty]
    private int _editKapasite = 4;

    [ObservableProperty]
    private bool _editAktif = true;

    public MasaYonetimViewModel(
        ILogger<MasaYonetimViewModel> logger,
        IMasaService masaService)
    {
        _logger = logger;
        _masaService = masaService;
    }

    [RelayCommand]
    public async Task LoadMasalarAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var masalar = await _masaService.GetAllMasalarAsync();
            Masalar.Clear();
            foreach (var m in masalar)
            {
                Masalar.Add(m);
            }
            _logger.LogDebug("{Count} masa yüklendi", Masalar.Count);
        });
    }

    [RelayCommand]
    private void NewMasa()
    {
        SelectedMasa = null;
        IsNew = true;
        IsEditing = true;
        EditMasaNo = $"M{Masalar.Count + 1}";
        EditBolum = MasaBolum.Iceri;
        EditKapasite = 4;
        EditAktif = true;
    }

    [RelayCommand]
    private void EditMasa(Masa? masa)
    {
        if (masa == null) return;
        
        SelectedMasa = masa;
        IsNew = false;
        IsEditing = true;
        EditMasaNo = masa.MasaNo;
        EditBolum = masa.Bolum;
        EditKapasite = masa.Kapasite;
        EditAktif = masa.Aktif;
    }

    [RelayCommand]
    private async Task SaveMasaAsync()
    {
        if (string.IsNullOrWhiteSpace(EditMasaNo))
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            if (IsNew)
            {
                var masa = new Masa
                {
                    MasaNo = EditMasaNo.Trim(),
                    Bolum = EditBolum,
                    Kapasite = EditKapasite,
                    Durum = MasaDurum.Bos,
                    Aktif = EditAktif
                };
                await _masaService.CreateMasaAsync(masa);
                _logger.LogInformation("Yeni masa oluşturuldu: {MasaNo}", masa.MasaNo);
            }
            else if (SelectedMasa != null)
            {
                SelectedMasa.MasaNo = EditMasaNo.Trim();
                SelectedMasa.Bolum = EditBolum;
                SelectedMasa.Kapasite = EditKapasite;
                SelectedMasa.Aktif = EditAktif;
                await _masaService.UpdateMasaAsync(SelectedMasa);
                _logger.LogInformation("Masa güncellendi: {MasaNo}", SelectedMasa.MasaNo);
            }

            IsEditing = false;
            await LoadMasalarAsync();
        });
    }

    [RelayCommand]
    private async Task DeleteMasaAsync(Masa? masa)
    {
        if (masa == null) return;

        await ExecuteBusyAsync(async () =>
        {
            await _masaService.DeleteMasaAsync(masa.Id);
            _logger.LogInformation("Masa silindi: {MasaNo}", masa.MasaNo);
            await LoadMasalarAsync();
        });
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        SelectedMasa = null;
    }
}

/// <summary>
/// Raporlar ViewModel
/// </summary>
public partial class RaporViewModel : ViewModelBase
{
    private readonly ILogger<RaporViewModel> _logger;
    private readonly IRaporService _raporService;

    [ObservableProperty]
    private string _selectedRaporTipi = "Gunluk";

    [ObservableProperty]
    private DateTime _selectedTarih = DateTime.Today;

    [ObservableProperty]
    private DateTime _baslangicTarihi = DateTime.Today.AddDays(-7);

    [ObservableProperty]
    private DateTime _bitisTarihi = DateTime.Today;

    [ObservableProperty]
    private int _selectedYil = DateTime.Now.Year;

    [ObservableProperty]
    private int _selectedAy = DateTime.Now.Month;

    // Günlük rapor
    [ObservableProperty]
    private GunlukSatisRapor? _gunlukRapor;

    // Ürün satışları
    [ObservableProperty]
    private ObservableCollection<UrunSatisRapor> _urunSatislari = new();

    // Ödeme tipi raporu
    [ObservableProperty]
    private ObservableCollection<OdemeTipiRapor> _odemeTipleri = new();

    // Aylık rapor
    [ObservableProperty]
    private AylikRapor? _aylikRapor;

    public List<int> Yillar { get; } = Enumerable.Range(2020, DateTime.Now.Year - 2019).Reverse().ToList();
    public List<AyItem> Aylar { get; } = new()
    {
        new(1, "Ocak"), new(2, "Şubat"), new(3, "Mart"), new(4, "Nisan"),
        new(5, "Mayıs"), new(6, "Haziran"), new(7, "Temmuz"), new(8, "Ağustos"),
        new(9, "Eylül"), new(10, "Ekim"), new(11, "Kasım"), new(12, "Aralık")
    };

    public RaporViewModel(
        ILogger<RaporViewModel> logger,
        IRaporService raporService)
    {
        _logger = logger;
        _raporService = raporService;
    }

    [RelayCommand]
    private void SelectRaporTipi(string tip)
    {
        SelectedRaporTipi = tip;
    }

    [RelayCommand]
    private async Task LoadGunlukRaporAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            GunlukRapor = await _raporService.GetGunlukSatisAsync(SelectedTarih);
            _logger.LogInformation("Günlük rapor yüklendi: {Tarih}", SelectedTarih.ToShortDateString());
        });
    }

    [RelayCommand]
    private async Task LoadUrunSatislariAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var sonuc = await _raporService.GetUrunSatislariAsync(BaslangicTarihi, BitisTarihi);
            UrunSatislari = new ObservableCollection<UrunSatisRapor>(sonuc);
            _logger.LogInformation("Ürün satışları yüklendi: {Adet} ürün", UrunSatislari.Count);
        });
    }

    [RelayCommand]
    private async Task LoadOdemeTipleriAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var sonuc = await _raporService.GetOdemeTipiRaporuAsync(BaslangicTarihi, BitisTarihi);
            OdemeTipleri = new ObservableCollection<OdemeTipiRapor>(sonuc);
            _logger.LogInformation("Ödeme tipi raporu yüklendi");
        });
    }

    [RelayCommand]
    private async Task LoadAylikRaporAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            AylikRapor = await _raporService.GetAylikRaporAsync(SelectedYil, SelectedAy);
            _logger.LogInformation("Aylık rapor yüklendi: {Ay} {Yil}", AylikRapor.AyAdi, SelectedYil);
        });
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (AylikRapor == null)
        {
            await LoadAylikRaporAsync();
        }

        if (AylikRapor == null) return;

        try
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Dosyası (*.csv)|*.csv",
                FileName = $"Rapor_{AylikRapor.AyAdi}_{AylikRapor.Yil}.csv",
                Title = "Raporu Dışa Aktar"
            };

            if (saveDialog.ShowDialog() == true)
            {
                await ExportToCsvAsync(saveDialog.FileName);
                _logger.LogInformation("Rapor dışa aktarıldı: {Dosya}", saveDialog.FileName);
                System.Windows.MessageBox.Show($"Rapor başarıyla kaydedildi:\n{saveDialog.FileName}", 
                    "Başarılı", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel export hatası");
            System.Windows.MessageBox.Show("Rapor kaydedilemedi: " + ex.Message, 
                "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task ExportToCsvAsync(string filePath)
    {
        if (AylikRapor == null) return;

        var lines = new List<string>
        {
            "CEMİLBEY Yemek Atölyesi - Aylık Rapor",
            $"Dönem: {AylikRapor.AyAdi} {AylikRapor.Yil}",
            "",
            "=== ÖZET ===",
            $"Toplam Adisyon;{AylikRapor.ToplamAdisyon}",
            $"Toplam Tutar;₺{AylikRapor.ToplamTutar:N2}",
            $"Toplam İndirim;₺{AylikRapor.ToplamIndirim:N2}",
            $"Net Tutar;₺{AylikRapor.NetTutar:N2}",
            "",
            "=== GÜNLÜK SATIŞLAR ===",
            "Tarih;Adisyon Sayısı;Toplam Tutar"
        };

        foreach (var gun in AylikRapor.GunlukOzetler)
        {
            lines.Add($"{gun.Tarih:dd.MM.yyyy};{gun.AdisyonSayisi};₺{gun.ToplamTutar:N2}");
        }

        lines.Add("");
        lines.Add("=== ÜRÜN SATIŞLARI ===");
        lines.Add("Ürün;Kategori;Satış Adedi;Birim Fiyat;Toplam Tutar;İkram Adedi");

        foreach (var urun in AylikRapor.UrunSatislari)
        {
            lines.Add($"{urun.UrunAd};{urun.KategoriAd};{urun.ToplamAdet};₺{urun.BirimFiyat:N2};₺{urun.ToplamTutar:N2};{urun.IkramAdet}");
        }

        lines.Add("");
        lines.Add("=== ÖDEME TİPLERİ ===");
        lines.Add("Ödeme Tipi;Adisyon Sayısı;Toplam Tutar;Yüzde");

        foreach (var odeme in AylikRapor.OdemeTipleri)
        {
            lines.Add($"{odeme.OdemeTipi};{odeme.AdisyonSayisi};₺{odeme.ToplamTutar:N2};%{odeme.Yuzde:N1}");
        }

        await System.IO.File.WriteAllLinesAsync(filePath, lines, System.Text.Encoding.UTF8);
    }
}

public record AyItem(int Ay, string Ad);

/// <summary>
/// Sıfırlama işlemleri ViewModel
/// </summary>
public partial class SifirlamaViewModel : ViewModelBase
{
    private readonly ILogger<SifirlamaViewModel> _logger;
    private readonly ISifirlamaService _sifirlamaService;

    [ObservableProperty]
    private VeritabaniIstatistik _istatistikler = new();

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isSuccess;

    [ObservableProperty]
    private bool _hasMessage;

    public SifirlamaViewModel(
        ILogger<SifirlamaViewModel> logger,
        ISifirlamaService sifirlamaService)
    {
        _logger = logger;
        _sifirlamaService = sifirlamaService;
    }

    [RelayCommand]
    public async Task LoadIstatistiklerAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            Istatistikler = await _sifirlamaService.GetIstatistiklerAsync();
            _logger.LogDebug("Veritabanı istatistikleri yüklendi");
        });
    }

    [RelayCommand]
    private async Task UrunleriSifirlaAsync()
    {
        var result = System.Windows.MessageBox.Show(
            "⚠️ TÜM ÜRÜNLERİ VE KATEGORİLERİ SİLMEK İSTEDİĞİNİZDEN EMİN MİSİNİZ?\n\nBu işlem geri alınamaz!",
            "Ürünleri Sıfırla",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
            return;

        // İkinci onay
        result = System.Windows.MessageBox.Show(
            "🚨 SON ONAY\n\nTüm ürünler ve kategoriler silinecek!\n\nDevam etmek istiyor musunuz?",
            "Emin misiniz?",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Stop);

        if (result != System.Windows.MessageBoxResult.Yes)
            return;

        await ExecuteBusyAsync(async () =>
        {
            var success = await _sifirlamaService.UrunleriSifirlaAsync();
            
            if (success)
            {
                StatusMessage = "✅ Ürünler ve kategoriler başarıyla silindi!";
                IsSuccess = true;
                _logger.LogWarning("Ürünler sıfırlandı");
            }
            else
            {
                StatusMessage = "❌ Ürünler silinirken bir hata oluştu!";
                IsSuccess = false;
            }
            
            HasMessage = true;
            await LoadIstatistiklerAsync();
        });
    }

    [RelayCommand]
    private async Task AdisyonlariSifirlaAsync()
    {
        var result = System.Windows.MessageBox.Show(
            "⚠️ TÜM ADİSYONLARI VE SATIŞ GEÇMİŞİNİ SİLMEK İSTEDİĞİNİZDEN EMİN MİSİNİZ?\n\nRaporlar sıfırlanacak!\nBu işlem geri alınamaz!",
            "Adisyonları Sıfırla",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
            return;

        // İkinci onay
        result = System.Windows.MessageBox.Show(
            "🚨 SON ONAY\n\nTüm satış geçmişi silinecek!\n\nDevam etmek istiyor musunuz?",
            "Emin misiniz?",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Stop);

        if (result != System.Windows.MessageBoxResult.Yes)
            return;

        await ExecuteBusyAsync(async () =>
        {
            var success = await _sifirlamaService.AdisyonlariSifirlaAsync();
            
            if (success)
            {
                StatusMessage = "✅ Adisyonlar ve satış geçmişi başarıyla silindi!";
                IsSuccess = true;
                _logger.LogWarning("Adisyonlar sıfırlandı");
            }
            else
            {
                StatusMessage = "❌ Adisyonlar silinirken bir hata oluştu!";
                IsSuccess = false;
            }
            
            HasMessage = true;
            await LoadIstatistiklerAsync();
        });
    }

    [RelayCommand]
    private async Task FabrikaAyarlariAsync()
    {
        var result = System.Windows.MessageBox.Show(
            "🔥 FABRİKA AYARLARINA DÖNMEK İSTEDİĞİNİZDEN EMİN MİSİNİZ?\n\n⚠️ TÜM VERİLER SİLİNECEK:\n• Ürünler\n• Kategoriler\n• Kullanıcılar (Admin hariç)\n• Masalar\n• Adisyonlar\n• Raporlar\n• Her şey!\n\nBU İŞLEM GERİ ALINAMAZ!",
            "Fabrika Ayarları",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Stop);

        if (result != System.Windows.MessageBoxResult.Yes)
            return;

        // İkinci onay
        result = System.Windows.MessageBox.Show(
            "💀 SON UYARI\n\nTÜM VERİLER KALICI OLARAK SİLİNECEK!\n\nSadece admin kullanıcı ve varsayılan masalar kalacak.\n\nBU İŞLEMİ ONAYLIYORUM, DEVAM ET!",
            "SON ONAY - GERİ DÖNÜŞÜ YOK",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Stop);

        if (result != System.Windows.MessageBoxResult.Yes)
            return;

        await ExecuteBusyAsync(async () =>
        {
            var success = await _sifirlamaService.FabrikaAyarlarinaDonenAsync();
            
            if (success)
            {
                StatusMessage = "✅ Fabrika ayarlarına başarıyla dönüldü!";
                IsSuccess = true;
                _logger.LogWarning("FABRİKA AYARLARINA DÖNÜLDÜ - TÜM VERİLER SİLİNDİ");
            }
            else
            {
                StatusMessage = "❌ Fabrika ayarlarına dönülürken bir hata oluştu!";
                IsSuccess = false;
            }
            
            HasMessage = true;
            await LoadIstatistiklerAsync();
        });
    }
}

// =====================================================
// FİYAT GÜNCELLEME VIEWMODEL
// =====================================================
public partial class FiyatGuncellemeViewModel : ObservableObject
{
    private readonly IUrunService _urunService;
    private readonly IKategoriService _kategoriService;
    private readonly ILogger<FiyatGuncellemeViewModel> _logger;

    public FiyatGuncellemeViewModel(IUrunService urunService, IKategoriService kategoriService, ILogger<FiyatGuncellemeViewModel> logger)
    {
        _urunService = urunService;
        _kategoriService = kategoriService;
        _logger = logger;
        _isZam = true;
        _yuzde = 10;
    }

    [ObservableProperty]
    private ObservableCollection<KategoriItem> _kategoriler = new();

    [ObservableProperty]
    private KategoriItem? _selectedKategori;

    [ObservableProperty]
    private bool _isZam;

    [ObservableProperty]
    private bool _isIndirim;

    [ObservableProperty]
    private decimal _yuzde;

    [ObservableProperty]
    private int _etkilenecekUrunSayisi;

    [ObservableProperty]
    private string _ornekFiyat = "";

    [ObservableProperty]
    private bool _hasResult;

    [ObservableProperty]
    private string _resultMessage = "";

    [ObservableProperty]
    private bool _isSuccess;

    [ObservableProperty]
    private bool _isBusy;

    private List<Urun> _tumUrunler = new();

    public async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            
            // Kategorileri yükle
            var kategoriler = await _kategoriService.GetAllAsync();
            var urunler = await _urunService.GetAllAsync();
            _tumUrunler = urunler.ToList();
            
            Kategoriler.Clear();
            Kategoriler.Add(new KategoriItem { Id = 0, Ad = "📦 Tüm Ürünler" });
            
            foreach (var k in kategoriler)
            {
                Kategoriler.Add(new KategoriItem { Id = k.Id, Ad = k.Ad });
            }
            
            SelectedKategori = Kategoriler.FirstOrDefault();
            UpdatePreview();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fiyat güncelleme verileri yüklenirken hata");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnIsZamChanged(bool value)
    {
        if (value) IsIndirim = false;
        UpdatePreview();
    }

    partial void OnIsIndirimChanged(bool value)
    {
        if (value) IsZam = false;
        UpdatePreview();
    }

    partial void OnSelectedKategoriChanged(KategoriItem? value)
    {
        UpdatePreview();
    }

    partial void OnYuzdeChanged(decimal value)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var urunler = GetEtkilenecekUrunler();
        EtkilenecekUrunSayisi = urunler.Count;
        
        if (urunler.Count > 0 && Yuzde > 0)
        {
            var ornekUrun = urunler.First();
            var eskiFiyat = ornekUrun.Fiyat;
            var yeniFiyat = IsZam 
                ? eskiFiyat * (1 + Yuzde / 100)
                : eskiFiyat * (1 - Yuzde / 100);
            
            OrnekFiyat = $"{ornekUrun.Ad}: {eskiFiyat:N2}₺ → {yeniFiyat:N2}₺";
        }
        else
        {
            OrnekFiyat = "";
        }
    }

    private List<Urun> GetEtkilenecekUrunler()
    {
        if (SelectedKategori == null || SelectedKategori.Id == 0)
        {
            return _tumUrunler;
        }
        
        return _tumUrunler.Where(u => u.KategoriId == SelectedKategori.Id).ToList();
    }

    [RelayCommand]
    private void SetYuzde(string yuzdeStr)
    {
        if (decimal.TryParse(yuzdeStr, out var y))
        {
            Yuzde = y;
        }
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (Yuzde <= 0)
        {
            ResultMessage = "❌ Lütfen geçerli bir yüzde girin!";
            IsSuccess = false;
            HasResult = true;
            return;
        }

        var urunler = GetEtkilenecekUrunler();
        if (urunler.Count == 0)
        {
            ResultMessage = "❌ Güncellenecek ürün bulunamadı!";
            IsSuccess = false;
            HasResult = true;
            return;
        }

        var islemTipi = IsZam ? "ZAM" : "İNDİRİM";
        var result = System.Windows.MessageBox.Show(
            $"📊 FİYAT GÜNCELLEMESİ\n\n" +
            $"İşlem: %{Yuzde} {islemTipi}\n" +
            $"Etkilenecek ürün: {urunler.Count} adet\n\n" +
            $"Bu işlemi onaylıyor musunuz?",
            "Fiyat Güncelleme Onayı",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes)
            return;

        try
        {
            IsBusy = true;
            var basarili = 0;
            var hatali = 0;

            foreach (var urun in urunler)
            {
                try
                {
                    // Ana fiyatı güncelle
                    var eskiFiyat = urun.Fiyat;
                    urun.Fiyat = IsZam 
                        ? eskiFiyat * (1 + Yuzde / 100)
                        : eskiFiyat * (1 - Yuzde / 100);
                    
                    // Yarım porsiyon fiyatını da güncelle
                    if (urun.YarimPorsiyonFiyat.HasValue)
                    {
                        urun.YarimPorsiyonFiyat = IsZam
                            ? urun.YarimPorsiyonFiyat.Value * (1 + Yuzde / 100)
                            : urun.YarimPorsiyonFiyat.Value * (1 - Yuzde / 100);
                    }
                    
                    // Yuvarla (kuruş hassasiyetinde)
                    urun.Fiyat = Math.Round(urun.Fiyat, 2);
                    if (urun.YarimPorsiyonFiyat.HasValue)
                        urun.YarimPorsiyonFiyat = Math.Round(urun.YarimPorsiyonFiyat.Value, 2);
                    
                    var success = await _urunService.UpdateAsync(urun);
                    if (success) basarili++;
                    else hatali++;
                }
                catch
                {
                    hatali++;
                }
            }

            // Listeyi yeniden yükle
            var yeniUrunler = await _urunService.GetAllAsync();
            _tumUrunler = yeniUrunler.ToList();
            UpdatePreview();

            if (hatali == 0)
            {
                ResultMessage = $"✅ {basarili} ürün başarıyla güncellendi!\n%{Yuzde} {islemTipi} uygulandı.";
                IsSuccess = true;
                _logger.LogInformation("Toplu fiyat güncelleme: {Sayi} ürün, %{Yuzde} {Tip}", basarili, Yuzde, islemTipi);
            }
            else
            {
                ResultMessage = $"⚠️ {basarili} ürün güncellendi, {hatali} ürün güncellenemedi!";
                IsSuccess = false;
            }
            HasResult = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Toplu fiyat güncelleme hatası");
            ResultMessage = "❌ Fiyatlar güncellenirken bir hata oluştu!";
            IsSuccess = false;
            HasResult = true;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class KategoriItem
{
    public int Id { get; set; }
    public string Ad { get; set; } = "";
}

