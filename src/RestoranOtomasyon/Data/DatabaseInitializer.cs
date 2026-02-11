using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace RestoranOtomasyon.Data;

/// <summary>
/// Veritabanı migration/initialization servisi
/// </summary>
public interface IDatabaseInitializer
{
    Task InitializeAsync();
}

/// <summary>
/// SQLite veritabanı başlatıcı - DDL ve seed data
/// </summary>
public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDatabaseConnection _dbConnection;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IDatabaseConnection dbConnection, ILogger<DatabaseInitializer> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Veritabanı başlatılıyor...");

        using var connection = await _dbConnection.CreateConnectionAsync();

        // Tabloları oluştur
        await CreateTablesAsync(connection);
        
        // Migration - Yeni sütunları ekle (mevcut DB için)
        await RunMigrationsAsync(connection);
        
        // Varsayılan verileri ekle
        await SeedDataAsync(connection);

        _logger.LogInformation("Veritabanı başlatma tamamlandı.");
    }

    private async Task RunMigrationsAsync(IDbConnection connection)
    {
        _logger.LogDebug("Migration'lar çalıştırılıyor...");

        // Urun tablosuna YarimPorsiyonFiyat sütunu ekle
        try
        {
            var columns = await connection.QueryAsync<string>(
                "SELECT name FROM pragma_table_info('Urun')");
            
            if (!columns.Contains("YarimPorsiyonFiyat"))
            {
                await connection.ExecuteAsync(
                    "ALTER TABLE Urun ADD COLUMN YarimPorsiyonFiyat REAL DEFAULT NULL");
                _logger.LogInformation("Urun tablosuna YarimPorsiyonFiyat sütunu eklendi");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("YarimPorsiyonFiyat migration: {Message}", ex.Message);
        }

        // AdisyonKalem tablosuna YarimPorsiyon sütunu ekle
        try
        {
            var columns = await connection.QueryAsync<string>(
                "SELECT name FROM pragma_table_info('AdisyonKalem')");
            
            if (!columns.Contains("YarimPorsiyon"))
            {
                await connection.ExecuteAsync(
                    "ALTER TABLE AdisyonKalem ADD COLUMN YarimPorsiyon INTEGER DEFAULT 0");
                _logger.LogInformation("AdisyonKalem tablosuna YarimPorsiyon sütunu eklendi");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("YarimPorsiyon migration: {Message}", ex.Message);
        }

        _logger.LogDebug("Migration'lar tamamlandı.");
    }

    private async Task CreateTablesAsync(IDbConnection connection)
    {
        _logger.LogDebug("Tablolar oluşturuluyor...");

        var ddl = @"
-- =============================================
-- KULLANICI & YETKİ
-- =============================================

CREATE TABLE IF NOT EXISTS Kullanici (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Ad              TEXT NOT NULL,
    PinHash         TEXT NOT NULL,
    Rol             TEXT NOT NULL CHECK(Rol IN ('Calisan', 'Yonetici', 'Admin')),
    Aktif           INTEGER DEFAULT 1,
    OlusturmaTarihi DATETIME DEFAULT CURRENT_TIMESTAMP,
    SonGirisTarihi  DATETIME
);

CREATE TABLE IF NOT EXISTS YetkiLog (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    KullaniciId     INTEGER,
    Islem           TEXT NOT NULL,
    Detay           TEXT,
    Tarih           DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (KullaniciId) REFERENCES Kullanici(Id)
);

-- =============================================
-- MASA & BÖLÜM
-- =============================================

CREATE TABLE IF NOT EXISTS Masa (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    MasaNo          TEXT NOT NULL UNIQUE,
    Bolum           TEXT NOT NULL CHECK(Bolum IN ('Iceri', 'Disari', 'GelAl', 'Paket')),
    Durum           TEXT DEFAULT 'Bos' CHECK(Durum IN ('Bos', 'Dolu', 'Hesap')),
    Kapasite        INTEGER DEFAULT 4,
    X               INTEGER DEFAULT 0,
    Y               INTEGER DEFAULT 0,
    Aktif           INTEGER DEFAULT 1
);

-- =============================================
-- ÜRÜN & KATEGORİ
-- =============================================

CREATE TABLE IF NOT EXISTS Kategori (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Ad              TEXT NOT NULL UNIQUE,
    Renk            TEXT DEFAULT '#3498db',
    Sira            INTEGER DEFAULT 0,
    Aktif           INTEGER DEFAULT 1
);

CREATE TABLE IF NOT EXISTS Urun (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    KategoriId      INTEGER NOT NULL,
    Ad              TEXT NOT NULL,
    Fiyat           REAL NOT NULL CHECK(Fiyat >= 0),
    YarimPorsiyonFiyat REAL DEFAULT NULL,
    Favori          INTEGER DEFAULT 0,
    Renk            TEXT DEFAULT '#2ecc71',
    Aktif           INTEGER DEFAULT 1,
    OlusturmaTarihi DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (KategoriId) REFERENCES Kategori(Id)
);

CREATE TABLE IF NOT EXISTS UrunNotu (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Ad              TEXT NOT NULL UNIQUE,
    Kisayol         TEXT,
    Aktif           INTEGER DEFAULT 1
);

-- =============================================
-- ADİSYON & SİPARİŞ
-- =============================================

CREATE TABLE IF NOT EXISTS Adisyon (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    MasaId          INTEGER,
    KullaniciId     INTEGER NOT NULL,
    Tip             TEXT NOT NULL CHECK(Tip IN ('Masa', 'GelAl', 'Paket')),
    Durum           TEXT DEFAULT 'Acik' CHECK(Durum IN ('Acik', 'Hesap', 'Odendi', 'Iptal')),
    ToplamTutar     REAL DEFAULT 0,
    IndirimTutar    REAL DEFAULT 0,
    IndirimNeden    TEXT,
    OdemeTipi       TEXT CHECK(OdemeTipi IN ('Nakit', 'Kart', 'Karisik')),
    NakitTutar      REAL DEFAULT 0,
    KartTutar       REAL DEFAULT 0,
    MusteriNot      TEXT,
    OlusturmaTarihi DATETIME DEFAULT CURRENT_TIMESTAMP,
    KapanmaTarihi   DATETIME,
    IptalNeden      TEXT,
    IptalEdenId     INTEGER,
    FOREIGN KEY (MasaId) REFERENCES Masa(Id),
    FOREIGN KEY (KullaniciId) REFERENCES Kullanici(Id),
    FOREIGN KEY (IptalEdenId) REFERENCES Kullanici(Id)
);

CREATE TABLE IF NOT EXISTS AdisyonKalem (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    AdisyonId       INTEGER NOT NULL,
    UrunId          INTEGER NOT NULL,
    Adet            INTEGER DEFAULT 1 CHECK(Adet > 0),
    BirimFiyat      REAL NOT NULL,
    YarimPorsiyon   INTEGER DEFAULT 0,
    Notlar          TEXT,
    Durum           TEXT DEFAULT 'Bekliyor' CHECK(Durum IN ('Bekliyor', 'Hazirlaniyor', 'Tamamlandi', 'Iptal', 'Ikram')),
    IkramNeden      TEXT,
    EklenmeTarihi   DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (AdisyonId) REFERENCES Adisyon(Id),
    FOREIGN KEY (UrunId) REFERENCES Urun(Id)
);

-- =============================================
-- FİNANS & RAPORLAMA
-- =============================================

CREATE TABLE IF NOT EXISTS GunSonu (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Tarih           DATE NOT NULL UNIQUE,
    ToplamCiro      REAL DEFAULT 0,
    NakitToplam     REAL DEFAULT 0,
    KartToplam      REAL DEFAULT 0,
    IptalToplam     REAL DEFAULT 0,
    IndirimToplam   REAL DEFAULT 0,
    IkramToplam     REAL DEFAULT 0,
    AdisyonSayisi   INTEGER DEFAULT 0,
    KapanisYapanId  INTEGER,
    KapanisTarihi   DATETIME,
    FOREIGN KEY (KapanisYapanId) REFERENCES Kullanici(Id)
);

CREATE TABLE IF NOT EXISTS Gider (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Tip             TEXT NOT NULL CHECK(Tip IN ('Fatura', 'Sigorta', 'Maas', 'Malzeme', 'Diger')),
    Aciklama        TEXT NOT NULL,
    Tutar           REAL NOT NULL,
    Tarih           DATE NOT NULL,
    DosyaYolu       TEXT,
    OlusturanId     INTEGER,
    OlusturmaTarihi DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (OlusturanId) REFERENCES Kullanici(Id)
);

CREATE TABLE IF NOT EXISTS SabitGider (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Ad              TEXT NOT NULL,
    Tutar           REAL,
    OdemeGunu       INTEGER,
    Hatirlatma      INTEGER DEFAULT 1,
    Aktif           INTEGER DEFAULT 1
);

-- =============================================
-- SİSTEM & LOG
-- =============================================

CREATE TABLE IF NOT EXISTS SistemAyar (
    Anahtar         TEXT PRIMARY KEY,
    Deger           TEXT,
    Aciklama        TEXT
);

CREATE TABLE IF NOT EXISTS IslemLog (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    KullaniciId     INTEGER,
    IslemTipi       TEXT NOT NULL,
    Tablo           TEXT,
    KayitId         INTEGER,
    EskiDeger       TEXT,
    YeniDeger       TEXT,
    Tarih           DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (KullaniciId) REFERENCES Kullanici(Id)
);

-- =============================================
-- İNDEKSLER
-- =============================================

CREATE INDEX IF NOT EXISTS idx_adisyon_tarih ON Adisyon(OlusturmaTarihi);
CREATE INDEX IF NOT EXISTS idx_adisyon_durum ON Adisyon(Durum);
CREATE INDEX IF NOT EXISTS idx_adisyon_masa ON Adisyon(MasaId);
CREATE INDEX IF NOT EXISTS idx_kalem_adisyon ON AdisyonKalem(AdisyonId);
CREATE INDEX IF NOT EXISTS idx_gider_tarih ON Gider(Tarih);
CREATE INDEX IF NOT EXISTS idx_log_tarih ON IslemLog(Tarih);
CREATE INDEX IF NOT EXISTS idx_urun_kategori ON Urun(KategoriId);
";

        // DDL'yi çalıştır
        await connection.ExecuteAsync(ddl);
        _logger.LogDebug("Tablolar oluşturuldu.");
    }

    private async Task SeedDataAsync(IDbConnection connection)
    {
        _logger.LogDebug("Varsayılan veriler kontrol ediliyor...");

        // Kullanıcı var mı kontrol et
        var userCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Kullanici");
        
        if (userCount == 0)
        {
            _logger.LogInformation("Varsayılan veriler ekleniyor...");

            // Varsayılan admin kullanıcısı (PIN: 1234)
            // BCrypt hash for "1234"
            var adminPinHash = BCrypt.Net.BCrypt.HashPassword("1234");
            var yoneticiPinHash = BCrypt.Net.BCrypt.HashPassword("5678");
            var calisanPinHash = BCrypt.Net.BCrypt.HashPassword("0000");

            await connection.ExecuteAsync(@"
                INSERT INTO Kullanici (Ad, PinHash, Rol) VALUES 
                (@Ad1, @Pin1, 'Admin'),
                (@Ad2, @Pin2, 'Yonetici'),
                (@Ad3, @Pin3, 'Calisan');
            ", new { 
                Ad1 = "Admin", Pin1 = adminPinHash,
                Ad2 = "Yönetici", Pin2 = yoneticiPinHash,
                Ad3 = "Çalışan", Pin3 = calisanPinHash
            });

            _logger.LogInformation("Varsayılan kullanıcılar oluşturuldu. Admin PIN: 1234, Yönetici PIN: 5678, Çalışan PIN: 0000");
        }

        // Masa var mı kontrol et
        var masaCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Masa");
        
        if (masaCount == 0)
        {
            // İç mekan masaları (4x2 = 8 masa)
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    var masaNo = $"IC-{y * 4 + x + 1}";
                    await connection.ExecuteAsync(
                        "INSERT INTO Masa (MasaNo, Bolum, X, Y) VALUES (@MasaNo, 'Iceri', @X, @Y)",
                        new { MasaNo = masaNo, X = x, Y = y });
                }
            }

            // Dış mekan masaları (2 masa)
            await connection.ExecuteAsync("INSERT INTO Masa (MasaNo, Bolum, X, Y) VALUES ('DIS-1', 'Disari', 0, 0)");
            await connection.ExecuteAsync("INSERT INTO Masa (MasaNo, Bolum, X, Y) VALUES ('DIS-2', 'Disari', 1, 0)");

            // Gel-Al ve Paket
            await connection.ExecuteAsync("INSERT INTO Masa (MasaNo, Bolum, X, Y) VALUES ('GELAL', 'GelAl', 0, 0)");
            await connection.ExecuteAsync("INSERT INTO Masa (MasaNo, Bolum, X, Y) VALUES ('PAKET', 'Paket', 0, 0)");

            _logger.LogInformation("Varsayılan masalar oluşturuldu (8 iç + 2 dış + GelAl + Paket)");
        }

        // Ürün notları var mı kontrol et
        var notCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM UrunNotu");
        
        if (notCount == 0)
        {
            await connection.ExecuteAsync(@"
                INSERT INTO UrunNotu (Ad, Kisayol) VALUES 
                ('Acılı', 'ACI'),
                ('Acısız', 'ACISIZ'),
                ('Soğansız', 'SGS'),
                ('Az Pişmiş', 'AZ'),
                ('Çok Pişmiş', 'COK'),
                ('Yanında Pilav', 'PIL'),
                ('Yanında Patates', 'PAT'),
                ('Bol Soslu', 'SOS');
            ");

            _logger.LogInformation("Varsayılan ürün notları oluşturuldu");
        }

        // Sistem ayarları var mı kontrol et
        var ayarCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SistemAyar");
        
        if (ayarCount == 0)
        {
            await connection.ExecuteAsync(@"
                INSERT INTO SistemAyar (Anahtar, Deger, Aciklama) VALUES 
                ('RESTORAN_ADI', 'Restoran', 'İşletme adı'),
                ('ADRES', '', 'İşletme adresi'),
                ('TELEFON', '', 'İletişim telefonu'),
                ('VERGI_NO', '', 'Vergi numarası'),
                ('YEDEK_SAAT', '03:00', 'Otomatik yedekleme saati'),
                ('YEDEK_GUN_SAYISI', '30', 'Kaç günlük yedek tutulsun'),
                ('DB_VERSION', '1', 'Veritabanı şema versiyonu');
            ");

            _logger.LogInformation("Varsayılan sistem ayarları oluşturuldu");
        }

        _logger.LogDebug("Seed data kontrolü tamamlandı.");
    }
}
