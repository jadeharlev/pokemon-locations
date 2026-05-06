using System.Net;
using System.Text.Json;
using PokemonLocations.WebServer.Models;

namespace PokemonLocations.WebServer.Clients;

public class StarTrekWeatherApiClient : IStarTrekWeatherApiClient {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient httpClient;

    public StarTrekWeatherApiClient(HttpClient httpClient) {
        this.httpClient = httpClient;
    }

    public async Task<IReadOnlyList<Planet>> GetAllAsync(CancellationToken ct = default) {
        var json = await httpClient.GetStringAsync("api/Planet", ct);
        return JsonSerializer.Deserialize<List<Planet>>(json, JsonOptions) ?? [];
    }

    public async Task<Planet?> GetByNameAsync(string name, CancellationToken ct = default) {
        using var response = await httpClient.GetAsync($"api/Planet/{Uri.EscapeDataString(name)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<Planet>(json, JsonOptions);
    }
}
