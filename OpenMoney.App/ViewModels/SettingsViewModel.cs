using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenMoney.Core.Models;
using OpenMoney.Core.Services;
using OpenMoney.Data.Repositories;

namespace OpenMoney.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AccountRepository _accounts;
    private readonly InvestmentRepository _investments;
    private readonly TransactionRepository _transactions;

    // Accounts
    [ObservableProperty] private Account? _selectedAccount;
    [ObservableProperty] private string _accountName = string.Empty;
    [ObservableProperty] private string _accountInstitution = string.Empty;

    // Investments
    [ObservableProperty] private Investment? _selectedInvestment;
    [ObservableProperty] private string _investmentName = string.Empty;
    [ObservableProperty] private string? _investmentTicker;
    [ObservableProperty] private InvestmentType _investmentType;
    [ObservableProperty] private decimal _investmentInitialPrice;

    // Import
    [ObservableProperty] private string _importFilePath = string.Empty;
    [ObservableProperty] private string _importStatus = string.Empty;

    public ObservableCollection<Account>    Accounts    { get; } = new();
    public ObservableCollection<Investment> Investments { get; } = new();
    public IEnumerable<InvestmentType> InvestmentTypes => Enum.GetValues<InvestmentType>();

    public SettingsViewModel(
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
    }

    [RelayCommand]
    private async Task SaveAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(AccountName)) return;
        if (SelectedAccount is null)
        {
            await _accounts.AddAsync(new Account { Name = AccountName, Institution = AccountInstitution });
        }
        else
        {
            SelectedAccount.Name        = AccountName;
            SelectedAccount.Institution = AccountInstitution;
            await _accounts.UpdateAsync(SelectedAccount);
        }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAccountAsync()
    {
        if (SelectedAccount is null) return;
        await _accounts.DeleteAsync(SelectedAccount.Id);
        await LoadAsync();
    }

    partial void OnSelectedAccountChanged(Account? value)
    {
        if (value is null) return;
        AccountName        = value.Name;
        AccountInstitution = value.Institution;
    }

    [RelayCommand]
    private async Task SaveInvestmentAsync()
    {
        if (string.IsNullOrWhiteSpace(InvestmentName)) return;
        if (SelectedInvestment is null)
        {
            await _investments.AddAsync(new Investment
            {
                Name         = InvestmentName,
                Ticker       = InvestmentTicker,
                Type         = InvestmentType,
                InitialPrice = InvestmentInitialPrice
            });
        }
        else
        {
            SelectedInvestment.Name         = InvestmentName;
            SelectedInvestment.Ticker       = InvestmentTicker;
            SelectedInvestment.Type         = InvestmentType;
            SelectedInvestment.InitialPrice = InvestmentInitialPrice;
            await _investments.UpdateAsync(SelectedInvestment);
        }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteInvestmentAsync()
    {
        if (SelectedInvestment is null) return;
        await _investments.DeleteAsync(SelectedInvestment.Id);
        await LoadAsync();
    }

    partial void OnSelectedInvestmentChanged(Investment? value)
    {
        if (value is null) return;
        InvestmentName         = value.Name;
        InvestmentTicker       = value.Ticker;
        InvestmentType         = value.Type;
        InvestmentInitialPrice = value.InitialPrice;
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dialog = new OpenFileDialog
        {
            Title  = "Select Transaction File",
            Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog() == true)
            ImportFilePath = dialog.FileName;
    }

    [RelayCommand]
    private async Task ImportTransactionsAsync()
    {
        if (!File.Exists(ImportFilePath)) { ImportStatus = "File not found."; return; }

        var text     = await File.ReadAllTextAsync(ImportFilePath);
        var invDict  = Investments.ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
        var acctDict = Accounts.ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);

        var firstPass = TransactionImporter.ImportFromText(text, invDict, acctDict);

        // Auto-create any investments that appeared in the file but weren't in the database
        List<string> createdNames = new();
        if (firstPass.NewInvestments.Any())
        {
            foreach (var (name, initialPrice) in firstPass.NewInvestments)
            {
                var newInv = await _investments.AddAsync(new Investment
                {
                    Name         = name,
                    InitialPrice = initialPrice,
                    Type         = InvestmentType.MutualFund,
                });
                invDict[name] = newInv;
                createdNames.Add(name);
            }
        }

        // Re-run with the now-complete investment dictionary if anything was created
        var importResult = createdNames.Count > 0
            ? TransactionImporter.ImportFromText(text, invDict, acctDict)
            : firstPass;

        var statusParts = new List<string>();

        if (importResult.Transactions.Any())
        {
            await _transactions.AddRangeAsync(importResult.Transactions);
            statusParts.Add($"Imported {importResult.Transactions.Count} transaction(s).");
        }
        else
        {
            statusParts.Add("No transactions imported.");
        }

        if (createdNames.Count > 0)
            statusParts.Add($"Created {createdNames.Count} new investment(s): {string.Join(", ", createdNames)}.");

        if (importResult.Errors.Any())
            statusParts.Add($"Errors: {string.Join("; ", importResult.Errors)}");

        ImportStatus = string.Join(" ", statusParts);

        if (createdNames.Count > 0)
            await LoadAsync();
    }
}
