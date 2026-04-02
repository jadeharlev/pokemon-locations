using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokemonLocations.Api.Data.Models;

[Table("locations")]
public class Location {
    [Key]
    public int LocationId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(500)]
    public string? VideoUrl { get; set; }
}
