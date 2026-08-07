using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BudgetTrackerApi.Data;
using BudgetTrackerApi.Models;
using BudgetTrackerApi.DTOs;

namespace BudgetTrackerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly BudgetDbContext _context;

    public AccountsController(BudgetDbContext context)
    {
        _context = context;
    }

    // GET: api/Accounts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountResponseDto>>> GetAccounts()
    {
        var accounts = await _context.Accounts.ToListAsync();
        var responseList = new List<AccountResponseDto>();

        foreach (var account in accounts)
        {
            var income = await _context.Transactions
                .Where(t => t.AccountId == account.Id && t.Type == "Income")
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var expense = await _context.Transactions
                .Where(t => t.AccountId == account.Id && t.Type == "Expense")
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            responseList.Add(new AccountResponseDto
            {
                Id = account.Id,
                UserId = account.UserId,
                Name = account.Name,
                OpeningBalance = account.Balance,
                CurrentBalance = account.Balance + income - expense,
                Currency = account.Currency,
                AccountType = account.AccountType
            });
        }

        return Ok(responseList);
    }

    // GET: api/Accounts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<AccountResponseDto>> GetAccount(int id)
    {
        var account = await _context.Accounts.FindAsync(id);

        if (account == null)
        {
            return NotFound(new { message = $"Account with ID {id} not found." });
        }

        var income = await _context.Transactions
            .Where(t => t.AccountId == id && t.Type == "Income")
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        var expense = await _context.Transactions
            .Where(t => t.AccountId == id && t.Type == "Expense")
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        return Ok(new AccountResponseDto
        {
            Id = account.Id,
            UserId = account.UserId,
            Name = account.Name,
            OpeningBalance = account.Balance,
            CurrentBalance = account.Balance + income - expense,
            Currency = account.Currency,
            AccountType = account.AccountType
        });
    }

    // POST: api/Accounts
    [HttpPost]
    public async Task<ActionResult<AccountResponseDto>> CreateAccount(CreateAccountDto dto)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
        if (!userExists)
        {
            return BadRequest(new { message = $"User with ID {dto.UserId} does not exist." });
        }

        var account = new Account
        {
            UserId = dto.UserId,
            Name = dto.Name,
            Balance = dto.Balance,
            Currency = dto.Currency,
            AccountType = dto.AccountType
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var response = new AccountResponseDto
        {
            Id = account.Id,
            UserId = account.UserId,
            Name = account.Name,
            OpeningBalance = account.Balance,
            CurrentBalance = account.Balance, // New account has 0 transactions
            Currency = account.Currency,
            AccountType = account.AccountType
        };

        return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, response);
    }
}
