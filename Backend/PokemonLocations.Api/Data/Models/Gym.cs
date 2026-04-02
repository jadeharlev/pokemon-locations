using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokemonLocations.Api.Data.Models;

[Table("gyms")]
public class Gym {
    [Key]
    public int GymId { get; set; }

    [Required]
    public int BuildingId { get; set; }

    [Required]
    [MaxLength(50)]
    public string GymType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string BadgeName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string GymLeader { get; set; } = string.Empty;

    [Required]
    public int GymOrder { get; set; }
}
