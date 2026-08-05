using Microsoft.EntityFrameworkCore;
using BudgetTrackerApi.Models;

namespace BudgetTrackerApi.Data;

public class BudgetDbContext : DbContext
{
    public BudgetDbContext(DbContextOptions<BudgetDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Groceries" },
            new Category { Id = 2, Name = "Rent" },
            new Category { Id = 3, Name = "Utilities" },
            new Category { Id = 4, Name = "Salary" },
            new Category { Id = 5, Name = "Entertainment" },
            new Category { Id = 6, Name = "Other" }
        );
    }
}
