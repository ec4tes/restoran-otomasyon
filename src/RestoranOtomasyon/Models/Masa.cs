namespace RestoranOtomasyon.Models;

/// <summary>
/// Masa bölüm türleri
/// </summary>
public enum MasaBolum
{
    Iceri,
    Disari,
    Teras,
    GelAl,
    Paket
}

/// <summary>
/// Masa durumları
/// </summary>
public enum MasaDurum
{
    Bos,
    Dolu,
    Hesap,
    Rezerve
}

/// <summary>
/// Masa entity sınıfı
/// </summary>
public class Masa
{
    public int Id { get; set; }
    public string MasaNo { get; set; } = string.Empty;
    public MasaBolum Bolum { get; set; }
    public MasaDurum Durum { get; set; } = MasaDurum.Bos;
    public int Kapasite { get; set; } = 4;
    public int X { get; set; }
    public int Y { get; set; }
    public bool Aktif { get; set; } = true;
}
