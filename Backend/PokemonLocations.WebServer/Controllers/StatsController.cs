using Microsoft.AspNetCore.Mvc;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Database.Repositories;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
[Route("/api/me/stats")]
public class StatsController : ControllerBase {
    private readonly IBadgeRepository badgeRepository;
    private readonly IVisitedBuildingRepository visitedBuildingRepository;

    public StatsController(
        IBadgeRepository badgeRepository,
        IVisitedBuildingRepository visitedBuildingRepository) {
        this.badgeRepository = badgeRepository;
        this.visitedBuildingRepository = visitedBuildingRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Get() {
        var userId = User.GetUserId();
        var badgesTask = badgeRepository.GetForUserAsync(userId);
        var visitedLocationsTask = visitedBuildingRepository.GetDistinctLocationIdsForUserAsync(userId);
        var visitedBuildingsTask = visitedBuildingRepository.GetForUserAsync(userId);
        await Task.WhenAll(badgesTask, visitedLocationsTask, visitedBuildingsTask);

        return Ok(new {
            gymsComplete = badgesTask.Result.Count,
            locationsVisited = visitedLocationsTask.Result.Count,
            buildingsVisited = visitedBuildingsTask.Result.Count
        });
    }
}
