namespace RestoranOtomasyon.Models;

/// <summary>
/// Sistem ayarları entity sınıfı
/// </summary>
public class SistemAyar
{
    public string Anahtar { get; set; } = string.Empty;
    public string? Deger { get; set; }
    public string? Aciklama { get; set; }
}

/// <summary>
/// İşlem log entity sınıfı
/// </summary>
public class IslemLog
{
    public int Id { get; set; }
    public int? KullaniciId { get; set; }
    public string IslemTipi { get; set; } = string.Empty;
    public string? Tablo { get; set; }
    public int? KayitId { get; set; }
    public string? EskiDeger { get; set; }
    public string? YeniDeger { get; set; }
    public DateTime Tarih { get; set; } = DateTime.Now;

    // Navigation
    public Kullanici? Kullanici { get; set; }
}

/// <summary>
/// Yetki log entity sınıfı
/// </summary>
public class YetkiLog
{
    public int Id { get; set; }
    public int? KullaniciId { get; set; }
    public string Islem { get; set; } = string.Empty;
    public string? Detay { get; set; }
    public DateTime Tarih { get; set; } = DateTime.Now;

    // Navigation
    public Kullanici? Kullanici { get; set; }
}
