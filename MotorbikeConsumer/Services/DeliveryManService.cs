using MotorbikeConsumer.Data;
using MotorbikeConsumer.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeliveryManConsumer.Services;

public interface IDeliveryManService
{
    Task CreateDeliveryManAsync(DeliveryMan deliveryMan);
}

public class DeliveryManService : IDeliveryManService
{
    private IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeliveryManService> _logger;
    public DeliveryManService(IServiceScopeFactory scopeFactory,
                              ILogger<DeliveryManService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task CreateDeliveryManAsync(DeliveryMan deliveryMan)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.DeliveryMans.Add(deliveryMan);
            await context.SaveChangesAsync();
            _logger.LogInformation($"{deliveryMan.Id} saved in database.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed database changes {ex.Message}");
        }
    }
}