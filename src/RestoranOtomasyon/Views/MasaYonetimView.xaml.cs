using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using RestoranOtomasyon.Models;
using RestoranOtomasyon.ViewModels;

namespace RestoranOtomasyon.Views;

public partial class MasaYonetimView : UserControl
{
    private readonly MasaYonetimViewModel _viewModel;

    public MasaYonetimView(MasaYonetimViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        
        Loaded += async (s, e) => await viewModel.LoadMasalarAsync();
        
        // Bölüm ComboBox selection
        BolumComboBox.SelectionChanged += (s, e) =>
        {
            if (BolumComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                _viewModel.EditBolum = Enum.Parse<MasaBolum>(tag);
            }
        };
        
        // Bölüm değişikliğini izle
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.EditBolum) || 
                (e.PropertyName == nameof(viewModel.IsEditing) && viewModel.IsEditing))
            {
                UpdateBolumSelection();
            }
        };
    }

    private void UpdateBolumSelection()
    {
        var bolumStr = _viewModel.EditBolum.ToString();
        foreach (ComboBoxItem item in BolumComboBox.Items)
        {
            if (item.Tag?.ToString() == bolumStr)
            {
                BolumComboBox.SelectedItem = item;
                break;
            }
        }
    }
}

// Converters
public class BolumToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MasaBolum bolum)
        {
            return bolum switch
            {
                MasaBolum.Iceri => "İçeri",
                MasaBolum.Disari => "Dışarı",
                MasaBolum.Teras => "Teras",
                MasaBolum.GelAl => "Gel-Al",
                MasaBolum.Paket => "Paket",
                _ => bolum.ToString()
            };
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class DurumToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MasaDurum durum)
        {
            return durum switch
            {
                MasaDurum.Bos => "Boş",
                MasaDurum.Dolu => "Dolu",
                MasaDurum.Rezerve => "Rezerve",
                _ => durum.ToString()
            };
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class DurumToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MasaDurum durum)
        {
            return durum switch
            {
                MasaDurum.Bos => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a6e3a1")),
                MasaDurum.Dolu => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f38ba8")),
                MasaDurum.Rezerve => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f9e2af")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6c7086"))
            };
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6c7086"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToNewEditMasaConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? "➕ Yeni Masa" : "✏️ Masa Düzenle";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
