namespace RestoranOtomasyon.Models;

/// <summary>
/// Kullanıcı rolleri
/// </summary>
public enum KullaniciRol
{
    Calisan,
    Yonetici,
    Admin
}

/// <summary>
/// Kullanıcı entity sınıfı
/// </summary>
public class Kullanici
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string PinHash { get; set; } = string.Empty;
    public KullaniciRol Rol { get; set; } = KullaniciRol.Calisan;
    public bool Aktif { get; set; } = true;
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public DateTime? SonGirisTarihi { get; set; }

    /// <summary>
    /// Kullanıcının belirli bir yetkiye sahip olup olmadığını kontrol eder
    /// </summary>
    public bool HasPermission(string permission)
    {
        return Rol switch
        {
            KullaniciRol.Admin => true, // Admin her şeyi yapabilir
            KullaniciRol.Yonetici => permission switch
            {
                "SIPARIS" or "ODEME" or "RAPOR" or "URUN_YONETIM" or "GIDER" or "FATURA" => true,
                _ => false
            },
            KullaniciRol.Calisan => permission switch
            {
                "SIPARIS" or "ODEME" => true,
                _ => false
            },
            _ => false
        };
    }
}
