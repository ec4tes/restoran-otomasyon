using Dapper;
using Microsoft.Extensions.Logging;
using RestoranOtomasyon.Data;
using RestoranOtomasyon.Models;

namespace RestoranOtomasyon.Services;

/// <summary>
/// Adisyon işlemleri servisi
/// </summary>
public interface IAdisyonService
{
    Task<Adisyon?> GetByIdAsync(int id);
    Task<Adisyon?> GetAktifByMasaAsync(int masaId);
    Task<IEnumerable<Adisyon>> GetAktifGelAlPaketAsync();
    Task<int> CreateAsync(Adisyon adisyon);
    Task<bool> UpdateAsync(Adisyon adisyon);
    Task<bool> UpdateDurumAsync(int adisyonId, AdisyonDurum durum);
    
    // Kalem işlemleri
    Task<IEnumerable<AdisyonKalem>> GetKalemlerAsync(int adisyonId);
    Task<int> AddKalemAsync(AdisyonKalem kalem);
    Task<bool> UpdateKalemAsync(AdisyonKalem kalem);
    Task<bool> UpdateKalemAdetAsync(int kalemId, int yeniAdet);
    Task<bool> UpdateKalemFiyatAsync(int kalemId, decimal yeniFiyat);
    Task<bool> RemoveKalemAsync(int kalemId);
    Task<bool> SetKalemNotAsync(int kalemId, string not);
    Task<bool> SetKalemIkramAsync(int kalemId, string neden);
    
    // Hesaplama
    Task<decimal> CalculateTotalAsync(int adisyonId);
    Task UpdateAdisyonTotalAsync(int adisyonId);
}

public class AdisyonService : IAdisyonService
{
    private readonly IDatabaseConnection _dbConnection;
    private readonly ILogger<AdisyonService> _logger;

    public AdisyonService(IDatabaseConnection dbConnection, ILogger<AdisyonService> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public async Task<Adisyon?> GetByIdAsync(int id)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var adisyon = await connection.QuerySingleOrDefaultAsync<Adisyon>(
            "SELECT * FROM Adisyon WHERE Id = @Id", new { Id = id });
        
        if (adisyon != null)
        {
            adisyon.Kalemler = (await GetKalemlerAsync(id)).ToList();
        }
        
        return adisyon;
    }

    public async Task<Adisyon?> GetAktifByMasaAsync(int masaId)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var adisyon = await connection.QuerySingleOrDefaultAsync<Adisyon>(@"
            SELECT * FROM Adisyon 
            WHERE MasaId = @MasaId AND Durum IN ('Acik', 'Hesap')
            ORDER BY OlusturmaTarihi DESC LIMIT 1",
            new { MasaId = masaId });
        
        if (adisyon != null)
        {
            adisyon.Kalemler = (await GetKalemlerAsync(adisyon.Id)).ToList();
        }
        
