using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using RestoranOtomasyon.ViewModels;

namespace RestoranOtomasyon.Views;

public partial class FiyatGuncellemeView : UserControl
{
    public FiyatGuncellemeView(FiyatGuncellemeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (s, e) => await viewModel.LoadDataAsync();
    }
}

public class BoolToZamColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isZam)
        {
            // Zam ise yeşil, indirim ise kırmızı
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(isZam ? "#a6e3a1" : "#f38ba8"));
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cdd6f4"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
