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
        var badges = await badgeRepository.GetForUserAsync(userId);
        var locations = await visitedLocationRepository.GetForUserAsync(userId);
        var buildings = await visitedBuildingRepository.GetForUserAsync(userId);

        return Ok(new {
            gymsComplete = badges.Count,
            locationsVisited = locations.Count,
            buildingsVisited = buildings.Count
        });
    }
}
