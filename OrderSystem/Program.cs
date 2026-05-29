using Microsoft.EntityFrameworkCore;
using OrderSystem.Data;
using OrderSystem.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=ordersystem.db";
    options.UseSqlite(connectionString);
});
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.MapControllers();
app.MapGet("/", () => Results.Ok(new
{
    name = "Retail Order System",
    health = "ok"
}));

app.Run();

public partial class Program
{
}
