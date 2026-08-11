using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BudgetTrackerApi.Data;
using BudgetTrackerApi.Models;
using BudgetTrackerApi.DTOs;

namespace BudgetTrackerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly BudgetDbContext _context;

    public UsersController(BudgetDbContext context)
    {
        _context = context;
    }

    // GET: api/Users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
    {
        var users = await _context.Users
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    // GET: api/Users/5
    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponseDto>> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} not found." });
        }

        return Ok(new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        });
    }

    // POST: api/Users (Register)
    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> CreateUser(CreateUserDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest(new { message = "A user with this email already exists." });
        }

        // Real BCrypt Hashing
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = passwordHash
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var response = new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, response);
    }

    // POST: api/Users/login (Login Stub)
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // Verify plain password against BCrypt hash
        bool isValidPassword = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!isValidPassword)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        return Ok(new { message = "Login successful!", userId = user.Id });
    }
}
