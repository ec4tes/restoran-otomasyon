using Dapper;
using Microsoft.Extensions.Logging;
using RestoranOtomasyon.Data;
using RestoranOtomasyon.Models;

namespace RestoranOtomasyon.Services;

#region Rapor Modelleri
public class GunlukSatisRapor
{
    public DateTime Tarih { get; set; }
    public int ToplamAdisyon { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal ToplamIndirim { get; set; }
    public decimal NetTutar => ToplamTutar - ToplamIndirim;
    public int NakitAdisyon { get; set; }
    public decimal NakitTutar { get; set; }
    public int KartAdisyon { get; set; }
    public decimal KartTutar { get; set; }
    public int KarisikAdisyon { get; set; }
    public decimal KarisikNakitTutar { get; set; }
    public decimal KarisikKartTutar { get; set; }
}

public class UrunSatisRapor
{
    public int UrunId { get; set; }
    public string UrunAd { get; set; } = "";
    public string KategoriAd { get; set; } = "";
    public int ToplamAdet { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal ToplamTutar { get; set; }
    public int IkramAdet { get; set; }
}

public class OdemeTipiRapor
{
    public string OdemeTipi { get; set; } = "";
    public int AdisyonSayisi { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal Yuzde { get; set; }
}

public class AylikRapor
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public string AyAdi { get; set; } = "";
    public int ToplamAdisyon { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal ToplamIndirim { get; set; }
    public decimal NetTutar => ToplamTutar - ToplamIndirim;
    public List<GunlukSatisOzet> GunlukOzetler { get; set; } = new();
    public List<UrunSatisRapor> UrunSatislari { get; set; } = new();
    public List<OdemeTipiRapor> OdemeTipleri { get; set; } = new();
}

public class GunlukSatisOzet
{
    public DateTime Tarih { get; set; }
    public int AdisyonSayisi { get; set; }
    public decimal ToplamTutar { get; set; }
}
#endregion

public interface IRaporService
{
    Task<GunlukSatisRapor> GetGunlukSatisAsync(DateTime tarih);
    Task<IEnumerable<UrunSatisRapor>> GetUrunSatislariAsync(DateTime baslangic, DateTime bitis);
    Task<IEnumerable<OdemeTipiRapor>> GetOdemeTipiRaporuAsync(DateTime baslangic, DateTime bitis);
    Task<AylikRapor> GetAylikRaporAsync(int yil, int ay);
}

public class RaporService : IRaporService
{
    private readonly IDatabaseConnection _dbConnection;
    private readonly ILogger<RaporService> _logger;
    
    private static readonly string[] AyAdlari = {
        "", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
        "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
    };

    public RaporService(IDatabaseConnection dbConnection, ILogger<RaporService> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public async Task<GunlukSatisRapor> GetGunlukSatisAsync(DateTime tarih)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        var baslangic = tarih.Date;
        var bitis = baslangic.AddDays(1);
        
        var rapor = new GunlukSatisRapor { Tarih = tarih.Date };
        
        // Tamamlanan adisyonlar
        var adisyonlar = await connection.QueryAsync<Adisyon>(@"
            SELECT * FROM Adisyon 
            WHERE Durum = 'Odendi' 
            AND OlusturmaTarihi >= @Baslangic AND OlusturmaTarihi < @Bitis",
            new { Baslangic = baslangic, Bitis = bitis });

        var adisyonList = adisyonlar.ToList();
        
        rapor.ToplamAdisyon = adisyonList.Count;
        rapor.ToplamTutar = adisyonList.Sum(a => a.ToplamTutar);
        rapor.ToplamIndirim = adisyonList.Sum(a => a.IndirimTutar);
        
        // Nakit
        var nakitler = adisyonList.Where(a => a.OdemeTipiValue == OdemeTipi.Nakit).ToList();
        rapor.NakitAdisyon = nakitler.Count;
        rapor.NakitTutar = nakitler.Sum(a => a.ToplamTutar - a.IndirimTutar);
        
        // Kart
        var kartlar = adisyonList.Where(a => a.OdemeTipiValue == OdemeTipi.Kart).ToList();
        rapor.KartAdisyon = kartlar.Count;
        rapor.KartTutar = kartlar.Sum(a => a.ToplamTutar - a.IndirimTutar);
        
        // Karışık
        var karisiklar = adisyonList.Where(a => a.OdemeTipiValue == OdemeTipi.Karisik).ToList();
        rapor.KarisikAdisyon = karisiklar.Count;
        rapor.KarisikNakitTutar = karisiklar.Sum(a => a.NakitTutar);
        rapor.KarisikKartTutar = karisiklar.Sum(a => a.KartTutar);

        _logger.LogInformation("Günlük satış raporu oluşturuldu: {Tarih}, {Adet} adisyon, ₺{Tutar}", 
            tarih.ToShortDateString(), rapor.ToplamAdisyon, rapor.NetTutar);

        return rapor;
    }

