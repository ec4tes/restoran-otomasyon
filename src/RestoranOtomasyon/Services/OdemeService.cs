using Dapper;
using Microsoft.Extensions.Logging;
using RestoranOtomasyon.Data;
using RestoranOtomasyon.Models;

namespace RestoranOtomasyon.Services;

/// <summary>
/// Ödeme sonucu
/// </summary>
public class OdemeSonuc
{
    public bool Basarili { get; set; }
    public string? Mesaj { get; set; }
    public decimal ParaUstu { get; set; }
    public int AdisyonId { get; set; }
}

/// <summary>
/// İndirim bilgisi
/// </summary>
public class IndirimBilgi
{
    public bool IsYuzde { get; set; }
    public decimal Deger { get; set; }
    public string Neden { get; set; } = "";
    public int OnaylayanId { get; set; }
}

/// <summary>
/// Ödeme servisi interface
/// </summary>
public interface IOdemeService
{
    Task<OdemeSonuc> ProcessNakitAsync(int adisyonId, decimal alinanTutar);
    Task<OdemeSonuc> ProcessKartAsync(int adisyonId);
    Task<OdemeSonuc> ProcessKarisikAsync(int adisyonId, decimal nakitTutar);
    Task<bool> ApplyIndirimAsync(int adisyonId, IndirimBilgi indirim);
    Task<bool> ApplyIkramAsync(int kalemId, string neden, int onaylayanId);
    Task<decimal> GetOdenecekTutarAsync(int adisyonId);
    Task<bool> CloseAdisyonAsync(int adisyonId, OdemeTipi odemeTipi, decimal nakitTutar, decimal kartTutar);
}

/// <summary>
/// Ödeme servisi implementasyonu
/// </summary>
public class OdemeService : IOdemeService
{
    private readonly IDatabaseConnection _dbConnection;
    private readonly IAdisyonService _adisyonService;
    private readonly IMasaService _masaService;
    private readonly ILogger<OdemeService> _logger;

    public OdemeService(
        IDatabaseConnection dbConnection,
        IAdisyonService adisyonService,
        IMasaService masaService,
        ILogger<OdemeService> logger)
    {
        _dbConnection = dbConnection;
        _adisyonService = adisyonService;
        _masaService = masaService;
        _logger = logger;
    }

    public async Task<decimal> GetOdenecekTutarAsync(int adisyonId)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        var adisyon = await connection.QuerySingleOrDefaultAsync<Adisyon>(
            "SELECT * FROM Adisyon WHERE Id = @Id",
            new { Id = adisyonId });
        
        if (adisyon == null) return 0;
        
