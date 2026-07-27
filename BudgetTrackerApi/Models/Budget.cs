namespace BudgetTrackerApi.Models;

public class Budget
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal AmountLimit { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
