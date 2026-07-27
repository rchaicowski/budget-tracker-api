var builder = WebApplication.CreateBuilder(args);

// 1. Register Services (Dependency Injection)
builder.Services.AddControllers();

var app = builder.Build();

// 2. Configure HTTP Request Pipeline (Middleware)
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
