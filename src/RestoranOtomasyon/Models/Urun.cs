namespace RestoranOtomasyon.Models;

/// <summary>
/// Kategori entity sınıfı
/// </summary>
public class Kategori
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Renk { get; set; } = "#3498db";
    public int Sira { get; set; }
    public bool Aktif { get; set; } = true;
}

/// <summary>
/// Ürün entity sınıfı
/// </summary>
public class Urun
{
    public int Id { get; set; }
    public int KategoriId { get; set; }
    public string Ad { get; set; } = string.Empty;
    public decimal Fiyat { get; set; }
    public decimal? YarimPorsiyonFiyat { get; set; }
    public bool YarimPorsiyonVar => YarimPorsiyonFiyat.HasValue && YarimPorsiyonFiyat.Value > 0;
    public bool Favori { get; set; }
    public string Renk { get; set; } = "#2ecc71";
    public string? Aciklama { get; set; }
    public bool Aktif { get; set; } = true;
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    // Navigation
    public Kategori? Kategori { get; set; }
    public string? KategoriAd { get; set; }
}

/// <summary>
/// Ürün not presetleri
/// </summary>
public class UrunNot
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string? Kisayol { get; set; }
    public bool Aktif { get; set; } = true;
}
