// using System.ComponentModel.DataAnnotations;
// using System.ComponentModel.DataAnnotations.Schema;

namespace RentalManager.Models.Entities;

// [Table("motorcycle")]
public class Motorbike
{
    // [Key]
    // [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    // [Column("id")]
    public string Id { get; set; }
    // [Column("year")]
    public int Year { get; set; } 
    // [Column("model")]
    public string Model { get; set; }
    // [Column("plate")]
    public string Plate { get; set; }
}