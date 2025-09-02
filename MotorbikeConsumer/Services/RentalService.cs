using MotorbikeConsumer.Data;
using MotorbikeConsumer.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MotorbikeConsumer.Utils;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace RentalConsumer.Services;

public interface IRentalService
{
    Task CreateRentalAsync(Rental rental);
    // Task<List<Rental>> GetRentalAsync();
    Task<Rental?> GetRentalAsync(string? id);
    Task<RentalResponse?> UpdateRentalAsync(string? id, DateTime newExpectedEndDate );
    // Task DeleteRentalAsync(Rental motorbike);
}

public class RentalService : IRentalService
{
    private IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RentalService> _logger;
    public RentalService(IServiceScopeFactory scopeFactory,
                         ILogger<RentalService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task CreateRentalAsync(Rental rental)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var deliveryMan = await context.DeliveryMans.FirstOrDefaultAsync(d => d.Id == rental.DeliveryManId);
            if (deliveryMan == null)
            {
                _logger.LogError($"{rental.DeliveryManId} does not exist");
                return;
            }
            if (deliveryMan.DriversLicenseType == DriversLicenseType.A ||
                deliveryMan.DriversLicenseType == DriversLicenseType.AB)
            {
                //also check if motorbike is available before
                var motorbike = await context.Motorbikes.FirstOrDefaultAsync(m => m.Id == rental.MotorbikeId);
                if (motorbike == null)
                {
                    _logger.LogError($"{rental.MotorbikeId} does not exist");
                    return;
                }
                //checking if motorbike is already being rented
                var otherRental = await context.Rentals.FirstOrDefaultAsync(m => m.MotorbikeId == rental.MotorbikeId);
                if (otherRental != null)
                {
                    _logger.LogError($"{rental.MotorbikeId} is already being rented.");
                    return;
                }
                //check if date range does not correspond with chosen plan?
                try
                {
                    context.Rentals.Add(rental);
                    await context.SaveChangesAsync();
                    _logger.LogInformation($"{rental.Id} saved in database");
                }
                catch (Exception ex) { _logger.LogError($"Failed database changes {ex.Message}"); }
                _logger.LogInformation($"{rental.Id} saved in database.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}");
        }
    }
    public async Task<Rental?> GetRentalAsync(string? id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogInformation("Rental Id not provided");
            }
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await context.Rentals.FirstOrDefaultAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get rental by id {id} {ex.Message}");
            return null;
        }
    }

    public async Task<RentalResponse?> UpdateRentalAsync(string? id, DateTime newExpectedEndDate)
    {
        RentalResponse? rentalResponse = null;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Rental? r = context.Rentals.Find(id);
            if (r != null)
            {
                rentalResponse = new RentalResponse(r);
                var uDaysDifference = (int)(newExpectedEndDate - r.StartDate).TotalDays;
                var lDaysDifference = (int)(r.ExpectedEndDate - r.StartDate).TotalDays;
                if (newExpectedEndDate == r.ExpectedEndDate)
                {
                    rentalResponse.TotalRentalValue = r.RentalType switch
                    {
                        RentalType.Days7 => uDaysDifference * (int)RentalTypePrices.Days7,
                        RentalType.Days15 => uDaysDifference * (int)RentalTypePrices.Days15,
                        RentalType.Days30 => uDaysDifference * (int)RentalTypePrices.Days30,
                        RentalType.Days45 => uDaysDifference * (int)RentalTypePrices.Days45,
                        RentalType.Days50 => uDaysDifference * (int)RentalTypePrices.Days50,
                        RentalType.unknown => 0,
                        _ => 0
                    };
                }
                else if (newExpectedEndDate > r.ExpectedEndDate)
                {
                    rentalResponse.TotalRentalValue = r.RentalType switch
                    {
                        RentalType.Days7 => lDaysDifference * (int)RentalTypePrices.Days7,
                        RentalType.Days15 => lDaysDifference * (int)RentalTypePrices.Days15,
                        RentalType.Days30 => lDaysDifference * (int)RentalTypePrices.Days30,
                        RentalType.Days45 => lDaysDifference * (int)RentalTypePrices.Days45,
                        RentalType.Days50 => lDaysDifference * (int)RentalTypePrices.Days50,
                        RentalType.unknown => 0,
                        _ => 0
                    };
                    rentalResponse.TotalRentalValue += (int)(newExpectedEndDate - r.ExpectedEndDate).TotalDays * 50;
                }
                else if (newExpectedEndDate < r.ExpectedEndDate)
                {
                    if (r.RentalType == RentalType.Days7)
                    {
                        rentalResponse.TotalRentalValue = uDaysDifference * (int)RentalTypePrices.Days7 +
                                                          (int)(r.ExpectedEndDate - newExpectedEndDate).TotalDays * 0.20;
                    }
                    else if (r.RentalType == RentalType.Days15)
                    {
                        rentalResponse.TotalRentalValue = uDaysDifference * (int)RentalTypePrices.Days15 +
                                                          (int)(r.ExpectedEndDate - newExpectedEndDate).TotalDays * 0.40;
                    }
                    else
                    {
                        rentalResponse.TotalRentalValue = r.RentalType switch
                        {
                            RentalType.Days30 => uDaysDifference * (int)RentalTypePrices.Days30,
                            RentalType.Days45 => uDaysDifference * (int)RentalTypePrices.Days45,
                            RentalType.Days50 => uDaysDifference * (int)RentalTypePrices.Days50,
                            RentalType.unknown => 0,
                            _ => 0
                        };
                    }
                }
                r.ExpectedEndDate = newExpectedEndDate;
                await context.SaveChangesAsync();
            }
            else
            {
                _logger.LogError($"{id} was not found");
            }
            return rentalResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}");
            return rentalResponse;
        }
    }
}

public class RentalResponse : Rental
{
    [SetsRequiredMembers]
    public RentalResponse(Rental r)
    {
        Id = r.Id;
        DeliveryManId = r.DeliveryManId;
        DeliveryMan = r.DeliveryMan;
        MotorbikeId = r.MotorbikeId;
        Motorbike = r.Motorbike;
        StartDate = r.StartDate;
        EndDate = r.EndDate;
        ExpectedEndDate = r.ExpectedEndDate;
        RentalType = r.RentalType;
    }
    public double? TotalRentalValue { get; set; }
}

