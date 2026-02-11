using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;
using Dapper;
using Microsoft.Extensions.Logging;

namespace RestoranOtomasyon.Data;

/// <summary>
/// SQLite veritabanı bağlantı yöneticisi
/// </summary>
public interface IDatabaseConnection
{
    IDbConnection CreateConnection();
    Task<IDbConnection> CreateConnectionAsync();
    string ConnectionString { get; }
}

/// <summary>
/// SQLite bağlantı implementasyonu
/// </summary>
public class DatabaseConnection : IDatabaseConnection
{
    private readonly ILogger<DatabaseConnection> _logger;
    public string ConnectionString { get; }

    public DatabaseConnection(ILogger<DatabaseConnection> logger)
    {
        _logger = logger;
        
        // Uygulama dizininde Data klasörü oluştur
        var appPath = AppDomain.CurrentDomain.BaseDirectory;
        var dataPath = Path.Combine(appPath, "Data");
        
        if (!Directory.Exists(dataPath))
        {
            Directory.CreateDirectory(dataPath);
            _logger.LogInformation("Data klasörü oluşturuldu: {DataPath}", dataPath);
        }

        var dbPath = Path.Combine(dataPath, "restoran.db");
        ConnectionString = $"Data Source={dbPath}";
        
        _logger.LogInformation("Veritabanı yolu: {DbPath}", dbPath);
    }

    public IDbConnection CreateConnection()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection(ConnectionString);
        connection.Open();
        
        // SQLite optimizasyonları
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;
            PRAGMA foreign_keys = ON;
            PRAGMA temp_store = MEMORY;
            PRAGMA cache_size = -64000;
        ";
        cmd.ExecuteNonQuery();
        
        return connection;
    }

    public async Task<IDbConnection> CreateConnectionAsync()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        
        // SQLite optimizasyonları
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;
            PRAGMA foreign_keys = ON;
            PRAGMA temp_store = MEMORY;
            PRAGMA cache_size = -64000;
        ";
        await cmd.ExecuteNonQueryAsync();
        
        return connection;
    }
}
