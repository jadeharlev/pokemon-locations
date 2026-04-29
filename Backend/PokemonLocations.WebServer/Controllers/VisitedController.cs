using Microsoft.AspNetCore.Mvc;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Database.Repositories;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
[Route("/api/me/visited")]
public class VisitedController : ControllerBase {
    private readonly IVisitedLocationRepository locationRepository;
    private readonly IVisitedBuildingRepository buildingRepository;
    private readonly IPokemonLocationsApiClient apiClient;

    public VisitedController(
        IVisitedLocationRepository locationRepository,
        IVisitedBuildingRepository buildingRepository,
        IPokemonLocationsApiClient apiClient) {
        this.locationRepository = locationRepository;
        this.buildingRepository = buildingRepository;
        this.apiClient = apiClient;
    }

    [HttpPut("locations/{locationId:int}")]
    public async Task<IActionResult> PutLocation(int locationId) {
        if (!await apiClient.ExistsAsync($"/locations/{locationId}")) {
            return NotFound(new { error = "not_found" });
        }
        await locationRepository.AddAsync(User.GetUserId(), locationId);
        return NoContent();
    }

    [HttpDelete("locations/{locationId:int}")]
    public async Task<IActionResult> DeleteLocation(int locationId) {
        await locationRepository.RemoveAsync(User.GetUserId(), locationId);
        return NoContent();
    }

    [HttpPut("buildings/{locationId:int}/{buildingId:int}")]
    public async Task<IActionResult> PutBuilding(int locationId, int buildingId) {
        if (!await apiClient.ExistsAsync($"/locations/{locationId}/buildings/{buildingId}")) {
            return NotFound(new { error = "not_found" });
        }
        await buildingRepository.AddAsync(User.GetUserId(), buildingId);
        return NoContent();
    }

    [HttpDelete("buildings/{locationId:int}/{buildingId:int}")]
    public async Task<IActionResult> DeleteBuilding(int locationId, int buildingId) {
        await buildingRepository.RemoveAsync(User.GetUserId(), buildingId);
        return NoContent();
    }
}