        return adisyon;
    }

    public async Task<IEnumerable<Adisyon>> GetAktifGelAlPaketAsync()
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var adisyonlar = await connection.QueryAsync<Adisyon>(@"
            SELECT * FROM Adisyon 
            WHERE MasaId IS NULL AND Durum IN ('Acik', 'Hesap') AND Tip IN ('GelAl', 'Paket')
            ORDER BY OlusturmaTarihi DESC");
        
        foreach (var adisyon in adisyonlar)
        {
            adisyon.Kalemler = (await GetKalemlerAsync(adisyon.Id)).ToList();
        }
        
        return adisyonlar;
    }

    public async Task<int> CreateAsync(Adisyon adisyon)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var id = await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO Adisyon (MasaId, KullaniciId, Tip, Durum, ToplamTutar, MusteriNot, OlusturmaTarihi) 
            VALUES (@MasaId, @KullaniciId, @Tip, @Durum, 0, @MusteriNot, CURRENT_TIMESTAMP);
            SELECT last_insert_rowid();",
            new { 
                adisyon.MasaId, 
                adisyon.KullaniciId, 
                Tip = adisyon.Tip.ToString(),
                Durum = AdisyonDurum.Acik.ToString(),
                adisyon.MusteriNot
            });
        
        _logger.LogInformation("Yeni adisyon oluşturuldu: {AdisyonId}, Masa: {MasaId}, Tip: {Tip}", 
            id, adisyon.MasaId, adisyon.Tip);
        return id;
    }

    public async Task<bool> UpdateAsync(Adisyon adisyon)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var affected = await connection.ExecuteAsync(@"
            UPDATE Adisyon SET 
                Durum = @Durum, ToplamTutar = @ToplamTutar, IndirimTutar = @IndirimTutar,
                IndirimNeden = @IndirimNeden, OdemeTipi = @OdemeTipi, NakitTutar = @NakitTutar,
                KartTutar = @KartTutar, MusteriNot = @MusteriNot, KapanmaTarihi = @KapanmaTarihi
            WHERE Id = @Id",
            new {
                adisyon.Id,
                Durum = adisyon.Durum.ToString(),
                adisyon.ToplamTutar,
                adisyon.IndirimTutar,
                adisyon.IndirimNeden,
                OdemeTipi = adisyon.OdemeTipiValue?.ToString(),
                adisyon.NakitTutar,
                adisyon.KartTutar,
                adisyon.MusteriNot,
                adisyon.KapanmaTarihi
            });
        return affected > 0;
    }

    public async Task<bool> UpdateDurumAsync(int adisyonId, AdisyonDurum durum)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var affected = await connection.ExecuteAsync(
            "UPDATE Adisyon SET Durum = @Durum WHERE Id = @Id",
            new { Id = adisyonId, Durum = durum.ToString() });
        
        if (affected > 0)
            _logger.LogInformation("Adisyon {AdisyonId} durumu güncellendi: {Durum}", adisyonId, durum);
        
        return affected > 0;
    }

    public async Task<IEnumerable<AdisyonKalem>> GetKalemlerAsync(int adisyonId)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        return await connection.QueryAsync<AdisyonKalem>(@"
            SELECT ak.*, u.Ad as UrunAd 
            FROM AdisyonKalem ak
            INNER JOIN Urun u ON ak.UrunId = u.Id
            WHERE ak.AdisyonId = @AdisyonId AND ak.Durum != 'Iptal'
            ORDER BY ak.EklenmeTarihi",
            new { AdisyonId = adisyonId });
    }

    public async Task<int> AddKalemAsync(AdisyonKalem kalem)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        // Aynı üründen ve aynı fiyattan var mı kontrol et (not hariç, yarım porsiyon aynı olmalı)
        var mevcutKalem = await connection.QueryFirstOrDefaultAsync<AdisyonKalem>(@"
            SELECT * FROM AdisyonKalem 
            WHERE AdisyonId = @AdisyonId AND UrunId = @UrunId 
                  AND YarimPorsiyon = @YarimPorsiyon
                  AND BirimFiyat = @BirimFiyat
                  AND (Notlar IS NULL OR Notlar = '') AND Durum = 'Bekliyor'",
            new { kalem.AdisyonId, kalem.UrunId, YarimPorsiyon = kalem.YarimPorsiyon ? 1 : 0, kalem.BirimFiyat });
        
        if (mevcutKalem != null)
        {
            // Adeti artır
            await connection.ExecuteAsync(
                "UPDATE AdisyonKalem SET Adet = Adet + @Adet WHERE Id = @Id",
                new { mevcutKalem.Id, kalem.Adet });
            
            await UpdateAdisyonTotalAsync(kalem.AdisyonId);
            _logger.LogDebug("Kalem adeti artırıldı: {KalemId}, Yeni adet: {Adet}", 
                mevcutKalem.Id, mevcutKalem.Adet + kalem.Adet);
            return mevcutKalem.Id;
        }
        
        // Yeni kalem ekle
        var id = await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO AdisyonKalem (AdisyonId, UrunId, Adet, BirimFiyat, YarimPorsiyon, Notlar, Durum, EklenmeTarihi) 
            VALUES (@AdisyonId, @UrunId, @Adet, @BirimFiyat, @YarimPorsiyon, @Notlar, 'Bekliyor', CURRENT_TIMESTAMP);
            SELECT last_insert_rowid();",
            new { kalem.AdisyonId, kalem.UrunId, kalem.Adet, kalem.BirimFiyat, 
                  YarimPorsiyon = kalem.YarimPorsiyon ? 1 : 0, kalem.Notlar });
        
        await UpdateAdisyonTotalAsync(kalem.AdisyonId);
        _logger.LogDebug("Yeni kalem eklendi: {KalemId}, Ürün: {UrunId}, YarımPorsiyon: {YP}", 
            id, kalem.UrunId, kalem.YarimPorsiyon);
        return id;
    }

    public async Task<bool> UpdateKalemAsync(AdisyonKalem kalem)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var affected = await connection.ExecuteAsync(@"
            UPDATE AdisyonKalem SET Adet = @Adet, Notlar = @Notlar, Durum = @Durum 
            WHERE Id = @Id",
            new { kalem.Id, kalem.Adet, kalem.Notlar, Durum = kalem.Durum.ToString() });
        
        if (affected > 0)
            await UpdateAdisyonTotalAsync(kalem.AdisyonId);
        
        return affected > 0;
    }

    public async Task<bool> UpdateKalemFiyatAsync(int kalemId, decimal yeniFiyat)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        var kalem = await connection.QuerySingleOrDefaultAsync<AdisyonKalem>(
            "SELECT * FROM AdisyonKalem WHERE Id = @Id", new { Id = kalemId });
        
        if (kalem == null) return false;
        
        // Eğer adet 1'den fazlaysa, 1 tanesini ayır ve yeni kalem oluştur
        if (kalem.Adet > 1)
        {
            // Mevcut kalemin adetini 1 azalt
            await connection.ExecuteAsync(
                "UPDATE AdisyonKalem SET Adet = Adet - 1 WHERE Id = @Id",
                new { Id = kalemId });
            
            // Yeni kalem oluştur (1 adet, yeni fiyatla)
            await connection.ExecuteAsync(@"
                INSERT INTO AdisyonKalem (AdisyonId, UrunId, BirimFiyat, Adet, YarimPorsiyon, Notlar, Durum, EklenmeTarihi)
                VALUES (@AdisyonId, @UrunId, @BirimFiyat, 1, @YarimPorsiyon, @Notlar, @Durum, CURRENT_TIMESTAMP)",
                new { 
                    kalem.AdisyonId, 
                    kalem.UrunId, 
                    BirimFiyat = yeniFiyat,
                    kalem.YarimPorsiyon,
                    kalem.Notlar,
                    Durum = kalem.Durum.ToString()
                });
            
            await UpdateAdisyonTotalAsync(kalem.AdisyonId);
            _logger.LogInformation("Kalem ayrıldı ve fiyatı güncellendi: {KalemId}, Yeni fiyat: {Fiyat}₺", kalemId, yeniFiyat);
            return true;
        }
        
        // Adet 1 ise direkt güncelle
        var affected = await connection.ExecuteAsync(
            "UPDATE AdisyonKalem SET BirimFiyat = @BirimFiyat WHERE Id = @Id",
            new { Id = kalemId, BirimFiyat = yeniFiyat });
        
        if (affected > 0)
        {
            await UpdateAdisyonTotalAsync(kalem.AdisyonId);
            _logger.LogInformation("Kalem fiyatı güncellendi: {KalemId}, Yeni fiyat: {Fiyat}", kalemId, yeniFiyat);
        }
        
        return affected > 0;
    }

    public async Task<bool> UpdateKalemAdetAsync(int kalemId, int yeniAdet)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        var kalem = await connection.QuerySingleOrDefaultAsync<AdisyonKalem>(
            "SELECT * FROM AdisyonKalem WHERE Id = @Id", new { Id = kalemId });
        
        if (kalem == null) return false;
        
        if (yeniAdet <= 0)
        {
            return await RemoveKalemAsync(kalemId);
        }
        
        var affected = await connection.ExecuteAsync(
            "UPDATE AdisyonKalem SET Adet = @Adet WHERE Id = @Id",
            new { Id = kalemId, Adet = yeniAdet });
        
        if (affected > 0)
        {
            await UpdateAdisyonTotalAsync(kalem.AdisyonId);
            _logger.LogDebug("Kalem adeti güncellendi: {KalemId}, Adet: {Adet}", kalemId, yeniAdet);
        }
        
        return affected > 0;
    }

    public async Task<bool> RemoveKalemAsync(int kalemId)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        var kalem = await connection.QuerySingleOrDefaultAsync<AdisyonKalem>(
            "SELECT * FROM AdisyonKalem WHERE Id = @Id", new { Id = kalemId });
        
        if (kalem == null) return false;
        
        var affected = await connection.ExecuteAsync(
            "UPDATE AdisyonKalem SET Durum = 'Iptal' WHERE Id = @Id",
            new { Id = kalemId });
        
        if (affected > 0)
        {
            await UpdateAdisyonTotalAsync(kalem.AdisyonId);
            _logger.LogInformation("Kalem silindi: {KalemId}", kalemId);
        }
        
        return affected > 0;
    }

    public async Task<bool> SetKalemNotAsync(int kalemId, string not)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var affected = await connection.ExecuteAsync(
            "UPDATE AdisyonKalem SET Notlar = @Notlar WHERE Id = @Id",
            new { Id = kalemId, Notlar = not });
        return affected > 0;
    }

    public async Task<bool> SetKalemIkramAsync(int kalemId, string neden)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        var kalem = await connection.QuerySingleOrDefaultAsync<AdisyonKalem>(
            "SELECT * FROM AdisyonKalem WHERE Id = @Id", new { Id = kalemId });
        
        if (kalem == null) return false;
        
        var affected = await connection.ExecuteAsync(
            "UPDATE AdisyonKalem SET Durum = 'Ikram', IkramNeden = @Neden WHERE Id = @Id",
            new { Id = kalemId, Neden = neden });
        
        if (affected > 0)
        {
            await UpdateAdisyonTotalAsync(kalem.AdisyonId);
            _logger.LogInformation("Kalem ikram edildi: {KalemId}, Neden: {Neden}", kalemId, neden);
        }
        
        return affected > 0;
    }

    public async Task<decimal> CalculateTotalAsync(int adisyonId)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var total = await connection.ExecuteScalarAsync<decimal?>(@"
            SELECT SUM(Adet * BirimFiyat) FROM AdisyonKalem 
            WHERE AdisyonId = @AdisyonId AND Durum NOT IN ('Iptal', 'Ikram')",
            new { AdisyonId = adisyonId });
        return total ?? 0;
    }

    public async Task UpdateAdisyonTotalAsync(int adisyonId)
    {
        var total = await CalculateTotalAsync(adisyonId);
        using var connection = await _dbConnection.CreateConnectionAsync();
        await connection.ExecuteAsync(
            "UPDATE Adisyon SET ToplamTutar = @Total WHERE Id = @Id",
            new { Id = adisyonId, Total = total });
    }
}
