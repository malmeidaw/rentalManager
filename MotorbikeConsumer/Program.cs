// See https://aka.ms/new-console-template for more information
using MotorbikeConsumer.Data;
using MotorbikeConsumer.Services;
using DeliveryManConsumer.Services;
using RentalConsumer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
Console.WriteLine($"appsettings path: {Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")}");
Console.WriteLine($"Does appsettings exist?: {File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"))}");

// Db
var connectionString = builder.Configuration.GetConnectionString("DbConnection");
if (string.IsNullOrEmpty(connectionString)) 
    throw new Exception("Missing DB connection string");

builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(connectionString,
        o => o.MigrationsHistoryTable("__EFMigrationsHistory", "rental_manager")));

// Services
builder.Services.AddScoped<IMotorbikeService, MotorbikeService>();
builder.Services.AddHostedService<MotorbikeConsumerService>();
builder.Services.AddScoped<IDeliveryManService, DeliveryManService>();
builder.Services.AddHostedService<DeliveryManConsumerService>();
builder.Services.AddScoped<IRentalService, RentalService>();
builder.Services.AddHostedService<RentalConsumerService>();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var host = builder.Build();

// Logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();

using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        if (await context.Database.CanConnectAsync())
        {
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any()) 
            {
                Console.WriteLine("Applying pending migrations");
                await context.Database.MigrateAsync();
                Console.WriteLine("Migrations applied");
            }
            else
            {
                Console.WriteLine("No pending Migrations.");
            }
        }
        else 
            throw new Exception("Failed to connect to database");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error applying database migrations: {ex.Message}");
        throw;
    }
}

try 
{ 
    await host.RunAsync(); 
}
catch (Exception ex)
{
    Console.WriteLine($"Error running Consumer: {ex.Message}");
    throw;
}