using Dapper;
using Microsoft.Extensions.Logging;
using RestoranOtomasyon.Data;
using RestoranOtomasyon.Models;

namespace RestoranOtomasyon.Services;

/// <summary>
/// Ürün işlemleri servisi
/// </summary>
public interface IUrunService
{
    Task<IEnumerable<Urun>> GetAllAsync();
    Task<IEnumerable<Urun>> GetByKategoriAsync(int kategoriId);
    Task<IEnumerable<Urun>> GetFavorilerAsync();
    Task<Urun?> GetByIdAsync(int id);
    Task<int> CreateAsync(Urun urun);
    Task<bool> UpdateAsync(Urun urun);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleFavoriAsync(int id);
    Task<IEnumerable<UrunNotu>> GetUrunNotlariAsync();
}

public class UrunService : IUrunService
{
    private readonly IDatabaseConnection _dbConnection;
    private readonly ILogger<UrunService> _logger;

    public UrunService(IDatabaseConnection dbConnection, ILogger<UrunService> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public async Task<IEnumerable<Urun>> GetAllAsync()
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        return await connection.QueryAsync<Urun>(
            "SELECT * FROM Urun WHERE Aktif = 1 ORDER BY Ad");
    }

    public async Task<IEnumerable<Urun>> GetByKategoriAsync(int kategoriId)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var urunler = await connection.QueryAsync<Urun>(
            "SELECT * FROM Urun WHERE KategoriId = @KategoriId AND Aktif = 1 ORDER BY Ad",
            new { KategoriId = kategoriId });
        _logger.LogDebug("Kategori {KategoriId} için {Count} ürün getirildi", kategoriId, urunler.Count());
        return urunler;
    }

    public async Task<IEnumerable<Urun>> GetFavorilerAsync()
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        return await connection.QueryAsync<Urun>(
            "SELECT * FROM Urun WHERE Favori = 1 AND Aktif = 1 ORDER BY Ad");
    }

    public async Task<Urun?> GetByIdAsync(int id)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Urun>(
            "SELECT * FROM Urun WHERE Id = @Id", new { Id = id });
    }

    public async Task<int> CreateAsync(Urun urun)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var id = await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO Urun (KategoriId, Ad, Fiyat, YarimPorsiyonFiyat, Favori, Renk, Aktif, OlusturmaTarihi) 
            VALUES (@KategoriId, @Ad, @Fiyat, @YarimPorsiyonFiyat, @Favori, @Renk, @Aktif, CURRENT_TIMESTAMP);
            SELECT last_insert_rowid();",
            urun);
        _logger.LogInformation("Yeni ürün oluşturuldu: {UrunAd}, Fiyat: {Fiyat}, YarımPorsiyon: {YarimFiyat}", 
            urun.Ad, urun.Fiyat, urun.YarimPorsiyonFiyat);
        return id;
    }

    public async Task<bool> UpdateAsync(Urun urun)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var affected = await connection.ExecuteAsync(@"
            UPDATE Urun SET KategoriId = @KategoriId, Ad = @Ad, Fiyat = @Fiyat, 
                           YarimPorsiyonFiyat = @YarimPorsiyonFiyat,
                           Favori = @Favori, Renk = @Renk, Aktif = @Aktif 
            WHERE Id = @Id", urun);
        if (affected > 0)
            _logger.LogInformation("Ürün güncellendi: {UrunId}", urun.Id);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var affected = await connection.ExecuteAsync(
            "UPDATE Urun SET Aktif = 0 WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }

    public async Task<bool> ToggleFavoriAsync(int id)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var affected = await connection.ExecuteAsync(
            "UPDATE Urun SET Favori = NOT Favori WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }

    public async Task<IEnumerable<UrunNotu>> GetUrunNotlariAsync()
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        return await connection.QueryAsync<UrunNotu>(
            "SELECT * FROM UrunNotu WHERE Aktif = 1 ORDER BY Ad");
    }
}

/// <summary>
/// Ürün notu modeli
/// </summary>
public class UrunNotu
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string? Kisayol { get; set; }
    public bool Aktif { get; set; } = true;
}
