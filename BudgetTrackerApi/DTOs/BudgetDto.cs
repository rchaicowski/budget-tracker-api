using System.ComponentModel.DataAnnotations;

namespace BudgetTrackerApi.DTOs;

public class CreateBudgetDto
{
    [Required]
    public int CategoryId { get; set; }

    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Monthly limit must be greater than 0.")]
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
