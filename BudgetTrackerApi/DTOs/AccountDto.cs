namespace BudgetTrackerApi.DTOs;

public class CreateAccountDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; } // Opening balance
    public string Currency { get; set; } = "USD";
    public string AccountType { get; set; } = "Checking";
}

public class AccountResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
}
