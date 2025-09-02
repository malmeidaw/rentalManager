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

string? appSettingsPath = null;
var commonPaths = new[]
{
    Path.Combine("..", "..", "..", "..", "appsettings.json"),
    Path.Combine("..", "..", "..", "appsettings.json"),
    Path.Combine("..", "..", "appsettings.json"),
    "appsettings.json"                                 
};

foreach (var relativePath in commonPaths)
{
    var fullPath = Path.GetFullPath(relativePath);
    if (File.Exists(fullPath))
    {
        appSettingsPath = fullPath;
        break;
    }
}


if (appSettingsPath == null)
    throw new FileNotFoundException("appsettings.json não encontrado");

builder.Configuration.AddJsonFile(appSettingsPath, optional: false, reloadOnChange: true);

builder.Configuration.AddEnvironmentVariables();

//Db
var connectionString = builder.Configuration.GetConnectionString("DbConnection");
if (string.IsNullOrEmpty(connectionString)) throw new Exception("Missing DB connection string");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DbConnection"),
                                            o => o.MigrationsHistoryTable("__EFMigrationsHistory", "rental_manager")));

//services
builder.Services.AddScoped<IMotorbikeService, MotorbikeService>();
builder.Services.AddHostedService<MotorbikeConsumerService>();
builder.Services.AddScoped<IDeliveryManService, DeliveryManService>();
builder.Services.AddHostedService<DeliveryManConsumerService>();
builder.Services.AddScoped<IRentalService, RentalService>();
builder.Services.AddHostedService<RentalConsumerService>();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var host = builder.Build();

//logging
// builder.Logging.ClearProviders();
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
            if (pendingMigrations.Any()) await context.Database.MigrateAsync();
        }
        else throw new Exception("Failed DB migrations");
    }
    catch (Exception ex)
    {
        throw new Exception($"Failed DB connection: {ex.Message}");
    }
}

try { await host.RunAsync(); }
catch (Exception ex)
{
    Console.WriteLine($"{ex.Message}");
}