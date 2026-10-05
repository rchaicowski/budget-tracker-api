using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BudgetTrackerApi.Data;
using BudgetTrackerApi.Models;
using BudgetTrackerApi.DTOs;

namespace BudgetTrackerApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly BudgetDbContext _context;

    public AccountsController(BudgetDbContext context)
    {
        _context = context;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // GET: api/Accounts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountResponseDto>>> GetAccounts()
    {
        var currentUserId = GetUserId();

        // Single SQL query calculating income and expenses per account directly in DB
        var accounts = await _context.Accounts
            .Where(a => a.UserId == currentUserId)
            .Select(a => new AccountResponseDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Name = a.Name,
                OpeningBalance = a.Balance,
                CurrentBalance = a.Balance 
                    + (a.Transactions.Where(t => t.Type == "Income").Sum(t => (decimal?)t.Amount) ?? 0m)
                    - (a.Transactions.Where(t => t.Type == "Expense").Sum(t => (decimal?)t.Amount) ?? 0m),
                Currency = a.Currency,
                AccountType = a.AccountType
            })
            .ToListAsync();

        return Ok(accounts);
    }

    // GET: api/Accounts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<AccountResponseDto>> GetAccount(int id)
    {
        var currentUserId = GetUserId();

        var account = await _context.Accounts
            .Where(a => a.Id == id && a.UserId == currentUserId)
            .Select(a => new AccountResponseDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Name = a.Name,
                OpeningBalance = a.Balance,
                CurrentBalance = a.Balance 
                    + (a.Transactions.Where(t => t.Type == "Income").Sum(t => (decimal?)t.Amount) ?? 0m)
                    - (a.Transactions.Where(t => t.Type == "Expense").Sum(t => (decimal?)t.Amount) ?? 0m),
                Currency = a.Currency,
                AccountType = a.AccountType
            })
            .FirstOrDefaultAsync();

        if (account == null)
        {
            return NotFound(new { message = $"Account with ID {id} not found." });
        }

        return Ok(account);
    }

    // POST: api/Accounts
    [HttpPost]
    public async Task<ActionResult<AccountResponseDto>> CreateAccount(CreateAccountDto dto)
    {
        var currentUserId = GetUserId();

        var account = new Account
        {
            UserId = currentUserId,
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
            CurrentBalance = account.Balance,
            Currency = account.Currency,
            AccountType = account.AccountType
        };

        return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, response);
    }
}
