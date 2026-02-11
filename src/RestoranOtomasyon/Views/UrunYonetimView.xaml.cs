using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RestoranOtomasyon.ViewModels;

namespace RestoranOtomasyon.Views;

public partial class UrunYonetimView : UserControl
{
    private readonly UrunYonetimViewModel _viewModel;

    public UrunYonetimView(UrunYonetimViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        
        Loaded += async (s, e) => await viewModel.LoadDataAsync();
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.FilterKategori = null;
    }
}

// Converters
public class BoolToFavoriConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? "⭐" : "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToNewEditUrunConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? "➕ Yeni Ürün" : "✏️ Ürün Düzenle";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
