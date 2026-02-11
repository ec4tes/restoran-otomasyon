using Dapper;
using Microsoft.Extensions.Logging;
using RestoranOtomasyon.Data;
using RestoranOtomasyon.Models;

namespace RestoranOtomasyon.Services;

/// <summary>
/// Kategori işlemleri servisi
/// </summary>
public interface IKategoriService
{
    Task<IEnumerable<Kategori>> GetAllAsync();
    Task<IEnumerable<Kategori>> GetActiveAsync();
    Task<Kategori?> GetByIdAsync(int id);
    Task<int> CreateAsync(Kategori kategori);
    Task<bool> UpdateAsync(Kategori kategori);
    Task<bool> DeleteAsync(int id);
}

public class KategoriService : IKategoriService
{
    private readonly IDatabaseConnection _dbConnection;
    private readonly ILogger<KategoriService> _logger;

    public KategoriService(IDatabaseConnection dbConnection, ILogger<KategoriService> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public async Task<IEnumerable<Kategori>> GetAllAsync()
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        return await connection.QueryAsync<Kategori>(
            "SELECT * FROM Kategori ORDER BY Sira, Ad");
    }

    public async Task<IEnumerable<Kategori>> GetActiveAsync()
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var kategoriler = await connection.QueryAsync<Kategori>(
            "SELECT * FROM Kategori WHERE Aktif = 1 ORDER BY Sira, Ad");
        _logger.LogDebug("{Count} aktif kategori getirildi", kategoriler.Count());
        return kategoriler;
    }

    public async Task<Kategori?> GetByIdAsync(int id)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Kategori>(
            "SELECT * FROM Kategori WHERE Id = @Id", new { Id = id });
    }

    public async Task<int> CreateAsync(Kategori kategori)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var id = await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO Kategori (Ad, Renk, Sira, Aktif) 
            VALUES (@Ad, @Renk, @Sira, @Aktif);
            SELECT last_insert_rowid();",
            kategori);
        _logger.LogInformation("Yeni kategori oluşturuldu: {KategoriAd}", kategori.Ad);
        return id;
    }

    public async Task<bool> UpdateAsync(Kategori kategori)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var affected = await connection.ExecuteAsync(@"
            UPDATE Kategori SET Ad = @Ad, Renk = @Renk, Sira = @Sira, Aktif = @Aktif 
            WHERE Id = @Id", kategori);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var affected = await connection.ExecuteAsync(
            "UPDATE Kategori SET Aktif = 0 WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }
}
