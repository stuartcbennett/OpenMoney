using OpenMoney.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace OpenMoney.App.Views;

public partial class PortfolioView : UserControl
{
    public PortfolioView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PortfolioViewModel vm)
            await vm.LoadAsync();
    }
}
