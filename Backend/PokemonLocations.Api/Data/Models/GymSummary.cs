namespace PokemonLocations.Api.Data.Models;

public class GymSummary {
    public int GymId { get; set; }
    public int BuildingId { get; set; }
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public string GymType { get; set; } = string.Empty;
    public string BadgeName { get; set; } = string.Empty;
    public string GymLeader { get; set; } = string.Empty;
    public int GymOrder { get; set; }
}
