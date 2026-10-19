using System.ComponentModel.DataAnnotations;

namespace BudgetTrackerApi.DTOs;

public class CreateBudgetDto
{
    [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be a valid category.")]
    public int CategoryId { get; set; }

    [Range(0.01, 9999999999.99, ErrorMessage = "Monthly limit must be between 0.01 and 9,999,999,999.99.")]
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
