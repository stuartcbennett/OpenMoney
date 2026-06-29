using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenMoney.Core.Models;
using OpenMoney.Core.Services;
using OpenMoney.Data.Repositories;

namespace OpenMoney.App.ViewModels;

public class InvestmentRowViewModel
{
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MarketValue { get; set; }
    public string? AnnualReturn { get; set; }
    public string? Roi1Yr { get; set; }
    public string? Roi3Yr { get; set; }
}

public class AccountNodeViewModel
{
    public string Name { get; set; } = string.Empty;
    public decimal MarketValue { get; set; }
    public string? AnnualReturn { get; set; }
    public string? Roi1Yr { get; set; }
    public string? Roi3Yr { get; set; }
    public ObservableCollection<InvestmentRowViewModel> Investments { get; } = new();
}

public partial class PortfolioViewModel : ObservableObject
{
    private readonly AccountRepository _accounts;
    private readonly InvestmentRepository _investments;
    private readonly TransactionRepository _transactions;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private decimal _totalMarketValue;

    public ObservableCollection<AccountNodeViewModel> AccountNodes { get; } = new();

    public PortfolioViewModel(
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
        IsLoading = true;
        AccountNodes.Clear();

        var accounts    = await _accounts.GetAllAsync();
        var investments = await _investments.GetAllAsync();
        var today       = DateTime.Today;

        decimal portfolioTotal = 0;

        foreach (var account in accounts)
        {
            var txList = await _transactions.GetByAccountAsync(account.Id);
            var node   = new AccountNodeViewModel { Name = account.Name };
            decimal accountTotal = 0;

            // Group by investment
            var byInvestment = txList.GroupBy(t => t.InvestmentId);
            foreach (var group in byInvestment)
            {
                var inv = investments.FirstOrDefault(i => i.Id == group.Key);
                if (inv is null) continue;

                decimal qty = group.Sum(t => t.Activity switch
                {
                    ActivityType.Buy              => t.Quantity,
                    ActivityType.ReinvestDividend => t.Quantity,
                    ActivityType.ReinvestInterest => t.Quantity,
                    ActivityType.Sell             => -t.Quantity,
                    _                             => 0
                });

                if (qty <= 0) continue;

                // Price priority: manual PriceHistory entry → latest transaction price → investment initial price
                decimal latestTransactionPrice = group.OrderByDescending(t => t.Date).First().Price;
                decimal latestPrice = await _investments.GetLatestPriceAsync(inv.Id)
                    ?? (latestTransactionPrice > 0 ? latestTransactionPrice : inv.InitialPrice);
                decimal marketValue = qty * latestPrice;
                accountTotal += marketValue;

                var txAll    = group.ToList();
                var returns  = ReturnCalculator.Calculate(txAll, marketValue, today);

                node.Investments.Add(new InvestmentRowViewModel
                {
                    Name         = inv.Name,
                    Quantity     = qty,
                    CurrentPrice = latestPrice,
                    MarketValue  = marketValue,
                    AnnualReturn = FormatPct(returns.YearToDate),
                    Roi1Yr       = FormatPct(returns.OneYear),
                    Roi3Yr       = FormatPct(returns.ThreeYear),
                });
            }

            node.MarketValue = accountTotal;
            portfolioTotal  += accountTotal;
            AccountNodes.Add(node);
        }

        TotalMarketValue = portfolioTotal;
        IsLoading = false;
    }

    private static string? FormatPct(decimal? v) =>
        v.HasValue ? $"{v.Value:P2}" : "—";
}
