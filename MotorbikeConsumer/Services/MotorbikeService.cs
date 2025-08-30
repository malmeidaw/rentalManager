using MotorbikeConsumer.Data;
using MotorbikeConsumer.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MotorbikeConsumer.Services;


public interface IMotorbikeService
{
    Task CreateMotorbikeAsync(Motorbike motorbike);
    Task<List<Motorbike>> GetMotorbikeAsync();
    Task<Motorbike?> GetMotorbikeAsync(string? id);
    Task<Motorbike?> GetMotorbikeByPlateAsync(string? plate);
    Task UpdateMotorbikeAsync(Motorbike motorbike);
    Task<DeleteResult?> DeleteMotorbikeAsync(Motorbike motorbike);
    void Notify2024(Motorbike motorbike);
}

public class MotorbikeService : IMotorbikeService
{
    private IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MotorbikeService> _logger;
    public MotorbikeService(IServiceScopeFactory scopeFactory,
                            ILogger<MotorbikeService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task CreateMotorbikeAsync(Motorbike motorbike)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Motorbikes.Add(motorbike);
        try
        {
            await context.SaveChangesAsync();
            _logger.LogInformation($"{motorbike.Id} saved in database.");
            Console.WriteLine($"{motorbike.Id} saved in database.");
        }
        catch (Exception ex) { _logger.LogError($"Failed database changes {ex.Message}"); }
        
    }
    public async Task<List<Motorbike>> GetMotorbikeAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.Motorbikes.ToListAsync();
    }
    public async Task<Motorbike?> GetMotorbikeAsync(string? id)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.Motorbikes.FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<Motorbike?> GetMotorbikeByPlateAsync(string? plate)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.Motorbikes.FirstOrDefaultAsync(m => m.Plate == plate);
    }
    public async Task UpdateMotorbikeAsync(Motorbike motorbike)
    {
        try
        {
            string id = motorbike.Id, newPlate = motorbike.Plate;
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var m = context.Motorbikes.Find(id);
            if (m != null) { m.Plate = newPlate; }
            else
            {
                _logger.LogError($"{id} not found in database");
            }
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}");
            Console.WriteLine($"{ex.Message}");
        }
    }
    public async Task<DeleteResult?> DeleteMotorbikeAsync(Motorbike motorbike)
    {
        string id = motorbike.Id;
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rentals = await context.Rentals.AnyAsync(r => r.MotorbikeId == id);
        if (rentals)
        {
            return new DeleteResult
            {
                Success = false,
                Message = $"There is a rental with this motorbike {id}",
            };
        }
        else
        {
            await context.Motorbikes.Where(m => m.Id == id).ExecuteDeleteAsync(); //check if it exists?
            try
            {
                await context.SaveChangesAsync();
                _logger.LogInformation($"{motorbike.Id} deleted from database.");
                Console.WriteLine($"{motorbike.Id} deleted from database.");
            }
            catch (Exception ex) { _logger.LogError($"Failed database changes {ex.Message}"); }
            return new DeleteResult
            {
                Success = true,
                Message = $"Motorbike {id} deleted",
            };
        }

    }
    public void Notify2024(Motorbike motorbike)
    {
        Console.WriteLine($"{motorbike.Id} is from 2024");
    }
}

public class DeleteResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}