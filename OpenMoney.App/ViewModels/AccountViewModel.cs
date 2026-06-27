using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenMoney.Core.Models;
using OpenMoney.Data.Repositories;

namespace OpenMoney.App.ViewModels;

public partial class AccountViewModel : ObservableObject
{
    private readonly AccountRepository _accounts;
    private readonly InvestmentRepository _investments;
    private readonly TransactionRepository _transactions;

    [ObservableProperty] private Account? _selectedAccount;
    [ObservableProperty] private Transaction? _selectedTransaction;
    [ObservableProperty] private bool _isEditing;

    // Edit form fields
    [ObservableProperty] private DateTime _editDate = DateTime.Today;
    [ObservableProperty] private Investment? _editInvestment;
    [ObservableProperty] private ActivityType _editActivity = ActivityType.Buy;
    [ObservableProperty] private decimal _editQuantity;
    [ObservableProperty] private decimal _editPrice;
    [ObservableProperty] private decimal _editTotal;
    [ObservableProperty] private string _editMemo = string.Empty;

    public ObservableCollection<Account>     Accounts     { get; } = new();
    public ObservableCollection<Investment>  Investments  { get; } = new();
    public ObservableCollection<Transaction> Transactions { get; } = new();
    public IEnumerable<ActivityType> ActivityTypes => Enum.GetValues<ActivityType>();

    public AccountViewModel(
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

    partial void OnSelectedAccountChanged(Account? value)
    {
        if (value != null)
            _ = LoadTransactionsAsync(value.Id);
    }

    private async Task LoadTransactionsAsync(int accountId)
    {
        var txs = await _transactions.GetByAccountAsync(accountId);
        Transactions.Clear();
        foreach (var t in txs) Transactions.Add(t);
    }

    [RelayCommand]
    private void StartAdd()
    {
        SelectedTransaction = null;
        ClearEditForm();
        IsEditing = true;
    }

    [RelayCommand]
    private void StartEdit()
    {
        if (SelectedTransaction is null) return;
        var t = SelectedTransaction;
        EditDate       = t.Date;
        EditInvestment = Investments.FirstOrDefault(i => i.Id == t.InvestmentId);
        EditActivity   = t.Activity;
        EditQuantity   = t.Quantity;
        EditPrice      = t.Price;
        EditTotal      = t.Total;
        EditMemo       = t.Memo ?? string.Empty;
        IsEditing      = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedAccount is null || EditInvestment is null) return;

        if (SelectedTransaction is null)
        {
            var tx = new Transaction
            {
                AccountId    = SelectedAccount.Id,
                InvestmentId = EditInvestment.Id,
                Date         = EditDate,
                Activity     = EditActivity,
                Quantity     = EditQuantity,
                Price        = EditPrice,
                Total        = EditTotal,
                Memo         = EditMemo
            };
            await _transactions.AddAsync(tx);
        }
        else
        {
            SelectedTransaction.Date         = EditDate;
            SelectedTransaction.InvestmentId = EditInvestment.Id;
            SelectedTransaction.Activity     = EditActivity;
            SelectedTransaction.Quantity     = EditQuantity;
            SelectedTransaction.Price        = EditPrice;
            SelectedTransaction.Total        = EditTotal;
            SelectedTransaction.Memo         = EditMemo;
            await _transactions.UpdateAsync(SelectedTransaction);
        }

        IsEditing = false;
        await LoadTransactionsAsync(SelectedAccount.Id);
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedTransaction is null || SelectedAccount is null) return;
        await _transactions.DeleteAsync(SelectedTransaction.Id);
        await LoadTransactionsAsync(SelectedAccount.Id);
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private async Task UpdatePriceAsync()
    {
        if (EditInvestment is null || EditPrice <= 0) return;
        await _investments.AddPriceAsync(EditInvestment.Id, DateTime.Today, EditPrice);
    }

    private void ClearEditForm()
    {
        EditDate       = DateTime.Today;
        EditInvestment = null;
        EditActivity   = ActivityType.Buy;
        EditQuantity   = 0;
        EditPrice      = 0;
        EditTotal      = 0;
        EditMemo       = string.Empty;
    }
}
