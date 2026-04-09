using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokemonLocations.Api.Data.Models;

[Table("location_images")]
public class LocationImage {
    [Key]
    public int ImageId { get; set; }

    [Required]
    public int LocationId { get; set; }

    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    [MaxLength(255)]
    public string? Caption { get; set; }
}
