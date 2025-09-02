using RentalManager.Utils;

namespace RentalManager.Models.Entities;

public class Rental
{
    public string? Id { get; set; }

    public string? DeliveryManId { get; set; }

    public string? MotorbikeId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? ExpectedEndDate { get; set; }
    public RentalType? RentalType { get; set; }
}