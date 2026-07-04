# OpenMoney

A personal finance desktop application for tracking investment accounts 

## Purpose

OpenMoney lets you manage multiple investment accounts, track transactions, monitor portfolio performance, and view return-on-investment calculations across different time horizons (YTD, 1-year, 3-year, all-time). Returns are calculated using the Modified Dietz method, which accounts for cash flows throughout the period.

## Design Overview

The solution is split into three projects:

| Project | Role |
|---|---|
| `OpenMoney.Core` | Domain models (Account, Investment, Transaction, PriceHistory) and services (ReturnCalculator, TransactionImporter) |
| `OpenMoney.Data` | EF Core 6 data layer backed by MySQL; one repository per entity |
| `OpenMoney.App` | WPF desktop UI with four tabs: Portfolio, Accounts, Reports, Settings |

**Key libraries:** MaterialDesignThemes (dark UI), OxyPlot (charts), CommunityToolkit.Mvvm (MVVM source generators), Microsoft.Extensions.DependencyInjection.

## Prerequisites

- .NET 10 SDK
- MySQL server running locally (or update the connection string to point elsewhere)

## Running the App

1. **Configure the database** — edit `OpenMoney.App/appsettings.json` and set your MySQL connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=openmoney;User=root;Password=;"
   }
   ```

2. **Build and run** from the solution root:
   ```
   dotnet run --project OpenMoney.App
   ```
   Or open `OpenMoney.sln` in Visual Studio and press **F5**.

   The database schema is created automatically on first launch via EF Core migrations — no manual SQL setup required.

## Importing Transactions

Transactions can be bulk-imported from the fixed-width text format used by legacy tools. See `InvestmentTransactionsSample.txt` for the expected format, then use the import option in the Settings tab.