        // Toplam - İndirim
        return adisyon.ToplamTutar - adisyon.IndirimTutar;
    }

    public async Task<OdemeSonuc> ProcessNakitAsync(int adisyonId, decimal alinanTutar)
    {
        var odenecek = await GetOdenecekTutarAsync(adisyonId);
        
        if (alinanTutar < odenecek)
        {
            return new OdemeSonuc
            {
                Basarili = false,
                Mesaj = $"Yetersiz tutar! Ödenmesi gereken: ₺{odenecek:N2}",
                AdisyonId = adisyonId
            };
        }

        var paraUstu = alinanTutar - odenecek;
        
        var success = await CloseAdisyonAsync(adisyonId, OdemeTipi.Nakit, alinanTutar, 0);
        
        if (success)
        {
            _logger.LogInformation("Nakit ödeme alındı: Adisyon {AdisyonId}, Tutar: {Tutar}, Para üstü: {ParaUstu}",
                adisyonId, alinanTutar, paraUstu);
            
            return new OdemeSonuc
            {
                Basarili = true,
                Mesaj = paraUstu > 0 ? $"Para üstü: ₺{paraUstu:N2}" : "Ödeme tamamlandı",
                ParaUstu = paraUstu,
                AdisyonId = adisyonId
            };
        }

        return new OdemeSonuc
        {
            Basarili = false,
            Mesaj = "Ödeme işlemi başarısız!",
            AdisyonId = adisyonId
        };
    }

    public async Task<OdemeSonuc> ProcessKartAsync(int adisyonId)
    {
        var odenecek = await GetOdenecekTutarAsync(adisyonId);
        
        var success = await CloseAdisyonAsync(adisyonId, OdemeTipi.Kart, 0, odenecek);
        
        if (success)
        {
            _logger.LogInformation("Kart ödeme alındı: Adisyon {AdisyonId}, Tutar: {Tutar}",
                adisyonId, odenecek);
            
            return new OdemeSonuc
            {
                Basarili = true,
                Mesaj = "Kart ile ödeme tamamlandı",
                ParaUstu = 0,
                AdisyonId = adisyonId
            };
        }

        return new OdemeSonuc
        {
            Basarili = false,
            Mesaj = "Kart ödeme işlemi başarısız!",
            AdisyonId = adisyonId
        };
    }

    public async Task<OdemeSonuc> ProcessKarisikAsync(int adisyonId, decimal nakitTutar)
    {
        var odenecek = await GetOdenecekTutarAsync(adisyonId);
        
        if (nakitTutar > odenecek)
        {
            return new OdemeSonuc
            {
                Basarili = false,
                Mesaj = $"Nakit tutar toplam tutardan fazla olamaz! Toplam: ₺{odenecek:N2}",
                AdisyonId = adisyonId
            };
        }

        var kartTutar = odenecek - nakitTutar;
        
        var success = await CloseAdisyonAsync(adisyonId, OdemeTipi.Karisik, nakitTutar, kartTutar);
        
        if (success)
        {
            _logger.LogInformation("Karışık ödeme alındı: Adisyon {AdisyonId}, Nakit: {Nakit}, Kart: {Kart}",
                adisyonId, nakitTutar, kartTutar);
            
            return new OdemeSonuc
            {
                Basarili = true,
                Mesaj = $"Ödeme tamamlandı (Nakit: ₺{nakitTutar:N2}, Kart: ₺{kartTutar:N2})",
                ParaUstu = 0,
                AdisyonId = adisyonId
            };
        }

        return new OdemeSonuc
        {
            Basarili = false,
            Mesaj = "Karışık ödeme işlemi başarısız!",
            AdisyonId = adisyonId
        };
    }

    public async Task<bool> ApplyIndirimAsync(int adisyonId, IndirimBilgi indirim)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        var adisyon = await connection.QuerySingleOrDefaultAsync<Adisyon>(
            "SELECT * FROM Adisyon WHERE Id = @Id",
            new { Id = adisyonId });
        
        if (adisyon == null) return false;
        
        decimal indirimTutar;
        if (indirim.IsYuzde)
        {
            indirimTutar = adisyon.ToplamTutar * (indirim.Deger / 100);
        }
        else
        {
            indirimTutar = indirim.Deger;
        }
        
        // İndirim toplam tutardan fazla olamaz
        if (indirimTutar > adisyon.ToplamTutar)
        {
            indirimTutar = adisyon.ToplamTutar;
        }

        var affected = await connection.ExecuteAsync(@"
            UPDATE Adisyon 
            SET IndirimTutar = @IndirimTutar, IndirimNeden = @IndirimNeden 
            WHERE Id = @Id",
            new { 
                Id = adisyonId, 
                IndirimTutar = indirimTutar,
                IndirimNeden = indirim.Neden 
            });
        
        if (affected > 0)
        {
            _logger.LogInformation("İndirim uygulandı: Adisyon {AdisyonId}, Tutar: {Tutar}, Neden: {Neden}, Onaylayan: {Onaylayan}",
                adisyonId, indirimTutar, indirim.Neden, indirim.OnaylayanId);
        }
        
        return affected > 0;
    }

    public async Task<bool> ApplyIkramAsync(int kalemId, string neden, int onaylayanId)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        // Kalemin adisyon ID'sini al
        var kalem = await connection.QuerySingleOrDefaultAsync<AdisyonKalem>(
            "SELECT * FROM AdisyonKalem WHERE Id = @Id",
            new { Id = kalemId });
        
        if (kalem == null) return false;
        
        // Kalemi ikram yap
        var affected = await connection.ExecuteAsync(@"
            UPDATE AdisyonKalem 
            SET Durum = 'Ikram', IkramNeden = @Neden 
            WHERE Id = @Id",
            new { Id = kalemId, Neden = neden });
        
        if (affected > 0)
        {
            // Adisyon toplamını güncelle
            await _adisyonService.UpdateAdisyonTotalAsync(kalem.AdisyonId);
            
            _logger.LogInformation("İkram yapıldı: Kalem {KalemId}, Neden: {Neden}, Onaylayan: {Onaylayan}",
                kalemId, neden, onaylayanId);
        }
        
        return affected > 0;
    }

    public async Task<bool> CloseAdisyonAsync(int adisyonId, OdemeTipi odemeTipi, decimal nakitTutar, decimal kartTutar)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();
        
        try
        {
            // Adisyonu al
            var adisyon = await connection.QuerySingleOrDefaultAsync<Adisyon>(
                "SELECT * FROM Adisyon WHERE Id = @Id",
                new { Id = adisyonId },
                transaction);
            
            if (adisyon == null || adisyon.Durum == AdisyonDurum.Odendi)
            {
                return false;
            }

            // Adisyonu kapat
            await connection.ExecuteAsync(@"
                UPDATE Adisyon 
                SET Durum = 'Odendi', 
                    OdemeTipi = @OdemeTipi, 
                    NakitTutar = @NakitTutar, 
                    KartTutar = @KartTutar,
                    KapanmaTarihi = CURRENT_TIMESTAMP
                WHERE Id = @Id",
                new { 
                    Id = adisyonId, 
                    OdemeTipi = odemeTipi.ToString(),
                    NakitTutar = nakitTutar,
                    KartTutar = kartTutar
                },
                transaction);

            // Masayı boşalt (Masa bazlı adisyonsa)
            if (adisyon.MasaId.HasValue)
            {
                await connection.ExecuteAsync(
                    "UPDATE Masa SET Durum = 'Bos' WHERE Id = @Id",
                    new { Id = adisyon.MasaId.Value },
                    transaction);
                
                _logger.LogInformation("Masa boşaltıldı: {MasaId}", adisyon.MasaId.Value);
            }

            transaction.Commit();
            _logger.LogInformation("Adisyon kapatıldı: {AdisyonId}, Ödeme: {OdemeTipi}", adisyonId, odemeTipi);
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "Adisyon kapatılırken hata: {AdisyonId}", adisyonId);
            return false;
        }
    }
}
