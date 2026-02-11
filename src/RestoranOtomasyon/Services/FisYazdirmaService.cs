using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using RestoranOtomasyon.Models;

namespace RestoranOtomasyon.Services;

/// <summary>
/// Termal yazıcı için fiş yazdırma servisi (80mm) - WPF Native
/// </summary>
public interface IFisYazdirmaService
{
    void YazdirAdisyon(Adisyon adisyon, string? odemeTipi = null);
    void YazdirOnizleme(Adisyon adisyon, string? odemeTipi = null);
}

public class FisYazdirmaService : IFisYazdirmaService
{
    private readonly ILogger<FisYazdirmaService> _logger;
    private const string RESTORAN_ADI = "CEMİLBEY Yemek Atölyesi";
    private const double FIS_GENISLIK = 280; // 80mm için yaklaşık piksel

    public FisYazdirmaService(ILogger<FisYazdirmaService> logger)
    {
        _logger = logger;
    }

    public void YazdirAdisyon(Adisyon adisyon, string? odemeTipi = null)
    {
        try
        {
            var doc = CreateFisDocument(adisyon, odemeTipi);
            
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, $"Fiş #{adisyon.Id}");
                _logger.LogInformation("Fiş yazdırıldı: Adisyon #{AdisyonId}", adisyon.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fiş yazdırma hatası");
            MessageBox.Show("Fiş yazdırılamadı: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void YazdirOnizleme(Adisyon adisyon, string? odemeTipi = null)
    {
        try
        {
            var doc = CreateFisDocument(adisyon, odemeTipi);
            
            var window = new Window
            {
                Title = $"Fiş Önizleme - #{adisyon.Id}",
                Width = 350,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush(Colors.White)
            };

            var viewer = new FlowDocumentScrollViewer
            {
                Document = doc,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            window.Content = viewer;
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fiş önizleme hatası");
            MessageBox.Show("Önizleme açılamadı: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private FlowDocument CreateFisDocument(Adisyon adisyon, string? odemeTipi)
    {
        var doc = new FlowDocument
        {
            PageWidth = FIS_GENISLIK,
            PagePadding = new Thickness(10),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11
        };

        // Başlık
        var baslik = new Paragraph(new Run(RESTORAN_ADI))
        {
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };
        doc.Blocks.Add(baslik);

        // Çizgi
        doc.Blocks.Add(CreateCizgi());

        // Fiş No ve Tarih
        var bilgi = new Paragraph
        {
            Margin = new Thickness(0, 5, 0, 5)
        };
        bilgi.Inlines.Add(new Run($"Fiş No: {adisyon.Id}"));
        bilgi.Inlines.Add(new LineBreak());
        bilgi.Inlines.Add(new Run($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}"));
        
        // Masa/Tip
        string tipBilgi = adisyon.Tip switch
        {
            AdisyonTip.GelAl => "GEL-AL",
            AdisyonTip.Paket => "PAKET",
            _ => $"Masa: {adisyon.MasaId}"
        };
        bilgi.Inlines.Add(new LineBreak());
        bilgi.Inlines.Add(new Run(tipBilgi) { FontWeight = FontWeights.Bold });
        doc.Blocks.Add(bilgi);

        // Çizgi
        doc.Blocks.Add(CreateCizgi());

        // Ürün Başlığı
        var urunBaslik = new Paragraph
        {
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 5, 0, 5)
        };
        urunBaslik.Inlines.Add(new Run("Ürün".PadRight(20)));
        urunBaslik.Inlines.Add(new Run("Ad".PadLeft(3)));
        urunBaslik.Inlines.Add(new Run("Tutar".PadLeft(10)));
        doc.Blocks.Add(urunBaslik);

        // Çizgi
        doc.Blocks.Add(CreateCizgi());

        // Ürünler
        foreach (var kalem in adisyon.Kalemler)
        {
            var urunAd = kalem.UrunAd ?? "Ürün";
            if (urunAd.Length > 18) urunAd = urunAd.Substring(0, 18) + "..";

            var urunSatir = new Paragraph
            {
                Margin = new Thickness(0, 2, 0, 2)
            };
            urunSatir.Inlines.Add(new Run(urunAd.PadRight(20)));
            urunSatir.Inlines.Add(new Run(kalem.Adet.ToString().PadLeft(3)));
            urunSatir.Inlines.Add(new Run($"₺{kalem.ToplamFiyat:N2}".PadLeft(10)));
            doc.Blocks.Add(urunSatir);

            // Not varsa
            if (!string.IsNullOrEmpty(kalem.Notlar))
            {
                var notSatir = new Paragraph(new Run($"  Not: {kalem.Notlar}"))
                {
                    FontSize = 9,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0)
                };
                doc.Blocks.Add(notSatir);
            }
        }

        // Çizgi
        doc.Blocks.Add(CreateCizgi());

        // Toplam
        var toplamSatir = new Paragraph
        {
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 5, 0, 5)
        };
        toplamSatir.Inlines.Add(new Run("TOPLAM:".PadRight(23)));
        toplamSatir.Inlines.Add(new Run($"₺{adisyon.ToplamTutar:N2}".PadLeft(10)));
        doc.Blocks.Add(toplamSatir);

        // Ödeme tipi
        if (!string.IsNullOrEmpty(odemeTipi))
        {
            var odemeSatir = new Paragraph(new Run($"Ödeme: {odemeTipi}"))
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 5, 0, 5)
            };
            doc.Blocks.Add(odemeSatir);
        }

        // Çizgi
        doc.Blocks.Add(CreateCizgi());

        // Alt bilgi
        var altBilgi = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 10, 0, 0)
        };
        altBilgi.Inlines.Add(new Run("Afiyet Olsun!") { FontWeight = FontWeights.Bold });
        altBilgi.Inlines.Add(new LineBreak());
        altBilgi.Inlines.Add(new Run("Teşekkür Ederiz"));
        doc.Blocks.Add(altBilgi);

        return doc;
    }

    private Paragraph CreateCizgi()
    {
        return new Paragraph(new Run(new string('-', 35)))
        {
            Margin = new Thickness(0, 2, 0, 2)
        };
    }
}
