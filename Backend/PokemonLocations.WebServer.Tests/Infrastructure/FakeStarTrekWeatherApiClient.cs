using System.Net;
using System.Net.Http.Headers;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Models;

namespace PokemonLocations.WebServer.Tests.Infrastructure;

public class FakeStarTrekWeatherApiClient : IStarTrekWeatherApiClient {
    private readonly Dictionary<string, Planet> planetsByName;

    public FakeStarTrekWeatherApiClient(params Planet[] planets) {
        planetsByName = planets.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }

    public Dictionary<string, (byte[] Bytes, string ContentType)> Images { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<Planet>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Planet>>(planetsByName.Values.ToList());

    public Task<Planet?> GetByNameAsync(string name, CancellationToken ct = default) =>
        Task.FromResult(planetsByName.TryGetValue(name, out var planet) ? planet : null);

    public Task<HttpResponseMessage> GetImageAsync(string fileName, CancellationToken ct = default) {
        if (!Images.TryGetValue(fileName, out var image)) {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
        var response = new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new ByteArrayContent(image.Bytes)
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
        return Task.FromResult(response);
    }
}
