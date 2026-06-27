using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenMoney.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private int _selectedTabIndex;

    public PortfolioViewModel Portfolio { get; }
    public AccountViewModel Accounts { get; }
    public ReportsViewModel Reports { get; }
    public SettingsViewModel Settings { get; }

    public MainViewModel(
        PortfolioViewModel portfolio,
        AccountViewModel accounts,
        ReportsViewModel reports,
        SettingsViewModel settings)
    {
        Portfolio = portfolio;
        Accounts  = accounts;
        Reports   = reports;
        Settings  = settings;
    }
}