    public async Task<IEnumerable<UrunSatisRapor>> GetUrunSatislariAsync(DateTime baslangic, DateTime bitis)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        var sonuc = await connection.QueryAsync<UrunSatisRapor>(@"
            SELECT 
                ak.UrunId,
                u.Ad AS UrunAd,
                k.Ad AS KategoriAd,
                u.Fiyat AS BirimFiyat,
                SUM(CASE WHEN ak.Durum != 'Ikram' THEN ak.Adet ELSE 0 END) AS ToplamAdet,
                SUM(CASE WHEN ak.Durum != 'Ikram' THEN ak.Adet * ak.BirimFiyat ELSE 0 END) AS ToplamTutar,
                SUM(CASE WHEN ak.Durum = 'Ikram' THEN ak.Adet ELSE 0 END) AS IkramAdet
            FROM AdisyonKalem ak
            INNER JOIN Adisyon a ON ak.AdisyonId = a.Id
            INNER JOIN Urun u ON ak.UrunId = u.Id
            LEFT JOIN Kategori k ON u.KategoriId = k.Id
            WHERE a.Durum = 'Odendi'
            AND a.OlusturmaTarihi >= @Baslangic AND a.OlusturmaTarihi < @Bitis
            GROUP BY ak.UrunId, u.Ad, k.Ad, u.Fiyat
            ORDER BY ToplamTutar DESC",
            new { Baslangic = baslangic.Date, Bitis = bitis.Date.AddDays(1) });

        _logger.LogInformation("Ürün satış raporu oluşturuldu: {Baslangic} - {Bitis}", 
            baslangic.ToShortDateString(), bitis.ToShortDateString());

        return sonuc;
    }

