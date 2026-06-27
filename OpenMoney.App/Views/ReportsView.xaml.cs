using OpenMoney.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace OpenMoney.App.Views;

public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ReportsViewModel vm)
            await vm.LoadAsync();
    }
}
