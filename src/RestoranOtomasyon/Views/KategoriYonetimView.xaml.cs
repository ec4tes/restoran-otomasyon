using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RestoranOtomasyon.ViewModels;

namespace RestoranOtomasyon.Views;

public partial class KategoriYonetimView : UserControl
{
    private readonly KategoriYonetimViewModel _viewModel;

    public KategoriYonetimView(KategoriYonetimViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        
        Loaded += async (s, e) => await viewModel.LoadKategorilerAsync();
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string color)
        {
            _viewModel.EditRenk = color;
        }
    }
}

// Converters
public class BoolToVisibilityInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToAktifConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? "✓ Aktif" : "✗ Pasif";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToNewEditConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? "➕ Yeni Kategori" : "✏️ Kategori Düzenle";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
