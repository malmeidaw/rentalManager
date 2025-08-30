using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MotorbikeConsumer.Utils;

namespace MotorbikeConsumer.Models.Entities;

[Table("rental", Schema = "rental_manager")]
public class Rental
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public required string Id { get; set; }

    [Column("delivery_man_id")]
    [Required]
    public required string DeliveryManId { get; set; }

    [ForeignKey("DeliveryManId")]
    public DeliveryMan? DeliveryMan { get; set; }
    [Column("motorbike_id")]
    [Required]
    public required string MotorbikeId { get; set; }
    [ForeignKey("MotorbikeId")]
    public Motorbike? Motorbike { get; set; }
    [Column("start_date")]
    public required DateTime StartDate { get; set; }
    [Column("end_date")]
    public required DateTime EndDate { get; set; }
    [Column("expected_end_date")]
    public required DateTime ExpectedEndDate { get; set; }
    [Column("rental_type")]
    public required RentalType RentalType { get; set; }
}