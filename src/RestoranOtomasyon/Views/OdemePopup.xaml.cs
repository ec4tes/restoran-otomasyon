using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using RestoranOtomasyon.ViewModels;

namespace RestoranOtomasyon.Views;

/// <summary>
/// OdemePopup.xaml code-behind
/// </summary>
public partial class OdemePopup : UserControl
{
    public OdemePopup()
    {
        InitializeComponent();
    }

    private void KalemBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && 
            element.DataContext is AdisyonKalemViewModel kalem &&
            DataContext is OdemeViewModel vm)
        {
            if (vm.IsAlmanUsuluMode && !kalem.IsOdendi)
            {
                vm.ToggleKalemSecimCommand.Execute(kalem);
            }
        }
    }
}

/// <summary>
/// Inverse boolean converter
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return value;
    }
}

/// <summary>
/// Alman usulü toggle button text converter
/// </summary>
public class BoolToAlmanUsuluTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isAlmanUsulu && isAlmanUsulu)
            return "🔄 Normal Mod";
        return "🍺 Alman Usulü";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Ödeme butonu text converter
/// </summary>
public class BoolToOdemeButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isAlmanUsulu && isAlmanUsulu)
            return "Seçilenleri Öde";
        return "Tamamını Öde";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
