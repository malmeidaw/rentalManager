using RentalManager.Utils;

namespace RentalManager.Models.Entities;

public class DeliveryMan
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string LegalId { get; set; } //unique //consider a legal_id validator
    public required DateTime BirthDate { get; set; }
    public required string DriversLicense { get; set; } //unique
    public required DriversLicenseType DriversLicenseType { get; set; }
    public string? DriversLicensePictureLocal { get; set; } 
}
