namespace BudgetTrackerApi.DTOs;

public class CreateTransactionDto
{
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "Expense"; // "Income" or "Expense"
}

public class TransactionResponseDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
