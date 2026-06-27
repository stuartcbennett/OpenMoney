using OpenMoney.Core.Models;

namespace OpenMoney.Core.Services;

public class ImportResult
{
    public List<Transaction> Transactions { get; } = new();
    public List<string> Errors { get; } = new();
    public string? AccountName { get; set; }
}

public static class TransactionImporter
{
    public static ImportResult ImportFromText(
        string text,
        IReadOnlyDictionary<string, Investment> investmentsByName,
        IReadOnlyDictionary<string, Account> accountsByName)
    {
        var result = new ImportResult();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        string? currentAccountName = null;
        bool inDataSection = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            // Account name appears after "Investment Transactions" header
            if (result.AccountName is null && !line.StartsWith("Investment Transactions") && !string.IsNullOrWhiteSpace(line) && !line.StartsWith("Date") && !line.StartsWith("==="))
            {
                result.AccountName = line.Trim();
                currentAccountName = result.AccountName;
                continue;
            }

            // Section separator / header row
            if (line.StartsWith("===") || line.TrimStart().StartsWith("Date"))
            {
                inDataSection = line.TrimStart().StartsWith("Date") || inDataSection;
                continue;
            }

            // Skip category headers like "Mutual Funds"
            if (!inDataSection) continue;
            if (!char.IsDigit(line.TrimStart().FirstOrDefault())) continue;

            var tx = ParseLine(line, investmentsByName, accountsByName, currentAccountName, result);
            if (tx != null)
                result.Transactions.Add(tx);
        }

        return result;
    }

    private static Transaction? ParseLine(
        string line,
        IReadOnlyDictionary<string, Investment> investmentsByName,
        IReadOnlyDictionary<string, Account> accountsByName,
        string? accountName,
        ImportResult result)
    {
        // Fixed-width columns based on sample:
        // Date(0-10), Investment(10-42), Activity(42-60), Qty(60-70), Price(70-78), Commission(78-90), Total(90+)
        try
        {
            if (line.Length < 50) return null;

            string datePart       = line[..10].Trim();
            string investmentPart = (line.Length > 42 ? line[10..42] : line[10..]).Trim();
            string activityPart   = (line.Length > 60 ? line[42..60] : line[42..]).Trim();
            string qtyPart        = (line.Length > 70 ? line[60..70] : "").Trim();
            string pricePart      = (line.Length > 82 ? line[70..82] : "").Trim().TrimStart('$');
            string totalPart      = (line.Length > 90 ? line[90..] : "").Trim();

            if (!DateTime.TryParse(datePart, out var date)) return null;
            if (!investmentsByName.TryGetValue(investmentPart, out var investment))
            {
                result.Errors.Add($"Unknown investment '{investmentPart}' on line: {line.Trim()}");
                return null;
            }
            if (accountName is null || !accountsByName.TryGetValue(accountName, out var account))
            {
                result.Errors.Add($"Unknown account '{accountName}'");
                return null;
            }

            ActivityType activity = activityPart switch
            {
                "Buy"                => ActivityType.Buy,
                "Sell"               => ActivityType.Sell,
                "Reinvest Dividend"  => ActivityType.ReinvestDividend,
                "Reinvest Interest"  => ActivityType.ReinvestInterest,
                _                    => ActivityType.Buy
            };

            decimal.TryParse(qtyPart,   System.Globalization.NumberStyles.Any, null, out var qty);
            decimal.TryParse(pricePart, System.Globalization.NumberStyles.Any, null, out var price);
            decimal.TryParse(totalPart, System.Globalization.NumberStyles.Any, null, out var total);

            // Derive missing value: Total = Qty * Price
            if (total == 0 && qty != 0 && price != 0) total = qty * price;
            if (price == 0 && qty != 0 && total != 0) price = total / qty;
            if (qty == 0 && price != 0 && total != 0) qty   = total / price;

            return new Transaction
            {
                Date         = date,
                InvestmentId = investment.Id,
                AccountId    = account.Id,
                Activity     = activity,
                Quantity     = qty,
                Price        = price,
                Total        = total
            };
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Parse error: {ex.Message} — line: {line.Trim()}");
            return null;
        }
    }
}
