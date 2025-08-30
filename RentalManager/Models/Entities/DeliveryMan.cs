// using System.ComponentModel.DataAnnotations;
// using System.ComponentModel.DataAnnotations.Schema;
using RentalManager.Utils;

namespace RentalManager.Models.Entities;

// [Table("deliverer_man")]
public class DeliveryMan
{
    // [Key]
    // [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    // [Column("id")]
    public required string Id { get; set; }
    // [Column("name")]
    public required string Name { get; set; }
    // [Column("cnpj")]
    public required string LegalId { get; set; } //unique //consider a legal_id validator
    // [Column("birth_date")]
    public required DateTime BirthDate { get; set; }
    // [Column("drivers_license")]
    public required string DriversLicense { get; set; } //unique
    // [Column("drivers_license_type")]
    public required DriversLicenseType DriversLicenseType { get; set; }
    public string? DriversLicensePictureLocal { get; set; } 
}
