using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokemonLocations.Api.Data.Models;

[Table("buildings")]
public class Building {
    [Key]
    public int BuildingId { get; set; }

    [Required]
    public int LocationId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public BuildingType BuildingType { get; set; }

    public string? Description { get; set; }

    public string? LandmarkDescription { get; set; }

    public Gym? Gym { get; set; }
}
