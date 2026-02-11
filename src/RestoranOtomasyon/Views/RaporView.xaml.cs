using System.Windows.Controls;
using RestoranOtomasyon.ViewModels;

namespace RestoranOtomasyon.Views;

public partial class RaporView : UserControl
{
    public RaporView(RaporViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
