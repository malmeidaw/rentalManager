using RentalManager.Utils;

namespace RentalManager.Models.Entities;

// [Table("rental", Schema = "rental_manager")]
public class Rental
{
    // [Key]
    // [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    // [Column("id")]
    public string? Id { get; set; }

    // [Column("delivery_man_id")]
    // [Required]
    public string? DeliveryManId { get; set; }

    // [ForeignKey("DeliveryManId")]
    // public required DeliveryMan DeliveryMan { get; set; }
    // [Column("motorbike_id")]
    // [Required]
    public string? MotorbikeId { get; set; }
    // [ForeignKey("MotorbikeId")]
    // public required Motorbike Motorbike { get; set; }
    // [Column("start_date")]
    public DateTime? StartDate { get; set; }
    // [Column("end_date")]
    public DateTime? EndDate { get; set; }
    // [Column("expected_end_date")]
    public DateTime? ExpectedEndDate { get; set; }
    // [Column("rental_type")]
    public RentalType? RentalType { get; set; }
}