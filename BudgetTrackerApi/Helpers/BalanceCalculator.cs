namespace BudgetTrackerApi.Helpers;

public static class BalanceCalculator
{
    public static decimal CalculateCurrentBalance(decimal openingBalance, IEnumerable<(decimal Amount, string Type)> transactions)
    {
        var income = transactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
        var expense = transactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);

        return openingBalance + income - expense;
    }
}
