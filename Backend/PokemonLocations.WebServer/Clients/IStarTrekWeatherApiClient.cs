using PokemonLocations.WebServer.Models;

namespace PokemonLocations.WebServer.Clients;

public interface IStarTrekWeatherApiClient {
    Task<IReadOnlyList<Planet>> GetAllAsync(CancellationToken ct = default);
    Task<Planet?> GetByNameAsync(string name, CancellationToken ct = default);
}
