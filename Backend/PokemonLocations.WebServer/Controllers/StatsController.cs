using Microsoft.AspNetCore.Mvc;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Database.Repositories;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
[Route("/api/me/stats")]
public class StatsController : ControllerBase {
    private readonly IBadgeRepository badgeRepository;
    private readonly IVisitedLocationRepository visitedLocationRepository;
    private readonly IVisitedBuildingRepository visitedBuildingRepository;

    public StatsController(
        IBadgeRepository badgeRepository,
        IVisitedLocationRepository visitedLocationRepository,
        IVisitedBuildingRepository visitedBuildingRepository) {
        this.badgeRepository = badgeRepository;
        this.visitedLocationRepository = visitedLocationRepository;
        this.visitedBuildingRepository = visitedBuildingRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Get() {
        var userId = User.GetUserId();
        var badgesTask = badgeRepository.GetForUserAsync(userId);
        var locationsTask = visitedLocationRepository.GetForUserAsync(userId);
        var buildingsTask = visitedBuildingRepository.GetForUserAsync(userId);
        await Task.WhenAll(badgesTask, locationsTask, buildingsTask);

        return Ok(new {
            gymsComplete = badgesTask.Result.Count,
            locationsVisited = locationsTask.Result.Count,
            buildingsVisited = buildingsTask.Result.Count
        });
    }
}
