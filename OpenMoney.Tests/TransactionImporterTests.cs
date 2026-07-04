using OpenMoney.Core.Models;
using OpenMoney.Core.Services;

namespace OpenMoney.Tests;

public class TransactionImporterTests
{
    [Fact]
    public void ImportFromText_ParsesQuotedQuantityWithThousandsSeparator()
    {
        const string text = """
Investment Transactions
Test Account
Date,Investment,Activity,Quantity,Price,Commission,Total
2022-07-27,TEST FUND ONE,Buy,"8,124.30055",$9.42,,76551.5312
2024-07-02,TEST FUND ONE,Reinvest Dividend,0.75319,$11.03,,8.3100
""";

        var investmentsByName = new Dictionary<string, Investment>
        {
            ["TEST FUND ONE"] = new Investment { Id = 7, Name = "TEST FUND ONE" }
        };

        var accountsByName = new Dictionary<string, Account>
        {
            ["Test Account"] = new Account { Id = 3, Name = "Test Account" }
        };

        var result = TransactionImporter.ImportFromText(text, investmentsByName, accountsByName);

        Assert.Equal(2, result.Transactions.Count);
        Assert.Equal(8124.30055m, result.Transactions[0].Quantity);
        Assert.Equal(0.75319m, result.Transactions[1].Quantity);
        Assert.Empty(result.Errors);
    }
}
