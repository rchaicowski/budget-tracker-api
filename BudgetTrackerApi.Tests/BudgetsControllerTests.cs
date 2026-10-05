using System.Security.Claims;
using BudgetTrackerApi.Controllers;
using BudgetTrackerApi.Data;
using BudgetTrackerApi.DTOs;
using BudgetTrackerApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BudgetTrackerApi.Tests;

public class BudgetsControllerTests
{
    private BudgetDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new BudgetDbContext(options);
    }

    private BudgetsController SetupControllerWithUser(BudgetDbContext context, int userId)
    {
        var controller = new BudgetsController(context);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "TestAuthentication"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return controller;
    }

    [Fact]
    public async Task GetBudgetStatus_CalculatesMonthlySpendAndRemainingCorrectly()
    {
        var context = GetInMemoryDbContext();
        var userId = 1;

        var category = new Category { Id = 1, Name = "Groceries" };
        var account = new Account { Id = 1, UserId = userId, Name = "Checking" };
        context.Categories.Add(category);
        context.Accounts.Add(account);

        var budget = new Budget
        {
            Id = 1,
            UserId = userId,
            CategoryId = 1,
            AmountLimit = 300m
        };
        context.Budgets.Add(budget);

        var now = DateTime.UtcNow;
        context.Transactions.AddRange(
            new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 100m, Type = "Expense", Date = now },
            new Transaction { Id = 2, AccountId = 1, CategoryId = 1, Amount = 50m, Type = "Expense", Date = now },
            // Transaction from previous month should be ignored
            new Transaction { Id = 3, AccountId = 1, CategoryId = 1, Amount = 200m, Type = "Expense", Date = now.AddMonths(-1) }
        );
        await context.SaveChangesAsync();

        var controller = SetupControllerWithUser(context, userId);

        var actionResult = await controller.GetBudgetStatus();
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsAssignableFrom<IEnumerable<BudgetResponseDto>>(okResult.Value).ToList();

        var groceryBudget = Assert.Single(response);
        Assert.Equal(300m, groceryBudget.MonthlyLimit);
        Assert.Equal(150m, groceryBudget.CurrentSpend);
        Assert.Equal(150m, groceryBudget.RemainingAmount);
    }
}
