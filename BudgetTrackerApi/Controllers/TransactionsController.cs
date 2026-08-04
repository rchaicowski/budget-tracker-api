using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BudgetTrackerApi.Data;
using BudgetTrackerApi.Models;
using BudgetTrackerApi.DTOs;

namespace BudgetTrackerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly BudgetDbContext _context;

    public TransactionsController(BudgetDbContext context)
    {
        _context = context;
    }

    // GET: api/Transactions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> GetTransactions()
    {
        var transactions = await _context.Transactions
            .Select(t => new TransactionResponseDto
            {
                Id = t.Id,
                AccountId = t.AccountId,
                Amount = t.Amount,
                Category = t.Category,
                Description = t.Description,
                Type = t.Type,
                Date = t.Date
            })
            .ToListAsync();

        return Ok(transactions);
    }

    // GET: api/Transactions/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponseDto>> GetTransaction(int id)
    {
        var transaction = await _context.Transactions.FindAsync(id);

        if (transaction == null)
        {
            return NotFound(new { message = $"Transaction with ID {id} not found." });
        }

        return Ok(new TransactionResponseDto
        {
            Id = transaction.Id,
            AccountId = transaction.AccountId,
            Amount = transaction.Amount,
            Category = transaction.Category,
            Description = transaction.Description,
            Type = transaction.Type,
            Date = transaction.Date
        });
    }

    // POST: api/Transactions
    [HttpPost]
    public async Task<ActionResult<TransactionResponseDto>> CreateTransaction(CreateTransactionDto dto)
    {
        var accountExists = await _context.Accounts.AnyAsync(a => a.Id == dto.AccountId);
        if (!accountExists)
        {
            return BadRequest(new { message = $"Account with ID {dto.AccountId} does not exist." });
        }

        var transaction = new Transaction
        {
            AccountId = dto.AccountId,
            Amount = dto.Amount,
            Category = dto.Category,
            Description = dto.Description,
            Type = dto.Type,
            Date = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        var response = new TransactionResponseDto
        {
            Id = transaction.Id,
            AccountId = transaction.AccountId,
            Amount = transaction.Amount,
            Category = transaction.Category,
            Description = transaction.Description,
            Type = transaction.Type,
            Date = transaction.Date
        };

        return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, response);
    }

    // PUT: api/Transactions/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTransaction(int id, CreateTransactionDto dto)
    {
        var transaction = await _context.Transactions.FindAsync(id);

        if (transaction == null)
        {
            return NotFound(new { message = $"Transaction with ID {id} not found." });
        }

        var accountExists = await _context.Accounts.AnyAsync(a => a.Id == dto.AccountId);
        if (!accountExists)
        {
            return BadRequest(new { message = $"Account with ID {dto.AccountId} does not exist." });
        }

        transaction.AccountId = dto.AccountId;
        transaction.Amount = dto.Amount;
        transaction.Category = dto.Category;
        transaction.Description = dto.Description;
        transaction.Type = dto.Type;

        await _context.SaveChangesAsync();

        return NoContent(); // HTTP 204
    }

    // DELETE: api/Transactions/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        var transaction = await _context.Transactions.FindAsync(id);

        if (transaction == null)
        {
            return NotFound(new { message = $"Transaction with ID {id} not found." });
        }

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();

        return NoContent(); // HTTP 204
    }
}
