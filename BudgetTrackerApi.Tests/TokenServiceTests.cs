using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using BudgetTrackerApi.Models;
using BudgetTrackerApi.Services;

namespace BudgetTrackerApi.Tests;

public class TokenServiceTests
{
    [Fact]
    public void GenerateToken_ShouldReturnTokenWithCorrectClaims()
    {
        // ARRANGE: Setup in-memory configuration & test user
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:Key", "SuperSecretKeyForBudgetTrackerApi2026ThatIsLongEnoughToPassValidation!"},
            {"Jwt:Issuer", "BudgetTrackerApi"},
            {"Jwt:Audience", "BudgetTrackerUsers"},
            {"Jwt:ExpiryMinutes", "60"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var tokenService = new TokenService(configuration);
        var testUser = new User { Id = 42, Email = "test@example.com", Username = "TestUser" };

        // ACT: Generate token and decode it
        var tokenString = tokenService.GenerateToken(testUser);
        var handler = new JwtSecurityTokenHandler();
        var decodedToken = handler.ReadJwtToken(tokenString);

        // ASSERT: Verify claims inside the JWT
        var userIdClaim = decodedToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var emailClaim = decodedToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        Assert.NotNull(tokenString);
        Assert.Equal("42", userIdClaim);
        Assert.Equal("test@example.com", emailClaim);
    }
}
