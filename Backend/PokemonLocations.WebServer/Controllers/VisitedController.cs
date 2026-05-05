using Microsoft.AspNetCore.Mvc;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Database.Repositories;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
[Route("/api/me/visited")]
public class VisitedController : ControllerBase {
    private readonly IVisitedBuildingRepository buildingRepository;
    private readonly IPokemonLocationsApiClient apiClient;

    public VisitedController(
        IVisitedBuildingRepository buildingRepository,
        IPokemonLocationsApiClient apiClient) {
        this.buildingRepository = buildingRepository;
        this.apiClient = apiClient;
    }

    [HttpPut("buildings/{locationId:int}/{buildingId:int}")]
    public async Task<IActionResult> PutBuilding(int locationId, int buildingId) {
        if (!await apiClient.ExistsAsync($"/locations/{locationId}/buildings/{buildingId}")) {
            return NotFound(new { error = "not_found" });
        }
        await buildingRepository.AddAsync(User.GetUserId(), locationId, buildingId);
        return NoContent();
    }

    [HttpDelete("buildings/{locationId:int}/{buildingId:int}")]
    public async Task<IActionResult> DeleteBuilding(int locationId, int buildingId) {
        await buildingRepository.RemoveAsync(User.GetUserId(), buildingId);
        return NoContent();
    }
}
