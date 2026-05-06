using System.ComponentModel.DataAnnotations;

namespace PokemonLocations.WebServer.Models.Requests;

public class UpdatePermanentPlanetRequest {
    [Required]
    public string PlanetName { get; set; } = string.Empty;
}
