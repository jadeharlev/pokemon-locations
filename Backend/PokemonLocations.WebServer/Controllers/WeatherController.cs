using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using PokemonLocations.WebServer.Clients;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
[Route("/api/planets")]
public class WeatherController : ControllerBase {
    private static readonly Regex SafeFileName = new(@"^[A-Za-z0-9._-]+$", RegexOptions.Compiled);

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

    [HttpGet("images/{fileName}")]
    public async Task<IActionResult> GetImage(string fileName, CancellationToken ct) {
        if (!SafeFileName.IsMatch(fileName) || fileName.Contains("..")) {
            return BadRequest();
        }

        using var upstream = await apiClient.GetImageAsync(fileName, ct);
        if (upstream.StatusCode == HttpStatusCode.NotFound) return NotFound();
        upstream.EnsureSuccessStatusCode();

        var bytes = await upstream.Content.ReadAsByteArrayAsync(ct);
        var contentType = upstream.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
        return File(bytes, contentType);
    }
}
