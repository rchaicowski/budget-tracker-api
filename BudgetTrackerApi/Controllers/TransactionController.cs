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

    // 1. Dependency Injection via Constructor
    public TransactionsController(BudgetDbContext context)
    {
        _context = context;
    }

    // 2. GET: api/transactions (Retrieve all transactions)
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
                Date = t.Date
            })
            .ToListAsync();

        return Ok(transactions);
    }

    // 3. POST: api/transactions (Create a new transaction)
    [HttpPost]
    public async Task<ActionResult<TransactionResponseDto>> CreateTransaction(CreateTransactionDto dto)
    {
        // Check if the referenced Account actually exists in PostgreSQL
        var accountExists = await _context.Accounts.AnyAsync(a => a.Id == dto.AccountId);
        if (!accountExists)
        {
            return BadRequest(new { message = $"Account with ID {dto.AccountId} does not exist." });
        }

        // Map DTO -> Database Entity
        var transaction = new Transaction
        {
            AccountId = dto.AccountId,
            Amount = dto.Amount,
            Category = dto.Category,
            Description = dto.Description,
            Date = DateTime.UtcNow
        };

        // Track and save to database
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Map Database Entity -> Response DTO
        var response = new TransactionResponseDto
        {
            Id = transaction.Id,
            AccountId = transaction.AccountId,
            Amount = transaction.Amount,
            Category = transaction.Category,
            Description = transaction.Description,
            Date = transaction.Date
        };

        return CreatedAtAction(nameof(GetTransactions), new { id = transaction.Id }, response);
    }
}
