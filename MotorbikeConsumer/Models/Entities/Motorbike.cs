using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MotorbikeConsumer.Models.Entities;

[Table("motorbike", Schema ="rental_manager")]
public class Motorbike
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public required string Id { get; set; }

    [Column("year")]
    public required int Year { get; set; }

    [Column("model")]
    public required string Model { get; set; }

    [Column("plate")]
    public required string Plate { get; set; }
}