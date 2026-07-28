using Microsoft.EntityFrameworkCore;
using BudgetTrackerApi.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Register DbContext with PostgreSQL
builder.Services.AddDbContext<BudgetDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

// 2. Register Swagger generator services into the Dependency Injection container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 3. Enable Swagger JSON endpoint and Swagger UI middleware (typically in Development mode)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
