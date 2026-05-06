namespace PokemonLocations.WebServer.Models;

public record Planet(
    string Name,
    string SolarSystem,
    double AtmosphericPressure,
    double MaxTemp,
    double MinTemp,
    string Description,
    string ImageUrl);
