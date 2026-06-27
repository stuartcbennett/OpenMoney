using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenMoney.Core.Models;
using OpenMoney.Data.Repositories;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace OpenMoney.App.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly AccountRepository _accounts;
    private readonly InvestmentRepository _investments;
    private readonly TransactionRepository _transactions;

    [ObservableProperty] private Account? _selectedAccount;
    [ObservableProperty] private Investment? _selectedInvestment;
    [ObservableProperty] private PlotModel _netWorthModel  = new();
    [ObservableProperty] private PlotModel _accountModel   = new();
    [ObservableProperty] private PlotModel _priceModel     = new();

    public ObservableCollection<Account>    Accounts    { get; } = new();
    public ObservableCollection<Investment> Investments { get; } = new();

    public ReportsViewModel(
        AccountRepository accounts,
        InvestmentRepository investments,
        TransactionRepository transactions)
    {
        _accounts     = accounts;
        _investments  = investments;
        _transactions = transactions;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var accts = await _accounts.GetAllAsync();
        Accounts.Clear();
        foreach (var a in accts) Accounts.Add(a);

        var invs = await _investments.GetAllAsync();
        Investments.Clear();
        foreach (var i in invs) Investments.Add(i);

        await BuildNetWorthChartAsync();
    }

    private async Task BuildNetWorthChartAsync()
    {
        var allTx = await _transactions.GetAllAsync();
        NetWorthModel = BuildCumulativeModel("Net Worth Over Time", allTx, OxyColors.Teal);
    }

    [RelayCommand]
    private async Task LoadAccountChartAsync()
    {
        if (SelectedAccount is null) return;
        var txs = await _transactions.GetByAccountAsync(SelectedAccount.Id);
        AccountModel = BuildCumulativeModel(SelectedAccount.Name, txs, OxyColors.LimeGreen);
    }

    [RelayCommand]
    private async Task LoadPriceChartAsync()
    {
        if (SelectedInvestment is null) return;
        var history = await _investments.GetPriceHistoryAsync(SelectedInvestment.Id);

        var model  = CreateDarkModel(SelectedInvestment.Name);
        var series = new LineSeries { Title = SelectedInvestment.Name, Color = OxyColors.Orange };
        foreach (var p in history)
            series.Points.Add(DateTimeAxis.CreateDataPoint(p.Date, (double)p.Price));

        model.Series.Add(series);
        model.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, StringFormat = "MMM yy" });
        model.Axes.Add(new LinearAxis   { Position = AxisPosition.Left,   StringFormat = "C" });
        PriceModel = model;
    }

    private static PlotModel BuildCumulativeModel(string title, IEnumerable<Transaction> transactions, OxyColor color)
    {
        var model  = CreateDarkModel(title);
        var series = new LineSeries { Title = title, Color = color };

        decimal running = 0;
        foreach (var g in transactions.OrderBy(t => t.Date).GroupBy(t => t.Date.Date))
        {
            running += g.Sum(t => t.Activity switch
            {
                ActivityType.Buy              => t.Total,
                ActivityType.ReinvestDividend => t.Total,
                ActivityType.ReinvestInterest => t.Total,
                ActivityType.Sell             => -t.Total,
                _                             => 0
            });
            series.Points.Add(DateTimeAxis.CreateDataPoint(g.Key, (double)running));
        }

        model.Series.Add(series);
        model.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, StringFormat = "MMM yy" });
        model.Axes.Add(new LinearAxis   { Position = AxisPosition.Left });
        return model;
    }

    private static PlotModel CreateDarkModel(string title)
    {
        return new PlotModel
        {
            Title            = title,
            Background       = OxyColor.FromRgb(48, 48, 48),
            TextColor        = OxyColors.White,
            PlotAreaBorderColor = OxyColor.FromRgb(80, 80, 80),
        };
    }
}
