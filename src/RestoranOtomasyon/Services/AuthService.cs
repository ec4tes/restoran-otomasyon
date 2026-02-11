using Dapper;
using Microsoft.Extensions.Logging;
using RestoranOtomasyon.Data;
using RestoranOtomasyon.Models;

namespace RestoranOtomasyon.Services;

/// <summary>
/// Kimlik doğrulama servisi
/// </summary>
public interface IAuthService
{
    Task<Kullanici?> ValidatePinAsync(string pin);
    Task<bool> ValidateManagerPinAsync(string pin);
    Task LogAuthAttemptAsync(int? kullaniciId, string islem, string? detay = null);
    
    // Kullanıcı CRUD
    Task<IEnumerable<Kullanici>> GetAllUsersAsync();
    Task<Kullanici?> GetUserByIdAsync(int id);
    Task<int> CreateUserAsync(Kullanici kullanici, string pin);
    Task UpdateUserAsync(Kullanici kullanici, string? newPin = null);
    Task DeleteUserAsync(int id);
}

/// <summary>
/// Kimlik doğrulama implementasyonu
/// </summary>
public class AuthService : IAuthService
{
    private readonly IDatabaseConnection _dbConnection;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IDatabaseConnection dbConnection, ILogger<AuthService> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public async Task<Kullanici?> ValidatePinAsync(string pin)
    {
        _logger.LogDebug("PIN doğrulama başlatıldı");

        using var connection = await _dbConnection.CreateConnectionAsync();
        
        // Tüm aktif kullanıcıları al
        var users = await connection.QueryAsync<Kullanici>(
            "SELECT * FROM Kullanici WHERE Aktif = 1");

        foreach (var user in users)
        {
            if (BCrypt.Net.BCrypt.Verify(pin, user.PinHash))
            {
                // Son giriş tarihini güncelle
                await connection.ExecuteAsync(
                    "UPDATE Kullanici SET SonGirisTarihi = @Tarih WHERE Id = @Id",
                    new { Tarih = DateTime.Now, Id = user.Id });

                await LogAuthAttemptAsync(user.Id, "PIN_GIRIS", $"Başarılı giriş: {user.Ad}");
                
                // Rol string'ini enum'a çevir
                user.Rol = Enum.Parse<KullaniciRol>(user.Rol.ToString());
                
                _logger.LogInformation("Kullanıcı giriş yaptı: {UserName} ({Role})", user.Ad, user.Rol);
                return user;
            }
        }

        await LogAuthAttemptAsync(null, "PIN_GIRIS", "Başarısız giriş denemesi");
        _logger.LogWarning("Başarısız PIN denemesi");
        return null;
    }

    public async Task<bool> ValidateManagerPinAsync(string pin)
    {
        _logger.LogDebug("Yönetici PIN doğrulama başlatıldı");

        using var connection = await _dbConnection.CreateConnectionAsync();
        
        // Yönetici ve Admin kullanıcıları al
        var managers = await connection.QueryAsync<Kullanici>(
            "SELECT * FROM Kullanici WHERE Aktif = 1 AND Rol IN ('Yonetici', 'Admin')");

        foreach (var user in managers)
        {
            if (BCrypt.Net.BCrypt.Verify(pin, user.PinHash))
            {
                await LogAuthAttemptAsync(user.Id, "YONETICI_PIN", $"Yönetici onayı: {user.Ad}");
                _logger.LogInformation("Yönetici PIN onaylandı: {UserName}", user.Ad);
                return true;
            }
        }

        await LogAuthAttemptAsync(null, "YONETICI_PIN", "Başarısız yönetici PIN denemesi");
        _logger.LogWarning("Başarısız yönetici PIN denemesi");
        return false;
    }

    public async Task LogAuthAttemptAsync(int? kullaniciId, string islem, string? detay = null)
    {
        try
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            await connection.ExecuteAsync(
                "INSERT INTO YetkiLog (KullaniciId, Islem, Detay) VALUES (@KullaniciId, @Islem, @Detay)",
                new { KullaniciId = kullaniciId, Islem = islem, Detay = detay });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yetki log kaydı başarısız");
        }
    }

    public async Task<IEnumerable<Kullanici>> GetAllUsersAsync()
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        var users = await connection.QueryAsync<Kullanici>("SELECT * FROM Kullanici ORDER BY Ad");
        return users;
    }

    public async Task<Kullanici?> GetUserByIdAsync(int id)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<Kullanici>(
            "SELECT * FROM Kullanici WHERE Id = @Id", new { Id = id });
    }

    public async Task<int> CreateUserAsync(Kullanici kullanici, string pin)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        var pinHash = BCrypt.Net.BCrypt.HashPassword(pin);
        
        var id = await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO Kullanici (Ad, PinHash, Rol, Aktif)
            VALUES (@Ad, @PinHash, @Rol, @Aktif);
            SELECT last_insert_rowid();",
            new 
            { 
                Ad = kullanici.Ad, 
                PinHash = pinHash, 
                Rol = kullanici.Rol.ToString(), 
                Aktif = kullanici.Aktif 
            });
        
        _logger.LogInformation("Yeni kullanıcı oluşturuldu: {Ad} (ID: {Id})", kullanici.Ad, id);
        return id;
    }

    public async Task UpdateUserAsync(Kullanici kullanici, string? newPin = null)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        if (!string.IsNullOrEmpty(newPin))
        {
            var pinHash = BCrypt.Net.BCrypt.HashPassword(newPin);
            await connection.ExecuteAsync(@"
                UPDATE Kullanici 
                SET Ad = @Ad, PinHash = @PinHash, Rol = @Rol, Aktif = @Aktif
                WHERE Id = @Id",
                new 
                { 
                    Id = kullanici.Id, 
                    Ad = kullanici.Ad, 
                    PinHash = pinHash, 
                    Rol = kullanici.Rol.ToString(), 
                    Aktif = kullanici.Aktif 
                });
        }
        else
        {
            await connection.ExecuteAsync(@"
                UPDATE Kullanici 
                SET Ad = @Ad, Rol = @Rol, Aktif = @Aktif
                WHERE Id = @Id",
                new 
                { 
                    Id = kullanici.Id, 
                    Ad = kullanici.Ad, 
                    Rol = kullanici.Rol.ToString(), 
                    Aktif = kullanici.Aktif 
                });
        }
        
        _logger.LogInformation("Kullanıcı güncellendi: {Ad} (ID: {Id})", kullanici.Ad, kullanici.Id);
    }

    public async Task DeleteUserAsync(int id)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM Kullanici WHERE Id = @Id", new { Id = id });
        _logger.LogInformation("Kullanıcı silindi: ID {Id}", id);
    }
}
