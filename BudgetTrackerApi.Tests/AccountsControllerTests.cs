using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BudgetTrackerApi.Controllers;
using BudgetTrackerApi.Data;
using BudgetTrackerApi.DTOs;
using BudgetTrackerApi.Models;

namespace BudgetTrackerApi.Tests;

public class AccountsControllerTests
{
    private BudgetDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test instance
            .Options;

        return new BudgetDbContext(options);
    }

    private AccountsController CreateControllerWithUserContext(BudgetDbContext context, int userId)
    {
        var controller = new AccountsController(context);

        // Fake HttpContext with JWT claims for current logged-in user
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        return controller;
    }

    [Fact]
    public async Task GetAccounts_AsUserA_ReturnsOnlyUserAAccounts()
    {
        // ARRANGE
        using var context = CreateInMemoryDbContext();

        var userA = new User { Id = 1, Username = "Alice", Email = "alice@example.com", PasswordHash = "hash" };
        var userB = new User { Id = 2, Username = "Bob", Email = "bob@example.com", PasswordHash = "hash" };

        var accountA = new Account { Id = 10, UserId = 1, Name = "Alice's Checking", Balance = 1000m };
        var accountB = new Account { Id = 20, UserId = 2, Name = "Bob's Checking", Balance = 500m };

        context.Users.AddRange(userA, userB);
        context.Accounts.AddRange(accountA, accountB);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithUserContext(context, userId: 1); // Authenticated as User A (Alice)

        // ACT
        var actionResult = await controller.GetAccounts();

        // ASSERT
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var accounts = Assert.IsAssignableFrom<IEnumerable<AccountResponseDto>>(okResult.Value);

        var accountList = accounts.ToList();
        Assert.Single(accountList); // Only 1 account should be returned
        Assert.Equal(10, accountList[0].Id);
        Assert.Equal("Alice's Checking", accountList[0].Name);
    }

    [Fact]
    public async Task GetAccount_UserAAccessingBobAccount_ReturnsNotFound()
    {
        // ARRANGE
        using var context = CreateInMemoryDbContext();

        var userA = new User { Id = 1, Username = "Alice", Email = "alice@example.com", PasswordHash = "hash" };
        var userB = new User { Id = 2, Username = "Bob", Email = "bob@example.com", PasswordHash = "hash" };

        var accountB = new Account { Id = 20, UserId = 2, Name = "Bob's Secret Account", Balance = 5000m };

        context.Users.AddRange(userA, userB);
        context.Accounts.Add(accountB);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithUserContext(context, userId: 1); // Authenticated as User A (Alice)

        // ACT: Alice tries to view Bob's account (ID 20)
        var actionResult = await controller.GetAccount(20);

        // ASSERT: Must return 404 NotFoundResult to prevent leaking information
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }
}
