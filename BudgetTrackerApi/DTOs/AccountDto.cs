namespace BudgetTrackerApi.DTOs;

public class CreateAccountDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
}

public class AccountResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
}
