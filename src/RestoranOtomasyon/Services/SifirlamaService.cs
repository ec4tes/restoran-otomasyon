using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;
using RestoranOtomasyon.Data;

namespace RestoranOtomasyon.Services;

public interface ISifirlamaService
{
    /// <summary>
    /// Sadece ürünleri ve kategorileri siler
    /// </summary>
    Task<bool> UrunleriSifirlaAsync();
    
    /// <summary>
    /// Sadece adisyonları ve satış geçmişini siler
    /// </summary>
    Task<bool> AdisyonlariSifirlaAsync();
    
    /// <summary>
    /// Tüm veritabanını temizler, sadece admin kullanıcı kalır
    /// </summary>
    Task<bool> FabrikaAyarlarinaDonenAsync();
    
    /// <summary>
    /// Veritabanı istatistiklerini getirir
    /// </summary>
    Task<VeritabaniIstatistik> GetIstatistiklerAsync();
}

public class VeritabaniIstatistik
{
    public int KategoriSayisi { get; set; }
    public int UrunSayisi { get; set; }
    public int AdisyonSayisi { get; set; }
    public int KullaniciSayisi { get; set; }
    public int MasaSayisi { get; set; }
    public decimal ToplamSatis { get; set; }
}

public class SifirlamaService : ISifirlamaService
{
    private readonly IDatabaseConnection _dbConnection;
    private readonly ILogger<SifirlamaService> _logger;

    public SifirlamaService(IDatabaseConnection dbConnection, ILogger<SifirlamaService> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public async Task<VeritabaniIstatistik> GetIstatistiklerAsync()
    {
        try
        {
            using var db = _dbConnection.CreateConnection();
            var istatistik = new VeritabaniIstatistik
            {
                KategoriSayisi = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Kategori"),
                UrunSayisi = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Urun"),
                AdisyonSayisi = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Adisyon"),
                KullaniciSayisi = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Kullanici"),
                MasaSayisi = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Masa"),
                ToplamSatis = await db.ExecuteScalarAsync<decimal>("SELECT COALESCE(SUM(ToplamTutar), 0) FROM Adisyon WHERE Durum = 'Odendi'")
            };
            
            return istatistik;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İstatistikler alınırken hata oluştu");
            return new VeritabaniIstatistik();
        }
    }

    public async Task<bool> UrunleriSifirlaAsync()
    {
        try
        {
            _logger.LogWarning("⚠️ ÜRÜNLER SİLİNİYOR...");
            
            using var db = _dbConnection.CreateConnection();
            
            // Önce adisyon kalemleri temizle (ürünlere bağlı)
            await db.ExecuteAsync("DELETE FROM AdisyonKalem");
            
            // Ürün notlarını temizle
            await db.ExecuteAsync("DELETE FROM UrunNotu");
            
            // Ürünleri temizle
            await db.ExecuteAsync("DELETE FROM Urun");
            
            // Kategorileri temizle
            await db.ExecuteAsync("DELETE FROM Kategori");
            
            // SQLite auto-increment sıfırla
            await db.ExecuteAsync("DELETE FROM sqlite_sequence WHERE name IN ('Urun', 'Kategori', 'UrunNotu', 'AdisyonKalem')");
            
            _logger.LogInformation("✅ Ürünler ve kategoriler başarıyla silindi");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ürünler silinirken hata oluştu");
            return false;
        }
    }

    public async Task<bool> AdisyonlariSifirlaAsync()
    {
        try
        {
            _logger.LogWarning("⚠️ ADİSYONLAR SİLİNİYOR...");
            
            using var db = _dbConnection.CreateConnection();
            
            // Adisyon kalemlerini temizle
            await db.ExecuteAsync("DELETE FROM AdisyonKalem");
            
            // Adisyonları temizle
            await db.ExecuteAsync("DELETE FROM Adisyon");
            
            // Gün sonu kayıtlarını temizle
            await db.ExecuteAsync("DELETE FROM GunSonu");
            
            // Giderleri temizle
            await db.ExecuteAsync("DELETE FROM Gider");
            
            // İşlem loglarını temizle
            await db.ExecuteAsync("DELETE FROM IslemLog");
            
            // Masaları boş duruma getir
            await db.ExecuteAsync("UPDATE Masa SET Durum = 'Bos'");
            
            // SQLite auto-increment sıfırla
            await db.ExecuteAsync("DELETE FROM sqlite_sequence WHERE name IN ('AdisyonKalem', 'Adisyon', 'GunSonu', 'Gider', 'IslemLog')");
            
            _logger.LogInformation("✅ Adisyonlar ve satış geçmişi başarıyla silindi");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Adisyonlar silinirken hata oluştu");
            return false;
        }
    }

    public async Task<bool> FabrikaAyarlarinaDonenAsync()
    {
        try
        {
            _logger.LogWarning("⚠️ FABRİKA AYARLARINA DÖNÜLÜYOR - TÜM VERİLER SİLİNİYOR...");
            
            using var db = _dbConnection.CreateConnection();
            
            // Tüm tabloları sırayla temizle (foreign key'lere dikkat)
            await db.ExecuteAsync("DELETE FROM AdisyonKalem");
            await db.ExecuteAsync("DELETE FROM Adisyon");
            await db.ExecuteAsync("DELETE FROM UrunNotu");
            await db.ExecuteAsync("DELETE FROM Urun");
            await db.ExecuteAsync("DELETE FROM Kategori");
            await db.ExecuteAsync("DELETE FROM GunSonu");
            await db.ExecuteAsync("DELETE FROM Gider");
            await db.ExecuteAsync("DELETE FROM SabitGider");
            await db.ExecuteAsync("DELETE FROM IslemLog");
            await db.ExecuteAsync("DELETE FROM YetkiLog");
            await db.ExecuteAsync("DELETE FROM SistemAyar");
            
            // Masaları sil (varsayılanlar hariç)
            await db.ExecuteAsync("DELETE FROM Masa");
            
            // Admin dışındaki kullanıcıları sil
            await db.ExecuteAsync("DELETE FROM Kullanici WHERE Rol != 'Admin'");
            
            // Varsayılan masaları oluştur
            await db.ExecuteAsync(@"
                INSERT INTO Masa (MasaNo, Bolum, Kapasite, Durum, Aktif) VALUES
                (1, 'Iceri', 4, 'Bos', 1),
                (2, 'Iceri', 4, 'Bos', 1),
                (3, 'Iceri', 6, 'Bos', 1),
                (4, 'Disari', 4, 'Bos', 1),
                (5, 'Disari', 4, 'Bos', 1),
                (6, 'Teras', 2, 'Bos', 1)
            ");
            
            // SQLite auto-increment sıfırla
            await db.ExecuteAsync("DELETE FROM sqlite_sequence");
            
            _logger.LogInformation("✅ Fabrika ayarlarına başarıyla dönüldü");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fabrika ayarlarına dönülürken hata oluştu");
            return false;
        }
    }
}
