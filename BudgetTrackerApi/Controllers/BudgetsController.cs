using System.Security.Claims;
using System.Text.Json;
using BudgetTrackerApi.Data;
using BudgetTrackerApi.DTOs;
using BudgetTrackerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace BudgetTrackerApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BudgetsController : ControllerBase
{
    private readonly BudgetDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<BudgetsController> _logger;

    public BudgetsController(BudgetDbContext context, IDistributedCache cache, ILogger<BudgetsController> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim!);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BudgetResponseDto>>> GetBudgets()
    {
        var userId = GetUserId();

        var budgets = await _context.Budgets
            .Where(b => b.UserId == userId)
            .ToListAsync();

        var categoryIds = budgets.Select(b => b.CategoryId).ToList();
        var categories = await _context.Categories
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var result = budgets.Select(b => new BudgetResponseDto
        {
            Id = b.Id,
            CategoryId = b.CategoryId,
            CategoryName = categories.TryGetValue(b.CategoryId, out var name) ? name : "Unknown",
            MonthlyLimit = b.AmountLimit
        });

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<BudgetResponseDto>> CreateBudget(CreateBudgetDto dto)
    {
        var userId = GetUserId();

        var category = await _context.Categories.FindAsync(dto.CategoryId);
        if (category == null)
        {
            return BadRequest("Invalid CategoryId.");
        }

        var existingBudget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.UserId == userId && b.CategoryId == dto.CategoryId);

        if (existingBudget != null)
        {
            return BadRequest("A budget for this category already exists.");
        }

        var budget = new Budget
        {
            UserId = userId,
            CategoryId = dto.CategoryId,
            AmountLimit = dto.MonthlyLimit
        };

        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        // Invalidate cache for current month's budget status
        var now = DateTime.UtcNow;
        var cacheKey = $"budget-status:{userId}:{now.Year}-{now.Month}";
        try
        {
            await _cache.RemoveAsync(cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate Redis cache for key {CacheKey}", cacheKey);
        }

        var response = new BudgetResponseDto
        {
            Id = budget.Id,
            CategoryId = budget.CategoryId,
            CategoryName = category.Name,
            MonthlyLimit = budget.AmountLimit
        };

        return CreatedAtAction(nameof(GetBudgets), new { id = budget.Id }, response);
    }

    [HttpGet("status")]
    public async Task<ActionResult<IEnumerable<BudgetResponseDto>>> GetBudgetStatus()
    {
        var userId = GetUserId();
        var now = DateTime.UtcNow;
        var cacheKey = $"budget-status:{userId}:{now.Year}-{now.Month}";

        // 1. Try to fetch from Redis cache
        try
        {
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogInformation("Cache hit for key {CacheKey}", cacheKey);
                var cachedResult = JsonSerializer.Deserialize<IEnumerable<BudgetResponseDto>>(cachedData);
                if (cachedResult != null)
                {
                    return Ok(cachedResult);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache failure; falling back to database query.");
        }

        _logger.LogInformation("Cache miss for key {CacheKey}. Querying database.", cacheKey);

        // 2. Cache miss: Query database
        var budgets = await _context.Budgets
            .Where(b => b.UserId == userId)
            .ToListAsync();

        var categories = await _context.Categories.ToDictionaryAsync(c => c.Id, c => c.Name);

        var spendByCategory = await _context.Transactions
            .Where(t => t.Account.UserId == userId
                        && t.Type == "Expense"
                        && t.Date.Month == now.Month
                        && t.Date.Year == now.Year)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Total);

        var result = budgets.Select(b =>
        {
            var currentSpend = spendByCategory.TryGetValue(b.CategoryId, out var total) ? total : 0m;
            return new BudgetResponseDto
            {
                Id = b.Id,
                CategoryId = b.CategoryId,
                CategoryName = categories.TryGetValue(b.CategoryId, out var name) ? name : "Unknown",
                MonthlyLimit = b.AmountLimit,
                CurrentSpend = currentSpend
            };
        }).ToList();

        // 3. Store result in Redis cache (expire in 5 minutes)
        try
        {
            var serializedData = JsonSerializer.Serialize(result);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(cacheKey, serializedData, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write to Redis cache for key {CacheKey}", cacheKey);
        }

        return Ok(result);
    }
}
