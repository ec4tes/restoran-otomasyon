using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using RestoranOtomasyon.Models;
using RestoranOtomasyon.ViewModels;

namespace RestoranOtomasyon.Views;

public partial class KullaniciYonetimView : UserControl
{
    private readonly KullaniciYonetimViewModel _viewModel;

    public KullaniciYonetimView(KullaniciYonetimViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        
        Loaded += async (s, e) => await viewModel.LoadKullanicilarAsync();
        
        // Rol ComboBox selection
        RolComboBox.SelectionChanged += (s, e) =>
        {
            if (RolComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                _viewModel.EditRol = Enum.Parse<KullaniciRol>(tag);
            }
        };
        
        // Rol değişikliğini izle
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.EditRol))
            {
                UpdateRolSelection();
            }
            if (e.PropertyName == nameof(viewModel.IsEditing) && viewModel.IsEditing)
            {
                PinBox.Clear();
                UpdateRolSelection();
            }
        };
    }

    private void UpdateRolSelection()
    {
        var rolStr = _viewModel.EditRol.ToString();
        foreach (ComboBoxItem item in RolComboBox.Items)
        {
            if (item.Tag?.ToString() == rolStr)
            {
                RolComboBox.SelectedItem = item;
                break;
            }
        }
    }

    private void PinBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.EditPin = PinBox.Password;
    }
}

// Converters
public class RolToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is KullaniciRol rol)
        {
            return rol switch
            {
                KullaniciRol.Calisan => "Çalışan",
                KullaniciRol.Yonetici => "Yönetici",
                KullaniciRol.Admin => "Admin",
                _ => rol.ToString()
            };
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class RolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is KullaniciRol rol)
        {
            return rol switch
            {
                KullaniciRol.Admin => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f38ba8")),
                KullaniciRol.Yonetici => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fab387")),
                KullaniciRol.Calisan => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#89b4fa")),
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

public class RolToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is KullaniciRol rol)
        {
            return rol switch
            {
                KullaniciRol.Admin => "🔑",
                KullaniciRol.Yonetici => "👔",
                KullaniciRol.Calisan => "👤",
                _ => "👤"
            };
        }
        return "👤";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToNewEditKullaniciConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? "➕ Yeni Kullanıcı" : "✏️ Kullanıcı Düzenle";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
