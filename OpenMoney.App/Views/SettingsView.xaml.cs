using OpenMoney.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace OpenMoney.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            await vm.LoadAsync();
    }
}
