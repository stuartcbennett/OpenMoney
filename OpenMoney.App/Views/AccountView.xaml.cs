using OpenMoney.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace OpenMoney.App.Views;

public partial class AccountView : UserControl
{
    public AccountView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AccountViewModel vm)
            await vm.LoadAsync();
    }
}
