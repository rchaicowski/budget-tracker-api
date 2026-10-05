namespace BudgetTrackerApi.DTOs;

public class CreateBudgetDto
{
    public int CategoryId { get; set; }
    public decimal MonthlyLimit { get; set; }
}

public class BudgetResponseDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal MonthlyLimit { get; set; }
    public decimal CurrentSpend { get; set; }
    public decimal RemainingAmount => MonthlyLimit - CurrentSpend;
}
