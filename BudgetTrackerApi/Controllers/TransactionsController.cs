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
                CategoryId = t.CategoryId,
                CategoryName = t.Category.Name,
                Amount = t.Amount,
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
        var transaction = await _context.Transactions
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null)
        {
            return NotFound(new { message = $"Transaction with ID {id} not found." });
        }

        return Ok(new TransactionResponseDto
        {
            Id = transaction.Id,
            AccountId = transaction.AccountId,
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category.Name,
            Amount = transaction.Amount,
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

        var category = await _context.Categories.FindAsync(dto.CategoryId);
        if (category == null)
        {
            return BadRequest(new { message = $"Category with ID {dto.CategoryId} does not exist." });
        }

        var transaction = new Transaction
        {
            AccountId = dto.AccountId,
            CategoryId = dto.CategoryId,
            Amount = dto.Amount,
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
            CategoryId = transaction.CategoryId,
            CategoryName = category.Name,
            Amount = transaction.Amount,
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

        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists)
        {
            return BadRequest(new { message = $"Category with ID {dto.CategoryId} does not exist." });
        }

        transaction.AccountId = dto.AccountId;
        transaction.CategoryId = dto.CategoryId;
        transaction.Amount = dto.Amount;
        transaction.Description = dto.Description;
        transaction.Type = dto.Type;

        await _context.SaveChangesAsync();

        return NoContent();
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

        return NoContent();
    }
}
