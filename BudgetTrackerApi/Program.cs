using Microsoft.EntityFrameworkCore;
using BudgetTrackerApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Register DbContext with PostgreSQL Npgsql provider
builder.Services.AddDbContext<BudgetDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
