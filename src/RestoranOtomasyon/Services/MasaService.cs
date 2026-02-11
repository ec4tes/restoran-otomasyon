using Dapper;
using Microsoft.Extensions.Logging;
using RestoranOtomasyon.Data;
using RestoranOtomasyon.Models;

namespace RestoranOtomasyon.Services;

/// <summary>
/// Masa işlemleri servisi
/// </summary>
public interface IMasaService
{
    Task<IEnumerable<Masa>> GetAllMasalarAsync();
    Task<IEnumerable<Masa>> GetMasalarByBolumAsync(MasaBolum bolum);
    Task<Masa?> GetMasaByIdAsync(int id);
    Task<bool> UpdateMasaDurumAsync(int masaId, MasaDurum yeniDurum);
    Task<Masa?> GetMasaByAdisyonAsync(int adisyonId);
    Task<int> GetAktifAdisyonCountAsync(int masaId);
    
    // CRUD
    Task<int> CreateMasaAsync(Masa masa);
    Task UpdateMasaAsync(Masa masa);
    Task DeleteMasaAsync(int id);
}

/// <summary>
/// Masa servisi implementasyonu
/// </summary>
public class MasaService : IMasaService
{
    private readonly IDatabaseConnection _dbConnection;
    private readonly ILogger<MasaService> _logger;

    public MasaService(IDatabaseConnection dbConnection, ILogger<MasaService> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public async Task<IEnumerable<Masa>> GetAllMasalarAsync()
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        var masalar = await connection.QueryAsync<Masa>(
            "SELECT * FROM Masa WHERE Aktif = 1 ORDER BY Bolum, MasaNo");
        
        _logger.LogDebug("{Count} masa getirildi", masalar.Count());
        return masalar;
    }

    public async Task<IEnumerable<Masa>> GetMasalarByBolumAsync(MasaBolum bolum)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        var masalar = await connection.QueryAsync<Masa>(
            "SELECT * FROM Masa WHERE Bolum = @Bolum AND Aktif = 1 ORDER BY MasaNo",
            new { Bolum = bolum.ToString() });
        
        return masalar;
    }

    public async Task<Masa?> GetMasaByIdAsync(int id)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        return await connection.QuerySingleOrDefaultAsync<Masa>(
            "SELECT * FROM Masa WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<bool> UpdateMasaDurumAsync(int masaId, MasaDurum yeniDurum)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        var affected = await connection.ExecuteAsync(
            "UPDATE Masa SET Durum = @Durum WHERE Id = @Id",
            new { Id = masaId, Durum = yeniDurum.ToString() });
        
        if (affected > 0)
        {
            _logger.LogInformation("Masa {MasaId} durumu güncellendi: {Durum}", masaId, yeniDurum);
        }
        
        return affected > 0;
    }

    public async Task<Masa?> GetMasaByAdisyonAsync(int adisyonId)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        return await connection.QuerySingleOrDefaultAsync<Masa>(@"
            SELECT m.* FROM Masa m
            INNER JOIN Adisyon a ON m.Id = a.MasaId
            WHERE a.Id = @AdisyonId",
            new { AdisyonId = adisyonId });
    }

    public async Task<int> GetAktifAdisyonCountAsync(int masaId)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Adisyon WHERE MasaId = @MasaId AND Durum IN ('Acik', 'Hesap')",
            new { MasaId = masaId });
    }

    public async Task<int> CreateMasaAsync(Masa masa)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        var id = await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO Masa (MasaNo, Bolum, Durum, Kapasite, X, Y, Aktif)
            VALUES (@MasaNo, @Bolum, @Durum, @Kapasite, @X, @Y, @Aktif);
            SELECT last_insert_rowid();",
            new 
            { 
                masa.MasaNo, 
                Bolum = masa.Bolum.ToString(), 
                Durum = masa.Durum.ToString(), 
                masa.Kapasite, 
                masa.X, 
                masa.Y, 
                masa.Aktif 
            });
        
        _logger.LogInformation("Yeni masa oluşturuldu: {MasaNo} (ID: {Id})", masa.MasaNo, id);
        return id;
    }

    public async Task UpdateMasaAsync(Masa masa)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        await connection.ExecuteAsync(@"
            UPDATE Masa 
            SET MasaNo = @MasaNo, Bolum = @Bolum, Kapasite = @Kapasite, Aktif = @Aktif
            WHERE Id = @Id",
            new 
            { 
                masa.Id, 
                masa.MasaNo, 
                Bolum = masa.Bolum.ToString(), 
                masa.Kapasite, 
                masa.Aktif 
            });
        
        _logger.LogInformation("Masa güncellendi: {MasaNo} (ID: {Id})", masa.MasaNo, masa.Id);
    }

    public async Task DeleteMasaAsync(int id)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM Masa WHERE Id = @Id", new { Id = id });
        _logger.LogInformation("Masa silindi: ID {Id}", id);
    }
}
