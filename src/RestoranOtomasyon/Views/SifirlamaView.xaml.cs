using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using RestoranOtomasyon.ViewModels;

namespace RestoranOtomasyon.Views;

public partial class SifirlamaView : UserControl
{
    public SifirlamaView(SifirlamaViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        
        // İstatistikleri yükle
        Loaded += async (s, e) => await viewModel.LoadIstatistiklerAsync();
    }
}

public class BoolToSuccessColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isSuccess)
        {
            return isSuccess 
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a6e3a1")) // Yeşil
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f38ba8")); // Kırmızı
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cdd6f4"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
