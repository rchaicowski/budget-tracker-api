namespace BudgetTrackerApi.DTOs;

// Incoming request payload when creating a transaction
public class CreateTransactionDto
{
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

// Outgoing JSON payload returned to the client
public class TransactionResponseDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
