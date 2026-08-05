namespace BudgetTrackerApi.DTOs;

public class CreateTransactionDto
{
    public int AccountId { get; set; }
    public int CategoryId { get; set; } // Updated
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "Expense";
}

public class TransactionResponseDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int CategoryId { get; set; } // Updated
    public string CategoryName { get; set; } = string.Empty; // Useful for UI display!
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
