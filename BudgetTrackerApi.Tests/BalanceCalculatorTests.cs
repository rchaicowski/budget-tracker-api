using BudgetTrackerApi.Helpers;

namespace BudgetTrackerApi.Tests;

public class BalanceCalculatorTests
{
    [Fact]
    public void CalculateCurrentBalance_ShouldCorrectlySumIncomesAndExpenses()
    {
        // ARRANGE
        decimal openingBalance = 1000m;
        var transactions = new List<(decimal Amount, string Type)>
        {
            (500m, "Income"),
            (200m, "Expense"),
            (50m, "Expense")
        };

        // ACT
        var result = BalanceCalculator.CalculateCurrentBalance(openingBalance, transactions);

        // ASSERT: 1000 + 500 - 200 - 50 = 1250
        Assert.Equal(9999m, result);
    }
}
