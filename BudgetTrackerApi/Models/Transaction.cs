namespace BudgetTrackerApi.Models;

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "Expense"; // "Income" or "Expense"
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public Account Account { get; set; } = null!;
}
