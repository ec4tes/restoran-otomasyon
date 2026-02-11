namespace RestoranOtomasyon.Models;

/// <summary>
/// Adisyon tipleri
/// </summary>
public enum AdisyonTip
{
    Masa,
    GelAl,
    Paket
}

/// <summary>
/// Adisyon durumları
/// </summary>
public enum AdisyonDurum
{
    Acik,
    Hesap,
    Odendi,
    Iptal
}

/// <summary>
/// Ödeme tipleri
/// </summary>
public enum OdemeTipi
{
    Nakit,
    Kart,
    Karisik
}

/// <summary>
/// Sipariş kalemi durumları
/// </summary>
public enum KalemDurum
{
    Bekliyor,
    Hazirlaniyor,
    Tamamlandi,
    Iptal,
    Ikram
}

/// <summary>
/// Adisyon entity sınıfı
/// </summary>
public class Adisyon
{
    public int Id { get; set; }
    public int? MasaId { get; set; }
    public int KullaniciId { get; set; }
    public AdisyonTip Tip { get; set; }
    public AdisyonDurum Durum { get; set; } = AdisyonDurum.Acik;
    public decimal ToplamTutar { get; set; }
    public decimal IndirimTutar { get; set; }
    public string? IndirimNeden { get; set; }
    
    // Veritabanından gelen string değer
    public string? OdemeTipi { get; set; }
    
    // Enum olarak erişim
    public OdemeTipi? OdemeTipiValue => string.IsNullOrEmpty(OdemeTipi) ? null : Enum.TryParse<OdemeTipi>(OdemeTipi, out var tip) ? tip : null;
    
    public decimal NakitTutar { get; set; }
    public decimal KartTutar { get; set; }
    public string? MusteriNot { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public DateTime? KapanmaTarihi { get; set; }
    public string? IptalNeden { get; set; }
    public int? IptalEdenId { get; set; }

    // Navigation
    public Masa? Masa { get; set; }
    public Kullanici? Kullanici { get; set; }
    public List<AdisyonKalem> Kalemler { get; set; } = new();
}

/// <summary>
/// Adisyon kalemi entity sınıfı
/// </summary>
public class AdisyonKalem
{
    public int Id { get; set; }
    public int AdisyonId { get; set; }
    public int UrunId { get; set; }
    public int Adet { get; set; } = 1;
    public decimal BirimFiyat { get; set; }
    public bool YarimPorsiyon { get; set; }
    public string? Notlar { get; set; }
    public KalemDurum Durum { get; set; } = KalemDurum.Bekliyor;
    public string? IkramNeden { get; set; }
    public DateTime EklenmeTarihi { get; set; } = DateTime.Now;

    // Navigation & Computed from JOIN
    public Adisyon? Adisyon { get; set; }
    public Urun? Urun { get; set; }
    public string? UrunAd { get; set; }

    // Display
    public string GosterimAd => YarimPorsiyon ? $"{UrunAd} (Y.P)" : UrunAd ?? "";

    // Computed
    public decimal ToplamFiyat => Durum == KalemDurum.Ikram ? 0 : Adet * BirimFiyat;
}