    public async Task<IEnumerable<OdemeTipiRapor>> GetOdemeTipiRaporuAsync(DateTime baslangic, DateTime bitis)
    {
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        var adisyonlar = await connection.QueryAsync<Adisyon>(@"
            SELECT * FROM Adisyon 
            WHERE Durum = 'Odendi' 
            AND OlusturmaTarihi >= @Baslangic AND OlusturmaTarihi < @Bitis",
            new { Baslangic = baslangic.Date, Bitis = bitis.Date.AddDays(1) });

        var liste = adisyonlar.ToList();
        var toplamGenelTutar = liste.Sum(a => a.ToplamTutar - a.IndirimTutar);
        
        var raporlar = new List<OdemeTipiRapor>();
        
        // Nakit
        var nakitler = liste.Where(a => a.OdemeTipiValue == OdemeTipi.Nakit).ToList();
        var nakitTutar = nakitler.Sum(a => a.ToplamTutar - a.IndirimTutar);
        raporlar.Add(new OdemeTipiRapor
        {
            OdemeTipi = "💵 Nakit",
            AdisyonSayisi = nakitler.Count,
            ToplamTutar = nakitTutar,
            Yuzde = toplamGenelTutar > 0 ? (nakitTutar / toplamGenelTutar) * 100 : 0
        });
        
        // Kart
        var kartlar = liste.Where(a => a.OdemeTipiValue == OdemeTipi.Kart).ToList();
        var kartTutar = kartlar.Sum(a => a.ToplamTutar - a.IndirimTutar);
        raporlar.Add(new OdemeTipiRapor
        {
            OdemeTipi = "💳 Kredi/Banka Kartı",
            AdisyonSayisi = kartlar.Count,
            ToplamTutar = kartTutar,
            Yuzde = toplamGenelTutar > 0 ? (kartTutar / toplamGenelTutar) * 100 : 0
        });
        
        // Karışık (Toplam nakit + kart olarak)
        var karisiklar = liste.Where(a => a.OdemeTipiValue == OdemeTipi.Karisik).ToList();
        var karisikNakit = karisiklar.Sum(a => a.NakitTutar);
        var karisikKart = karisiklar.Sum(a => a.KartTutar);
        
        if (karisiklar.Any())
        {
            raporlar.Add(new OdemeTipiRapor
            {
                OdemeTipi = "🔄 Karışık (Nakit Kısmı)",
                AdisyonSayisi = karisiklar.Count,
                ToplamTutar = karisikNakit,
                Yuzde = toplamGenelTutar > 0 ? (karisikNakit / toplamGenelTutar) * 100 : 0
            });
            raporlar.Add(new OdemeTipiRapor
            {
                OdemeTipi = "🔄 Karışık (Kart Kısmı)",
                AdisyonSayisi = karisiklar.Count,
                ToplamTutar = karisikKart,
                Yuzde = toplamGenelTutar > 0 ? (karisikKart / toplamGenelTutar) * 100 : 0
            });
        }

        _logger.LogInformation("Ödeme tipi raporu oluşturuldu: {Baslangic} - {Bitis}", 
            baslangic.ToShortDateString(), bitis.ToShortDateString());

        return raporlar;
    }

    public async Task<AylikRapor> GetAylikRaporAsync(int yil, int ay)
    {
        var baslangic = new DateTime(yil, ay, 1);
        var bitis = baslangic.AddMonths(1);
        
        using var connection = await _dbConnection.CreateConnectionAsync();
        
        var rapor = new AylikRapor
        {
            Yil = yil,
            Ay = ay,
            AyAdi = AyAdlari[ay]
        };
        
        // Tüm adisyonlar
        var adisyonlar = await connection.QueryAsync<Adisyon>(@"
            SELECT * FROM Adisyon 
            WHERE Durum = 'Odendi' 
            AND OlusturmaTarihi >= @Baslangic AND OlusturmaTarihi < @Bitis",
            new { Baslangic = baslangic, Bitis = bitis });

        var liste = adisyonlar.ToList();
        
        rapor.ToplamAdisyon = liste.Count;
        rapor.ToplamTutar = liste.Sum(a => a.ToplamTutar);
        rapor.ToplamIndirim = liste.Sum(a => a.IndirimTutar);
        
        // Günlük özet
        rapor.GunlukOzetler = liste
            .GroupBy(a => a.OlusturmaTarihi.Date)
            .Select(g => new GunlukSatisOzet
            {
                Tarih = g.Key,
                AdisyonSayisi = g.Count(),
                ToplamTutar = g.Sum(a => a.ToplamTutar - a.IndirimTutar)
            })
            .OrderBy(o => o.Tarih)
            .ToList();
        
        // Ürün satışları
        rapor.UrunSatislari = (await GetUrunSatislariAsync(baslangic, bitis.AddDays(-1))).ToList();
        
        // Ödeme tipleri
        rapor.OdemeTipleri = (await GetOdemeTipiRaporuAsync(baslangic, bitis.AddDays(-1))).ToList();

        _logger.LogInformation("Aylık rapor oluşturuldu: {Ay} {Yil}", rapor.AyAdi, yil);

        return rapor;
    }
}
