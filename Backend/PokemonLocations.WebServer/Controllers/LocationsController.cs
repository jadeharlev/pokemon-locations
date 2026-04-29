using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Database.Repositories;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
[Route("/api/locations")]
public class LocationsController : ControllerBase {
    private readonly IPokemonLocationsApiClient apiClient;
    private readonly IVisitedLocationRepository visitedRepository;

    public LocationsController(
        IPokemonLocationsApiClient apiClient,
        IVisitedLocationRepository visitedRepository) {
        this.apiClient = apiClient;
        this.visitedRepository = visitedRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() {
        var response = await apiClient.GetWithStatusAsync("/locations");
        if (response.StatusCode != 200) return ProxyError(response);

        var locations = JsonNode.Parse(response.Body!)!.AsArray();
        var visitedIds = await visitedRepository.GetForUserAsync(User.GetUserId());
        var visitedSet = new HashSet<int>(visitedIds);

        foreach (var location in locations) {
            var id = location!["locationId"]!.GetValue<int>();
            location["visited"] = visitedSet.Contains(id);
        }

        return Content(locations.ToJsonString(), "application/json");
    }

    [HttpGet("{locationId:int}")]
    public async Task<IActionResult> GetById(int locationId) {
        var response = await apiClient.GetWithStatusAsync($"/locations/{locationId}");
        if (response.StatusCode != 200) return ProxyError(response);

        var location = JsonNode.Parse(response.Body!)!.AsObject();
        var visitedIds = await visitedRepository.GetForUserAsync(User.GetUserId());
        location["visited"] = visitedIds.Contains(locationId);
        location["userImages"] = new JsonArray();

        return Content(location.ToJsonString(), "application/json");
    }

    private IActionResult ProxyError(ApiResponse response) {
        if (response.StatusCode == 404) return NotFound();
        return StatusCode(502);
    }
}
