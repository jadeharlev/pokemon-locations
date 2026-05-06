using Microsoft.AspNetCore.Mvc;
using PokemonLocations.WebServer.Clients;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
[Route("/api/planets")]
public class WeatherController : ControllerBase {
    private readonly IStarTrekWeatherApiClient apiClient;

    public WeatherController(IStarTrekWeatherApiClient apiClient) {
        this.apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) {
        var planets = await apiClient.GetAllAsync(ct);
        return Ok(planets);
    }

    [HttpGet("{name}")]
    public async Task<IActionResult> GetByName(string name, CancellationToken ct) {
        var planet = await apiClient.GetByNameAsync(name, ct);
        return planet is null ? NotFound() : Ok(planet);
    }
}
