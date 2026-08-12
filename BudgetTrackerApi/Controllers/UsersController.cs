using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BudgetTrackerApi.Data;
using BudgetTrackerApi.Models;
using BudgetTrackerApi.DTOs;
using BudgetTrackerApi.Services;

namespace BudgetTrackerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly BudgetDbContext _context;
    private readonly TokenService _tokenService;

    public UsersController(BudgetDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    // ... GetUsers, GetUser, CreateUser remain unchanged ...

    // POST: api/Users/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        bool isValidPassword = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!isValidPassword)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // Generate actual JWT token
        var token = _tokenService.GenerateToken(user);

        return Ok(new
        {
            token = token,
            userId = user.Id
        });
    }
}
