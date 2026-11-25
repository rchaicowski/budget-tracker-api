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
    private sealed class ThrowingCache : IDistributedCache
    {
        private static InvalidOperationException Down() => new("Redis down");

        public byte[]? Get(string key) => throw Down();
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => throw Down();
        public void Refresh(string key) => throw Down();
        public Task RefreshAsync(string key, CancellationToken token = default) => throw Down();
        public void Remove(string key) => throw Down();
        public Task RemoveAsync(string key, CancellationToken token = default) => throw Down();
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => throw Down();
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => throw Down();
    }

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

    private BudgetsController SetupControllerWithCustomCache(BudgetDbContext context, IDistributedCache cache, int userId)
    {
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

        return controller;
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

        Assert.Equal(2, listRefreshed.Count);
        Assert.Equal(600m, listRefreshed.First(b => b.CategoryId == 1).MonthlyLimit);
    }

    [Fact]
    public async Task CreateTransaction_InvalidatesCachedBudgetStatus()
    {
        var context = GetInMemoryDbContext();
        var userId = 1;
        context.Categories.Add(new Category { Id = 1, Name = "Groceries" });
        context.Accounts.Add(new Account { Id = 1, UserId = userId, Name = "Checking" });
        context.Budgets.Add(new Budget { Id = 1, UserId = userId, CategoryId = 1, AmountLimit = 300m });
        await context.SaveChangesAsync();

        var (budgetsController, cache) = SetupControllerWithCache(context, userId);
        var transactionsController = new TransactionsController(
            context, cache, NullLogger<TransactionsController>.Instance)
        {
            ControllerContext = budgetsController.ControllerContext
        };

        // Populate the cache: nothing spent yet
        var first = await budgetsController.GetBudgetStatus();
        var before = Assert.IsAssignableFrom<IEnumerable<BudgetResponseDto>>(
            Assert.IsType<OkObjectResult>(first.Result).Value).Single();
        Assert.Equal(0m, before.CurrentSpend);

        // A new expense should clear the cached status
        await transactionsController.CreateTransaction(new CreateTransactionDto
        {
            AccountId = 1, CategoryId = 1, Amount = 40m, Description = "Test", Type = "Expense"
        });

        var second = await budgetsController.GetBudgetStatus();
        var after = Assert.IsAssignableFrom<IEnumerable<BudgetResponseDto>>(
            Assert.IsType<OkObjectResult>(second.Result).Value).Single();
        Assert.Equal(40m, after.CurrentSpend);
    }

    [Fact]
    public async Task GetBudgetStatus_WhenCacheIsDown_FallsBackToDatabase()
    {
        var context = GetInMemoryDbContext();
        var userId = 1;

        context.Categories.Add(new Category { Id = 1, Name = "Groceries" });
        context.Accounts.Add(new Account { Id = 1, UserId = userId, Name = "Checking" });
        context.Budgets.Add(new Budget { Id = 1, UserId = userId, CategoryId = 1, AmountLimit = 300m });
        context.Transactions.Add(new Transaction
        {
            Id = 1,
            AccountId = 1,
            CategoryId = 1,
            Amount = 75m,
            Type = "Expense",
            Date = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = SetupControllerWithCustomCache(context, new ThrowingCache(), userId);

        var result = await controller.GetBudgetStatus();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var budgets = Assert.IsAssignableFrom<IEnumerable<BudgetResponseDto>>(okResult.Value);
        var budget = Assert.Single(budgets);
        Assert.Equal(75m, budget.CurrentSpend);
    }

    [Fact]
    public async Task CreateBudget_WhenCacheIsDown_ReturnsCreatedAtActionResult()
    {
        var context = GetInMemoryDbContext();
        var userId = 1;

        context.Categories.Add(new Category { Id = 1, Name = "Groceries" });
        await context.SaveChangesAsync();

        var controller = SetupControllerWithCustomCache(context, new ThrowingCache(), userId);
        var dto = new CreateBudgetDto
        {
            CategoryId = 1,
            MonthlyLimit = 500m
        };

        var result = await controller.CreateBudget(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<BudgetResponseDto>(createdResult.Value);
        Assert.Equal(1, response.CategoryId);
        Assert.Equal(500m, response.MonthlyLimit);
    }
}
