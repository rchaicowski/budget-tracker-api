namespace BudgetTrackerApi.Models;

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int CategoryId { get; set; } // Replaces string Category
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "Expense";
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public Account Account { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
