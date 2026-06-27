using Microsoft.Extensions.DependencyInjection;
using OpenMoney.App.ViewModels;
using System.Windows;

namespace OpenMoney.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = App.Services.GetRequiredService<MainViewModel>();
        DataContext = vm;
    }
}
