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
        var accounts = await _context.Accounts
            .Select(a => new AccountResponseDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Name = a.Name,
                Balance = a.Balance,
                Currency = a.Currency
            })
            .ToListAsync();

        return Ok(accounts);
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

        return Ok(new AccountResponseDto
        {
            Id = account.Id,
            UserId = account.UserId,
            Name = account.Name,
            Balance = account.Balance,
            Currency = account.Currency
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
            Currency = dto.Currency
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var response = new AccountResponseDto
        {
            Id = account.Id,
            UserId = account.UserId,
            Name = account.Name,
            Balance = account.Balance,
            Currency = account.Currency
        };

        return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, response);
    }
}
