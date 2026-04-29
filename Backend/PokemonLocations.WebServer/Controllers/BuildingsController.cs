using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Extensions;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
public class BuildingsController : ControllerBase {
    private readonly IPokemonLocationsApiClient apiClient;
    private readonly IVisitedBuildingRepository visitedRepository;

    public BuildingsController(
        IPokemonLocationsApiClient apiClient,
        IVisitedBuildingRepository visitedRepository) {
        this.apiClient = apiClient;
        this.visitedRepository = visitedRepository;
    }

    [HttpGet("/api/locations/{locationId:int}/buildings")]
    public async Task<IActionResult> GetAll(int locationId) {
        var response = await apiClient.GetWithStatusAsync($"/locations/{locationId}/buildings");
        if (response.StatusCode != 200) return this.ProxyError(response);

        var buildings = JsonNode.Parse(response.Body!)!.AsArray();
        var visitedIds = await visitedRepository.GetForUserAsync(User.GetUserId());
        var visitedSet = new HashSet<int>(visitedIds);

        foreach (var building in buildings) {
            var id = building!["buildingId"]!.GetValue<int>();
            building["visited"] = visitedSet.Contains(id);
        }

        return Content(buildings.ToJsonString(), "application/json");
    }

    [HttpGet("/api/locations/{locationId:int}/buildings/{buildingId:int}")]
    public async Task<IActionResult> GetById(int locationId, int buildingId) {
        var response = await apiClient.GetWithStatusAsync($"/locations/{locationId}/buildings/{buildingId}");
        if (response.StatusCode != 200) return this.ProxyError(response);

        return Content(response.Body!, "application/json");
    }
}
