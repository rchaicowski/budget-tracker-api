using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BudgetTrackerApi.Controllers;
using BudgetTrackerApi.Data;
using BudgetTrackerApi.DTOs;
using BudgetTrackerApi.Helpers;
using BudgetTrackerApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
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

    private (BudgetsController Controller, IDistributedCache Cache) SetupControllerWithCache(BudgetDbContext context, int userId)
    {
        IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var logger = NullLogger<BudgetsController>.Instance;

        var controller = new BudgetsController(context, cache, logger);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "TestAuthentication"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return (controller, cache);
    }

    [Theory]
    [InlineData(0, 100)]            // invalid category
    [InlineData(1, 0)]              // zero limit
    [InlineData(1, -50)]            // negative limit
    [InlineData(1, 99999999999)]    // above database column max
    public void CreateBudgetDto_InvalidValues_FailValidation(int categoryId, decimal limit)
    {
        var dto = new CreateBudgetDto { CategoryId = categoryId, MonthlyLimit = limit };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);

        Assert.False(isValid);
    }

    [Fact]
    public void CreateBudgetDto_ValidValues_PassesValidation()
    {
        var dto = new CreateBudgetDto { CategoryId = 1, MonthlyLimit = 300m };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);

        Assert.True(isValid);
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

        var (controller, _) = SetupControllerWithCache(context, userId);

        var actionResult = await controller.GetBudgetStatus();
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsAssignableFrom<IEnumerable<BudgetResponseDto>>(okResult.Value).ToList();

        var groceryBudget = Assert.Single(response);
        Assert.Equal(300m, groceryBudget.MonthlyLimit);
        Assert.Equal(150m, groceryBudget.CurrentSpend);
        Assert.Equal(150m, groceryBudget.RemainingAmount);
    }

    [Fact]
    public async Task GetBudgetStatus_ServesFromCacheAndInvalidatesOnCreateBudget()
    {
        var context = GetInMemoryDbContext();
        var userId = 1;

        var category = new Category { Id = 1, Name = "Utilities" };
        var account = new Account { Id = 1, UserId = userId, Name = "Checking" };
        context.Categories.Add(category);
        context.Accounts.Add(account);

        var budget = new Budget
        {
            Id = 1,
            UserId = userId,
            CategoryId = 1,
            AmountLimit = 500m
        };
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();

        var (controller, cache) = SetupControllerWithCache(context, userId);

        // 1. First call populates cache
        var firstResult = await controller.GetBudgetStatus();
        var ok1 = Assert.IsType<OkObjectResult>(firstResult.Result);
        var list1 = Assert.IsAssignableFrom<IEnumerable<BudgetResponseDto>>(ok1.Value).ToList();
        Assert.Equal(500m, list1[0].MonthlyLimit);

        // Modify database behind the scenes (simulating direct change)
        budget.AmountLimit = 600m;
        await context.SaveChangesAsync();

        // 2. Second call should return cached value (500m) instead of updated DB value (600m)
        var cachedResult = await controller.GetBudgetStatus();
        var okCached = Assert.IsType<OkObjectResult>(cachedResult.Result);
        var listCached = Assert.IsAssignableFrom<IEnumerable<BudgetResponseDto>>(okCached.Value).ToList();
        Assert.Equal(500m, listCached[0].MonthlyLimit);

        // 3. Create a new budget to trigger cache invalidation
        var newCategory = new Category { Id = 2, Name = "Entertainment" };
        context.Categories.Add(newCategory);
        await context.SaveChangesAsync();

        var createDto = new CreateBudgetDto { CategoryId = 2, MonthlyLimit = 200m };
        await controller.CreateBudget(createDto);

        // 4. Subsequent status call should pull fresh data reflecting the invalidation
        var refreshedResult = await controller.GetBudgetStatus();
        var okRefreshed = Assert.IsType<OkObjectResult>(refreshedResult.Result);
        var listRefreshed = Assert.IsAssignableFrom<IEnumerable<BudgetResponseDto>>(okRefreshed.Value).ToList();
        
        // Should now contain both budgets with updated DB values
        Assert.Equal(2, listRefreshed.Count);
        Assert.Equal(600m, listRefreshed.First(b => b.CategoryId == 1).MonthlyLimit);
    }
}
