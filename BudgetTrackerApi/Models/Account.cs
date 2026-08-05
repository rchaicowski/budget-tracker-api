namespace BudgetTrackerApi.Models;

public class Account
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; } // Opening balance
    public string Currency { get; set; } = "USD";
    public string AccountType { get; set; } = "Checking"; // Checking, Savings, Credit Card

    public User User { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
