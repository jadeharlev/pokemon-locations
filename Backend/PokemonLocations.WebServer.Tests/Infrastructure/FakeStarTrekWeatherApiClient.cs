using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Models;

namespace PokemonLocations.WebServer.Tests.Infrastructure;

public class FakeStarTrekWeatherApiClient : IStarTrekWeatherApiClient {
    private readonly Dictionary<string, Planet> planetsByName;

    public FakeStarTrekWeatherApiClient(params Planet[] planets) {
        planetsByName = planets.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }

    public Task<IReadOnlyList<Planet>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Planet>>(planetsByName.Values.ToList());

    public Task<Planet?> GetByNameAsync(string name, CancellationToken ct = default) =>
        Task.FromResult(planetsByName.TryGetValue(name, out var planet) ? planet : null);
}
