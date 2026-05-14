namespace PokemonLocations.WebServer.Models.Responses;

public record StatsResponse(
    int GymsComplete,
    int LocationsVisited,
    int BuildingsVisited,
    int MaxGymsComplete,
    int MaxLocationsVisited,
    int MaxBuildingsVisited);
